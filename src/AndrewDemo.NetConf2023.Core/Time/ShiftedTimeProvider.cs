using System;

namespace AndrewDemo.NetConf2023.Core.Time
{
    public sealed class ShiftedTimeProvider : TimeProvider
    {
        private readonly TimeProvider _inner;
        private readonly TimeSpan _offset;
        private readonly TimeZoneInfo _localTimeZone;

        public ShiftedTimeProvider(TimeProvider inner, DateTime expectedStartupLocal, TimeZoneInfo localTimeZone)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _localTimeZone = localTimeZone ?? throw new ArgumentNullException(nameof(localTimeZone));

            var normalizedExpectedStartupLocal = DateTime.SpecifyKind(expectedStartupLocal, DateTimeKind.Unspecified);
            var expectedStartupUtc = TimeZoneInfo.ConvertTimeToUtc(normalizedExpectedStartupLocal, _localTimeZone);
            var actualStartupUtc = _inner.GetUtcNow().UtcDateTime;

            _offset = expectedStartupUtc - actualStartupUtc;
        }

        public override TimeZoneInfo LocalTimeZone => _localTimeZone;

        public override DateTimeOffset GetUtcNow()
        {
            return _inner.GetUtcNow().Add(_offset);
        }
    }
}
