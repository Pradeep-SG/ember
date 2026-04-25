using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Breathing;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;

namespace Kindrith.Tests.EditMode
{
    // Pins the P1 today contract from docs/analytics-taxonomy.md §2.5. Drives a deterministic
    // happy-path battle through a fake clock + recording emitter, then asserts every emit's
    // key set matches the canonical contract — missing key → fail, extra key → fail.
    public class P1ParameterAuditTests
    {
        sealed class Recording : IBattleEventEmitter
        {
            public readonly List<(string name, IDictionary<string, object> p)> Events = new List<(string, IDictionary<string, object>)>();
            public void Emit(string name, IDictionary<string, object> parameters)
                => Events.Add((name, parameters));
        }

        // Canonical P1-today key sets per analytics-taxonomy.md §2.5.
        static readonly Dictionary<string, HashSet<string>> ExpectedKeys = new Dictionary<string, HashSet<string>>
        {
            ["shadow_battle_phase_entered"]   = new HashSet<string> { "battle_id", "phase", "entry_context" },
            ["shadow_battle_tap_registered"]  = new HashSet<string> { "battle_id", "cycle_index", "offset_ms", "grade" },
            ["shadow_battle_dialogue_choice"] = new HashSet<string> { "battle_id", "node_id", "option_index", "option_class", "latency_ms" },
            ["shadow_battle_finisher_beat"]   = new HashSet<string> { "battle_id", "beat_index", "landed", "offset_ms" },
            // shadow_battle_completed has an optional abandon_reason; the audit checks
            // either the win-shape or the abandon-shape below.
        };

        [Test]
        public void HappyPath_AllShadowBattleEmitsMatchTaxonomy()
        {
            var emitter = new Recording();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var clock = new FakeClock();
            var bclock = new BreathingClock(clock);
            bclock.Start();
            var phase1 = new Phase1Controller(clock, bclock, new TapEvaluator(),
                sm.Context.DemonBeads, sm.Context, emitter, () => { });
            phase1.Start();
            clock.SetNowMs(BreathingCadence.TapTargetMs);
            bclock.Tick();
            phase1.RegisterTap();

            sm.Advance(); // → Phase2

            var tree = BuildTinyTree();
            var runner = new DialogueRunner(tree, clock);
            var phase2 = new Phase2Controller(runner, sm.Context, emitter, clock, () => { });
            phase2.Start();
            phase2.Choose(0);

            sm.Advance(); // → Phase3

            var sequencer = new FinisherSequencer(clock);
            var phase3 = new Phase3Controller(clock, sequencer, sm.Context, emitter, () => { });
            phase3.Start();
            clock.SetNowMs(1_000); sequencer.RegisterTap(1_000); // beat 0 perfect

            sm.Advance(); // → Outcome (emits shadow_battle_completed)

            foreach (var (name, p) in emitter.Events)
            {
                if (name == "shadow_battle_completed")
                {
                    var keys = new HashSet<string>(p.Keys);
                    var winShape = new HashSet<string> { "battle_id", "outcome", "phase3_beats_landed", "final_beads_extinguished" };
                    var abandonShape = new HashSet<string>(winShape) { "abandon_reason" };
                    Assert.That(keys.SetEquals(winShape) || keys.SetEquals(abandonShape),
                        $"shadow_battle_completed has unexpected keys: [{string.Join(",", keys)}]");
                    continue;
                }

                Assert.That(ExpectedKeys.ContainsKey(name),
                    $"Unaudited event emitted: '{name}'. Add it to ExpectedKeys or analytics-taxonomy.md.");
                var actual = new HashSet<string>(p.Keys);
                var expected = ExpectedKeys[name];
                var missing = expected.Except(actual).ToList();
                var extra = actual.Except(expected).ToList();
                Assert.IsEmpty(missing, $"{name}: missing keys [{string.Join(",", missing)}]");
                Assert.IsEmpty(extra, $"{name}: extra keys [{string.Join(",", extra)}]");
            }
        }

        [Test]
        public void Abandon_CompletedEmit_HasAbandonReason()
        {
            var emitter = new Recording();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);
            sm.Abandon("user_chose_abandon");

            var completed = emitter.Events.Single(e => e.name == "shadow_battle_completed");
            Assert.AreEqual("user_chose_abandon", completed.p["abandon_reason"]);
            Assert.AreEqual("abandon", completed.p["outcome"]);
        }

        static DialogueTree BuildTinyTree()
        {
            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.Archetype = ArchetypeId.PermissionGiver;
            tree.EntryNodeId = "node_0";
            tree.Nodes = new[]
            {
                new DialogueNode
                {
                    Id = "node_0",
                    DemonLine = "...",
                    Options = new[]
                    {
                        new DialogueOption { Text = "A", Class = OptionClass.Counter, LeadsToNodeId = "node_1" },
                    },
                },
                new DialogueNode
                {
                    Id = "node_1",
                    DemonLine = "...",
                    Options = new[]
                    {
                        new DialogueOption { Text = "B", Class = OptionClass.Counter, LeadsToNodeId = null },
                    },
                },
            };
            return tree;
        }
    }
}
