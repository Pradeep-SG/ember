using System;

namespace Kindrith.Core
{
    public sealed class SystemDayClock : IDayClock
    {
        public DateTime LocalNow => DateTime.Now;
        public DateTime LocalMidnightToday => DateTime.Today;

        public int DaysBetween(DateTime a, DateTime b) => (b.Date - a.Date).Days;
    }
}
