#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Kindrith.Loot;

namespace Kindrith.Editor
{
    // WP-13 importer stub. The brief envisions a CSV/JSON pipeline that re-stamps
    // Phase2Catalog.asset from a source-of-truth file. For now this is a sanity
    // check — the menu item asserts the catalog has the spec's expected shape
    // (15 items, 12 cosmetic, 3 functional, weights sum to ~1.0). Wire a real
    // CSV importer when the cosmetic catalog grows past hand-edit comfort.
    public static class LootCatalogImporter
    {
        const string CatalogPath = "Assets/Settings/Loot/Phase2Catalog.asset";

        [MenuItem("Tools/Kindrith/Validate Phase 2 Loot Catalog")]
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LootCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"LootCatalogImporter: catalog not found at {CatalogPath}");
                return;
            }

            int total = catalog.Items?.Length ?? 0;
            int cosmetic = catalog.Items?.Count(i => !i.IsFunctional) ?? 0;
            int functional = catalog.Items?.Count(i => i.IsFunctional) ?? 0;
            float sum = catalog.WeightCommon + catalog.WeightUncommon + catalog.WeightRare
                      + catalog.WeightEpic + catalog.WeightLegendary;

            Debug.Log($"LootCatalogImporter: {total} items ({cosmetic} cosmetic, {functional} functional), Σweights={sum:F4}");

            if (total != 15) Debug.LogWarning($"Expected 15 items per loot-spec.md; found {total}.");
            if (cosmetic != 12) Debug.LogWarning($"Expected 12 cosmetic; found {cosmetic}.");
            if (functional != 3) Debug.LogWarning($"Expected 3 functional; found {functional}.");
            if (Mathf.Abs(sum - 1f) > 1e-3f) Debug.LogWarning($"Σweights drifted from 1.0: {sum:F4}");

            // Functional must not appear at Epic/Legendary per spec — the high-tier
            // drops are the cosmetic carrots that drive Pillar 3.
            var highTierFunctional = catalog.Items
                ?.Where(i => i.IsFunctional && (i.Rarity == Rarity.Epic || i.Rarity == Rarity.Legendary))
                .ToArray();
            if (highTierFunctional != null && highTierFunctional.Length > 0)
            {
                Debug.LogWarning($"Found {highTierFunctional.Length} functional items at Epic/Legendary — spec keeps high-tier slots cosmetic.");
            }
        }
    }
}
#endif
