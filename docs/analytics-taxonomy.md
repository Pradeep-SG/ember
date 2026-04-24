# Analytics event taxonomy

**Status:** spec only. Not wired to any analytics service in Phase 0 or Phase 1. Phase 2 wires the emission layer; Phase 3 ships it to a service (Amplitude or PostHog — decided then).

The point of defining this now: analytics you don't plan in advance is analytics you can't answer questions with. We lock naming and parameter shape before a single event fires.

---

## 1. Conventions

### Naming

- **`snake_case`**, always.
- **Verb-noun form**: `shadow_battle_started`, `oath_taken`, `resonance_tier_changed`.
- **Past tense for events that record a fact**: `shadow_battle_completed`, not `shadow_battle_complete`.
- **Present-imperative for state-change triggers** where the act itself is the event: `app_opened`, `app_backgrounded`.
- **No redundant scoping** — don't prefix every event with `ember_`. The service knows.

### Parameter naming

- `snake_case`.
- Units in the name where they could be ambiguous: `duration_ms`, not `duration`; `distance_m`, not `distance`.
- IDs named `<entity>_id`: `oath_id`, `chain_id`, `demon_id`, `battle_id`.
- Enums are lowercase strings; never integer codes. E.g. `outcome: "win"`, not `outcome: 1`.
- Timestamps: ISO 8601 UTC strings in the event envelope; individual parameters use `*_at` suffix.

### Common parameters on every event

Every event carries:

| Parameter | Type | Notes |
| --- | --- | --- |
| `player_id` | string (ULID) | Stable across sessions. Anonymous until account. |
| `session_id` | string (ULID) | New session on every cold start. |
| `app_version` | string | Semver. |
| `platform` | enum | `ios` for Phase 1. |
| `build_type` | enum | `debug`, `testflight`, `release`. |
| `locale` | string | BCP 47. |
| `timestamp_local` | ISO 8601 | Device local time. |
| `timestamp_utc` | ISO 8601 | Server / device UTC. |

---

## 2. Event list

Grouped by subsystem. Events marked **P1** fire in Phase 1; the rest are stubs for the phases that introduce them.

### 2.1 App lifecycle (P1)

#### `app_opened` (P1)
Fires on cold start or after >30s background.

| Parameter | Type | Notes |
| --- | --- | --- |
| `resume_reason` | enum | `cold_start`, `background_return`, `notification_tap`, `url_scheme` |
| `time_since_last_open_ms` | integer | null on first open |

#### `app_backgrounded` (P1)
Fires when the app is no longer foreground.

| Parameter | Type | Notes |
| --- | --- | --- |
| `foreground_duration_ms` | integer | |
| `on_screen` | string | The screen the player was on when they left |

---

### 2.2 Onboarding (Phase 2)

#### `onboarding_started`
#### `onboarding_step_completed`
Param: `step_id` (`warden_name`, `class_select`, `first_oath`, `first_chain`, `tutorial_battle`).
#### `onboarding_completed`
Param: `total_duration_ms`, `skipped_steps` (array).
#### `onboarding_abandoned`
Param: `last_step_id`, `time_on_last_step_ms`.

---

### 2.3 Oaths (Phase 2)

#### `oath_taken`
Params: `oath_id`, `class_id`, `cadence_kind`, `cadence_per_week`, `why_char_count`, `title_char_count`. We log character counts, not the text itself.

#### `oath_paused`
Params: `oath_id`, `days_active_before_pause`.

#### `oath_resumed`
Params: `oath_id`, `days_paused`.

#### `oath_retired`
Params: `oath_id`, `days_active_total`, `retirement_reason` (enum: `not_working`, `life_change`, `achieved`, `other`).

#### `oath_completion_logged`
Params: `oath_id`, `value_duration_minutes` (nullable), `streak_days_current`, `resonance_before`, `resonance_after`, `loot_id`, `loot_rarity`.

---

### 2.4 Chains and Demons (P1 for Chain, P1 for Demon within Shadow Battle context)

#### `chain_named` (Phase 2)
Params: `chain_id`, `severity_hint`, `title_char_count`.

#### `chain_lapse_logged` (Phase 2)
Params: `chain_id`, `demon_id`, `severity` (`minor`, `moderate`, `major`), `demon_hp_before`, `demon_hp_after`, `days_since_last_lapse`.

#### `chain_retired` (Phase 2)
Params: `chain_id`, `days_active_total`, `demon_state_at_retirement`.

#### `demon_summoned` (Phase 2)
Params: `chain_id`, `demon_id`, `archetype_pool` (array of archetype ids).

#### `demon_slain` (Phase 2)
Params: `chain_id`, `demon_id`, `days_alive`, `total_battles_fought`, `total_battles_won`.

---

### 2.5 Shadow Battle (P1 — critical)

This is the subsystem we care most about in Phase 1. Events are verbose.

#### `shadow_battle_started` (P1)
Params:
- `battle_id`
- `chain_id` (nullable in Phase 1 if no Chain system yet — still log)
- `demon_id` (nullable in Phase 1)
- `demon_archetype` (enum: `permission_giver`, `tender_excuse`, `tomorrows_warden`, `accountant`, `comparison`)
- `trigger` (enum: `user_resist_tap`, `dev_menu`, `notification_tap`)
- `time_since_last_battle_ms` (nullable)

#### `shadow_battle_phase_entered` (P1)
Params:
- `battle_id`
- `phase` (enum: `rhythm_breathing`, `choice`, `finisher`)
- `entry_context` — for Phase 2: `beads_remaining`, `clarity_pool_start`; for Phase 3: `counters_landed_in_phase2`.

