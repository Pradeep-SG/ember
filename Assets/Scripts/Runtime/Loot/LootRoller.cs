using System;
using Kindrith.Core;

namespace Kindrith.Loot
{
    // Rolls a LootDrop from a LootCatalog. Floor sets the minimum rarity (rarities below
    // collapse into the floor). resonanceLuckMultiplier scales upper-rarity weights and
    // re-normalizes so Σweights = 1.0; lower rarities are clamped non-negative.
    //
    // The "functional vs cosmetic" boundary: only battle Win / Critical Win sources roll
    // functional items. Other sources (oath_completion etc.) reroll a cosmetic if the
    // first pick is functional.
    public sealed class LootRoller
    {
        readonly LootCatalog _catalog;
        readonly IRandomProvider _rng;

        public LootRoller(LootCatalog catalog, IRandomProvider rng)
        {
            _catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        // Source determines floor + functional eligibility per loot-spec §4.
        public static class Source
        {
            public const string OathCompletion = "oath_completion";
            public const string ShadowBattleWin = "shadow_battle_win";
            public const string ShadowBattleCriticalWin = "shadow_battle_critical_win";
        }

        public LootDrop RollForSource(string source, float resonanceLuckMultiplier = 1f)
        {
            var floor = FloorForSource(source);
            bool functionalsAllowed = FunctionalsAllowedFor(source);
            return Roll(floor, resonanceLuckMultiplier, functionalsAllowed, source);
        }

        public LootDrop Roll(Rarity floor = Rarity.Common, float resonanceLuckMultiplier = 1f,
            bool functionalsAllowed = true, string source = null)
        {
            var weights = ResolvedWeights(floor, resonanceLuckMultiplier);
            var rarity = PickRarity(weights);
            var item = PickItemAtRarity(rarity, functionalsAllowed);
            if (item == null) return LootDrop.None;
            return new LootDrop(item.CatalogId, item.Rarity, item.IsFunctional, source);
        }

        public static Rarity FloorForSource(string source)
        {
            switch (source)
            {
                case Source.OathCompletion: return Rarity.Common;
                case Source.ShadowBattleWin: return Rarity.Uncommon;
                case Source.ShadowBattleCriticalWin: return Rarity.Rare;
                default: return Rarity.Common;
            }
        }

        public static bool FunctionalsAllowedFor(string source)
            => source == Source.ShadowBattleWin || source == Source.ShadowBattleCriticalWin;

        // Phase 1 → Phase 2: Resonance tier maps to a luck multiplier per loot-spec §3.
        public static float LuckMultiplierForTier(string resonanceTier)
        {
            switch (resonanceTier)
            {
                case "dim": return 0.85f;
                case "warm": return 1.0f;
                case "bright": return 1.15f;
                case "radiant": return 1.3f;
                default: return 1.0f;
            }
        }

        // Returns a length-5 array of normalized weights (index = (int)Rarity).
        // Weights below floor collapse into the floor's bucket. Upper-rarity weights
        // are scaled by `mult`; if mult > 1, lower rarities are reduced proportionally.
        // Final array sums to ~1.0 after re-normalization.
        public float[] ResolvedWeights(Rarity floor, float mult)
        {
            var w = new float[5];
            // Start with base weights collapsed to floor.
            float collapsed = 0f;
            for (int r = 0; r < 5; r++)
            {
                float baseW = _catalog.WeightFor((Rarity)r);
                if ((Rarity)r < floor) { collapsed += baseW; w[r] = 0f; }
                else w[r] = baseW;
            }
            w[(int)floor] += collapsed;

            // Apply luck: scale rarities >= Uncommon by mult; the displaced mass is
            // taken from / pushed back into Common.
            if (Math.Abs(mult - 1f) > 1e-6f && floor < Rarity.Uncommon)
            {
                float upperBefore = w[(int)Rarity.Uncommon] + w[(int)Rarity.Rare]
                                  + w[(int)Rarity.Epic] + w[(int)Rarity.Legendary];
                float upperAfter = upperBefore * mult;
                float delta = upperAfter - upperBefore;
                w[(int)Rarity.Uncommon]  *= mult;
                w[(int)Rarity.Rare]      *= mult;
                w[(int)Rarity.Epic]      *= mult;
                w[(int)Rarity.Legendary] *= mult;
                w[(int)Rarity.Common] = Math.Max(0f, w[(int)Rarity.Common] - delta);
            }

            // Normalize.
            float sum = 0f;
            for (int i = 0; i < 5; i++) sum += w[i];
            if (sum <= 0f) { w[(int)floor] = 1f; return w; }
            for (int i = 0; i < 5; i++) w[i] /= sum;
            return w;
        }

        Rarity PickRarity(float[] weights)
        {
            float r = _rng.NextFloat();
            float cum = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cum += weights[i];
                if (r <= cum) return (Rarity)i;
            }
            return (Rarity)(weights.Length - 1);
        }

        LootCatalogItem PickItemAtRarity(Rarity rarity, bool functionalsAllowed)
        {
            // Walk the items at this rarity; if functionals aren't allowed, skip them.
            // If the rarity has no eligible items, walk down rarities until we find one.
            for (int r = (int)rarity; r >= 0; r--)
            {
                int count = 0;
                for (int i = 0; i < _catalog.Items.Length; i++)
                {
                    var it = _catalog.Items[i];
                    if ((int)it.Rarity != r) continue;
                    if (!functionalsAllowed && it.IsFunctional) continue;
                    count++;
                }
                if (count == 0) continue;

                int pick = _rng.NextInt(0, count);
                int seen = 0;
                for (int i = 0; i < _catalog.Items.Length; i++)
                {
                    var it = _catalog.Items[i];
                    if ((int)it.Rarity != r) continue;
                    if (!functionalsAllowed && it.IsFunctional) continue;
                    if (seen == pick) return it;
                    seen++;
                }
            }
            return null;
        }
    }
}
