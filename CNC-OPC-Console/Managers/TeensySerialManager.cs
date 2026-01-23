using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

/// <summary>
/// Manages connection to Teensy OCP device via USB Serial
/// Receives CSV-formatted messages for reliable communication
/// Single Responsibility: Serial communication with Teensy device
/// </summary>
class TeensySerialManager
{
    private CrossPlatformSerialPort? _serialPort;

    public TeensySerialManager()
    {
    }

    /// <summary>
    /// Discover and connect to Teensy OCP device
    /// </summary>
    public bool DiscoverAndConnect()
    {
        return DiscoverAndConnect(message => Console.WriteLine(message));
    }

    /// <summary>
    /// Discover and connect to Teensy OCP device with custom logging
    /// </summary>
    public bool DiscoverAndConnect(Action<string> log)
    {
        try
        {
            var ports = CrossPlatformSerialPort.GetPortNames().OrderBy(p => p).ToList();

            if (ports.Count == 0)
            {
                log("✗ No serial ports found");
                return false;
            }

            log($"Found {ports.Count} serial port(s):");
            foreach (var port in ports)
            {
                log($"  - {port}");
            }

            // Try each port to find Teensy
            foreach (var portName in ports)
            {
                try
                {
                    log($"\n  Trying {portName}...");
                    _serialPort = new CrossPlatformSerialPort(portName, ConnectionConfig.BAUD_RATE, Parity.None, 8, StopBits.One);
                    _serialPort.ReadTimeout = ConnectionConfig.POST_OPEN_DELAY_MS;
                    _serialPort.Open();

                    if (!_serialPort.IsOpen)
                    {
                        log($"    ✗ Failed to open");
                        continue;
                    }

                    // Wait for device reset
                    Thread.Sleep(ConnectionConfig.POST_OPEN_DELAY_MS);

                    // Synchronize to message boundaries
                    log($"    Looking for handshake...");
                    bool foundHandshake = false;

                    for (int attempt = 0; attempt < ConnectionConfig.MAX_HANDSHAKE_ATTEMPTS; attempt++)
                    {
                        var line = ReadLine(ConnectionConfig.HANDSHAKE_TIMEOUT_MS);
                        if (line != null && !string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith(ConnectionConfig.TEENSY_HANDSHAKE_ID))
                            {
                                log($"    Received: {line}");
                                foundHandshake = true;
                                break;
                            }
                        }
                    }

                    if (foundHandshake)
                    {
                        _serialPort.ReadTimeout = ConnectionConfig.READ_TIMEOUT_MS;
                        log($"\n✓ Connected to Teensy on {portName} at {ConnectionConfig.BAUD_RATE} baud");
                        log($"  Starting CSV read loop...");
                        return true;
                    }
                    else
                    {
                        log($"    ✗ No handshake received");
                        _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                }
                catch (Exception ex)
                {
                    log($"    ✗ Error: {ex.Message}");
                    _serialPort?.Close();
                    _serialPort?.Dispose();
                    _serialPort = null;
                }
            }

            log("\n✗ No Teensy device found on any port");
            return false;
        }
        catch (Exception ex)
        {
            log($"✗ Device discovery failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read a single line from serial port with timeout
    /// </summary>
    private string? ReadLine(int timeoutMs)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return null;

        return _serialPort.ReadLine(timeoutMs);
    }

    /// <summary>
    /// Read a CSV message from serial port
    /// </summary>
    public string? ReadCSVMessage(CancellationToken cancellationToken = default)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return null;

        return ReadLine(ConnectionConfig.READ_TIMEOUT_MS);
    }

    /// <summary>
    /// Read message (returns text data)
    /// </summary>
    public (string? textData, byte[]? binaryData)? ReadMessage(CancellationToken cancellationToken = default)
    {
        var csv = ReadCSVMessage(cancellationToken);
        if (csv != null)
        {
            return (csv, null);
        }
        return null;
    }

    /// <summary>
    /// Check if device is connected
    /// </summary>
    public bool IsConnected => _serialPort?.IsOpen ?? false;

    /// <summary>
    /// Disconnect and clean up
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Error disconnecting: {ex.Message}");
        }
    }
}
