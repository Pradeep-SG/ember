using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Kindrith.Dialogue
{
    public sealed class DialogueMarkdownParser
    {
        public sealed class Result
        {
            public List<ParsedTree> Trees = new List<ParsedTree>();
            public List<string> Errors = new List<string>();
            public bool IsValid => Errors.Count == 0;
        }

        public sealed class ParsedTree
        {
            public ArchetypeId Archetype;
            public string EntryNodeId = "node_0";
            public List<DialogueNode> Nodes = new List<DialogueNode>();
        }

        // ### 2.1 Archetype 1 — "The Permission-giver"
        static readonly Regex ArchetypeHeading =
            new Regex(@"^###\s+\d+\.\d+\s+Archetype\s+\d+\s*[—-]\s*""([^""]+)""\s*$", RegexOptions.Compiled);

        // Matches both **Node_0 (opening)** (parens inside bold) and **Node_1a** (after Counter A)
        // (parens after bold). Anchored at start of line; nothing after the captured id needs to
        // satisfy a closing **, since both shapes appear in the source markdown.
        static readonly Regex NodeHeading =
            new Regex(@"^\*\*Node_(\w+)", RegexOptions.Compiled);

        // > *"demon line"* [optional stage direction]
        static readonly Regex DemonLineRx =
            new Regex(@"^>\s*\*""([^""]*)""\*", RegexOptions.Compiled);

        // - **A. (Counter)** "option text"
        static readonly Regex OptionLineRx =
            new Regex(@"^-\s*\*\*([A-Z])\.\s*\(([A-Za-z]+)\)\*\*\s*""([^""]*)""\s*$", RegexOptions.Compiled);

        public Result Parse(string markdown)
        {
            var result = new Result();
            var lines = (markdown ?? string.Empty).Replace("\r\n", "\n").Split('\n');

            ValidateForbiddenContent(lines, result.Errors);

            for (int i = 0; i < lines.Length; i++)
            {
                var match = ArchetypeHeading.Match(lines[i]);
                if (!match.Success) continue;

                var archetype = MapArchetype(match.Groups[1].Value);
                if (!archetype.HasValue) continue;

                int sectionEnd = lines.Length;
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (ArchetypeHeading.IsMatch(lines[j])) { sectionEnd = j; break; }
                }

                var tree = ParseTree(archetype.Value, lines, i + 1, sectionEnd, result.Errors);
                if (tree != null) result.Trees.Add(tree);
                i = sectionEnd - 1;
            }

            return result;
        }

        ParsedTree ParseTree(ArchetypeId archetype, string[] lines, int start, int end, List<string> errors)
        {
            var tree = new ParsedTree { Archetype = archetype };

            int i = start;
            while (i < end)
            {
                var nodeMatch = NodeHeading.Match(lines[i]);
                if (!nodeMatch.Success) { i++; continue; }

                var suffix = nodeMatch.Groups[1].Value.ToLowerInvariant();
                var node = new DialogueNode { Id = "node_" + suffix };

                int j = i + 1;
                while (j < end && string.IsNullOrWhiteSpace(lines[j])) j++;

                if (j < end)
                {
                    var demonMatch = DemonLineRx.Match(lines[j]);
                    if (demonMatch.Success)
                    {
                        node.DemonLine = demonMatch.Groups[1].Value;
                        j++;
                    }
                    else
                    {
                        errors.Add($"Expected demon line after Node_{suffix}");
                    }
                }

                var options = new List<DialogueOption>();
                while (j < end)
                {
                    var line = lines[j];
                    if (string.IsNullOrWhiteSpace(line)) { j++; continue; }
                    if (NodeHeading.IsMatch(line)) break;
                    if (line.StartsWith("###") || line.StartsWith("####") || line.StartsWith("---")) break;

                    var optMatch = OptionLineRx.Match(line);
                    if (optMatch.Success)
                    {
                        var letter = optMatch.Groups[1].Value;
                        var className = optMatch.Groups[2].Value;
                        var text = optMatch.Groups[3].Value;

                        if (!Enum.TryParse<OptionClass>(className, ignoreCase: false, out var optClass))
                        {
                            errors.Add($"Node {node.Id}: option class '{className}' is not Counter/Deflect/Agree");
                            j++;
                            continue;
                        }

                        options.Add(new DialogueOption
                        {
                            Text = text,
                            Class = optClass,
                            LeadsToNodeId = ComputeLeadsToId(node.Id, letter),
                        });
                    }
                    j++;
                }

                if (options.Count > 0)
                {
                    if (options.Count != 3)
                    {
                        errors.Add($"Node {node.Id}: expected 3 options, got {options.Count}");
                    }
                    node.Options = options.ToArray();
                }
                else
                {
                    node.Options = Array.Empty<DialogueOption>();
                    if (node.Id.StartsWith("node_1") && node.Id.Length > "node_1".Length)
                    {
                        node.NextNodeId = "node_2";
                    }
                }

                tree.Nodes.Add(node);
                i = j;
            }

            return tree;
        }

        static string ComputeLeadsToId(string nodeId, string letter)
        {
            string suffixLetter = letter.ToLowerInvariant();
            if (nodeId == "node_0")
            {
                return "node_1" + suffixLetter;
            }
            if (nodeId == "node_2")
            {
                return "node_3" + suffixLetter;
            }
            return null;
        }

        static ArchetypeId? MapArchetype(string name)
        {
            var normalized = name.Replace("'", "").Replace(" ", "").Replace("-", "").ToLowerInvariant();
            if (normalized.StartsWith("the")) normalized = normalized.Substring(3);

            switch (normalized)
            {
                case "permissiongiver": return ArchetypeId.PermissionGiver;
                case "tenderexcuse": return ArchetypeId.TenderExcuse;
                case "tomorrowswarden": return ArchetypeId.TomorrowsWarden;
                case "accountant": return ArchetypeId.Accountant;
                case "comparison": return ArchetypeId.Comparison;
                default: return null;
            }
        }

        // Per archetype-doc §3 rules 3 + 4: no emoji, no `!`, no ALL CAPS word.
        // Applied to spoken content only (demon lines and option text).
        static void ValidateForbiddenContent(string[] lines, List<string> errors)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                string content = null;

                var demonMatch = DemonLineRx.Match(line);
                if (demonMatch.Success) content = demonMatch.Groups[1].Value;
                else
                {
                    var optMatch = OptionLineRx.Match(line);
                    if (optMatch.Success) content = optMatch.Groups[3].Value;
                }
                if (content == null) continue;

                if (HasEmoji(content)) errors.Add($"Line {i + 1}: contains emoji-like character");
                if (content.Contains('!')) errors.Add($"Line {i + 1}: contains '!'");
                if (HasAllCapsWord(content)) errors.Add($"Line {i + 1}: contains ALL CAPS word");
            }
        }

        static bool HasEmoji(string text)
        {
            foreach (char c in text)
            {
                // Most emoji live in higher Unicode planes (surrogate pairs in UTF-16).
                if (char.IsSurrogate(c)) return true;
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.OtherSymbol) return true;
            }
            return false;
        }

        static bool HasAllCapsWord(string text)
        {
            var words = Regex.Matches(text, @"[A-Za-z]+");
            foreach (Match m in words)
            {
                var word = m.Value;
                if (word.Length >= 2 && word == word.ToUpperInvariant() && word != word.ToLowerInvariant())
                    return true;
            }
            return false;
        }
    }
}
