using System.IO;
using System.Linq;
using NUnit.Framework;
using Kindrith.Dialogue;

namespace Kindrith.Tests.EditMode
{
    public class DialogueMarkdownParserTests
    {
        const string ValidArchetypeMarkdown = @"
### 2.1 Archetype 1 — ""The Permission-giver""

#### Dialogue tree

**Node_0 (opening)**
> *""You've been so disciplined. One won't undo any of it.""*

- **A. (Counter)** ""One is how every streak ends.""
- **B. (Deflect)** ""Maybe. I don't know.""
- **C. (Agree)** ""You're right.""

**Node_1a** (after Counter A)
> *""Okay. I hear you.""*

**Node_1b** (after Deflect B)
> *""Of course you don't know.""*

**Node_1c** (after Agree C)
> *""You have. And you deserve some ease.""*

**Node_2 (double-down)**
> *""Look, tomorrow morning you'll still be the person who did the hard thing.""*

- **A. (Counter)** ""The person I'm becoming wouldn't pick one up.""
- **B. (Deflect)** ""I just want to get past this moment.""
- **C. (Agree)** ""You're right. Nothing changes.""

**Node_3a** (after Counter A)
> *""...That's a stronger answer than I expected.""*

**Node_3b** (after Deflect B)
> *""Past this moment, sure.""*

**Node_3c** (after Agree C)
> *""Good. Let's step out for some air.""*
";

        [Test]
        public void ValidArchetype_ParsesToOneTreeWithEightNodes()
        {
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(ValidArchetypeMarkdown);

            Assert.IsTrue(result.IsValid, "Errors: " + string.Join("; ", result.Errors));
            Assert.AreEqual(1, result.Trees.Count);
            var tree = result.Trees[0];
            Assert.AreEqual(ArchetypeId.PermissionGiver, tree.Archetype);
            Assert.AreEqual(8, tree.Nodes.Count);
            Assert.AreEqual("node_0", tree.EntryNodeId);
        }

        [Test]
        public void ChoiceNodes_HaveThreeOptionsWithDistinctClasses()
        {
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(ValidArchetypeMarkdown);
            var tree = result.Trees[0];

            var node0 = tree.Nodes.First(n => n.Id == "node_0");
            Assert.AreEqual(3, node0.Options.Length);
            Assert.AreEqual(OptionClass.Counter, node0.Options[0].Class);
            Assert.AreEqual(OptionClass.Deflect, node0.Options[1].Class);
            Assert.AreEqual(OptionClass.Agree,   node0.Options[2].Class);
            Assert.AreEqual("node_1a", node0.Options[0].LeadsToNodeId);
            Assert.AreEqual("node_1b", node0.Options[1].LeadsToNodeId);
            Assert.AreEqual("node_1c", node0.Options[2].LeadsToNodeId);

            var node2 = tree.Nodes.First(n => n.Id == "node_2");
            Assert.AreEqual(3, node2.Options.Length);
            Assert.AreEqual("node_3a", node2.Options[0].LeadsToNodeId);
        }

        [Test]
        public void TransitionalNodes_PointToNode2()
        {
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(ValidArchetypeMarkdown);
            var tree = result.Trees[0];

            foreach (var id in new[] { "node_1a", "node_1b", "node_1c" })
            {
                var node = tree.Nodes.First(n => n.Id == id);
                Assert.AreEqual(0, node.Options.Length);
                Assert.AreEqual("node_2", node.NextNodeId);
            }
        }

        [Test]
        public void TerminalNodes_HaveNoOptionsAndNoNextNodeId()
        {
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(ValidArchetypeMarkdown);
            var tree = result.Trees[0];

            foreach (var id in new[] { "node_3a", "node_3b", "node_3c" })
            {
                var node = tree.Nodes.First(n => n.Id == id);
                Assert.AreEqual(0, node.Options.Length);
                Assert.IsTrue(string.IsNullOrEmpty(node.NextNodeId));
            }
        }

        [Test]
        public void RejectsExclamation()
        {
            var bad = ValidArchetypeMarkdown.Replace("One won't undo any of it.", "One won't undo any of it!");
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(bad);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("'!'")));
        }

        [Test]
        public void RejectsAllCapsWord()
        {
            var bad = ValidArchetypeMarkdown.Replace("Maybe. I don't know.", "MAYBE. I don't know.");
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(bad);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("ALL CAPS")));
        }

        [Test]
        public void RejectsEmoji()
        {
            // U+1F525 fire emoji, encoded as surrogate pair
            var bad = ValidArchetypeMarkdown.Replace("Maybe. I don't know.", "Maybe. I don't know. 🔥");
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(bad);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("emoji")));
        }

        [Test]
        public void ParsesAllThreePhaseOneArchetypesFromRealFile()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "docs", "demon-archetypes.md");
            if (!File.Exists(path))
            {
                Assert.Inconclusive($"demon-archetypes.md not found at {path}");
                return;
            }

            var md = File.ReadAllText(path);
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(md);

            Assert.IsTrue(result.IsValid, "Errors: " + string.Join("; ", result.Errors));
            Assert.AreEqual(5, result.Trees.Count, "All 5 archetypes should parse (Phase 1 ships 3)");

            var phase1 = result.Trees
                .Where(t => t.Archetype == ArchetypeId.PermissionGiver
                         || t.Archetype == ArchetypeId.TenderExcuse
                         || t.Archetype == ArchetypeId.TomorrowsWarden)
                .ToList();
            Assert.AreEqual(3, phase1.Count);
            foreach (var tree in phase1)
            {
                Assert.AreEqual(8, tree.Nodes.Count, $"{tree.Archetype} should have 8 nodes");
            }
        }
    }
}
