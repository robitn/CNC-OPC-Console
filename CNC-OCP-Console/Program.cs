/*
 * CNC-OCP-Console - Program.cs
 * Main application entry point for CNC OPC Console.
 * Responsible for orchestration, initialization, and data flow coordination.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentroidAPI;

/// <summary>
/// Main application entry point - coordinates initialization and data flow
/// Responsible for orchestration only, delegates to specialized managers
/// </summary>
public partial class Program
{
    private static CNCPipe? _cncPipe;
    private static CNCPipe.Job? _cncJob;
    private static CNCPipe.Parameter? _cncParameter;
    private static CNCPipe.Plc? _cncPlc;

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("CNC Teensy OCP to CentroidAPI Console Application");
            Console.WriteLine("===================================================\n");

            // Initialize CNCPipe
            Console.WriteLine("[INIT] Creating CNCPipe...");
            _cncPipe = new CNCPipe();
            Console.Out.Flush();

            // Validate CNCPipe was properly constructed
            if (!_cncPipe.IsConstructed())
            {
                Console.Error.WriteLine("? CNCPipe failed to initialize properly");
                Console.WriteLine("\nTroubleshooting:");
                Console.WriteLine("- Verify CentroidAPI.dll is accessible");
                Console.WriteLine("- Ensure CNC control software is running");
                Console.WriteLine("- Check system permissions for CNC API access");
                Environment.Exit(1);
            }

            Console.WriteLine("✓ CNCPipe initialized and constructed");

            // Initialize CNCPipe.Job once for reuse throughout application
            Console.WriteLine("[INIT] Initializing CNCPipe.Job...");
            _cncJob = new CNCPipe.Job(_cncPipe);
            Console.WriteLine("✓ CNCPipe.Job initialized");

            // Initialize CNCPipe.Parameter once for reuse throughout application
            Console.WriteLine("[INIT] Initializing CNCPipe.Parameter...");
            _cncParameter = new CNCPipe.Parameter(_cncPipe);
            Console.WriteLine("✓ CNCPipe.Parameter initialized");

            // Initialize CNCPipe.Plc once for reuse throughout application
            Console.WriteLine("[INIT] Initializing CNCPipe.Plc...");
            _cncPlc = new CNCPipe.Plc(_cncPipe);
            Console.WriteLine("✓ CNCPipe.Plc initialized");

            // Initialize Teensy device manager via Serial
            Console.WriteLine("[INIT] Creating TeensySerialManager...");
            var teensyManager = new TeensySerialManager();
            Console.WriteLine("✓ Teensy Serial Manager initialized");

            // Discover and connect to Teensy device
            Console.WriteLine("[INIT] Discovering and connecting to Teensy...");
            if (!teensyManager.DiscoverAndConnect())
            {
                Console.Error.WriteLine("? Failed to discover and connect to Teensy device");
                Console.WriteLine("\nTroubleshooting:");
                Console.WriteLine("- Verify Teensy device is connected via USB");
                Console.WriteLine("- Check Device Manager for COM ports");
                Console.WriteLine("- Ensure Teensy firmware sends 'TEENSY_OCP_001' at startup");
                Environment.Exit(1);
            }

            Console.WriteLine("? Connected to Teensy device\n");

            // Start receiving and processing data with auto-reconnection
            var cts = new CancellationTokenSource();
            var readTask = ReceiveAndProcessDataWithReconnection(teensyManager, cts.Token);

            // Handle Ctrl+C to gracefully shutdown
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n\nShutting down gracefully...");
                cts.Cancel();
            };

            await readTask;

            teensyManager.Disconnect();
            Console.WriteLine("? Disconnected from Teensy device");
            Console.WriteLine("Application ended successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n? Error: {ex.Message}");
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    static async Task ReceiveAndProcessDataWithReconnection(TeensySerialManager teensyManager, CancellationToken cancellationToken)
    {
        var dataConverter = new SerialToSettingsConverter();
        var displayService = new DataDisplayService();
        var connectionMonitor = new ConnectionMonitor(teensyManager);

        Console.WriteLine("Listening for Teensy serial data...\n");
        Console.Out.Flush();

        int nullCount = 0;
        int parseFailCount = 0;
        int successCount = 0;
        var startWarningTime = DateTime.Now;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check connection and attempt reconnection if needed
                if (!await connectionMonitor.TryReconnectAsync(cancellationToken))
                {
                    continue;
                }

                // Read CSV message from serial port
                var message = teensyManager.ReadMessage(cancellationToken);

                if (message == null)
                {
                    nullCount++;

                    if (connectionMonitor.ShouldReconnect())
                    {
                        await connectionMonitor.ForceReconnectAsync(cancellationToken);
                        continue;
                    }

                    // Display waiting message if we haven't received any data yet
                    if (successCount == 0)
                    {
                        displayService.DisplayWaitingMessage(nullCount, ref startWarningTime);
                    }

                    await Task.Delay(5, cancellationToken);
                    continue;
                }

                connectionMonitor.RecordSuccess();

                // Parse message
                Dictionary<string, object>? settings = null;
                if (message.Value.textData != null)
                {
                    settings = dataConverter.Convert(message.Value.textData);
                    if (settings != null && settings.Count > 0)
                        successCount++;
                    else
                        parseFailCount++;
                }
                else
                {
                    parseFailCount++;
                }

                // Display and apply settings
                if (settings != null && settings.Count > 0)
                {
                    displayService.DisplaySettings(settings);
                    ApplySettings(settings);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error processing data: {ex.Message}");
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    private static readonly Dictionary<string, Action<CNCPipe, object>> SettingMappers = new()
    {
        // Informational settings (no CNC action required)
        ["device_id"] = (cnc, value) => { /* Informational only */ },
        ["device_version"] = (cnc, value) => { /* Informational only */ },
        ["switches_raw"] = (cnc, value) => { /* Raw value - individual switches handled below */ },
        ["feedrate_percent"] = (cnc, value) => { /* Derived from feedrate_value */ },

        ["step_size"] = (cnc, value) => { /* Informational - step_index is used */ },
        ["Command"] = (cnc, value) => { /* Fallback parser - ignore */ },
        ["Value"] = (cnc, value) => { /* Fallback parser - ignore */ },

        // Feedrate control
        ["feedrate_value"] = (cnc, value) =>
        {
            // TODO: if (value is double d) cnc.state.SetFeedRate(d);
        },
        // Switch mappings (map to appropriate PLC bits)
        ["switch_enabled"] = (cnc, value) =>
        {
            // TODO: if (value is bool b) cnc.plc.SetPlcBit(1, b);
        },
        ["switch_feedhold"] = (cnc, value) =>
        {
            // Set feedhold skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinFeedHold, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for feedhold switch");
            }
        },
        ["switch_cycleStart"] = (cnc, value) =>
        {
            // Set cycle start skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinCycleStart, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for cycle start switch");
            }
        },
        ["switch_cycleStop"] = (cnc, value) =>
        {
            // Set cycle stop skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinCycleCancel, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for cycle stop switch");
            }
        },
        ["switch_toolCheck"] = (cnc, value) =>
        {
            // Set tool check skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinToolCheck, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for tool check switch");
            }
        },
        ["switch_incContPressed"] = (cnc, value) =>
        {
            // Set Inc/Cont skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinIncCont, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for Inc/Cont switch");
            }
        },
        ["switch_slowFastPressed"] = (cnc, value) =>
        {
            // Set Slow/Fast Jog skin event state
            if (_cncPipe != null && _cncPipe.IsConstructed() && _cncPlc != null)
            {
                _cncPlc.SetSkinEventState(SkinEvents.SkinFastSlowJog, (bool)value ? 1 : 0);
            }
            else
            {
                throw new Exception("  ? CNC Pipe or PLC not initialized for Slow/Fast Jog switch");
            }
        },

        // Jog step size control
        ["step_index"] = (cnc, value) =>
        {
            // TODO: if (value is int i) cnc.parameter.SetMachineParameter(xxx, i);
        },

        // Encoder positions (if used separately from deltas)
        ["encoder_posX"] = (cnc, value) =>
        {
            // TODO: if (value is double d) cnc.dro.SetDroValue(1, d);
        },
        ["encoder_posY"] = (cnc, value) =>
        {
            // TODO: if (value is double d) cnc.dro.SetDroValue(2, d);
        },
        ["encoder_posZ"] = (cnc, value) =>
        {
            // TODO: if (value is double d) cnc.dro.SetDroValue(3, d);
        },
    };

    static void ApplySettings(Dictionary<string, object> settings)
    {
        // Handle combined encoder delta move (G-Code generation)
        if (settings.TryGetValue("encoder_deltaX", out var deltaX) &&
            settings.TryGetValue("encoder_deltaY", out var deltaY) &&
            settings.TryGetValue("encoder_deltaZ", out var deltaZ) &&
            settings.TryGetValue("step_size", out var stepSize))
        {
            try
            {
                if ((double)deltaX != 0.0 || (double)deltaY != 0.0 || (double)deltaZ != 0.0)
                {
                    Console.WriteLine($"  > Applying encoder deltas: ΔX={deltaX}, ΔY={deltaY}, ΔZ={deltaZ}, Step Size={stepSize}");

                    var gcode = GenerateG1Move((double)deltaX, (double)deltaY, (double)deltaZ, settings);

                    CNCPipe.ReturnCode? returnCode = _cncJob?.RunCommand(gcode, false);
                    if (returnCode != CNCPipe.ReturnCode.SUCCESS)
                    {
                        throw new Exception($"CNC command failed with code: {returnCode}");
                    }
                }

                // Remove processed keys
                settings.Remove("encoder_deltaX");
                settings.Remove("encoder_deltaY");
                settings.Remove("encoder_deltaZ");
                // Also remove feedrate if used
                settings.Remove("feedrate_value");
                // Optionally remove individual encoder positions if present
                settings.Remove("encoder_posX");
                settings.Remove("encoder_posY");
                settings.Remove("encoder_posZ");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ? Failed to apply encoder deltas: {ex.Message}");
            }
        }

        // Handle remaining individual settings
        foreach (var setting in settings)
        {
            try
            {
                if (SettingMappers.TryGetValue(setting.Key, out var mapper) && _cncPipe != null)
                {
                    mapper(_cncPipe, setting.Value);
                }
                else
                {
                    Console.Error.WriteLine($"  ! No mapping defined for setting: {setting.Key}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ? Failed to apply {setting.Key}: {ex.Message}");
            }
        }
    }

    private static string GenerateG1Move(double dx, double dy, double dz, Dictionary<string, object> settings)
    {
        var stepSize = settings.ContainsKey("step_size") ? (double)settings["step_size"] : 0.01;
        dx *= stepSize;
        dy *= stepSize;
        dz *= stepSize;
        var gcode = $"G1 X{dx:#0.0####} Y{dy:#0.0####} Z{dz:#0.0####}";
        if (settings.TryGetValue("feedrate_value", out var feedrate))
        {
            gcode += $" F{feedrate}";
        }
        return gcode;
    }
}
