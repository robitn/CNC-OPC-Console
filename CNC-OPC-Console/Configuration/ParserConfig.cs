using System.Collections.Generic;

namespace CNC_OPC_Console.Configuration
{
    /// <summary>
    /// Configuration for serial data parser mappings and constants
    /// </summary>
    public static class ParserConfig
    {
        /// <summary>
        /// Maps step index values to their human-readable size strings
        /// </summary>
        public static readonly Dictionary<int, string> StepSizeMap = new()
        {
            { 0, "0.01x" },
            { 1, "0.1x" },
            { 2, "1x" }
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
