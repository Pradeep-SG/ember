# Phase 2 backlog

This file collects work that was deferred out of Phase 1 (the Shadow Battle prototype). When a WP scope leaves something on the table, or implementation surfaces a "do this later," it lands here with a one-line note and the originating WP. Phase 2 begins after Phase 1 ships to TestFlight.

## Resolved in Phase 2

- ~~**Real ULID library**~~ — shipped in WP-09 (`Kindrith.Core.Ulid`, 26-char Crockford base32 with millisecond-monotonic ordering).
- ~~**DEBUG_BATTLE preprocessor define**~~ — shipped in WP-10. `RewardScreen.cs:115` debug block now gated by `#if DEBUG_BATTLE`; symbol set in `ProjectSettings/ProjectSettings.asset` for Standalone (covers Editor). iOS dev/release scheme split tracked in `phase3-backlog.md`.
- ~~**Sub-threshold Resume sheet UI**~~ — shipped in WP-10 (`UI/ResumeOrAbandonSheet.cs`). Resume re-arms phase clocks via `BattleStateMachine.Resume(durMs)` → `Resumed` event; controllers + `DialogueRunner` bump their start anchors. Abandon routes to `sm.Abandon("user_chose_abandon")`.
- ~~**Phase 1 `app_backgrounded` foreground duration accuracy**~~ — shipped in WP-10. `Boot.OnApplicationPause(true)` now computes from `_lastAppOpenedUtc` (a `DateTime` recorded on every `app_opened` emit) via the testable `Boot.ComputeForegroundMs` static helper.
- ~~**Analytics taxonomy alignment**~~ — shipped in WP-10. Code-side renames (`archetype` → `demon_archetype`, `beats_landed` → `phase3_beats_landed`, `beat` → `beat_index`); `battle_id` added to every shadow-battle emit; phase enum values lowercased. `docs/analytics-taxonomy.md` §2.5 split into "P1 today (audit-pinned)" and "Phase 2 expansion" subsets. `EditMode/P1ParameterAuditTests.cs` pins the contract.
- ~~**Brief/spec corrections (8 nodes, 800 ms hold, event count)**~~ — shipped in WP-10. `docs/phase1-shadow-battle-spec.md` §3.1 explicitly states 8 nodes per tree; §4.1 affirms 800±120 ms hold; new §6.1 enumerates the 9 distinct Phase 1 event names with the 11+-row count for a clean win.
- ~~**CI flake (exit 139) investigation**~~ — investigated in WP-10. game-ci hadn't published 6000.4.3f1 (our local) at WP-10 time; pin stays at 6000.4.2f1, `gh run rerun <id>` is still the workaround. Bump deferred to a focused follow-up once a newer image lands. Detail in `docs/phase2-implementation-notes.md` WP-10 section.

## Still deferred

### Production battle UI (WP-08 polish, mostly shipped — these are stragglers)
- **Bootstrap → ShadowBattle additive load**: brief WP-08 acceptance specifies additive scene loading, but Bootstrap ships as a single-scene shell. Once a separate `ShadowBattle.unity` is needed (currently programmatic UI is fine), switch Bootstrap to load it additively on Resist-tap.

### Infrastructure
- **Real analytics service**: `NdjsonAnalyticsSink` writes to local storage only; Phase 2 wires Amplitude or PostHog behind the `IAnalyticsSink` interface. The bus/envelope split is already in place. Owner: WP-19 / WP-20 closeout.

### Battle behaviour
- **Phase 1 full-clear bonus**: spec §2 mentions a "+10% Phase 2 timer" Clarity-early bonus when beads extinguish before 90 s. Owner: WP-11 (Resonance system) wires this via `BattleFullClearBonus`.
- **Phase 2 Counter/Agree resonance side-effects**: spec §3 lists Counter `+0.1 Clarity` and Agree `+1 bead (regen)`. WP-05 emits the dialogue_choice event but doesn't apply the bead/clarity effect. Owner: WP-11 (`BattleClarityHook`).

### Dialogue
- **Per-archetype mid-battle switching**: archetype-doc §5 — let the player name their Demon and tie archetypes to Chain types. Owner: WP-15 onward.
- **Variation in opening lines**: 2-3 alternate Node_0 lines per archetype to reduce week-2 repetition.
- **Personalization** of demon names.

### Phase 1 emit expansion (Phase 2 work — currently marked "P2 expansion" in analytics-taxonomy.md §2.5)
- `total_duration_ms`, `phase1/2/3_duration_ms` on `shadow_battle_completed` — needs duration tracking on `BattleContext`.
- `phase1_taps_perfect/clean/loose/miss` counters — needs aggregation on `Phase1Controller`.
- `phase2_counters/deflects/agrees` counters — already partially tracked via `_context.CountersInPhase2`, needs deflects + agrees too.
- `clarity_awarded` — needs Resonance/Clarity system (WP-11).
- `chain_id`, `demon_id` on `shadow_battle_started` — needs Chain/Demon entities (WP-14 onboarding wires the first instances).
- `time_since_last_battle_ms` — needs a "last battle ended" timestamp store.
- Dedicated `shadow_battle_abandoned` event — split from the current `shadow_battle_completed` once the richer payload is needed.
