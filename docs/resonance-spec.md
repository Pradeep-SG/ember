# Resonance specification

**Status:** authored in WP-11. The Resonance meter is the player's at-a-glance signal of how alive their practice is — it climbs with strong days and decays on missed days. The decay is *visible*, not punishing: the bar dims, but the realm stays. Pillar 2 ("loss-averse but kind") is the design constraint — Resonance is a reflection, not a stick.

## 1. State and storage

`<persistentDataPath>/kindrith/resonance.json` (single record per player). Schema: `Kindrith.Data.ResonanceState` (see `data-model.md` §7).

- `current_value: float` ∈ [0, 100]; clamped at both ends.
- `tier: string` — derived; `dim | warm | bright | radiant`.
- `sanctuary_days_remaining_this_week: int` — counter, reset on local Monday 04:00.
- `last_sanctuary_used_at: ISO 8601`.

## 2. Tuning constants

`Kindrith.Resonance.ResonanceTuning` (ScriptableObject; instance shipped at `Assets/Settings/Resonance/DefaultResonanceTuning.asset`).

| Field | Default | Phase 3 server-tunable? |
| --- | --- | --- |
| `DecayPerMissedDay` | 6.0 | Yes |
| `GainPerStrongDay` | 4.0 | Yes |
| `DimMaxValue` | 25 | No (UX semantics anchor here) |
| `WarmMaxValue` | 60 | No |
| `BrightMaxValue` | 85 | No |
| `SanctuaryDaysMaxPerWeek` | 1 | Yes |

Phase 3 will source the tunable values from a server config so we can A/B without app updates. Tier breakpoints stay client-side because they're tied to the meter's visual states.

## 3. Decay / gain rule

Run by `DailyResonanceTicker.TickIfNeeded()` on app foreground. Idempotent within a local day, walks day-by-day across multi-day gaps.

For each `prevDay`:

- If a Sanctuary was used on `prevDay` → no change.
- **Strong day** (`prevDay` had ≥1 `oath_completed` AND no `chain_lapse_logged` of severity ≥ moderate) → `+GainPerStrongDay`.
- **Missed day** (no `oath_completed` and no Sanctuary) → `-DecayPerMissedDay`.
- Otherwise (e.g., partial day, low-severity lapse) → no change.

Reset of the weekly sanctuary counter happens whenever the cursor crosses into a Monday (local time). This is conservative — if the app sat backgrounded for a full week, the cursor walks each Monday and resets each time, but the counter is set, not incremented, so the result is correct.

## 4. Tier transitions

Tier is derived from `current_value`:

| Tier | Range |
| --- | --- |
| `Dim` | ≤ 25 |
| `Warm` | (25, 60] |
| `Bright` | (60, 85] |
| `Radiant` | > 85 |

`ResonanceMeter.TierChanged` fires `(from, to)` whenever `current_value` crosses a breakpoint. Phase 3 will emit `resonance_tier_changed` and `resonance_daily_snapshot` analytics events (Phase 2 records them locally only).

## 5. Sanctuary Day

A free day off without decay penalty. `RecordMissedDay` is *not* called for a day where Sanctuary is active.

- `UseSanctuary()` decrements `sanctuary_days_remaining_this_week` and stamps `last_sanctuary_used_at = UtcNow`.
- The ticker checks `last_sanctuary_used_at.ToLocalTime().Date == prevDay.Date` before applying decay/gain rules.
- The counter resets weekly on Monday 04:00 local time. Hard-coded; not server-tunable.

## 6. Battle hooks

Phase 1 deferred Counter/Agree side-effects and the Phase-1 full-clear bonus. WP-11 wires them.

### `BattleClarityHook` (Kindrith.ShadowBattle/BattleClarityHook.cs)

Subscribes to `DialogueRunner.OptionChosen` and `BattleStateMachine.Transitioned`:

- **Counter** chosen → `BattleContext.ClarityPool += 0.1` (clamped to `[0, 1.0]`).
- **Agree** chosen → `Beads.Regen(1)` (clamped to `Beads.Max`).
- On transition into `Outcome` with `Win` or `CriticalWin` → fires the `onClarityBuffEarned` callback. The hook does not write to disk directly; the UI layer (`BattleRunner`) owns the `WardenRecord.clarity_expires_at = UtcNow + 24h` write so `ShadowBattle` doesn't depend on `Persistence`.

### `BattleFullClearBonus` (Kindrith.ShadowBattle/BattleFullClearBonus.cs)

Static helper. `ApplyTo(context, runner)` checks `Phase1ElapsedMs < 90_000 AND DemonBeads.Extinguished` and, if both are true, sets `runner.OptionTimeoutMs = DefaultOptionTimeoutMs * 1.1` (22 s instead of 20 s). `BattleRunner` calls this once before constructing `Phase2Controller`.

## 7. Out of scope

- **Multi-Oath weighting** — `data-model.md` §1's `resonance_contribution` is hard-coded to 1.0 in Phase 2; one Oath completion = one strong-day gate.
- **Per-class bonuses to Resonance** — Phase 3.
- **`resonance_tier_changed` / `resonance_daily_snapshot` analytics emission** — wired in WP-19/WP-20 alongside the analytics-service hookup.
- **Animated meter visuals** — Phase 3 swaps in the bar from `art-direction.md`. WP-11 ships a static bar.

## 8. View layer

`Kindrith.UI.ResonanceMeterView` mounts under the home canvas above the title. Subscribes to `ResonanceMeter.TierChanged` and repaints the fill width + tier color. Tier colors are placeholders pulled from `Palette` (Bone-darken for Dim, Hearth for Warm, Hearth-bright for Bright, Bone for Radiant). Phase 3 art replaces them.
