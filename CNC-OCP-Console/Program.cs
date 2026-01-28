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

                // Check for heartbeat timeout (no messages received for extended period)
                if (connectionMonitor.IsHeartbeatTimeout())
                {
                    bool reconnected = await connectionMonitor.ForceReconnectAsync(
                        cancellationToken,
                        $"No messages (data or heartbeat) received in {ConnectionConfig.HEARTBEAT_TIMEOUT_MS / 1000} seconds"
                    );
                    if (!reconnected)
                    {
                        continue;
                    }
                    // Reset counters after successful reconnection
                    nullCount = 0;
                    startWarningTime = DateTime.Now;
                }

                // Read CSV message from serial port
                var message = teensyManager.ReadMessage(cancellationToken);

                if (message == null)
                {
                    nullCount++;

                    // Display waiting message if we haven't received any data yet
                    if (successCount == 0)
                    {
                        displayService.DisplayWaitingMessage(nullCount, ref startWarningTime);
                    }

                    await Task.Delay(5, cancellationToken);
                    continue;
                }

                // Record message received (resets heartbeat timer)
                connectionMonitor.RecordMessageReceived();

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
        // Heartbeat message (sent periodically by Teensy to maintain connection)
        ["heartbeat"] = (cnc, value) => { /* Connection keepalive - no action needed */ },

        // Encoder deltas - handled separately in ApplySettings for G-code generation
        ["enc_x"] = (cnc, value) => { /* Handled in ApplySettings */ },
        ["enc_y"] = (cnc, value) => { /* Handled in ApplySettings */ },
        ["enc_z"] = (cnc, value) => { /* Handled in ApplySettings */ },

        // Absolute encoder positions (informational)
        ["abs_x"] = (cnc, value) => { /* Informational only */ },
        ["abs_y"] = (cnc, value) => { /* Informational only */ },
        ["abs_z"] = (cnc, value) => { /* Informational only */ },

        // Feedrate control
        ["feedrate"] = (cnc, value) =>
        {
            // TODO: if (value is double d) cnc.state.SetFeedRate(d);
        },

        // Switch mappings (map to appropriate skin events)
        ["enabled"] = (cnc, value) =>
        {
            // TODO: if (value is bool b) cnc.plc.SetPlcBit(1, b);
        },
        ["feed_hold_pressed"] = (cnc, value) =>
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
        ["cycle_start_pressed"] = (cnc, value) =>
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
        ["cycle_stop_pressed"] = (cnc, value) =>
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
        ["tool_check_pressed"] = (cnc, value) =>
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
        ["inc_cont_pressed"] = (cnc, value) =>
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
        ["slow_fast_pressed"] = (cnc, value) =>
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
    };

    static void ApplySettings(Dictionary<string, object> settings)
    {
        // Handle encoder delta move (G-Code generation)
        // Process any encoder changes that were sent (event-driven: only changed axes are sent)
        if (settings.ContainsKey("enc_x") || settings.ContainsKey("enc_y") || settings.ContainsKey("enc_z"))
        {
            try
            {
                // Get delta values (default to 0.0 if axis not present in this update)
                var deltaX = settings.TryGetValue("enc_x", out var dx) ? (double)dx : 0.0;
                var deltaY = settings.TryGetValue("enc_y", out var dy) ? (double)dy : 0.0;
                var deltaZ = settings.TryGetValue("enc_z", out var dz) ? (double)dz : 0.0;

                if (deltaX != 0.0 || deltaY != 0.0 || deltaZ != 0.0)
                {
                    // Get step index if present (for step size calculation)
                    var stepIndex = settings.TryGetValue("step_index", out var si) ? (int)si : 0;
                    Console.WriteLine($"  > Applying encoder deltas: ΔX={deltaX}, ΔY={deltaY}, ΔZ={deltaZ}, Step Index={stepIndex}");

                    var gcode = GenerateG1Move(deltaX, deltaY, deltaZ, settings);

                    CNCPipe.ReturnCode? returnCode = _cncJob?.RunCommand(gcode, false);
                    if (returnCode != CNCPipe.ReturnCode.SUCCESS)
                    {
                        throw new Exception($"CNC command failed with code: {returnCode}");
                    }
                }

                // Remove processed encoder keys
                settings.Remove("enc_x");
                settings.Remove("enc_y");
                settings.Remove("enc_z");
                // Also remove related keys if present
                settings.Remove("abs_x");
                settings.Remove("abs_y");
                settings.Remove("abs_z");
                settings.Remove("feedrate");
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
        // Map step_index to actual step sizes (adjust these values based on your requirements)
        var stepSizes = new[] { 0.001, 0.01, 0.1, 1.0, 10.0 };
        var stepIndex = settings.TryGetValue("step_index", out var si) ? (int)si : 1;
        var stepSize = (stepIndex >= 0 && stepIndex < stepSizes.Length) ? stepSizes[stepIndex] : 0.01;

        dx *= stepSize;
        dy *= stepSize;
        dz *= stepSize;
        var gcode = $"G1 X{dx:#0.0####} Y{dy:#0.0####} Z{dz:#0.0####}";
        if (settings.TryGetValue("feedrate", out var feedrate))
        {
            gcode += $" F{feedrate}";
        }
        return gcode;
    }
}
