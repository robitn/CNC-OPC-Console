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
        /// Maps step index values to their actual step sizes
        /// Matches the step sizes defined in Program.cs GenerateG1Move
        /// </summary>
        public static readonly Dictionary<int, double> StepSizeMap = new()
        {
            { 0, 0.001 },
            { 1, 0.01 },
            { 2, 0.1 },
            { 3, 1.0 },
            { 4, 10.0 }
        };

        /// <summary>
        /// Default step size for unknown indices
        /// </summary>
        public const double DefaultStepSize = 0.01;

        /// <summary>
        /// Maximum feedrate value (8-bit)
        /// </summary>
        public const double MaxFeedrateValue = 255.0;
    }
}
