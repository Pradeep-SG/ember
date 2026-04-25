using System.Collections.Generic;
using NUnit.Framework;
using Kindrith.Core;
using Kindrith.Breathing;

namespace Kindrith.Tests.EditMode
{
    public class BreathingClockTests
    {
        [Test]
        public void Start_FiresCycleStartedForCycleZero()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            var fired = new List<int>();
            clock.CycleStarted += fired.Add;

            clock.Start();

            Assert.AreEqual(new[] { 0 }, fired.ToArray());
            Assert.AreEqual(0, clock.CycleIndex);
        }

        [Test]
        public void After19000ms_CycleIndexIsOne()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            clock.Start();

            fake.SetNowMs(19_000);
            clock.Tick();

            Assert.AreEqual(1, clock.CycleIndex);
            Assert.AreEqual(0, clock.CycleElapsedMs);
        }

        [Test]
        public void At11000ms_CurrentBeatIsExhaleAndEventFiredOnce()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            int exhaleFiredCount = 0;
            int? lastFiredCycle = null;
            clock.ExhaleStarted += i =>
            {
                exhaleFiredCount++;
                lastFiredCycle = i;
            };
            clock.Start();

            fake.SetNowMs(11_000);
            clock.Tick();

            Assert.AreEqual(BreathBeat.Exhale, clock.CurrentBeat);
            Assert.AreEqual(1, exhaleFiredCount);
            Assert.AreEqual(0, lastFiredCycle);
        }

        [Test]
        public void ExhaleStarted_DoesNotRefireSameCycle()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            int exhaleFiredCount = 0;
            clock.ExhaleStarted += _ => exhaleFiredCount++;
            clock.Start();

            fake.SetNowMs(11_000); clock.Tick();
            fake.SetNowMs(11_500); clock.Tick();
            fake.SetNowMs(18_999); clock.Tick();

            Assert.AreEqual(1, exhaleFiredCount);
        }

        [Test]
        public void Pause_PreservesElapsed()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            clock.Start();

            fake.SetNowMs(5_000);
            clock.Tick();
            Assert.AreEqual(5_000, clock.CycleElapsedMs);

            clock.Pause();
            fake.Advance(10_000);
            clock.Resume();
            clock.Tick();

            Assert.AreEqual(5_000, clock.CycleElapsedMs);
        }

        [Test]
        public void CurrentBeat_TransitionsAtBoundaries()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            clock.Start();

            fake.SetNowMs(0);      clock.Tick(); Assert.AreEqual(BreathBeat.Inhale,  clock.CurrentBeat);
            fake.SetNowMs(3_999);  clock.Tick(); Assert.AreEqual(BreathBeat.Inhale,  clock.CurrentBeat);
            fake.SetNowMs(4_000);  clock.Tick(); Assert.AreEqual(BreathBeat.HoldTop, clock.CurrentBeat);
            fake.SetNowMs(10_999); clock.Tick(); Assert.AreEqual(BreathBeat.HoldTop, clock.CurrentBeat);
            fake.SetNowMs(11_000); clock.Tick(); Assert.AreEqual(BreathBeat.Exhale,  clock.CurrentBeat);
            fake.SetNowMs(18_999); clock.Tick(); Assert.AreEqual(BreathBeat.Exhale,  clock.CurrentBeat);
        }

        [Test]
        public void CycleProgress01_RampsLinearlyAcrossCycle()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            clock.Start();

            fake.SetNowMs(0); clock.Tick();
            Assert.That(clock.CycleProgress01, Is.EqualTo(0f).Within(1e-4f));

            fake.SetNowMs(BreathingCadence.CycleMs / 2); clock.Tick();
            Assert.That(clock.CycleProgress01, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void SkippedTick_FiresMissedCycleAndExhaleEvents()
        {
            var fake = new FakeClock();
            var clock = new BreathingClock(fake);
            var cycles = new List<int>();
            var exhales = new List<int>();
            clock.CycleStarted += cycles.Add;
            clock.ExhaleStarted += exhales.Add;
            clock.Start();

            // Jump straight from 0 to mid-cycle 2 (2 * 19000 + 11000 = 49000).
            fake.SetNowMs(49_000);
            clock.Tick();

            Assert.AreEqual(new[] { 0, 1, 2 }, cycles.ToArray());
            Assert.AreEqual(new[] { 0, 1, 2 }, exhales.ToArray());
        }
    }
}
