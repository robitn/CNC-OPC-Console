/*
 * CNC-OCP-Console - SerialToSettingsConverter.cs
 * Converts serial data (CSV, JSON, text) into CNC machine settings.
 * Handles parsing and conversion logic for incoming data.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System;
using System.Collections.Generic;
using System.Text.Json;
using CNC_OCP_Console.Configuration;

/// <summary>
/// Converts serial data (JSON and text) into CNC machine settings
/// Responsible only for data parsing and conversion, not for I/O
/// </summary>
class SerialToSettingsConverter
{
    /// <summary>
    /// Convert serial CSV data to machine settings
    /// 
    /// Expected formats:
    /// OCP CSV (new): TEENSY_OCP_001,1.0.0,0,0,0,100,200,300,4,1,25
    ///                (id,version,encX,encY,encZ,absX,absY,absZ,switches,stepIndex,feedrate)
    /// OCP CSV (old): TEENSY_OCP_001,1.0.0,0,0,0,4,1,25
    ///                (id,version,encX,encY,encZ,switches,stepIndex,feedrate)
    /// OCP JSON: {"ocp":{"device":{...},"switches":{...},"feedrate":{...},"encoders":{...}}}
    /// Generic JSON: {"command": "spindle", "value": 1200}
    /// CSV: SPINDLE,1200
    /// Text: SPINDLE=1200
    /// </summary>
    public Dictionary<string, object>? Convert(string data)
    {
        return ConvertText(data);
    }

    private Dictionary<string, object>? ConvertText(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        var settings = new Dictionary<string, object>();

        try
        {
            data = data.Trim();

            // Try JSON parsing first
            if (data.StartsWith("{"))
            {
                return ParseJson(data);
            }

            // Try CSV format (COMMAND,VALUE)
            if (data.Contains(","))
            {
                return ParseCsv(data);
            }

            // Try key=value format (COMMAND=VALUE)
            if (data.Contains("="))
            {
                return ParseKeyValue(data);
            }

            // Fallback: treat as plain command with value
            var parts = data.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1)
            {
                settings["Command"] = parts[0];
                if (parts.Length >= 2 && double.TryParse(parts[1], out var value))
                {
                    settings["Value"] = value;
                }
            }

            return settings.Count > 0 ? settings : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"? Conversion error: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, object>? ParseJson(string data)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(data))
            {
                var root = doc.RootElement;
                var settings = new Dictionary<string, object>();

                // Check if this is an OCP message
                if (root.TryGetProperty("ocp", out var ocpElement))
                {
                    return ParseOCPMessage(ocpElement);
                }

                // Fallback: generic JSON parsing
                foreach (var property in root.EnumerateObject())
                {
                    string key = property.Name;
                    object value = property.Value.ValueKind switch
                    {
                        JsonValueKind.Number => property.Value.GetDouble(),
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => property.Value.GetRawText()
                    };

                    settings[key] = value;
                }

                return settings.Count > 0 ? settings : null;
            }
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"? JSON parse error: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, object>? ParseOCPMessage(JsonElement ocpElement)
    {
        var settings = new Dictionary<string, object>();

        // Parse device info
        if (ocpElement.TryGetProperty("device", out var device))
        {
            if (device.TryGetProperty("id", out var id))
                settings["device_id"] = id.GetString() ?? "";
            if (device.TryGetProperty("version", out var version))
                settings["device_version"] = version.GetString() ?? "";
        }

        // Parse switches
        if (ocpElement.TryGetProperty("switches", out var switches))
        {
            if (switches.TryGetProperty("enabled", out var enabled))
                settings["switch_enabled"] = enabled.GetBoolean();
            if (switches.TryGetProperty("feedhold", out var feedhold))
                settings["switch_feedhold"] = feedhold.GetBoolean();
            if (switches.TryGetProperty("cycleStart", out var cycleStart))
                settings["switch_cycleStart"] = cycleStart.GetBoolean();
            if (switches.TryGetProperty("cycleStop", out var cycleStop))
                settings["switch_cycleStop"] = cycleStop.GetBoolean();
            if (switches.TryGetProperty("toolCheck", out var toolCheck))
                settings["switch_toolCheck"] = toolCheck.GetBoolean();
            if (switches.TryGetProperty("stepIndex", out var stepIndex))
                settings["switch_stepIndex"] = stepIndex.GetInt32();
            if (switches.TryGetProperty("stepSize", out var stepSize))
                settings["switch_stepSize"] = stepSize.GetInt32();
        }

        // Parse feedrate
        if (ocpElement.TryGetProperty("feedrate", out var feedrate))
        {
            if (feedrate.TryGetProperty("value", out var value))
                settings["feedrate_value"] = value.GetDouble();
            if (feedrate.TryGetProperty("minValue", out var minValue))
                settings["feedrate_min"] = minValue.GetDouble();
            if (feedrate.TryGetProperty("maxValue", out var maxValue))
                settings["feedrate_max"] = maxValue.GetDouble();
        }

        // Parse encoders
        if (ocpElement.TryGetProperty("encoders", out var encoders))
        {
            if (encoders.TryGetProperty("deltaX", out var deltaX))
                settings["encoder_deltaX"] = deltaX.GetDouble();
            if (encoders.TryGetProperty("deltaY", out var deltaY))
                settings["encoder_deltaY"] = deltaY.GetDouble();
            if (encoders.TryGetProperty("deltaZ", out var deltaZ))
                settings["encoder_deltaZ"] = deltaZ.GetDouble();
            if (encoders.TryGetProperty("posX", out var posX))
                settings["encoder_posX"] = posX.GetDouble();
            if (encoders.TryGetProperty("posY", out var posY))
                settings["encoder_posY"] = posY.GetDouble();
            if (encoders.TryGetProperty("posZ", out var posZ))
                settings["encoder_posZ"] = posZ.GetDouble();
        }

        return settings.Count > 0 ? settings : null;
    }

    private void ParseOCPCsvData(string[] parts, Dictionary<string, object> settings)
    {
        // Parse device ID and version
        settings["device_id"] = parts[0].Trim();

        if (parts.Length > 1)
            settings["device_version"] = parts[1].Trim();

        // Parse encoder deltas
        if (parts.Length > 2 && double.TryParse(parts[2].Trim(), out var encX))
            settings["encoder_deltaX"] = encX;

        if (parts.Length > 3 && double.TryParse(parts[3].Trim(), out var encY))
            settings["encoder_deltaY"] = encY;

        if (parts.Length > 4 && double.TryParse(parts[4].Trim(), out var encZ))
            settings["encoder_deltaZ"] = encZ;

        // Parse absolute encoder positions (new firmware)
        if (parts.Length > 5 && double.TryParse(parts[5].Trim(), out var absX))
            settings["encoder_posX"] = absX;

        if (parts.Length > 6 && double.TryParse(parts[6].Trim(), out var absY))
            settings["encoder_posY"] = absY;

        if (parts.Length > 7 && double.TryParse(parts[7].Trim(), out var absZ))
            settings["encoder_posZ"] = absZ;

        // Parse switches (now at index 8)
        if (parts.Length > 8 && int.TryParse(parts[8].Trim(), out var switches))
        {
            settings["switches_raw"] = switches;
            settings["switch_enabled"] = (switches & 0x01) != 0;
            settings["switch_feedhold"] = (switches & 0x02) != 0;
            settings["switch_cycleStart"] = (switches & 0x04) != 0;
            settings["switch_cycleStop"] = (switches & 0x08) != 0;
            settings["switch_toolCheck"] = (switches & 0x10) != 0;
        }

        // Parse step index and size (now at index 9)
        if (parts.Length > 9 && int.TryParse(parts[9].Trim(), out var stepIndex))
        {
            settings["step_index"] = stepIndex;
            settings["step_size"] = ParserConfig.StepSizeMap.TryGetValue(stepIndex, out var size)
                ? size
                : ParserConfig.DefaultStepSize;
        }

        // Parse feedrate (now at index 10)
        if (parts.Length > 10 && int.TryParse(parts[10].Trim(), out var feedrate))
        {
            settings["feedrate_value"] = feedrate;
            settings["feedrate_percent"] = (feedrate / ParserConfig.MaxFeedrateValue) * 100.0;
        }
    }

    private Dictionary<string, object>? ParseCsv(string data)
    {
        var settings = new Dictionary<string, object>();
        var parts = data.Split(',');

        // Check if this is the Teensy OCP format (id,version,encX,encY,encZ,switches,stepIndex,feedrate)
        if (parts.Length >= 2 && parts[0].Trim().StartsWith("TEENSY_OCP"))
        {
            ParseOCPCsvData(parts, settings);
        }
        else if (parts.Length >= 8)
        {
            // Generic CSV format - try to map to Teensy OCP format structure
            // Supports both old (8 fields) and new (11 fields) formats:
            // Old: deviceId,version,encX,encY,encZ,switches,stepIndex,feedrate
            // New: deviceId,version,encX,encY,encZ,absX,absY,absZ,switches,stepIndex,feedrate
            ParseOCPCsvData(parts, settings);
        }

        return settings.Count > 0 ? settings : null;
    }

    private Dictionary<string, object>? ParseKeyValue(string data)
    {
        var settings = new Dictionary<string, object>();
        var pairs = data.Split(new[] { ';', '&' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string valueStr = parts[1].Trim();

                if (double.TryParse(valueStr, out var doubleValue))
                {
                    settings[key] = doubleValue;
                }
                else if (bool.TryParse(valueStr, out var boolValue))
                {
                    settings[key] = boolValue;
                }
                else
                {
                    settings[key] = valueStr;
                }
            }
        }

        return settings.Count > 0 ? settings : null;
    }
}
