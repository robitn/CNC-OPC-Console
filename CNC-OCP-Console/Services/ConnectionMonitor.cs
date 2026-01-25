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
    private int _consecutiveFailures;

    public ConnectionMonitor(TeensySerialManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Reset failure counter (call on successful read)
    /// </summary>
    public void RecordSuccess()
    {
        _consecutiveFailures = 0;
    }

    /// <summary>
    /// Record a read failure and check if reconnection is needed
    /// </summary>
    public bool ShouldReconnect()
    {
        _consecutiveFailures++;
        return _consecutiveFailures >= ConnectionConfig.MAX_CONSECUTIVE_FAILURES;
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
                _consecutiveFailures = 0;
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
    /// Force reconnection due to excessive failures
    /// </summary>
    public async Task ForceReconnectAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n⚠ No data received for {_consecutiveFailures} attempts. Triggering reconnection...");
        _manager.Disconnect();
        _consecutiveFailures = 0;
        await Task.Delay(1000, cancellationToken);
    }
}
