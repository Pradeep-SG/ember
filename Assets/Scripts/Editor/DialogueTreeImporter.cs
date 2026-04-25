using System.IO;
using UnityEditor;
using UnityEngine;
using Kindrith.Dialogue;

namespace Kindrith.Editor
{
    public static class DialogueTreeImporter
    {
        const string SourcePath = "docs/demon-archetypes.md";
        const string OutputFolder = "Assets/Settings/DialogueTrees";

        [MenuItem("Tools/Kindrith/Regenerate Dialogue Trees")]
        public static void Regenerate()
        {
            if (!File.Exists(SourcePath))
            {
                Debug.LogError($"DialogueTreeImporter: source markdown not found at {SourcePath}");
                return;
            }

            var markdown = File.ReadAllText(SourcePath);
            var parser = new DialogueMarkdownParser();
            var result = parser.Parse(markdown);

            if (!result.IsValid)
            {
                foreach (var err in result.Errors) Debug.LogError($"DialogueTreeImporter: {err}");
                return;
            }

            EnsureFolder(OutputFolder);

            int generated = 0;
            foreach (var parsed in result.Trees)
            {
                if (parsed.Archetype != ArchetypeId.PermissionGiver &&
                    parsed.Archetype != ArchetypeId.TenderExcuse &&
                    parsed.Archetype != ArchetypeId.TomorrowsWarden)
                {
                    continue; // Phase 1 ships only these three.
                }

                var assetPath = $"{OutputFolder}/{parsed.Archetype}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<DialogueTree>(assetPath);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                var asset = ScriptableObject.CreateInstance<DialogueTree>();
                asset.Archetype = parsed.Archetype;
                asset.EntryNodeId = parsed.EntryNodeId;
                asset.Nodes = parsed.Nodes.ToArray();

                AssetDatabase.CreateAsset(asset, assetPath);
                generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"DialogueTreeImporter: regenerated {generated} dialogue tree(s) in {OutputFolder}/");
        }

        static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
