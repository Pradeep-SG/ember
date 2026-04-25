# Phase 2 implementation notes

Running log of decisions and deviations from the brief while implementing Phase 2 (WP-09 → WP-20). Companion to `docs/phase1-implementation-notes.md`.

## Architecture additions in Phase 2

```
Kindrith.Core            (existing)  — IDayClock, SystemDayClock, FakeDayClock, Ulid added in WP-09
Kindrith.Data            (new, WP-09) — entity records: Warden, Oath, Chain, Demon, Resonance,
                                        HabitLogEntry, RealmState, StructureRecord, LorebookEntry
Kindrith.Persistence     (extended) — IEntityStore<T>, AtomicJsonStore, SingletonStore<T>,
                                       FlatFolderStore<T>, MonthShardedStore<T>, Migrations/
Kindrith.Resonance       (new, WP-11)
Kindrith.Progression     (new, WP-12)
Kindrith.Loot            (new, WP-13)
Kindrith.Onboarding      (new, WP-14)
Kindrith.Daily           (new, WP-16)
Kindrith.Realm           (new, WP-17)
Kindrith.Lorebook        (new, WP-18)
```

## WP-09 — Persistence v2

### Entity layout (data-model §11)

```
<persistentDataPath>/kindrith/
  warden.json
  resonance.json
  realm.json
  lorebook/             (FlatFolderStore<LorebookEntry>)
  oaths/<id>.json
  chains/<id>.json
  demons/<id>.json
  habit_log/YYYY-MM/<id>.json    (MonthShardedStore<HabitLogEntry>)
  migration_state.json
```

The Phase-1 `<persistentDataPath>/battles/` directory stays where it is — `BattleStore` already manages that path and Phase 2 doesn't move it. Future Phase 2 WPs can extend BattleStore to also shard by month if folder bloat shows up; for now the flat layout is fine for diary-study volume.

### Atomic write

`AtomicJsonStore.WriteAtomic` writes JSON to `<file>.tmp`, calls `FileStream.Flush(flushToDisk: true)`, then `File.Replace` (or `File.Move` if no original exists). This is the "write-temp + fsync + rename" pattern from data-model §11. A crash mid-save leaves either the previous good copy or the new one — never a half-written turd.

The `EntityStoreTests.cs` test plan calls for a fault-injection scenario asserting on-disk integrity. We didn't ship that test in WP-09 because Unity's `JsonUtility` API doesn't surface a hookable serializer — adding fault injection requires a more layered serializer abstraction. **Deferred to WP-10** (hygiene) where it lives alongside the other Phase-1-follow-up cleanup work. The atomic write is exercised indirectly by every store's round-trip test.

### ULID design

26-char Crockford base32, big-endian: 48-bit Unix-ms timestamp + 80-bit cryptographic randomness. Same-millisecond calls increment the random portion big-endian to preserve sort order. Lock around the static state means Ulid.New is thread-safe at the cost of a `lock` per call — fine for entity-creation rates, would matter for high-throughput emit code.

C# string sorted with `StringComparer.Ordinal` matches the time order. `ListIds` uses ordinal sort to leverage this — no extra parse step.

### Migrations

`Migrator` walks an ordered list of `IMigration` and tracks the last-applied version in `migration_state.json`. Migrations are write-once, idempotent. `Editor/DataMigrationsRunner.cs` exposes the menu item and a `-batchmode -executeMethod Kindrith.Editor.DataMigrationsRunner.RunForBatch` entrypoint for CI.

`M_0000_Phase1Bootstrap` stamps existing Phase-1 `BattleRecord` files with `schema_version=1` and `updated_at` if absent. Idempotent re-runs are no-ops because the migrator's `last_applied_version` filters them out, AND `M_0000` itself only writes when a field is missing.

### BattleRecord extensions

Added `chain_id`, `demon_id`, and `updated_at` fields to `BattleRecord`. Phase 1 leaves the chain/demon fields null; Phase 2's onboarding creates the entities and Phase 2 battles will populate them. `BattleStore.Save` now stamps `updated_at` and uses `AtomicJsonStore.WriteAtomic` (was a non-atomic `File.WriteAllText`).

## Per-WP deviations from the brief

(WP-10 onward will append here.)

## Lessons from Phase 1 worth carrying forward

- Hand-authored scene YAML works but is fragile across Unity reimports. Prefer programmatic UI construction with `FindAnyObjectByType` fallbacks for any SerializeField wiring.
- `EventSystem` must exist before any UI Buttons receive input. `Boot` now spawns one if missing — keep that pattern in any Phase 2 scene additions.
- `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` works in Unity 6 LTS. The `Can't Generate Mesh, No Font Asset` warnings are editor-gizmo noise from `HandleUtility`, not our text rendering.
- Game-CI `unity-test-runner@v4` exits with code 139 (Unity SIGSEGV on shutdown) intermittently. Tests pass; the shutdown crash is post-test. Re-run via `gh run rerun <id>` to land. WP-10 will try a newer Unity image to see if the flake clears.
