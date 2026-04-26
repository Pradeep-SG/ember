# Loot specification

**Status:** authored in WP-13. Variable-rarity drops on every battle Win and (once WP-15 wires habit logging) every Oath completion. Phase 2 ships a 15-item placeholder catalog skewed cosmetic per Pillar 3.

## 1. Catalog

`Settings/Loot/Phase2Catalog.asset` (`Kindrith.Loot.LootCatalog`). **Exactly 15 items**: 12 cosmetic, 3 functional (matches the 80/20 directional ratio at small N).

### Inventory

| Rarity | # | Items |
| --- | --- | --- |
| Common (5) | 4 cosmetic + 1 functional | Warm Lantern · Scholar's Shawl · Weathered Path Stone · "Quiet Resolve" title · Resonance Seed (Minor) |
| Uncommon (4) | 3 cosmetic + 1 functional | Dim Brazier · Warden's Sash · "Threshold Keeper" title · Clarity Pinch |
| Rare (3) | 2 cosmetic + 1 functional | Grove Seedling · Ember Cloak · Sanctuary Reprieve |
| Epic (2) | 2 cosmetic | Constellation Thread · Runed Mantle |
| Legendary (1) | 1 cosmetic | Dawn Chime |

**Functional items live only in Common/Uncommon/Rare.** Epic and Legendary are cosmetic-only — the high-tier carrots that drive Pillar 3. The editor menu `Tools/Kindrith/Validate Phase 2 Loot Catalog` warns if this drifts.

Display names + icon paths are placeholders; visuals land alongside WP-15's loot-drop animation.

## 2. Rarity distribution

Base weights (Resonance tier `Warm`, the player's first non-zero state):

| Rarity | Weight |
| --- | --- |
| Common | 0.60 |
| Uncommon | 0.25 |
| Rare | 0.12 |
| Epic | 0.025 |
| Legendary | 0.005 |

Σ = 1.0. The pull on the Legendary slot is intentionally rare — 1 in 200 rolls at Warm, ~1 in 154 at Radiant — so the high tier reads as "earned" and not as a daily expectation.

## 3. Resonance luck multiplier

Applied to Uncommon/Rare/Epic/Legendary weights, then re-normalized so Σ stays at 1.0. Common takes the displaced mass (or gives it back, if luck < 1.0).

| Resonance tier | `resonanceLuckMultiplier` |
| --- | --- |
| Dim | 0.85 |
| Warm | 1.00 |
| Bright | 1.15 |
| Radiant | 1.30 |

The multiplier is intentionally gentle — the spec is loss-averse but kind: even at Radiant the player isn't drowning in Legendaries; they're just slightly more likely to see the rare cosmetics they're trying to earn.

## 4. Floor per source

`LootRoller.RollForSource(string source, …)` looks up:

| Source | Floor | Functionals allowed? |
| --- | --- | --- |
| `oath_completion` | Common | **No** |
| `shadow_battle_win` | Uncommon | Yes |
| `shadow_battle_critical_win` | Rare | Yes |

**Floor logic:** rarities below the floor collapse into the floor's bucket. So `oath_completion` rolls the full distribution; `shadow_battle_win` zeros out Common (0.60 mass joins Uncommon's 0.25 → 0.85 then renormalize); Critical Win zeros out Common + Uncommon (0.85 mass joins Rare's 0.12 → 0.97 then renormalize).

**The functional-on-oath ban preserves Pillar 3:** real-world habit completions are rewarded with cosmetics ("becoming someone"), never with stat micro-bonuses ("gaining a number"). Functionals only ride along on the battle-Win track, where they read as ammunition for the next fight rather than a habit-tracker stat trickle.

## 5. Public API

```csharp
namespace Kindrith.Loot
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public readonly struct LootDrop
    {
        public readonly string CatalogId;
        public readonly Rarity Rarity;
        public readonly bool IsFunctional;
        public readonly string Source;
    }

    public sealed class LootRoller
    {
        public LootRoller(LootCatalog catalog, IRandomProvider rng);

        // Source-aware: floors + functional gate per §4.
        public LootDrop RollForSource(string source, float resonanceLuckMultiplier = 1f);

        // Raw entry point — useful for tests and dev menus.
        public LootDrop Roll(Rarity floor = Rarity.Common, float resonanceLuckMultiplier = 1f,
            bool functionalsAllowed = true, string source = null);

        public static class Source
        {
            public const string OathCompletion = "oath_completion";
            public const string ShadowBattleWin = "shadow_battle_win";
            public const string ShadowBattleCriticalWin = "shadow_battle_critical_win";
        }
    }
}
```

## 6. Battle integration

`BattleRunner.RollBattleLoot` (called from `OnPhase3Complete` after XP grant):

- Picks the source from `BattleContext.Outcome` (Win or CriticalWin → roll; Loss/Abandon → no drop).
- Reads the current Resonance tier from the meter (lowercased), passes through `LootRoller.LuckMultiplierForTier`.
- Calls `RollForSource(source, luck)`. Emits `loot_rolled` analytics with `source`, `catalog_id`, `rarity`, `is_functional`, `luck_multiplier`.
- The drop event lands in the analytics sink. WP-15 wires the on-screen loot-drop animation and the inventory grant; for now the drop is recorded in analytics only.

Habit-completion drops wire in WP-15.

## 7. Out of scope

- **IAP-purchased cosmetics** — Phase 3.
- **Battle-pass-only items** — Phase 3.
- **Equipping / displaying cosmetics on the realm** — WP-17 owns that; this WP ends at the drop-and-record step.
- **Real cosmetic icons** — placeholder string `IconPath`s only. The view layer (WP-15) resolves them; Phase 3 art pass produces final assets.
- **CSV/JSON catalog importer** — `Editor/LootCatalogImporter.cs` ships as a validation stub. A real importer lands when the catalog grows past hand-edit comfort.
