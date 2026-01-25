/*
 * CNC-OCP-Console - CrossPlatformSerialPort.cs
 * Provides a cross-platform serial port wrapper for Windows and macOS.
 * Unifies serial communication interface for the application.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

/// <summary>
/// Cross-platform serial port wrapper providing unified interface for Windows and macOS
/// </summary>
class CrossPlatformSerialPort : IDisposable
{
    private ISerialPort? _serialPort;

    public bool IsOpen => _serialPort?.IsOpen ?? false;
    public int BytesToRead => _serialPort?.BytesToRead ?? 0;
    public int ReadTimeout { get; set; }
    public int WriteTimeout { get; set; }

    /// <summary>
    /// Get available serial port names in a cross-platform manner
    /// </summary>
    public static string[] GetPortNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return SerialPort.GetPortNames();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, enumerate /dev/tty.* and /dev/cu.* devices
            var ttyPorts = System.IO.Directory.GetFiles("/dev", "tty.*");
            var cuPorts = System.IO.Directory.GetFiles("/dev", "cu.*");

            // Prefer cu.* devices (callout devices) for serial communication
            // Filter out non-USB serial devices (Bluetooth, debug, wireless)
            var allPorts = cuPorts.Concat(ttyPorts)
                .Where(p =>
                {
                    var name = p.ToLower();
                    return !name.Contains("bluetooth")
                        && !name.Contains("debug")
                        && !name.Contains("wlan")
                        && !name.Contains("jbl")
                        && !name.Contains("airpods")
                        && !name.Contains("beats")
                        && !name.Contains("bose")
                        && !name.Contains("sony")
                        && !name.Contains("h97")  // Common headphone model
                        && !name.Contains("tune");  // JBL Tune series
                })
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            return allPorts;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // On Linux, look for /dev/ttyUSB* and /dev/ttyACM* devices
            var usbPorts = System.IO.Directory.Exists("/dev")
                ? System.IO.Directory.GetFiles("/dev", "ttyUSB*")
                    .Concat(System.IO.Directory.GetFiles("/dev", "ttyACM*"))
                    .OrderBy(p => p)
                    .ToArray()
                : Array.Empty<string>();
            return usbPorts;
        }

        return Array.Empty<string>();
    }

    public CrossPlatformSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _serialPort = new WindowsSerialPort(portName, baudRate, parity, dataBits, stopBits);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _serialPort = new MacOSSerialPort(portName, baudRate);
        }
        else
        {
            throw new PlatformNotSupportedException("Serial port communication not supported on this platform");
        }
    }

    public void Open() => _serialPort?.Open();
    public void Close() => _serialPort?.Close();
    public char ReadChar() => _serialPort?.ReadChar() ?? throw new InvalidOperationException("Serial port not available");
    public string? ReadLine(int timeoutMs) => _serialPort?.ReadLine(timeoutMs);
    public void Dispose() => _serialPort?.Dispose();
}

interface ISerialPort : IDisposable
{
    bool IsOpen { get; }
    int BytesToRead { get; }
    void Open();
    void Close();
    char ReadChar();
    string? ReadLine(int timeoutMs);
}
