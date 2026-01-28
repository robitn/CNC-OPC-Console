/*
 * CNC-OCP-Console - ConnectionMonitor.cs
 * Monitors connection health and manages automatic reconnection for Teensy device.
 * Handles connection lifecycle management.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors connection health and handles automatic reconnection
/// Single Responsibility: Connection lifecycle management
/// </summary>
class ConnectionMonitor
{
    private readonly TeensySerialManager _manager;
    private DateTime _lastMessageReceived;

    public ConnectionMonitor(TeensySerialManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _lastMessageReceived = DateTime.Now;
    }

    /// <summary>
    /// Record successful message receipt (data or heartbeat)
    /// </summary>
    public void RecordMessageReceived()
    {
        _lastMessageReceived = DateTime.Now;
    }

    /// <summary>
    /// Check if heartbeat timeout has been exceeded
    /// </summary>
    public bool IsHeartbeatTimeout()
    {
        var timeSinceLastMessage = (DateTime.Now - _lastMessageReceived).TotalMilliseconds;
        return timeSinceLastMessage > ConnectionConfig.HEARTBEAT_TIMEOUT_MS;
    }

    /// <summary>
    /// Attempt reconnection with retry logic
    /// </summary>
    public async Task<bool> TryReconnectAsync(CancellationToken cancellationToken)
    {
        if (!_manager.IsConnected)
        {
            Console.WriteLine("\n⚠ Teensy disconnected. Attempting to reconnect...");
            _manager.Disconnect();  // Clean up

            await Task.Delay(ConnectionConfig.RECONNECT_DELAY_MS, cancellationToken);

            if (_manager.DiscoverAndConnect())
            {
                Console.WriteLine("✓ Reconnected to Teensy device\n");
                _lastMessageReceived = DateTime.Now;
                return true;
            }
            else
            {
                Console.WriteLine($"✗ Reconnection failed. Retrying in {ConnectionConfig.RECONNECT_RETRY_DELAY_MS / 1000} seconds...");
                await Task.Delay(ConnectionConfig.RECONNECT_RETRY_DELAY_MS, cancellationToken);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Force reconnection due to heartbeat timeout or connection loss
    /// </summary>
    public async Task<bool> ForceReconnectAsync(CancellationToken cancellationToken, string reason = "No heartbeat received")
    {
        Console.WriteLine($"\n⚠ {reason}. Triggering reconnection...");
        _manager.Disconnect();
        _lastMessageReceived = DateTime.Now;

        await Task.Delay(ConnectionConfig.RECONNECT_DELAY_MS, cancellationToken);

        if (_manager.DiscoverAndConnect())
        {
            Console.WriteLine("✓ Reconnected to Teensy device\n");
            _lastMessageReceived = DateTime.Now;
            return true;
        }
        else
        {
            Console.WriteLine($"✗ Reconnection failed. Retrying in {ConnectionConfig.RECONNECT_RETRY_DELAY_MS / 1000} seconds...");
            await Task.Delay(ConnectionConfig.RECONNECT_RETRY_DELAY_MS, cancellationToken);
            return false;
        }
    }
}
