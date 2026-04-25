using System;

namespace Kindrith.Core
{
    public interface IDayClock
    {
        DateTime LocalNow { get; }
        DateTime LocalMidnightToday { get; }
        int DaysBetween(DateTime a, DateTime b);
    }
}
