# Progression specification

**Status:** authored in WP-12. Phase 2 ships levels 1–10 with one class evolution at Lv 10. Pillar 1 ("identity-first") is the design constraint — level-ups must read as *becoming someone* (the Scholar visual swap is the primary signal), not as a stat upgrade.

## 1. State and storage

Lives on `WardenRecord` (`<persistentDataPath>/kindrith/warden.json`):

- `level: int` — current level. Defaults to 1.
- `xp_total: int` — cumulative XP earned, ever. Caps at the slice ceiling (Lv 10).
- `xp_in_level: int` — XP since the last level threshold. Recomputed on every grant.

The cumulative-plus-derived layout lets us recompute `xp_in_level` from `xp_total` and the curve at any point — no risk of the two drifting apart.

## 2. XP curve

`Kindrith.Progression.ProgressionTuning` (ScriptableObject; default at `Assets/Settings/Progression/DefaultProgressionTuning.asset`).

`XpToReachLevel[N]` = cumulative XP required to *reach* level N. `XpToReachLevel[1] = 0` (every player starts at Lv 1). `XpToReachLevel[11] = XpToReachLevel[10]` caps the slice (Lv 11+ deferred per WP-12 out-of-scope).

| Level | Cumulative XP | Slice Δ |
| --- | --- | --- |
| 1 | 0 | — |
| 2 | 100 | 100 |
| 3 | 220 | 120 |
| 4 | 370 | 150 |
| 5 | 550 | 180 |
| 6 | 770 | 220 |
| 7 | 1030 | 260 |
| 8 | 1330 | 300 |
| 9 | 1670 | 340 |
| 10 | 2050 | 380 |

**Cap derivation:** target ~14 days of average play = 5 quest completions/day × 14 days × 25 XP = 1750, plus a handful of battle wins (~5 wins × 60 XP = 300) → ~2050. The slice deltas grow gently so the early levels feel snappy and the late levels feel earned without becoming a grind.

## 3. XP sources

Per-source defaults on `ProgressionTuning`:

| Source | XP | Notes |
| --- | --- | --- |
| Oath completion | 25 | Phase 2 wires in WP-15 (habit logging). |
| Shadow Battle Win | 60 | Wired in WP-12: `BattleRunner.GrantBattleXp`. |
| Critical Win | 100 | Same. |
| Loss / Abandon | 0 | No XP. Pillar 2 prohibits XP penalties; this is "no reward," not "punishment." |

**No skill-tree node selection in Phase 2.** Phase 3 may add it.

## 4. `GrantXp` semantics

`Levels.GrantXp(int amount, string source)`:

1. Adds `amount` to `xp_total`.
2. Walks up one level at a time while `xp_total >= XpToReachLevel[level + 1]`. Each crossing fires `LeveledUp(from, to)`. A single grant that crosses two levels fires two events — never batched.
3. At Lv 10 with overflow, `xp_total` is clamped to the slice ceiling so `xp_in_level` stays meaningful (and the bar reads "Max").
4. On crossing into Lv 10, `ScholarEvolution.Apply` runs: stamps `WardenRecord.cosmetics_equipped.outfit_id = "scholar_evolved_v1"` and fires `ScholarEvolved(newLevel)`. Emits `class_evolution_triggered` (analytics-taxonomy.md §2.8).
5. Persists the WardenRecord via `WardenStore.Save`.
6. Emits `xp_granted` with `xp_amount`, `source`, `level_after`, `xp_total_after`, `levels_crossed`. (WP-20 will fold these into the analytics-taxonomy contract.)

## 5. Scholar evolution

Phase 2 ships one class evolution at Lv 10 — the Scholar visual swap. The evolved cosmetic id is reserved as `"scholar_evolved_v1"` in `ScholarEvolution.EvolvedOutfitId`. The Phase 2 hearth view (WP-17) reads from `WardenRecord.cosmetics_equipped.outfit_id` to render the swap.

The evolution event fires once when crossing into Lv 10. If the Warden was already Lv 10 on app start, the event does not re-fire (we only emit on the level-crossing edge).

## 6. View layer

`Kindrith.UI.XpBarView` mounts under the home canvas above the Resonance bar. Subscribes to `Levels.LeveledUp` and refreshes; `HomeShell.RefreshXpBar()` is callable for within-level updates after a grant. Lv 10 renders as `"Lv 10 (Max)"`.

## 7. Out of scope

- **Lv 11+** — Phase 2 caps at 10. Phase 3 reopens.
- **Skill-tree node selection** — Phase 3.
- **XP penalties** — Pillar 2 forbids.
- **Multi-class evolutions** — Phase 2 has one (Scholar). Other classes ship in Phase 3.
- **`xp_granted` / `level_up` taxonomy entries** — emitted in Phase 2 but formal taxonomy refresh deferred to WP-20 alongside the analytics expansion.
