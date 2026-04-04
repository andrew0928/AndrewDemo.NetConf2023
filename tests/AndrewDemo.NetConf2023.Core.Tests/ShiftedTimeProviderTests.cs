using System;
using AndrewDemo.NetConf2023.Core.Time;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public sealed class ShiftedTimeProviderTests
    {
        [Fact]
        public void GetUtcNow_WhenShiftedModeIsConfigured_ReturnsShiftedUtcTime()
        {
            var inner = new FixedTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
            var provider = new ShiftedTimeProvider(
                inner,
                new DateTime(2026, 4, 4, 9, 0, 0, DateTimeKind.Unspecified),
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"));

            Assert.Equal(
                new DateTimeOffset(2026, 4, 4, 1, 0, 0, TimeSpan.Zero),
                provider.GetUtcNow());
        }

        [Fact]
        public void TimeProviderFactory_WhenSystemMode_ReturnsInnerProvider()
        {
            var inner = new FixedTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));

            var provider = TimeProviderFactory.Create(new TimeOptions
            {
                Mode = "System",
                TimeZoneId = "Asia/Taipei"
            }, inner);

            Assert.Same(inner, provider);
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
