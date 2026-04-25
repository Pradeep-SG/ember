using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class BattleStateMachineTests
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

        static BattleStateMachine MakeMachine() => new BattleStateMachine(new NoOpBattleEventEmitter());

        [Test]
        public void StartBattle_FromIdle_TransitionsToPhase1()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            Assert.AreEqual(BattlePhase.Phase1, sm.Current);
            Assert.IsNotNull(sm.Context);
            Assert.AreEqual(ArchetypeId.PermissionGiver, sm.Context.Archetype);
        }

        [Test]
        public void StartBattle_WhenAlreadyInBattle_Throws()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            Assert.Throws<InvalidOperationException>(() => sm.StartBattle(ArchetypeId.TenderExcuse));
        }

        [Test]
        public void Advance_FromIdle_Throws()
        {
            var sm = MakeMachine();
            Assert.Throws<InvalidOperationException>(() => sm.Advance());
        }

        [Test]
        public void Abandon_FromIdle_Throws()
        {
            var sm = MakeMachine();
            Assert.Throws<InvalidOperationException>(() => sm.Abandon("test"));
        }

        [Test]
        public void FullCycle_TransitionsThroughEveryPhase()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            Assert.AreEqual(BattlePhase.Phase1, sm.Current);

            sm.Advance(); Assert.AreEqual(BattlePhase.Phase2, sm.Current);
            sm.Advance(); Assert.AreEqual(BattlePhase.Phase3, sm.Current);
            sm.Advance(); Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            sm.Advance(); Assert.AreEqual(BattlePhase.Idle, sm.Current);
            Assert.IsNull(sm.Context, "Context should clear when returning to Idle");
        }

        [Test]
        public void Abandon_FromPhase1_RoutesToOutcomeWithAbandonOutcome()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Abandon("user_quit");

            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(BattleOutcome.Abandon, sm.Context.Outcome);
        }

        [Test]
        public void Abandon_FromPhase3_RoutesToOutcomeWithAbandonOutcome()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Advance(); sm.Advance(); // Phase1 → Phase2 → Phase3
            sm.Abandon("user_quit");

            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(BattleOutcome.Abandon, sm.Context.Outcome);
        }

        [Test]
        public void Transitioned_FiresOnEveryTransition()
        {
            var sm = MakeMachine();
            var transitions = new List<(BattlePhase from, BattlePhase to)>();
            sm.Transitioned += (from, to) => transitions.Add((from, to));

            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Advance();
            sm.Advance();

            Assert.AreEqual(3, transitions.Count);
            Assert.AreEqual((BattlePhase.Idle, BattlePhase.Phase1), transitions[0]);
            Assert.AreEqual((BattlePhase.Phase1, BattlePhase.Phase2), transitions[1]);
            Assert.AreEqual((BattlePhase.Phase2, BattlePhase.Phase3), transitions[2]);
        }

        [Test]
        public void Phase2Entry_EmitsBeadsRemainingInEntryContext()
        {
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Advance(); // Phase1 → Phase2

            var phase2Entry = emitter.Events.Last(e =>
                e.name == "shadow_battle_phase_entered" &&
                e.parameters.TryGetValue("phase", out var p) && p.ToString() == "phase2");

            Assert.IsTrue(phase2Entry.parameters["entry_context"] is IDictionary<string, object>);
            var entryCtx = (IDictionary<string, object>)phase2Entry.parameters["entry_context"];
            Assert.AreEqual(5, entryCtx["beads_remaining"]);
        }

        [Test]
        public void Phase1Entry_EntryContextIsEmpty()
        {
            var emitter = new RecordingEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var phase1Entry = emitter.Events.Last(e =>
                e.name == "shadow_battle_phase_entered" &&
                e.parameters.TryGetValue("phase", out var p) && p.ToString() == "phase1");

            var entryCtx = (IDictionary<string, object>)phase1Entry.parameters["entry_context"];
            Assert.IsFalse(entryCtx.ContainsKey("beads_remaining"));
        }

        [Test]
        public void HandleBackgroundResume_AboveThreshold_Abandons()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.HandleBackgroundResume(60_000);

            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(BattleOutcome.Abandon, sm.Context.Outcome);
        }

        [Test]
        public void HandleBackgroundResume_BelowThreshold_NoTransition()
        {
            var sm = MakeMachine();
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.HandleBackgroundResume(59_999);

            Assert.AreEqual(BattlePhase.Phase1, sm.Current);
            Assert.AreEqual(BattleOutcome.None, sm.Context.Outcome);
        }

        [Test]
        public void HandleBackgroundResume_FromIdle_NoOp()
        {
            var sm = MakeMachine();
            sm.HandleBackgroundResume(120_000);
            Assert.AreEqual(BattlePhase.Idle, sm.Current);
        }
    }
}