#### `shadow_battle_tap_registered` (P1)
Fires on every tap during Phase 1. High-volume; batch on emission.

Params:
- `battle_id`
- `cycle_index` (0-indexed within the battle)
- `offset_ms` (relative to intended tap moment; negative = early, positive = late)
- `grade` (enum: `perfect`, `clean`, `loose`, `miss`)

#### `shadow_battle_dialogue_choice` (P1)
Params:
- `battle_id`
- `node_id` (string: `node_0`, `node_2`)
- `option_index` (0, 1, 2)
- `option_class` (enum: `counter`, `deflect`, `agree`)
- `latency_ms` (time from prompt to pick)

#### `shadow_battle_finisher_beat` (P1)
Params:
- `battle_id`
- `beat_index` (0, 1, 2)
- `landed` (bool)
- `offset_ms` (nullable when not landed)

#### `shadow_battle_completed` (P1)
Params:
- `battle_id`
- `outcome` (enum: `win`, `critical_win`, `loss`, `abandon`)
- `total_duration_ms`
- `phase1_duration_ms`, `phase2_duration_ms`, `phase3_duration_ms` (nullable if phase not reached)
- `phase1_taps_perfect`, `phase1_taps_clean`, `phase1_taps_loose`, `phase1_taps_miss`
- `phase2_counters`, `phase2_deflects`, `phase2_agrees`
- `phase3_beats_landed`
- `final_beads_extinguished`
- `clarity_awarded`

#### `shadow_battle_abandoned` (P1)
Params:
- `battle_id`
- `abandoned_at_phase` (enum)
- `abandon_reason` (enum: `user_exit`, `force_quit`, `background_timeout`)
- `time_in_phase_ms`

---

### 2.6 Resonance (Phase 2)

#### `resonance_tier_changed`
Params: `from_tier`, `to_tier`, `current_value`, `reason` (`daily_update`, `sanctuary_used`, `multi_day_gain`, `lapse`).

#### `resonance_daily_snapshot`
Fires once per local day on first app_open of the day.
Params: `value`, `tier`, `days_since_last_below_25`, `sanctuary_days_remaining`.

#### `sanctuary_day_used`
Params: `value_at_use`.

---

### 2.7 Daily loop (Phase 2)

#### `dawn_council_viewed`
Params: `quest_count`, `active_oaths_count`.

#### `dawn_council_quest_hinted_loot`
Params: `quest_id`, `hinted_rarity`.

#### `evening_tending_entered`
Params: `structures_upgraded_today`, `loot_unassigned_count`.

#### `evening_tending_completed`
Params: `duration_ms`, `actions_taken` (array of strings).

#### `hearth_night_started`
Params: `week_of_year`, `week_resonance_peak`, `boss_id`.

---

### 2.8 Narrative / UI (Phase 2+)

#### `lorebook_entry_generated`
#### `lorebook_entry_viewed`
#### `class_evolution_triggered`
#### `cosmetic_equipped`
#### `cosmetic_viewed`

---

### 2.9 Feedback and quality-of-life

#### `feedback_submitted` (P1)
Params: `feedback_kind` (`bug`, `idea`, `praise`, `other`), `char_count`.
We **do not** transmit the text itself via analytics — it lands in a separate support sink with different retention rules.

#### `crash_recovered` (P1)
Params: `last_screen`, `uptime_ms_before_crash`.

---

### 2.10 Monetization (Phase 3+)

All monetization events pre-named so tests in Phase 3 can assert the taxonomy:

- `battle_pass_viewed`
- `battle_pass_tier_progressed`
- `battle_pass_purchased` — params: `tier_free_when_purchased`, `price_usd_cents`, `currency`.
- `ember_plus_subscribed`
- `ember_plus_cancelled`
- `cosmetic_purchase_initiated`
- `cosmetic_purchase_completed`
- `iap_failed` — params: `sku`, `error_code`.

---

## 3. What we do not log

Non-negotiable:

- **Player-authored text** (Oath titles, Chain titles, Why fields, journal notes, feedback text). Character counts OK; contents never. This is sensitive behavioral data.
- **Exact relapse counts as a rate metric on a server leaderboard.** Aggregated trend dashboards only; nothing that shames individuals.
- **PII**: no name, email, phone in event params. Those live in auth / profile systems with different retention.
- **Device identifiers beyond a stable anonymous ULID.** No IDFA, no IDFV.

---

## 4. Sampling and batching

- **Tap events** (`shadow_battle_tap_registered`) are high-volume. Batch on emission: buffer in-memory, flush to the analytics SDK at phase transitions. Never drop.
- **All other events** emit one-by-one.
- **Offline queueing**: if the SDK is unreachable, persist to local queue (`<persistentDataPath>/kindrith/analytics_queue/`), flush on next online moment. Cap queue at 10,000 events (FIFO drop).

---

## 5. Governance

- Any new event or parameter requires a PR that updates *this* doc and the emission layer in the same commit.
- Dead events get deprecated, not deleted — mark `@deprecated since <version>` and stop emitting, keep the definition for 6 months so dashboards don't break silently.
- Naming review: two-reviewer rule — Pradeep + one — on every new event until we have 20 events shipped, then the naming conventions are stable enough to self-review.

---

*This taxonomy is not wired to any service in Phase 0 or Phase 1. When Phase 2 wires emission, update §2's P1 markers to match what actually fires.*
