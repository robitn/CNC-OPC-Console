/*
 * CNC-OCP-Console - ParserConfig.cs
 * Defines configuration and mappings for serial data parser.
 * Contains constants and lookup tables for parsing logic.
 *
 * Copyright (c) 2026 Timothy Robinson / Github: robitn. Licensed under the MIT License.
 *
 * This file is part of the CNC-OCP-Console project.
 */
using System.Collections.Generic;

namespace CNC_OCP_Console.Configuration
{
    /// <summary>
    /// Configuration for serial data parser mappings and constants
    /// </summary>
    public static class ParserConfig
    {
        /// <summary>
        /// Maps step index values to their human-readable size strings
        /// </summary>
        public static readonly Dictionary<int, double> StepSizeMap = new()
        {
            { 0, 0.01 },
            { 1, 0.1 },
            { 2, 1.0 }
        };

        /// <summary>
        /// Default step size for unknown indices
        /// </summary>
        public const string DefaultStepSize = "unknown";

        /// <summary>
        /// Maximum feedrate value (8-bit)
        /// </summary>
        public const double MaxFeedrateValue = 255.0;
    }
}
