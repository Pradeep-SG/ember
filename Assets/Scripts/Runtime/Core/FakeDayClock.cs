using System;

namespace Kindrith.Core
{
    public sealed class FakeDayClock : IDayClock
    {
        public DateTime LocalNow { get; set; } = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Local);

        public DateTime LocalMidnightToday => LocalNow.Date;

        public int DaysBetween(DateTime a, DateTime b) => (b.Date - a.Date).Days;

        public void Advance(TimeSpan delta) { LocalNow += delta; }
        public void AdvanceDays(int days) { LocalNow = LocalNow.AddDays(days); }
        public void SetLocalNow(DateTime t) { LocalNow = t; }
    }
}
