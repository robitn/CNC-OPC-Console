using System;
using System.IO.Ports;

/// <summary>
/// Windows serial port implementation using System.IO.Ports
/// </summary>
class WindowsSerialPort : ISerialPort
{
    private SerialPort? _serialPort;

    public bool IsOpen => _serialPort?.IsOpen ?? false;
    public int BytesToRead => _serialPort?.BytesToRead ?? 0;

    public WindowsSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
    }

    public void Open() => _serialPort?.Open();
    public void Close() => _serialPort?.Close();
    public char ReadChar()
    {
        if (_serialPort == null)
            throw new InvalidOperationException("Port not open");
        int charCode = _serialPort.ReadChar();
        return charCode >= 0 ? (char)charCode : throw new IOException("No data available");
    }

    public string? ReadLine(int timeoutMs)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return null;

        _serialPort.ReadTimeout = timeoutMs;
        try
        {
            return _serialPort.ReadLine();
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    public void Dispose() => _serialPort?.Dispose();
}
