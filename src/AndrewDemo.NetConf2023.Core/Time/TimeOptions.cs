using System;

namespace AndrewDemo.NetConf2023.Core.Time
{
    public sealed class TimeOptions
    {
        public const string SectionName = "Time";

        public string Mode { get; set; } = "System";

        public string? ExpectedStartupLocal { get; set; }

        public string TimeZoneId { get; set; } = "Asia/Taipei";

        public bool IsShiftedMode()
        {
            return string.Equals(Mode, "Shifted", StringComparison.OrdinalIgnoreCase);
        }
    }
}
