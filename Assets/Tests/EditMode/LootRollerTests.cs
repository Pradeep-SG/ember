using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Kindrith.Core;
using Kindrith.Loot;

namespace Kindrith.Tests.EditMode
{
    public class LootRollerTests
    {
        // Reproduces the brief's Phase 2 catalog shape for tests so we don't depend on
        // the on-disk asset. Mirrors loot-spec §1: 12 cosmetic + 3 functional, weights
        // sum to 1.0 at the Warm-tier baseline.
        static LootCatalog BuildCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<LootCatalog>();
            catalog.WeightCommon = 0.60f;
            catalog.WeightUncommon = 0.25f;
            catalog.WeightRare = 0.12f;
            catalog.WeightEpic = 0.025f;
            catalog.WeightLegendary = 0.005f;
            catalog.Items = new[]
            {
                new LootCatalogItem { CatalogId = "c_common_a",    Rarity = Rarity.Common,    IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_common_b",    Rarity = Rarity.Common,    IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_common_c",    Rarity = Rarity.Common,    IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_common_d",    Rarity = Rarity.Common,    IsFunctional = false },
                new LootCatalogItem { CatalogId = "f_common",      Rarity = Rarity.Common,    IsFunctional = true  },
                new LootCatalogItem { CatalogId = "c_uncommon_a",  Rarity = Rarity.Uncommon,  IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_uncommon_b",  Rarity = Rarity.Uncommon,  IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_uncommon_c",  Rarity = Rarity.Uncommon,  IsFunctional = false },
                new LootCatalogItem { CatalogId = "f_uncommon",    Rarity = Rarity.Uncommon,  IsFunctional = true  },
                new LootCatalogItem { CatalogId = "c_rare_a",      Rarity = Rarity.Rare,      IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_rare_b",      Rarity = Rarity.Rare,      IsFunctional = false },
                new LootCatalogItem { CatalogId = "f_rare",        Rarity = Rarity.Rare,      IsFunctional = true  },
                new LootCatalogItem { CatalogId = "c_epic_a",      Rarity = Rarity.Epic,      IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_epic_b",      Rarity = Rarity.Epic,      IsFunctional = false },
                new LootCatalogItem { CatalogId = "c_legendary",   Rarity = Rarity.Legendary, IsFunctional = false },
            };
            return catalog;
        }

        [Test]
        public void DistributionMatches_Within1Point5Percent_At10kRolls()
        {
            // Warm tier (luck 1.0), Common floor — the baseline distribution.
            var catalog = BuildCatalog();
            var roller = new LootRoller(catalog, new SystemRandomProvider(seed: 42));
            var counts = new Dictionary<Rarity, int>
            {
                { Rarity.Common, 0 }, { Rarity.Uncommon, 0 }, { Rarity.Rare, 0 },
                { Rarity.Epic, 0 }, { Rarity.Legendary, 0 },
            };
            const int N = 10_000;
            for (int i = 0; i < N; i++)
            {
                var drop = roller.Roll(Rarity.Common, 1f);
                counts[drop.Rarity]++;
            }
            AssertCloseTo(0.60f, counts[Rarity.Common] / (float)N, 0.015f, "Common");
            AssertCloseTo(0.25f, counts[Rarity.Uncommon] / (float)N, 0.015f, "Uncommon");
            AssertCloseTo(0.12f, counts[Rarity.Rare] / (float)N, 0.015f, "Rare");
            AssertCloseTo(0.025f, counts[Rarity.Epic] / (float)N, 0.015f, "Epic");
            AssertCloseTo(0.005f, counts[Rarity.Legendary] / (float)N, 0.015f, "Legendary");
        }

        [Test]
        public void Floor_CommonForOath_UncommonForWin_RareForCriticalWin()
        {
            Assert.AreEqual(Rarity.Common, LootRoller.FloorForSource(LootRoller.Source.OathCompletion));
            Assert.AreEqual(Rarity.Uncommon, LootRoller.FloorForSource(LootRoller.Source.ShadowBattleWin));
            Assert.AreEqual(Rarity.Rare, LootRoller.FloorForSource(LootRoller.Source.ShadowBattleCriticalWin));
        }

        [Test]
        public void Functionals_OnlyOnBattleSources()
        {
            Assert.IsFalse(LootRoller.FunctionalsAllowedFor(LootRoller.Source.OathCompletion));
            Assert.IsTrue(LootRoller.FunctionalsAllowedFor(LootRoller.Source.ShadowBattleWin));
            Assert.IsTrue(LootRoller.FunctionalsAllowedFor(LootRoller.Source.ShadowBattleCriticalWin));
        }

        [Test]
        public void OathCompletion_NeverRollsFunctional()
        {
            var catalog = BuildCatalog();
            var roller = new LootRoller(catalog, new SystemRandomProvider(seed: 7));
            for (int i = 0; i < 5_000; i++)
            {
                var drop = roller.RollForSource(LootRoller.Source.OathCompletion);
                Assert.IsFalse(drop.IsFunctional, $"oath_completion rolled functional: {drop.CatalogId}");
            }
        }

        [Test]
        public void Floor_CollapsesLowerRaritiesIntoFloor()
        {
            var catalog = BuildCatalog();
            var roller = new LootRoller(catalog, new SystemRandomProvider(seed: 13));
            // With Rare floor, Common + Uncommon collapse into Rare's bucket.
            var w = roller.ResolvedWeights(Rarity.Rare, 1f);
            Assert.AreEqual(0f, w[(int)Rarity.Common]);
            Assert.AreEqual(0f, w[(int)Rarity.Uncommon]);
            // Sum still equals 1.0 (re-normalized).
            float sum = 0f; foreach (var x in w) sum += x;
            Assert.AreEqual(1f, sum, 1e-4f);
        }

        [Test]
        public void Luck_PreservesWeightSumAfterNormalization()
        {
            var catalog = BuildCatalog();
            var roller = new LootRoller(catalog, new SystemRandomProvider(seed: 99));
            foreach (var luck in new[] { 0.85f, 1.0f, 1.15f, 1.3f })
            {
                var w = roller.ResolvedWeights(Rarity.Common, luck);
                float sum = 0f; foreach (var x in w) sum += x;
                Assert.AreEqual(1f, sum, 1e-4f, $"Σweights drift at luck={luck}");
            }
        }

        [Test]
        public void Luck_TierMappingMatchesSpec()
        {
            Assert.AreEqual(0.85f, LootRoller.LuckMultiplierForTier("dim"));
            Assert.AreEqual(1.0f, LootRoller.LuckMultiplierForTier("warm"));
            Assert.AreEqual(1.15f, LootRoller.LuckMultiplierForTier("bright"));
            Assert.AreEqual(1.3f, LootRoller.LuckMultiplierForTier("radiant"));
        }

        [Test]
        public void HighLuck_IncreasesUpperRarityFrequency()
        {
            var catalog = BuildCatalog();
            var lowLuck = new LootRoller(catalog, new SystemRandomProvider(seed: 1));
            var highLuck = new LootRoller(catalog, new SystemRandomProvider(seed: 1));
            int lowUpper = 0, highUpper = 0;
            const int N = 5_000;
            for (int i = 0; i < N; i++)
            {
                if (lowLuck.Roll(Rarity.Common, 0.85f).Rarity >= Rarity.Rare) lowUpper++;
                if (highLuck.Roll(Rarity.Common, 1.3f).Rarity >= Rarity.Rare) highUpper++;
            }
            Assert.That(highUpper, Is.GreaterThan(lowUpper),
                $"Radiant luck (1.3) should yield more Rare+ than Dim (0.85). low={lowUpper}, high={highUpper}");
        }

        [Test]
        public void RollForSource_CriticalWinFloorsAtRare()
        {
            var catalog = BuildCatalog();
            var roller = new LootRoller(catalog, new SystemRandomProvider(seed: 5));
            for (int i = 0; i < 1_000; i++)
            {
                var drop = roller.RollForSource(LootRoller.Source.ShadowBattleCriticalWin);
                Assert.That((int)drop.Rarity, Is.GreaterThanOrEqualTo((int)Rarity.Rare),
                    $"Critical win floored at Rare; got {drop.Rarity} for {drop.CatalogId}");
            }
        }

        static void AssertCloseTo(float expected, float actual, float tolerance, string label)
        {
            Assert.That(System.Math.Abs(expected - actual), Is.LessThanOrEqualTo(tolerance),
                $"{label}: expected ~{expected:F4}, got {actual:F4} (tolerance ±{tolerance:F4})");
        }
    }
}
