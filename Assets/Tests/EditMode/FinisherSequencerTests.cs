using System.Collections.Generic;
using NUnit.Framework;
using Kindrith.Core;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class FinisherSequencerTests
    {
        // Default targets (from FinisherSequencer constants):
        //   Expand   = 1000 ms
        //   Contract = 2500 ms
        //   Hold start = 4000 ms (release expected at 4800 ms)
        //   Tap window: ±180 ms; Hold tolerance: ±120 ms.

        [TestCase(1_000, true)]   // exact
        [TestCase(1_180, true)]   // upper boundary
        [TestCase(1_181, false)]  // just outside (late)
        [TestCase(820,   true)]   // lower boundary (early)
        [TestCase(819,   false)]  // just outside (early)
        public void ExpandTap_LandsWithin180msOfTarget(int tapAtMs, bool expected)
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            bool? landed = null;
            seq.BeatResolved += (beat, l, _) => { if (beat == FinisherBeat.ExpandTap) landed = l; };
            seq.Start();

            clock.SetNowMs(tapAtMs);
            seq.RegisterTap(tapAtMs);

            Assert.AreEqual(expected, landed);
        }

        [TestCase(2_500, true)]
        [TestCase(2_680, true)]
        [TestCase(2_681, false)]
        [TestCase(2_320, true)]
        [TestCase(2_319, false)]
        public void ContractTap_LandsWithin180msOfTarget(int tapAtMs, bool expected)
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            bool? landed = null;
            seq.BeatResolved += (beat, l, _) => { if (beat == FinisherBeat.ContractTap) landed = l; };
            seq.Start();

            // Resolve the ExpandTap first so the sequencer advances to ContractTap.
            clock.SetNowMs(1_000);
            seq.RegisterTap(1_000);

            clock.SetNowMs(tapAtMs);
            seq.RegisterTap(tapAtMs);

            Assert.AreEqual(expected, landed);
        }

        [TestCase(800, true,  TestName = "Exact 800 ms hold lands")]
        [TestCase(680, true,  TestName = "800-120 boundary lands")]
        [TestCase(920, true,  TestName = "800+120 boundary lands")]
        [TestCase(679, false, TestName = "Below tolerance fails")]
        [TestCase(921, false, TestName = "Above tolerance fails")]
        public void HoldRelease_LandsWithin120msOfHoldDuration(int holdDurationMs, bool expected)
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            bool? landed = null;
            seq.BeatResolved += (beat, l, _) => { if (beat == FinisherBeat.HoldRelease) landed = l; };
            seq.Start();

            // Walk through the first two beats so the sequencer reaches HoldRelease.
            clock.SetNowMs(1_000); seq.RegisterTap(1_000);
            clock.SetNowMs(2_500); seq.RegisterTap(2_500);

            int holdStartAtMs = 4_000;
            clock.SetNowMs(holdStartAtMs); seq.RegisterHoldStart(holdStartAtMs);
            int releaseAtMs = holdStartAtMs + holdDurationMs;
            clock.SetNowMs(releaseAtMs); seq.RegisterHoldRelease(releaseAtMs);

            Assert.AreEqual(expected, landed);
        }

        [Test]
        public void AllThreeLanded_BeatsLandedIs3_AndComplete()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            seq.Start();

            clock.SetNowMs(1_000); seq.RegisterTap(1_000);
            clock.SetNowMs(2_500); seq.RegisterTap(2_500);
            clock.SetNowMs(4_000); seq.RegisterHoldStart(4_000);
            clock.SetNowMs(4_800); seq.RegisterHoldRelease(4_800);

            Assert.AreEqual(3, seq.BeatsLanded);
            Assert.IsTrue(seq.IsComplete);
        }

        [Test]
        public void AllThreeMissed_BeatsLandedIsZero()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            seq.Start();

            // 200ms early (outside ±180), 200ms late (outside ±180), hold 1500ms (outside ±120)
            clock.SetNowMs(800);  seq.RegisterTap(800);
            clock.SetNowMs(2_700); seq.RegisterTap(2_700);
            clock.SetNowMs(4_000); seq.RegisterHoldStart(4_000);
            clock.SetNowMs(5_500); seq.RegisterHoldRelease(5_500);

            Assert.AreEqual(0, seq.BeatsLanded);
            Assert.IsTrue(seq.IsComplete);
        }

        [Test]
        public void BeatResolved_FiresWithCorrectOffsets()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            var resolved = new List<(FinisherBeat beat, bool landed, int offset)>();
            seq.BeatResolved += (beat, landed, off) => resolved.Add((beat, landed, off));
            seq.Start();

            clock.SetNowMs(1_100); seq.RegisterTap(1_100);    // +100 from 1000 → landed
            clock.SetNowMs(2_320); seq.RegisterTap(2_320);    // -180 from 2500 → landed
            clock.SetNowMs(4_000); seq.RegisterHoldStart(4_000);
            clock.SetNowMs(4_700); seq.RegisterHoldRelease(4_700);  // 700ms hold → -100 from 800 → landed

            Assert.AreEqual(3, resolved.Count);
            Assert.AreEqual((FinisherBeat.ExpandTap,   true, 100), resolved[0]);
            Assert.AreEqual((FinisherBeat.ContractTap, true, -180), resolved[1]);
            Assert.AreEqual((FinisherBeat.HoldRelease, true, -100), resolved[2]);
        }

        [Test]
        public void HardCap_AfterPhase3MaxDuration_AutoMissesUnresolvedBeats()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            var resolved = new List<FinisherBeat>();
            seq.BeatResolved += (beat, _, _) => resolved.Add(beat);
            seq.Start();

            // Resolve only the first beat, then advance past the 30s cap.
            clock.SetNowMs(1_000); seq.RegisterTap(1_000);

            clock.SetNowMs(FinisherSequencer.Phase3MaxDurationMs);
            seq.Tick();

            Assert.IsTrue(seq.IsComplete);
            Assert.AreEqual(new[] { FinisherBeat.ExpandTap, FinisherBeat.ContractTap, FinisherBeat.HoldRelease }, resolved.ToArray());
            Assert.AreEqual(1, seq.BeatsLanded);
        }

        [Test]
        public void RegisterHoldRelease_BeforeHoldStart_IsNoOp()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            seq.Start();

            clock.SetNowMs(1_000); seq.RegisterTap(1_000);
            clock.SetNowMs(2_500); seq.RegisterTap(2_500);

            // Skip RegisterHoldStart, jump straight to RegisterHoldRelease.
            clock.SetNowMs(4_800);
            seq.RegisterHoldRelease(4_800);

            Assert.IsFalse(seq.IsComplete);
            Assert.AreEqual(2, seq.BeatsLanded);
        }

        [Test]
        public void RegisterTap_AfterAllTapBeatsResolved_IsNoOp()
        {
            var clock = new FakeClock();
            var seq = new FinisherSequencer(clock);
            seq.Start();

            clock.SetNowMs(1_000); seq.RegisterTap(1_000);
            clock.SetNowMs(2_500); seq.RegisterTap(2_500);

            // Extra tap should be ignored — HoldRelease uses dedicated RegisterHold methods.
            clock.SetNowMs(3_000);
            seq.RegisterTap(3_000);

            Assert.IsFalse(seq.IsComplete);
            Assert.AreEqual(2, seq.BeatsLanded);
        }
    }
}
