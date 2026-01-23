using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentroidAPI;

/// <summary>
/// Main application entry point - coordinates initialization and data flow
/// Responsible for orchestration only, delegates to specialized managers
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("CNC Teensy OCP to CentroidAPI Console Application");
            Console.WriteLine("===================================================\n");

            // Initialize CNCPipe
            Console.WriteLine("[INIT] Creating CNCPipe...");
            var cncPipe = new CNCPipe();
            Console.Out.Flush();

            // Validate CNCPipe was properly constructed
            if (!cncPipe.IsConstructed())
            {
                Console.Error.WriteLine("? CNCPipe failed to initialize properly");
                Console.WriteLine("\nTroubleshooting:");
                Console.WriteLine("- Verify CentroidAPI.dll is accessible");
                Console.WriteLine("- Ensure CNC control software is running");
                Console.WriteLine("- Check system permissions for CNC API access");
                Environment.Exit(1);
            }

            Console.WriteLine("✓ CNCPipe initialized and constructed");

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
            var readTask = ReceiveAndProcessDataWithReconnection(teensyManager, cncPipe, cts.Token);

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

    static async Task ReceiveAndProcessDataWithReconnection(TeensySerialManager teensyManager, CNCPipe cncPipe, CancellationToken cancellationToken)
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
                    ApplySettings(cncPipe, settings);
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

    static void ApplySettings(CNCPipe cncPipe, Dictionary<string, object> settings)
    {
        foreach (var setting in settings)
        {
            try
            {
                // TODO: Map OCP settings to CentroidAPI calls
                // Example: if (setting.Key == "feedrate_value") { cncPipe.SetFeedrate(setting.Value); }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ? Failed to apply {setting.Key}: {ex.Message}");
            }
        }
    }
}
