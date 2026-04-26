using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    public class ResonanceBattleHookTests
    {
        sealed class NoopEmitter : IBattleEventEmitter
        {
            public void Emit(string name, IDictionary<string, object> parameters) { }
        }

        static DialogueTree BuildSimpleTree()
        {
            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.Archetype = ArchetypeId.PermissionGiver;
            tree.EntryNodeId = "n0";
            tree.Nodes = new[]
            {
                new DialogueNode
                {
                    Id = "n0",
                    DemonLine = "...",
                    Options = new[]
                    {
                        new DialogueOption { Text = "C", Class = OptionClass.Counter, LeadsToNodeId = null },
                        new DialogueOption { Text = "D", Class = OptionClass.Deflect, LeadsToNodeId = null },
                        new DialogueOption { Text = "A", Class = OptionClass.Agree,   LeadsToNodeId = null },
                    },
                },
            };
            return tree;
        }

        [Test]
        public void Beads_Regen_ClampsToMax()
        {
            var beads = new Beads(max: 5);
            beads.Damage(5); // empty
            Assert.AreEqual(0, beads.Remaining);
            beads.Regen(10);
            Assert.AreEqual(5, beads.Remaining);
        }

        [Test]
        public void Beads_Regen_NoOpOnNonPositive()
        {
            var beads = new Beads(max: 5);
            beads.Damage(2); // 3
            int last = -1;
            beads.Changed += v => last = v;
            beads.Regen(0);
            beads.Regen(-2);
            Assert.AreEqual(3, beads.Remaining);
            Assert.AreEqual(-1, last, "Changed should not fire on no-op Regen");
        }

        [Test]
        public void ClarityHook_Counter_AddsClarity()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var clock = new FakeClock();
            var runner = new DialogueRunner(BuildSimpleTree(), clock);
            using var hook = new BattleClarityHook(sm.Context, runner, sm, () => { });

            runner.Choose(0); // Counter
            Assert.AreEqual(BattleClarityHook.ClarityPerCounter, sm.Context.ClarityPool, 1e-6);
        }

        [Test]
        public void ClarityHook_Counter_ClampsAt1()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var clock = new FakeClock();
            var runner = new DialogueRunner(BuildSimpleTree(), clock);
            using var hook = new BattleClarityHook(sm.Context, runner, sm, () => { });
            // Bypass DialogueRunner advancing — pump the same node 12 times.
            sm.Context.ClarityPool = 0.95f;
            runner.Choose(0); // +0.1 → 1.0 clamp
            Assert.AreEqual(1.0f, sm.Context.ClarityPool);
        }

        [Test]
        public void ClarityHook_Agree_RegensBeads()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Context.DemonBeads.Damage(3); // 2 left

            var clock = new FakeClock();
            var runner = new DialogueRunner(BuildSimpleTree(), clock);
            using var hook = new BattleClarityHook(sm.Context, runner, sm, () => { });

            runner.Choose(2); // Agree
            Assert.AreEqual(3, sm.Context.DemonBeads.Remaining);
        }

        [Test]
        public void ClarityHook_OnWinTransition_FiresBuffEarned()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var runner = new DialogueRunner(BuildSimpleTree(), new FakeClock());
            int buffEarnedCount = 0;
            using var hook = new BattleClarityHook(sm.Context, runner, sm, () => buffEarnedCount++);

            // Set up a Win: Phase 1 cleared (beads extinguished), 2 beats landed.
            sm.Context.DemonBeads.Damage(5);
            sm.Context.BeatsLandedInPhase3 = 2;

            sm.Advance(); sm.Advance(); sm.Advance(); // → Outcome resolves Win
            Assert.AreEqual(BattleOutcome.Win, sm.Context.Outcome);
            Assert.AreEqual(1, buffEarnedCount);
            Assert.IsTrue(hook.ClarityBuffEarned);
        }

        [Test]
        public void ClarityHook_OnLossTransition_DoesNotFireBuff()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var runner = new DialogueRunner(BuildSimpleTree(), new FakeClock());
            int buffEarnedCount = 0;
            using var hook = new BattleClarityHook(sm.Context, runner, sm, () => buffEarnedCount++);

            // Loss: 0 beats with beads remaining.
            sm.Context.BeatsLandedInPhase3 = 0;
            sm.Advance(); sm.Advance(); sm.Advance();
            Assert.AreEqual(BattleOutcome.Loss, sm.Context.Outcome);
            Assert.AreEqual(0, buffEarnedCount);
        }

        [Test]
        public void FullClearBonus_NotApplied_WhenBeadsRemaining()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Context.Phase1ElapsedMs = 70_000;

            var runner = new DialogueRunner(BuildSimpleTree(), new FakeClock());
            BattleFullClearBonus.ApplyTo(sm.Context, runner);
            Assert.AreEqual(DialogueRunner.DefaultOptionTimeoutMs, runner.OptionTimeoutMs);
        }

        [Test]
        public void FullClearBonus_NotApplied_WhenOver90s()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Context.DemonBeads.Damage(5);
            sm.Context.Phase1ElapsedMs = 90_000;

            var runner = new DialogueRunner(BuildSimpleTree(), new FakeClock());
            BattleFullClearBonus.ApplyTo(sm.Context, runner);
            Assert.AreEqual(DialogueRunner.DefaultOptionTimeoutMs, runner.OptionTimeoutMs);
        }

        [Test]
        public void FullClearBonus_Applied_WhenFullClearAnd80s()
        {
            var emitter = new NoopEmitter();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Context.DemonBeads.Damage(5);
            sm.Context.Phase1ElapsedMs = 80_000;

            var runner = new DialogueRunner(BuildSimpleTree(), new FakeClock());
            BattleFullClearBonus.ApplyTo(sm.Context, runner);
            Assert.AreEqual((int)(DialogueRunner.DefaultOptionTimeoutMs * 1.1f), runner.OptionTimeoutMs);
        }
    }
}
