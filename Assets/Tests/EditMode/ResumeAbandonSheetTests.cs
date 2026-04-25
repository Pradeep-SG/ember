using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;
using Kindrith.UI;

namespace Kindrith.Tests.EditMode
{
    // Routing + clock re-arm behavior for the Resume/Abandon paths.
    // The MonoBehaviour sheet itself is exercised by the BattleRunner integration; here we
    // pin the state-machine surface and the controller Resume hooks.
    public class ResumeAbandonSheetTests
    {
        sealed class Recording : IBattleEventEmitter
        {
            public readonly List<(string name, IDictionary<string, object> p)> Events = new List<(string, IDictionary<string, object>)>();
            public void Emit(string name, IDictionary<string, object> parameters)
                => Events.Add((name, parameters));
        }

        [Test]
        public void Resume_FromIdle_NoOp()
        {
            var sm = new BattleStateMachine(new Recording());
            // Idle on construction. Resume should not throw.
            sm.Resume(5_000);
            Assert.AreEqual(BattlePhase.Idle, sm.Current);
        }

        [Test]
        public void Resume_FiresResumedEventWithDuration()
        {
            var emitter = new Recording();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            int captured = -1;
            sm.Resumed += dur => captured = dur;
            sm.Resume(12_345);

            Assert.AreEqual(12_345, captured);
        }

        [Test]
        public void Phase1_Resume_BumpsStartTimeForward()
        {
            var clock = new FakeClock();
            var bclock = new Kindrith.Breathing.BreathingClock(clock);
            bclock.Start();
            var sm = new BattleStateMachine(new Recording());
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var phase1 = new Phase1Controller(clock, bclock, new TapEvaluator(),
                sm.Context.DemonBeads, sm.Context, new Recording(), () => { });
            phase1.Start();

            // 30s of "real" elapsed before background.
            clock.SetNowMs(30_000);
            phase1.Tick();
            Assert.AreEqual(30_000, sm.Context.Phase1ElapsedMs);

            // Simulate 20s background — clock advances but Resume bumps the anchor.
            clock.SetNowMs(50_000);
            phase1.Resume(20_000);
            phase1.Tick();
            Assert.AreEqual(30_000, sm.Context.Phase1ElapsedMs,
                "Phase 1 elapsed should exclude the 20s background gap");
        }

        [Test]
        public void Abandon_FromSubThresholdPath_RoutesUserChoseAbandon()
        {
            var emitter = new Recording();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            sm.Abandon("user_chose_abandon");
            var completed = emitter.Events.Single(e => e.name == "shadow_battle_completed");
            Assert.AreEqual("user_chose_abandon", completed.p["abandon_reason"]);
        }

        [Test]
        public void HandleBackgroundResume_AtThreshold_Abandons()
        {
            var emitter = new Recording();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            sm.HandleBackgroundResume(60_000);
            Assert.AreEqual(BattleOutcome.Abandon, sm.Context.Outcome);
            var completed = emitter.Events.Single(e => e.name == "shadow_battle_completed");
            Assert.AreEqual("background_timeout", completed.p["abandon_reason"]);
        }

        [Test]
        public void HandleBackgroundResume_BelowThreshold_DoesNotAbandon()
        {
            var sm = new BattleStateMachine(new Recording());
            sm.StartBattle(ArchetypeId.PermissionGiver);

            sm.HandleBackgroundResume(59_999);
            Assert.AreEqual(BattlePhase.Phase1, sm.Current,
                "Below threshold leaves the battle in Phase1 — UI shows ResumeOrAbandonSheet.");
        }

        [Test]
        public void ComputeForegroundMs_FromAppOpenedTimestamp_TenSecondsApart()
        {
            var opened = new DateTime(2026, 4, 26, 14, 0, 0, DateTimeKind.Utc);
            var backgrounded = opened.AddSeconds(10);
            Assert.AreEqual(10_000, Kindrith.UI.Boot.ComputeForegroundMs(opened, backgrounded));
        }

        [Test]
        public void ComputeForegroundMs_NegativeDelta_ClampsToZero()
        {
            var opened = new DateTime(2026, 4, 26, 14, 0, 0, DateTimeKind.Utc);
            var backgrounded = opened.AddSeconds(-5);
            Assert.AreEqual(0, Kindrith.UI.Boot.ComputeForegroundMs(opened, backgrounded));
        }
    }
}
