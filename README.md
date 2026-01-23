# CNC Teensy OCP to CentroidAPI Console Application

A robust C# console application that bridges Teensy-based Operator Control Panels (OCP) with Centroid CNC control systems via reliable serial communication.

## 🎯 Overview

This application establishes a reliable communication bridge between:

- **Teensy microcontroller** running OCP firmware (Operator Control Panel)
- **Centroid CNC control software** via CentroidAPI
- **USB Serial communication** with automatic reconnection and error recovery

## ✨ Key Features

### 🔌 Reliable Serial Communication

- **Automatic device discovery**: Scans all available serial ports
- **Handshake protocol**: Validates Teensy identity (`TEENSY_OCP_001`)
- **Message synchronization**: Handles partial/corrupted messages gracefully
- **Bluetooth filtering**: Excludes audio devices from port enumeration

### 🔄 Auto-Reconnection

- **Connection monitoring**: Detects disconnections within 5 seconds
- **Automatic recovery**: Reconnects without manual intervention
- **Graceful degradation**: Continues attempting reconnection indefinitely
- **State preservation**: Maintains CNC control state across reconnections

### 📊 Data Processing

- **CSV parsing**: Handles Teensy OCP format: `TEENSY_OCP_001,1.0.0,encX,encY,encZ,switches,stepIndex,feedrate`
- **Named parameters**: Maps CSV fields to semantic CNC settings
- **Validation**: Filters corrupted encoder values and noise
- **Real-time display**: Throttled console output (200ms intervals)

### 🏗️ Architecture

- **Single Responsibility Principle**: Each class has one clear purpose
- **Dependency Injection**: Services are loosely coupled
- **Configuration-driven**: All timing and behavior constants centralized
- **Error resilience**: Comprehensive exception handling and logging

## 📁 Project Structure

```
CNC-OPC-Console/
├── Configuration/
│   └── ConnectionConfig.cs          # Connection parameters and constants
├── Converters/
│   └── SerialToSettingsConverter.cs # CSV/JSON parsing to CNC settings
├── Managers/
│   └── TeensySerialManager.cs       # Serial communication management
├── Serial/
│   └── WindowsSerialPort.cs        # Windows System.IO.Ports wrapper
├── Services/
│   ├── ConnectionMonitor.cs        # Connection health and reconnection
│   └── DataDisplayService.cs       # Throttled console output
├── Program.cs                       # Main application orchestration
├── CentroidAPI.cs                   # Centroid CNC API wrapper
└── CNC-OPC-Console.csproj          # .NET 9.0 project file
```

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** installed
- **Centroid CNC control software** running
- **Teensy microcontroller** with OCP firmware
- **USB connection** between Teensy and host computer

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd CNC-OPC-Console

# Build the application
dotnet build
```

### Usage

```bash
# Run the application
dotnet run

# Or from the project directory
cd CNC-OPC-Console
dotnet run
```

### Expected Output

```
CNC Teensy OCP to CentroidAPI Console Application
===================================================

[INIT] Creating CNCPipe...
✓ CNCPipe initialized and constructed
[INIT] Creating TeensySerialManager...
✓ Teensy Serial Manager initialized
[INIT] Discovering and connecting to Teensy...
Found 5 serial port(s):
  - COM3
  - COM4
  - COM5
  - COM6
  - COM7

  Trying COM3...
    Received: TEENSY_OCP_001,1.0.0,0,0,0,4,0,56
✓ Connected to Teensy on COM3 at 115200 baud
  Starting CSV read loop...

Listening for Teensy serial data...

[14:23:15.123] Settings:
  device_id: TEENSY_OCP_001
  device_version: 1.0.0
  encoder_deltaX: 0
  encoder_deltaY: 0
  encoder_deltaZ: 0
  switches_raw: 4
  switch_enabled: false
  switch_feedhold: true
  switch_cycleStart: false
  switch_cycleStop: false
  switch_toolCheck: false
  step_index: 0
  step_size: 1x
  feedrate_value: 56
  feedrate_percent: 21.96
