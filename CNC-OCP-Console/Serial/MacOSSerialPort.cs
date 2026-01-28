/*
 * CNC-OCP-Console - MacOSSerialPort.cs
 * Implements native macOS serial port communication using POSIX APIs.
 * Provides platform-specific serial I/O for macOS.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Native macOS serial port implementation using POSIX APIs
/// </summary>
class MacOSSerialPort : ISerialPort
{
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int read(int fd, byte[] buf, int count);

    [DllImport("libc", SetLastError = true)]
    private static extern int write(int fd, byte[] buf, int count);

    [DllImport("libc", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);

    private const int O_RDWR = 2;
    private const int O_NONBLOCK = 4;
    private const int F_SETFL = 4;

    private int _fileDescriptor = -1;
    private string _portName;
    private StringBuilder _lineBuffer = new StringBuilder();

    public bool IsOpen => _fileDescriptor >= 0;
    public int BytesToRead { get; private set; }

    public MacOSSerialPort(string portName, int baudRate)
    {
        _portName = portName;
    }

    public void Open()
    {
        // Open port with O_NONBLOCK to prevent indefinite blocking
        _fileDescriptor = open(_portName, O_RDWR | O_NONBLOCK);
        if (_fileDescriptor < 0)
            throw new IOException($"Failed to open port {_portName}");

        // Keep port in non-blocking mode for reading with timeout
        // (macOS doesn't reliably support termios baud rate setting via P/Invoke)
    }

    public void Close()
    {
        if (_fileDescriptor >= 0)
        {
            close(_fileDescriptor);
            _fileDescriptor = -1;
        }
    }

    public char ReadChar()
    {
        if (_fileDescriptor < 0)
            throw new InvalidOperationException("Port not open");

        byte[] buffer = new byte[1];
        int bytesRead = read(_fileDescriptor, buffer, 1);

        if (bytesRead <= 0)
            throw new TimeoutException("No data available");

        return (char)buffer[0];
    }

    public string? ReadLine(int timeoutMs)
    {
        if (_fileDescriptor < 0)
            return null;

        var startTime = DateTime.Now;
        byte[] buffer = new byte[256];

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            // Try to read available data
            int bytesRead = read(_fileDescriptor, buffer, buffer.Length);

            if (bytesRead > 0)
            {
                // Process received bytes
                for (int i = 0; i < bytesRead; i++)
                {
                    char c = (char)buffer[i];

                    if (c == '\n')
                    {
                        // Found complete line
                        string line = _lineBuffer.ToString().Trim();
                        _lineBuffer.Clear();
                        return line;
                    }
                    else if (c != '\r')
                    {
                        // Add to buffer (skip \r)
                        _lineBuffer.Append(c);

                        // Prevent buffer overflow
                        if (_lineBuffer.Length > 1024)
                        {
                            _lineBuffer.Clear();
                        }
                    }
                }
            }
            else
            {
                // No data available, short sleep
                System.Threading.Thread.Sleep(5);
            }
        }

        return null;
    }

    public void WriteLine(string message)
    {
        if (_fileDescriptor < 0)
            throw new InvalidOperationException("Port not open");

        // Add newline and convert to bytes
        string messageWithNewline = message + "\n";
        byte[] bytes = Encoding.UTF8.GetBytes(messageWithNewline);

        int bytesWritten = write(_fileDescriptor, bytes, bytes.Length);
        if (bytesWritten < 0)
            throw new IOException("Failed to write to serial port");
    }

    public void Dispose()
    {
        Close();
    }
}
