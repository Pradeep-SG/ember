using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kindrith.Loot
{
    [Serializable]
    public sealed class LootCatalogItem
    {
        public string CatalogId;
        public string DisplayName;
        public Rarity Rarity;
        public bool IsFunctional; // false = cosmetic
        public string IconPath;   // resolved by view layer (Resources or Addressables)
    }

    [CreateAssetMenu(menuName = "Kindrith/Loot Catalog", fileName = "LootCatalog")]
    public sealed class LootCatalog : ScriptableObject
    {
        public LootCatalogItem[] Items = Array.Empty<LootCatalogItem>();

        // Base rarity weights — match the brief's Warm-tier distribution.
        // Sum = 1.0; LootRoller normalizes after applying floor + luck multiplier.
        public float WeightCommon    = 0.60f;
        public float WeightUncommon  = 0.25f;
        public float WeightRare      = 0.12f;
        public float WeightEpic      = 0.025f;
        public float WeightLegendary = 0.005f;

        public IEnumerable<LootCatalogItem> ItemsAtRarity(Rarity r)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Rarity == r) yield return Items[i];
            }
        }

        public float WeightFor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return WeightCommon;
                case Rarity.Uncommon: return WeightUncommon;
                case Rarity.Rare: return WeightRare;
                case Rarity.Epic: return WeightEpic;
                case Rarity.Legendary: return WeightLegendary;
                default: return 0f;
            }
        }
    }
}
