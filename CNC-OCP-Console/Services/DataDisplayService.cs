using System;
using System.Collections.Generic;

/// <summary>
/// Handles console display of OCP data with throttling
/// Single Responsibility: User interface / data presentation
/// </summary>
class DataDisplayService
{
    private DateTime _lastDisplayTime = DateTime.MinValue;

    /// <summary>
    /// Display settings to console if throttle period has elapsed
    /// </summary>
    public void DisplaySettings(Dictionary<string, object> settings)
    {
        var now = DateTime.Now;
        if ((now - _lastDisplayTime).TotalMilliseconds < ConnectionConfig.DISPLAY_THROTTLE_MS)
            return;

        _lastDisplayTime = now;
        Console.WriteLine($"[{now:HH:mm:ss.fff}] Settings:");

        foreach (var kvp in settings)
        {
            string value = FormatValue(kvp.Value);
            Console.WriteLine($"  {kvp.Key}: {value}");
        }
    }

    /// <summary>
    /// Format value for display based on type
    /// </summary>
    private static string FormatValue(object value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            double d => d.ToString("F2"),
            string s => s.Length > 30 ? s.Substring(0, 27) + "..." : s,
            _ => value?.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Display startup waiting message (throttled)
    /// </summary>
    public void DisplayWaitingMessage(int attemptCount, ref DateTime lastWarningTime)
    {
        if ((DateTime.Now - lastWarningTime).TotalSeconds > ConnectionConfig.STARTUP_WAIT_SECONDS)
        {
            Console.WriteLine($"⏳ Waiting for Teensy serial data (attempt {attemptCount})...");
            lastWarningTime = DateTime.Now;
        }
    }
}
