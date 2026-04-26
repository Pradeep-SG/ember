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

### WP-10 — Hygiene

**Strings.cs lives in Kindrith.Core, not Kindrith.UI.** Brief specified `namespace Kindrith.UI`, but `RewardScreen` (in Kindrith.ShadowBattle) needs to consume the same constants and Kindrith.UI already references Kindrith.ShadowBattle — putting Strings in UI would create a cycle. Core has no dependencies, so it's the structurally correct home. File path stays `Assets/Scripts/Runtime/Core/Strings.cs`; namespace is `Kindrith.Core`.

**Phase 1 emit contract aligned with implementation.** The original analytics-taxonomy.md §2.5 listed many keys (`total_duration_ms`, `phase1_taps_*`, `clarity_awarded`, `chain_id`, `demon_id`, etc.) that Phase 1 doesn't track. WP-10 splits each event table into "P1 today (audit-pinned)" and "Phase 2 expansion" subsets. The audit test in `EditMode/P1ParameterAuditTests.cs` only enforces the P1-today set. Code-side renames done in this WP: `archetype` → `demon_archetype`, `beats_landed` → `phase3_beats_landed`, `beat` → `beat_index`. Phase enum values now lowercase via `BattlePhase.ToString().ToLowerInvariant()`. `battle_id` added to every shadow-battle emit. The dead `shadow_battle_started` emit in `Boot.OnResistRequested` (only fired when `_battleRunner == null`, didn't follow the contract) is removed; that path now logs a warning.

**`shadow_battle_abandoned` is folded into `shadow_battle_completed`.** Phase 1 emits one terminal event per battle. `outcome=abandon` plus optional `abandon_reason` carries the abandon signal. The dedicated `shadow_battle_abandoned` event is reclassified as Phase 2 work in the taxonomy doc — it'll split out when Phase 2 adds `abandoned_at_phase` + `time_in_phase_ms`.

**`Phase2Controller` constructor now takes `IClock`.** Required for `latency_ms` computation on `shadow_battle_dialogue_choice`. `BattleRunner` and the WP-08 happy-path test pass through the existing `SystemClock` / `FakeClock`.

**`DEBUG_BATTLE` define is set on `Standalone` only.** Brief asked for "the Editor and a dedicated dev iOS scheme." The Editor uses Standalone defines, so that's covered. iOS dev/release scheme split doesn't exist yet (the build pipeline ships in a later WP); the iOS define gets added at scheme creation time. Phase 3 backlog tracks this.

**CI flake (exit-139) triage outcome.** game-ci hadn't published the 6000.4.3f1 (our local) image at WP-10 time — pin stays at 6000.4.2f1. The flake didn't reproduce on the WP-10 PR's CI run, but historical recurrence rate makes a clean run inconclusive. Deferred bump to a focused follow-up once a newer image lands; `gh run rerun <id>` remains the workaround for the rare exit-139 occurrence.

### WP-11 — Resonance system

**`BattleClarityHook` lives in `ShadowBattle` but doesn't depend on Persistence.** The brief implied the hook writes `WardenRecord.clarity_expires_at` directly. Doing so would force `Kindrith.ShadowBattle` to reference `Kindrith.Persistence` and `Kindrith.Data`, expanding the assembly's surface. Instead the hook fires an `Action onClarityBuffEarned` callback; the UI layer (`BattleRunner.OnClarityBuffEarned`) owns the disk write because UI already references Persistence. Same pattern as the Resume sheet's controller wiring in WP-10.

**`Harness_Resonance.unity` scene deferred.** The brief listed it as optional ("PlayMode optional, scene-loaded in `Harness_Resonance.unity`"). Phase 2 EditMode tests cover the meter mechanics, ticker idempotency, sanctuary semantics, and battle hooks. The visual harness is a "playtest the meter color" scene that's straightforward to build later when we have the real animated bar from art-direction. Tracked as backlog rather than shipped in this WP.

**`ResonanceMeterView` is a placeholder bar.** Tier colors are derived programmatically from `Palette` (Bone-darken for Dim, Hearth for Warm, brightened-Hearth for Bright, Bone for Radiant). The Phase 3 art pass swaps in the proper animated bar from `art-direction.md`. The view subscribes to `TierChanged` so the swap will be drop-in.

**`ResonanceTuning.SanctuaryDaysMaxPerWeek` defaults are stamped onto the persisted state on first load.** `ResonanceMeter`'s constructor reads `LoadOrCreate()` → if `sanctuary_days_max_per_week == 0` (fresh record) it copies tuning defaults in. This avoids a separate "bootstrap" code path; the meter just self-corrects on first instantiation.

**`DialogueRunner.OptionTimeoutMs` migrated from `const` to instance property.** Required for `BattleFullClearBonus` to set the `1.1×` bonus. The default value lives in `DefaultOptionTimeoutMs` (still a `const`) so existing test references work.

**`BattleContext.ClarityPool` (float, 0..1) added.** Phase 2 Counter chosen → `+0.1` clamped at `1.0`; consumed downstream by Phase 3 / reward-screen logic in later WPs. Phase 1 had no Clarity pool concept; we're introducing it now.

## Lessons from Phase 1 worth carrying forward

- Hand-authored scene YAML works but is fragile across Unity reimports. Prefer programmatic UI construction with `FindAnyObjectByType` fallbacks for any SerializeField wiring.
- `EventSystem` must exist before any UI Buttons receive input. `Boot` now spawns one if missing — keep that pattern in any Phase 2 scene additions.
- `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` works in Unity 6 LTS. The `Can't Generate Mesh, No Font Asset` warnings are editor-gizmo noise from `HandleUtility`, not our text rendering.
- Game-CI `unity-test-runner@v4` exits with code 139 (Unity SIGSEGV on shutdown) intermittently. Tests pass; the shutdown crash is post-test. Re-run via `gh run rerun <id>` to land. WP-10 will try a newer Unity image to see if the flake clears.