```

## 🔧 Teensy Firmware Requirements

Your Teensy firmware must send CSV data in this exact format:

```cpp
// Send OCP data every 20ms (50Hz update rate)
char buffer[64];
int len = snprintf(buffer, sizeof(buffer),
    "%s,%s,%d,%d,%d,%d,%d,%d\n",
    "TEENSY_OCP_001",  // Device ID
    "1.0.0",           // Firmware version
    encX,               // Encoder X delta
    encY,               // Encoder Y delta
    encZ,               // Encoder Z delta
    switches,           // Switch byte (bitfield)
    stepIndex,          // Step multiplier (0=1x, 1=10x, 2=100x)
    feedrate);          // Feedrate (0-255)

Serial.println(buffer);
```

### Switch Bit Mapping

- **Bit 0 (0x01)**: Enabled
- **Bit 1 (0x02)**: Feed Hold
- **Bit 2 (0x04)**: Cycle Start
- **Bit 3 (0x08)**: Cycle Stop
- **Bit 4 (0x10)**: Tool Check

## ⚙️ Configuration

All timing and behavior parameters are centralized in `Configuration/ConnectionConfig.cs`:

```csharp
// Serial communication
public const int BAUD_RATE = 115200;
public const int READ_TIMEOUT_MS = 100;

// Reconnection behavior
public const int MAX_CONSECUTIVE_FAILURES = 50;  // ~5 seconds
public const int RECONNECT_DELAY_MS = 2000;
public const int RECONNECT_RETRY_DELAY_MS = 5000;

// Display throttling
public const int DISPLAY_THROTTLE_MS = 200;
```

## 🐛 Troubleshooting

### Connection Issues

```
? No Teensy device found on any port
```

- Verify Teensy is connected via USB
- Check that Teensy firmware sends handshake: `Serial.println("TEENSY_OCP_001,1.0.0,0,0,0,0,0,0");`
- Check available COM ports in Device Manager

### Data Reception Issues

```
⏳ Waiting for Teensy serial data (attempt 123)...
```

- Verify Teensy sends data at expected baud rate (115200)
- Check Teensy message format matches: `TEENSY_OCP_001,1.0.0,...`
- Ensure messages end with `\n` (newline)

### Centroid API Issues

```
? CNCPipe failed to initialize properly
```

- Verify Centroid CNC control software is running
- Check CentroidAPI.dll is accessible
- Ensure proper system permissions for CNC API access

## 🔍 Development

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

### Testing

```bash
# Run tests (when implemented)
dotnet test

# Run with verbose logging
dotnet run --verbose
```

### Architecture Principles

#### Single Responsibility Principle (SRP)

- **TeensySerialManager**: Serial communication only
- **ConnectionMonitor**: Connection health monitoring
- **DataDisplayService**: UI/output formatting
- **SerialToSettingsConverter**: Data parsing only

#### Don't Repeat Yourself (DRY)

- All constants in `ConnectionConfig.cs`
- Shared serial port enumeration logic
- Common error handling patterns

#### Dependency Inversion

- Services depend on abstractions (interfaces)
- Pluggable logging via callbacks

## 📋 Dependencies

- **.NET 9.0** - Target framework
- **System.IO.Ports 4.5.0** - Serial port communication
- **CentroidAPI.dll** - Centroid CNC control API (external)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes following SRP and DRY principles
4. Add/update tests
5. Submit a pull request

## 📄 License

## License

This project is licensed under the MIT License - see the {Link: LICENSE file https://opensource.org/license/mit} for details.

## 🙋 Support

For issues and questions:

- Check the troubleshooting section above
- Review Teensy firmware compatibility
- Verify Centroid CNC software integration
- Check system permissions and USB connectivity

---

**Built with reliability and maintainability in mind.** 🚀
