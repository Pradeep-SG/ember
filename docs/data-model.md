# Data model sketch

**Status:** Phase 1 = local-only, persisted to the device. Phase 3 = cloud sync via Firebase. Schemas below are designed to migrate without breaking change.

Naming: `snake_case` for fields (matches analytics), C# PascalCase at the runtime layer. `id` fields are ULIDs (time-ordered, safe for client generation, safe for later cloud sync merging). Timestamps are ISO 8601 UTC strings. All durations are integer milliseconds.

All entities carry `schema_version` (integer). A migration strategy lives in `Scripts/Runtime/Persistence/` when we get there.

---

## 1. Oath

Player's commitment to a positive habit.

```json
{
  "id": "01HW5GKZ...",                  // ULID
  "schema_version": 1,
  "title": "Read 20 minutes",           // player-authored
  "why": "I want to be a person who thinks for myself.",  // player-authored, long-form
  "cadence": {
    "kind": "daily",                    // daily | weekly | custom
    "per_week": 7,                      // 1..7 for weekly; 7 for daily
    "anchor_time_local": "21:00"        // optional preferred time
  },
  "class_id": "scholar",                // warrior | scholar | monk | ranger
  "status": "active",                   // active | paused | retired
  "created_at": "2026-04-24T10:00:00Z",
  "paused_at": null,
  "retired_at": null,
  "resonance_contribution": 1.0         // weighting factor, default 1.0
}
```

Invariant: `status` transitions are `active ↔ paused` and `active → retired`; once retired, the Oath is archived but not deleted.

---

## 2. Chain

Player's named negative habit. Summons a Demon.

```json
{
  "id": "01HW5GL0...",
  "schema_version": 1,
  "title": "Scrolling after 10pm",
  "severity_hint": "moderate",          // mild | moderate | severe — player self-report, informs Demon HP
  "demon_archetype_ids": ["voidwalker", "tender_excuse"],  // see demon-archetypes.md + §4 Demon
  "demon_id": "01HW5GL1...",            // FK to Demon entity
  "status": "active",                   // active | retired
  "created_at": "2026-04-24T10:00:00Z",
  "retired_at": null,
  "last_lapse_at": null
}
```

Invariant: exactly one active Demon per active Chain. Retiring a Chain retires its Demon.

---

## 3. Class

Enumerated, not user-created. Defines skill tree, resource affinities, Warden appearance accents.

```json
{
  "id": "scholar",
  "display_name": "Scholar",
  "resource_affinity": ["wisdom", "focus"],
  "skill_tree_id": "scholar_v1",
  "warden_accent_color": "#EDE1CC"      // Bone
}
```

MVP classes: `warrior`, `scholar`, `monk`, `ranger`. Stored statically; not user-mutable. Versioned by `skill_tree_id` for balance iteration without data migration.

---

## 4. Demon

Persistent enemy bound to a Chain. Tracks HP, encounters, and eventual death.

```json
{
  "id": "01HW5GL1...",
  "schema_version": 1,
  "chain_id": "01HW5GL0...",
  "archetype_primary": "tomorrows_warden",  // drives voice + art
  "archetype_pool": ["tomorrows_warden", "permission_giver"],  // which dialogue trees can trigger
  "hp_max": 100,                        // scales with severity_hint
  "hp_current": 73,
  "damage_dealt_total": 27,
  "regen_events": [                     // relapse log
    { "at": "2026-04-22T14:30:00Z", "hp_restored": 15, "severity": "minor" }
  ],
  "kill_criteria": {
    "consecutive_days_at_zero_hp": 7    // must stay at 0 for N days to confirm kill
  },
  "state": "alive",                     // alive | dying | slain
  "created_at": "2026-04-10T10:00:00Z",
  "slain_at": null
}
```

Invariants:
- `hp_current` ≥ 0; clamped at `hp_max`.
- On a Shadow Battle Win, `hp_current` decreases by the battle's damage.
- On a logged lapse, `hp_current` increases *proportionally* to lapse severity — never to full `hp_max` unless a catastrophic relapse is explicitly flagged.
- `state` transitions: `alive → dying` when `hp_current = 0`; `dying → slain` after 7 consecutive days at 0; `dying → alive` if any HP regen event occurs before the 7 days.

