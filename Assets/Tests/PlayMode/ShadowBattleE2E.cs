using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using Kindrith.Breathing;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.PlayMode
{
    // Walks the state machine through full Phase 1 → Phase 2 → Phase 3 → Outcome flows
    // with scripted FakeClock time and verifies the emitted events + final BattleOutcome.
    // Expanded in WP-07 to cover OutcomeRouter wiring.
    public class ShadowBattleE2E
    {
        sealed class RecordingEmitter : IBattleEventEmitter
        {
            public readonly List<(string name, IDictionary<string, object> parameters)> Events =
                new List<(string, IDictionary<string, object>)>();

            public void Emit(string name, IDictionary<string, object> parameters)
            {
                Events.Add((name, parameters));
            }
        }

        [UnityTest]
        public IEnumerator Phase1_TimesOutAt90s_AdvancesToPhase2_WithBeadsRemaining()
        {
            var fakeClock = new FakeClock();
            var breathingClock = new BreathingClock(fakeClock);
            breathingClock.Start();

            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var beads = sm.Context.DemonBeads;
            var phase1 = new Phase1Controller(
                fakeClock, breathingClock, new TapEvaluator(), beads, sm.Context, emitter, sm.Advance);
            phase1.Start();

            // Advance scripted clock through 90_000 ms in 1s chunks.
            while (fakeClock.NowMs < 90_000)
            {
                fakeClock.Advance(1_000);
                breathingClock.Tick();
                phase1.Tick();
            }

            Assert.AreEqual(BattlePhase.Phase2, sm.Current,
                "Phase1 should auto-advance to Phase2 after 90_000 ms");
            Assert.AreEqual(90_000, sm.Context.Phase1ElapsedMs);
            Assert.AreEqual(5, beads.Remaining,
                "No taps registered, so all five beads should remain");

            var phase2Entry = emitter.Events.Last(e =>
                e.name == "shadow_battle_phase_entered" &&
                e.parameters.TryGetValue("phase", out var p) && p.ToString() == "Phase2");
            var entryCtx = (IDictionary<string, object>)phase2Entry.parameters["entry_context"];
            Assert.AreEqual(5, entryCtx["beads_remaining"]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase3_TwoBeatsLanded_OutcomeResolvesToWin()
        {
            var fakeClock = new FakeClock();
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            // Skip Phase 1 / Phase 2 — the state machine doesn't care how the controllers ran;
            // OutcomeRouter only consumes BeatsLandedInPhase3 + DemonBeads.Remaining.
            sm.Advance(); // Phase1 → Phase2
            sm.Advance(); // Phase2 → Phase3
            Assert.AreEqual(BattlePhase.Phase3, sm.Current, "expected Phase3 after two Advances");

            var sequencer = new FinisherSequencer(fakeClock);
            var phase3 = new Phase3Controller(fakeClock, sequencer, sm.Context, emitter, sm.Advance);
            phase3.Start();

            // Land beats 1 and 2; miss the hold (1500 ms is well outside 800 ± 120).
            fakeClock.SetNowMs(1_000); sequencer.RegisterTap(1_000);
            fakeClock.SetNowMs(2_500); sequencer.RegisterTap(2_500);
            fakeClock.SetNowMs(4_000); sequencer.RegisterHoldStart(4_000);
            fakeClock.SetNowMs(5_500); sequencer.RegisterHoldRelease(5_500);

            phase3.Tick();

            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(2, sm.Context.BeatsLandedInPhase3);
            Assert.AreEqual(BattleOutcome.Win, sm.Context.Outcome);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase3_AllThreeBeats_OutcomeResolvesToCriticalWin()
        {
            var fakeClock = new FakeClock();
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Advance(); sm.Advance(); // Phase1 → Phase2 → Phase3

            var sequencer = new FinisherSequencer(fakeClock);
            var phase3 = new Phase3Controller(fakeClock, sequencer, sm.Context, emitter, sm.Advance);
            phase3.Start();

            fakeClock.SetNowMs(1_000); sequencer.RegisterTap(1_000);
            fakeClock.SetNowMs(2_500); sequencer.RegisterTap(2_500);
            fakeClock.SetNowMs(4_000); sequencer.RegisterHoldStart(4_000);
            fakeClock.SetNowMs(4_800); sequencer.RegisterHoldRelease(4_800);

            phase3.Tick();

            Assert.AreEqual(3, sm.Context.BeatsLandedInPhase3);
            Assert.AreEqual(BattleOutcome.CriticalWin, sm.Context.Outcome);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase3_MissAllWithBeadsRemaining_OutcomeResolvesToLoss()
        {
            var fakeClock = new FakeClock();
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Advance(); sm.Advance();

            var sequencer = new FinisherSequencer(fakeClock);
            var phase3 = new Phase3Controller(fakeClock, sequencer, sm.Context, emitter, sm.Advance);
            phase3.Start();

            // Tap 300 ms early on each tap beat (outside ±180 ms), hold 1500 ms.
            fakeClock.SetNowMs(700);   sequencer.RegisterTap(700);
            fakeClock.SetNowMs(2_200); sequencer.RegisterTap(2_200);
            fakeClock.SetNowMs(4_000); sequencer.RegisterHoldStart(4_000);
            fakeClock.SetNowMs(5_500); sequencer.RegisterHoldRelease(5_500);

            phase3.Tick();

            Assert.AreEqual(0, sm.Context.BeatsLandedInPhase3);
            Assert.AreEqual(5, sm.Context.DemonBeads.Remaining);
            Assert.AreEqual(BattleOutcome.Loss, sm.Context.Outcome);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AbandonDuringPhase1_OutcomePreservedAsAbandon()
        {
            var fakeClock = new FakeClock();
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            sm.Abandon("user_quit");

            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(BattleOutcome.Abandon, sm.Context.Outcome);
            yield return null;
        }
    }
}
