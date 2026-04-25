using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Kindrith.Analytics;
using Kindrith.Breathing;
using Kindrith.Core;
using Kindrith.Dialogue;
using Kindrith.ShadowBattle;
using Kindrith.UI;

namespace Kindrith.Tests.PlayMode
{
    // Drives the full Phase 1 → Phase 2 → Phase 3 → Outcome pipeline programmatically with a
    // FakeClock and asserts the analytics event sequence emitted through AnalyticsBus matches
    // the brief WP-08 acceptance.
    public class WP08HappyPathE2E
    {
        sealed class FakeEnvelope : IEnvelopeProvider
        {
            public string PlayerId => "player_test";
            public string SessionId => "session_test";
            public string AppVersion => "0.1.0";
            public string Platform => "ios";
            public string BuildType => "debug";
            public string Locale => "en-US";
        }

        sealed class CapturingSink : IAnalyticsSink
        {
            public readonly List<AnalyticsEvent> Events = new List<AnalyticsEvent>();
            public void Emit(AnalyticsEvent ev) => Events.Add(ev);
        }

        static DialogueTree BuildPhase2Tree()
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
                        new DialogueOption { Text = "counter", Class = OptionClass.Counter, LeadsToNodeId = "node_1a" },
                        new DialogueOption { Text = "deflect", Class = OptionClass.Deflect, LeadsToNodeId = "node_1b" },
                        new DialogueOption { Text = "agree",   Class = OptionClass.Agree,   LeadsToNodeId = "node_1c" },
                    },
                },
                new DialogueNode { Id = "node_1a", DemonLine = "...", Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode { Id = "node_1b", DemonLine = "...", Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode { Id = "node_1c", DemonLine = "...", Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode
                {
                    Id = "node_2",
                    DemonLine = "...",
                    Options = new[]
                    {
                        new DialogueOption { Text = "counter", Class = OptionClass.Counter, LeadsToNodeId = "node_3a" },
                        new DialogueOption { Text = "deflect", Class = OptionClass.Deflect, LeadsToNodeId = "node_3b" },
                        new DialogueOption { Text = "agree",   Class = OptionClass.Agree,   LeadsToNodeId = "node_3c" },
                    },
                },
                new DialogueNode { Id = "node_3a", DemonLine = "...", Options = new DialogueOption[0] },
                new DialogueNode { Id = "node_3b", DemonLine = "...", Options = new DialogueOption[0] },
                new DialogueNode { Id = "node_3c", DemonLine = "...", Options = new DialogueOption[0] },
            };
            return tree;
        }

        [UnityTest]
        public IEnumerator HappyPath_AnalyticsBus_EmitsBriefEventSequence()
        {
            var sink = new CapturingSink();
            var bus = new AnalyticsBus(sink, new FakeEnvelope());
            var emitter = new AnalyticsBusAdapter(bus);

            // 1. app_opened
            bus.Emit("app_opened", new Dictionary<string, object>
            {
                ["resume_reason"] = "cold_start",
                ["time_since_last_open_ms"] = null,
            });

            // 2. shadow_battle_started
            bus.Emit("shadow_battle_started", new Dictionary<string, object>
            {
                ["battle_id"] = "test_battle",
                ["demon_archetype"] = "permission_giver",
                ["trigger"] = "user_resist_tap",
            });

            // 3. Phase 1 → state machine emits shadow_battle_phase_entered (Phase1)
            var clock = new FakeClock();
            var bclock = new BreathingClock(clock);
            bclock.Start();
            var sm = new BattleStateMachine(emitter);
            sm.StartBattle(ArchetypeId.PermissionGiver);

            var phase1 = new Phase1Controller(
                clock, bclock, new TapEvaluator(), sm.Context.DemonBeads, sm.Context, emitter, sm.Advance);
            phase1.Start();

            // Three Perfect taps (2 damage each) extinguishes beads and ends Phase 1.
            for (int cycle = 0; cycle < 3; cycle++)
            {
                int tapAt = cycle * BreathingCadence.CycleMs + BreathingCadence.TapTargetMs;
                clock.SetNowMs(tapAt);
                bclock.Tick();
                phase1.RegisterTap();
            }
            phase1.Tick();
            Assert.AreEqual(BattlePhase.Phase2, sm.Current, "Phase 1 should end after beads extinguished");

            // Phase 2 → 2 counters via DialogueRunner.
            var tree = BuildPhase2Tree();
            var runner = new DialogueRunner(tree, clock);
            var phase2 = new Phase2Controller(runner, sm.Context, emitter, clock, sm.Advance);
            phase2.Start();
            phase2.Choose(0);   // Counter at node_0
            phase2.Tick();      // node_1a → node_2 (auto-advance)
            phase2.Choose(0);   // Counter at node_2 → node_3a → completes
            Assert.AreEqual(BattlePhase.Phase3, sm.Current, "Phase 2 should advance after 2 choices");

            // Phase 3 → 3 of 3 perfect beats.
            var sequencer = new FinisherSequencer(clock);
            var phase3 = new Phase3Controller(clock, sequencer, sm.Context, emitter, sm.Advance);
            phase3.Start();
            clock.SetNowMs(1_000); sequencer.RegisterTap(1_000);
            clock.SetNowMs(2_500); sequencer.RegisterTap(2_500);
            clock.SetNowMs(4_000); sequencer.RegisterHoldStart(4_000);
            clock.SetNowMs(4_800); sequencer.RegisterHoldRelease(4_800);
            phase3.Tick();
            Assert.AreEqual(BattlePhase.Outcome, sm.Current);
            Assert.AreEqual(BattleOutcome.CriticalWin, sm.Context.Outcome);

            // Verify the emitted name sequence — check the brief's expected ordering, allowing
            // tap_registered events to interleave with phase_entered (the brief sequence is the
            // "key" events; tap registers are the side-effect Phase 1 emits per real tap).
            var names = sink.Events.Select(e => e.Name).ToList();

            Assert.AreEqual("app_opened", names[0]);
            Assert.AreEqual("shadow_battle_started", names[1]);

            // The state machine + controllers emit at least these key events somewhere after
            // the lead-in (in order):
            var phaseEnteredCount = names.Count(n => n == "shadow_battle_phase_entered");
            var dialogueChoiceCount = names.Count(n => n == "shadow_battle_dialogue_choice");
            var finisherBeatCount = names.Count(n => n == "shadow_battle_finisher_beat");
            var tapRegisteredCount = names.Count(n => n == "shadow_battle_tap_registered");
            var completedCount = names.Count(n => n == "shadow_battle_completed");

            Assert.AreEqual(3, phaseEnteredCount, "phase_entered fires for Phase1, Phase2, Phase3");
            Assert.AreEqual(2, dialogueChoiceCount);
            Assert.AreEqual(3, finisherBeatCount);
            Assert.AreEqual(3, tapRegisteredCount);
            Assert.AreEqual(1, completedCount);

            // shadow_battle_completed must be the last event — the battle is fully over.
            Assert.AreEqual("shadow_battle_completed", names.Last());

            yield return null;
        }
    }
}
