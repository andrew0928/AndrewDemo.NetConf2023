using System;
using System.Globalization;

namespace AndrewDemo.NetConf2023.Core.Time
{
    public static class TimeProviderFactory
    {
        public static TimeProvider Create(TimeOptions? options, TimeProvider? inner = null)
        {
            var resolvedInner = inner ?? TimeProvider.System;
            if (options == null || !options.IsShiftedMode())
            {
                return resolvedInner;
            }

            if (string.IsNullOrWhiteSpace(options.ExpectedStartupLocal))
            {
                throw new InvalidOperationException("Time:ExpectedStartupLocal is required when Time:Mode is Shifted.");
            }

            if (string.IsNullOrWhiteSpace(options.TimeZoneId))
            {
                throw new InvalidOperationException("Time:TimeZoneId is required when Time:Mode is Shifted.");
            }

            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
            var expectedStartupLocal = DateTime.Parse(
                options.ExpectedStartupLocal,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces);

            return new ShiftedTimeProvider(resolvedInner, expectedStartupLocal, localTimeZone);
        }
    }
}