---

## 5. Shadow Battle event

Emitted on every started battle. The richest single record in the model — critical for analytics, tuning, and replay in later phases.

```json
{
  "id": "01HW5GL2...",
  "schema_version": 1,
  "chain_id": "01HW5GL0...",
  "demon_id": "01HW5GL1...",
  "demon_archetype_used": "permission_giver",
  "started_at": "2026-04-24T21:15:03Z",
  "ended_at": "2026-04-24T21:18:11Z",
  "total_duration_ms": 188420,
  "trigger": "user_resist_tap",         // user_resist_tap | dev_menu | notification_tap

  "phase1": {
    "reached": true,
    "duration_ms": 90000,
    "taps": [
      { "cycle_index": 0, "offset_ms": -120, "grade": "perfect" },
      { "cycle_index": 1, "offset_ms": 310, "grade": "clean" },
      { "cycle_index": 2, "offset_ms": null, "grade": "miss" }
    ],
    "beads_extinguished": 3
  },
  "phase2": {
    "reached": true,
    "duration_ms": 52000,
    "turns": [
      { "node_id": "node_0", "option_class": "counter", "latency_ms": 4200 },
      { "node_id": "node_2", "option_class": "deflect", "latency_ms": 9800 }
    ]
  },
  "phase3": {
    "reached": true,
    "duration_ms": 28000,
    "beats_landed": 2
  },

  "outcome": "win",                     // win | critical_win | loss | abandon
  "clarity_awarded": 1,
  "placeholder_reward_id": "shard_of_bone_placeholder"
}
```

Invariant: phases are monotonic — `phase3.reached` implies `phase2.reached` implies `phase1.reached`. Abandon can occur at any phase; the phase boundary records show where.

This schema is verbose on purpose. Phase 1's whole value is the data we collect; undercollecting here is the worst mistake we can make.

---

## 6. Habit log entry

Record of a positive-habit completion or a negative-habit lapse.

```json
{
  "id": "01HW5GL3...",
  "schema_version": 1,
  "kind": "oath_completed",             // oath_completed | chain_lapse
  "oath_id": "01HW5GKZ...",             // present for oath_completed
  "chain_id": null,                     // present for chain_lapse
  "logged_at": "2026-04-24T08:30:00Z",
  "value": {                            // shape varies by kind
    "duration_minutes": 22              // for oath_completed with a duration-Oath
  },
  "severity": null,                     // for chain_lapse: minor | moderate | major
  "note": null,                         // optional player free-text
  "reward": {
    "xp": 25,
    "loot_drop_id": "cosmetic_placeholder_042",
    "rarity": "uncommon"
  }
}
```

Phase 1 only needs `chain_lapse` entries (no Oaths shipped until Phase 2), but we ship the full schema now so Phase 2 doesn't need a migration.

---

## 7. Resonance state

Per-player meter, not per-Oath.

```json
{
  "schema_version": 1,
  "current_value": 74.2,                // 0.0 to 100.0
  "tier": "warm",                       // dim | warm | bright | radiant
  "last_updated_at": "2026-04-24T23:59:00Z",
  "decay_per_missed_day": 6.0,          // points/day, tunable
  "gain_per_strong_day": 4.0,           // points/day, tunable
  "sanctuary_days_remaining_this_week": 1,
  "sanctuary_days_max_per_week": 1,
  "last_sanctuary_used_at": "2026-04-21T00:00:00Z"
}
```

Invariants:
- `current_value` ∈ [0, 100]; never negative, never above 100.
- Decay runs at local midnight based on whether the day qualified as "strong" (≥1 Oath completed AND no unmitigated Chain lapse).
- A used Sanctuary Day halts decay for that day.
- `tier` is derived from `current_value`: dim ≤25, warm 25–60, bright 60–85, radiant >85.

---

## 8. Warden

The player's avatar. Phase 1 doesn't render the Warden on screen (no realm yet), but the record exists because onboarding (Phase 2) needs a place to stash choices.

