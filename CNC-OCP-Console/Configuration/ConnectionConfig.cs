/*
 * CNC-OCP-Console - ConnectionConfig.cs
 * Defines configuration constants for Teensy connection and reconnection behavior.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
/// <summary>
/// Configuration constants for Teensy connection and reconnection behavior
/// </summary>
static class ConnectionConfig
{
    // Serial port settings
    public const string TEENSY_HANDSHAKE_ID = "TEENSY_OCP_001";
    public const string HANDSHAKE_REQUEST = "HANDSHAKE";
    public const int BAUD_RATE = 115200;
    public const int READ_TIMEOUT_MS = 100;

    // Connection retry settings
    public const int HEARTBEAT_INTERVAL_MS = 10000;   // Teensy should send heartbeat every 10 seconds
    public const int HEARTBEAT_TIMEOUT_MS = 15000;    // Reconnect if no message received in 15 seconds
    public const int RECONNECT_DELAY_MS = 2000;       // Wait before attempting reconnection
    public const int RECONNECT_RETRY_DELAY_MS = 5000; // Wait between failed reconnection attempts

    // Handshake settings
    public const int HANDSHAKE_TIMEOUT_MS = 500;     // Timeout per line during handshake
    public const int MAX_HANDSHAKE_ATTEMPTS = 30;    // Max lines to read for sync
    public const int POST_OPEN_DELAY_MS = 2000;      // Wait for device reset after port open

    // Display settings
    public const int DISPLAY_THROTTLE_MS = 200;      // Minimum time between console updates
    public const int STARTUP_WAIT_SECONDS = 5;       // Warning delay for first message
}
