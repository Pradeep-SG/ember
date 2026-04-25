using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Core;
using Kindrith.Dialogue;

namespace Kindrith.Tests.EditMode
{
    public class DialogueRunnerTests
    {
        static DialogueTree MakeTestTree()
        {
            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.Archetype = ArchetypeId.PermissionGiver;
            tree.EntryNodeId = "node_0";
            tree.Nodes = new[]
            {
                new DialogueNode
                {
                    Id = "node_0",
                    DemonLine = "opening",
                    Options = new[]
                    {
                        new DialogueOption { Text = "counter", Class = OptionClass.Counter, LeadsToNodeId = "node_1a" },
                        new DialogueOption { Text = "deflect", Class = OptionClass.Deflect, LeadsToNodeId = "node_1b" },
                        new DialogueOption { Text = "agree",   Class = OptionClass.Agree,   LeadsToNodeId = "node_1c" },
                    },
                },
                new DialogueNode { Id = "node_1a", DemonLine = "after counter", Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode { Id = "node_1b", DemonLine = "after deflect", Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode { Id = "node_1c", DemonLine = "after agree",   Options = new DialogueOption[0], NextNodeId = "node_2" },
                new DialogueNode
                {
                    Id = "node_2",
                    DemonLine = "double down",
                    Options = new[]
                    {
                        new DialogueOption { Text = "counter", Class = OptionClass.Counter, LeadsToNodeId = "node_3a" },
                        new DialogueOption { Text = "deflect", Class = OptionClass.Deflect, LeadsToNodeId = "node_3b" },
                        new DialogueOption { Text = "agree",   Class = OptionClass.Agree,   LeadsToNodeId = "node_3c" },
                    },
                },
                new DialogueNode { Id = "node_3a", DemonLine = "final counter", Options = new DialogueOption[0] },
                new DialogueNode { Id = "node_3b", DemonLine = "final deflect", Options = new DialogueOption[0] },
                new DialogueNode { Id = "node_3c", DemonLine = "final agree",   Options = new DialogueOption[0] },
            };
            return tree;
        }

        [Test]
        public void Constructor_EntersEntryNode()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);
            Assert.AreEqual("node_0", runner.Current.Id);
        }

        [Test]
        public void CounterCounter_Completes_With2CountersTaken()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);

            runner.Choose(0); // counter at Node_0 → node_1a
            runner.Tick();    // node_1a auto-advances → node_2
            runner.Choose(0); // counter at Node_2 → node_3a → complete

            Assert.IsTrue(runner.IsComplete);
            Assert.AreEqual(2, runner.CountersTaken);
            Assert.AreEqual("node_3a", runner.Current.Id);
        }

        [Test]
        public void DeflectAgree_Completes_With0CountersTaken()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);

            runner.Choose(1); // deflect → node_1b
            runner.Tick();    // → node_2
            runner.Choose(2); // agree → node_3c

            Assert.IsTrue(runner.IsComplete);
            Assert.AreEqual(0, runner.CountersTaken);
        }

        [Test]
        public void Timeout_AutoPicksDeflect()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);
            var chosen = new List<OptionClass>();
            runner.OptionChosen += (opt, cls) => chosen.Add(cls);

            // Sit on Node_0 for 20s.
            clock.SetNowMs(20_000);
            runner.Tick();

            Assert.AreEqual(1, chosen.Count);
            Assert.AreEqual(OptionClass.Deflect, chosen[0]);
        }

        [Test]
        public void Tick_AutoAdvancesTransitionalNodes()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);

            runner.Choose(0); // node_0 → node_1a
            Assert.AreEqual("node_1a", runner.Current.Id);

            runner.Tick();    // node_1a → node_2
            Assert.AreEqual("node_2", runner.Current.Id);
        }

        [Test]
        public void NodeEntered_FiresForEveryVisitedNode()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);
            var visited = new List<string>();
            runner.NodeEntered += n => visited.Add(n.Id);

            runner.Choose(0);
            runner.Tick();
            runner.Choose(0);

            // Constructor enters node_0 BEFORE we subscribe, so we expect node_1a, node_2, node_3a.
            Assert.AreEqual(new[] { "node_1a", "node_2", "node_3a" }, visited.ToArray());
        }

        [Test]
        public void Choose_AfterComplete_IsNoOp()
        {
            var clock = new FakeClock();
            var runner = new DialogueRunner(MakeTestTree(), clock);

            runner.Choose(0);
            runner.Tick();
            runner.Choose(0);
            Assert.IsTrue(runner.IsComplete);

            runner.Choose(0); // Should be ignored
            Assert.AreEqual(2, runner.CountersTaken);
        }
    }
}