```json
{
  "id": "01HW5GL4...",
  "schema_version": 1,
  "display_name": "Ash",
  "class_id": "scholar",                // nullable until onboarding complete
  "created_at": "2026-04-10T10:00:00Z",
  "level": 1,
  "xp_total": 0,
  "xp_in_level": 0,
  "cosmetics_equipped": {
    "outfit_id": null,
    "title_id": null,
    "realm_biome_id": null
  },
  "cosmetics_owned": [],                // loot drop IDs
  "clarity_expires_at": null            // ISO timestamp for active Clarity buff
}
```

Phase 1 fills in only `id`, `created_at`, `clarity_expires_at`. Everything else waits.

---

## 9. Realm state

Phase 1 has no realm. Schema stubbed so Phase 2 has a target.

```json
{
  "schema_version": 1,
  "biome_id": "forest_default",
  "structures": [
    { "id": "hearth", "level": 1, "dimmed": false },
    { "id": "atelier", "level": 0, "locked": true }
  ],
  "shadow_territory_pct": 0.0,          // 0..1, how much map is shadow-overrun
  "last_tended_at": null
}
```

---

## 10. Async-social reservation

Risk 4 in the roadmap: if Hearth Night (weekly) proves lonely, we may need async social features in v1.1. The data model reserves for it now so we don't retrofit later.

Reserved entity stubs (not implemented Phase 1):

```json
// LeaderboardEntry
{
  "id": "...",
  "player_id": "...",
  "metric": "resonance_peak_weekly",    // resonance_peak_weekly | shadow_battles_won_weekly | chain_longest_shadow_day
  "value": 92.1,
  "period": "2026-W17",
  "visibility": "friends"               // private | friends | public
}

// SharedRealmEvent (e.g., seasonal rituals, guild raids)
{
  "id": "...",
  "kind": "hearth_night_ritual",
  "participants": ["player_id_a", "player_id_b"],
  "period": "2026-W17",
  "state": "open"
}
```

These reserve semantic space. The Phase 1 client never reads or writes them; a Phase 3 backend may begin populating them. Design implication: **player_id** must exist from Phase 1 onward (generated client-side, stable across reinstalls — tied to iOS keychain-backed identifier).

---

## 11. Local persistence (Phase 1)

**Storage:** Unity's `Application.persistentDataPath` + a JSON-per-entity-kind layout:

```
<persistentDataPath>/kindrith/
  warden.json
  resonance.json
  realm.json
  oaths/
    <oath_id>.json
  chains/
    <chain_id>.json
  demons/
    <demon_id>.json
  shadow_battles/
    YYYY-MM/
      <event_id>.json
  habit_log/
    YYYY-MM/
      <entry_id>.json
```

Rationale: JSON-per-entity keeps merge-ability simple if we later sync to Firestore (each file maps naturally to a document). Month-sharding the event and log folders prevents directory bloat.

**Writes** are always atomic (write-to-temp, fsync, rename). We're storing "I relapsed" on someone's phone — losing a record to a crash-during-write is unacceptable.

**Reads** are lazy: only the index-level files (warden, resonance, realm) load on app start. History files load on demand.

---

## 12. Migration path to Firebase (Phase 3)

When we add cloud sync:

- Each JSON file becomes a Firestore document under `/players/{player_id}/<collection>/<doc_id>`.
- `schema_version` drives runtime migration; we never run migrations server-side on arbitrary client data.
- Conflict resolution: last-write-wins per field, with a client-side `updated_at` per entity (add to every schema **now** if not already present — we ship that before Phase 1 closes).
- Sensitive records (habit log, shadow battles — these encode sensitive behavior) encrypt at rest with a device-derived key synced through iCloud Keychain.

---

## 13. What's intentionally not here

- **Subscription / IAP state.** Phase 3 concern.
- **Notification preferences.** Phase 3 concern; keep server-side, not local.
- **Analytics event payloads.** See `analytics-taxonomy.md`. Analytics is emit-once; the canonical entity data is here.
- **Crash / error reports.** Sentry territory.

---

*Changes to this doc require a dated amendment and a `schema_version` bump on any affected entity.*
