using System;

namespace AndrewDemo.NetConf2023.Core.Time
{
    public static class TimeProviderDateTimeExtensions
    {
        public static DateTime GetUtcDateTime(this TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            return timeProvider.GetUtcNow().UtcDateTime;
        }

        public static DateTime GetLocalDateTime(this TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            return timeProvider.GetLocalNow().DateTime;
        }
    }
}
