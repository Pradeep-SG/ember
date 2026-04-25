using System;
using NUnit.Framework;
using Kindrith.Core;

namespace Kindrith.Tests.EditMode
{
    public class DayClockTests
    {
        [Test]
        public void DaysBetween_SameDay_IsZero()
        {
            var c = new FakeDayClock { LocalNow = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Local) };
            Assert.AreEqual(0, c.DaysBetween(c.LocalNow, c.LocalNow));
        }

        [Test]
        public void DaysBetween_NextDay_IsOne()
        {
            var c = new FakeDayClock();
            var today = new DateTime(2026, 4, 26, 23, 59, 0, DateTimeKind.Local);
            var tomorrow = new DateTime(2026, 4, 27, 0, 1, 0, DateTimeKind.Local);
            Assert.AreEqual(1, c.DaysBetween(today, tomorrow));
        }

        [Test]
        public void DaysBetween_DST_SpringForward_StillCountsAsOneDay()
        {
            // US/Pacific spring-forward: 2026-03-08 02:00 -> 03:00. Real elapsed is 23 hours,
            // but local-day counting must say 1 day between Mar 8 and Mar 9.
            var c = new FakeDayClock();
            var dayBefore = new DateTime(2026, 3, 8, 12, 0, 0, DateTimeKind.Local);
            var dayAfter = new DateTime(2026, 3, 9, 12, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(1, c.DaysBetween(dayBefore, dayAfter));
        }

        [Test]
        public void DaysBetween_DST_FallBack_StillCountsAsOneDay()
        {
            // US/Pacific fall-back: 2026-11-01 02:00 -> 01:00. Real elapsed is 25 hours.
            var c = new FakeDayClock();
            var dayBefore = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Local);
            var dayAfter = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(1, c.DaysBetween(dayBefore, dayAfter));
        }

        [Test]
        public void LocalMidnightToday_StripsTimeOfDay()
        {
            var c = new FakeDayClock { LocalNow = new DateTime(2026, 4, 26, 13, 45, 12, DateTimeKind.Local) };
            var midnight = c.LocalMidnightToday;
            Assert.AreEqual(0, midnight.Hour);
            Assert.AreEqual(0, midnight.Minute);
            Assert.AreEqual(0, midnight.Second);
            Assert.AreEqual(c.LocalNow.Date, midnight);
        }

        [Test]
        public void Advance_AdvancesLocalNow()
        {
            var c = new FakeDayClock { LocalNow = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Local) };
            c.Advance(TimeSpan.FromHours(3));
            Assert.AreEqual(15, c.LocalNow.Hour);
        }

        [Test]
        public void AdvanceDays_RollsOverDateBoundary()
        {
            var c = new FakeDayClock { LocalNow = new DateTime(2026, 4, 26, 23, 30, 0, DateTimeKind.Local) };
            c.AdvanceDays(1);
            Assert.AreEqual(27, c.LocalNow.Day);
            Assert.AreEqual(23, c.LocalNow.Hour);
        }
    }
}
