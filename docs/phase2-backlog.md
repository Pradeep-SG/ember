# Phase 2 backlog

This file collects work that was deferred out of Phase 1 (the Shadow Battle prototype). When a WP scope leaves something on the table, or implementation surfaces a "do this later," it lands here with a one-line note and the originating WP. Phase 2 begins after Phase 1 ships to TestFlight.

## Deferred from Phase 1

### Production battle UI (WP-08 polish)
- **Phase 1 tap input**: real `Input System`-driven tap detection wired into `Phase1Controller.RegisterTap`. WP-03's `TapDebugLogger` shows the pattern; WP-08 doesn't ship the production version.
- **Phase 2 dialogue UI**: render the demon line + 3 option buttons (Counter / Deflect / Agree) per `DialogueRunner.Current.Options`. Hook to `DialogueRunner.Choose(idx)`.
- **Phase 3 finisher cues**: animated visual cues for ExpandTap (expanding ring), ContractTap (contracting ring), HoldRelease (1-second hold ring). Hook to `FinisherSequencer.RegisterTap` / `RegisterHoldStart` / `RegisterHoldRelease`.
- **Bootstrap → ShadowBattle additive load**: brief WP-08 acceptance specifies additive scene loading, but Bootstrap ships as a single-scene shell. Once ShadowBattle.unity has the production battle UI, switch Bootstrap to load it additively on Resist-tap.

### Infrastructure
- **Real ULID library**: `BattleContext.BattleId` and `BattleRecord.id` use `Guid.NewGuid().ToString("N")` — fine for Phase 1 uniqueness but the data-model spec calls for ULIDs (sortable by time). Drop in a ULID lib (or write a small one) before networked analytics.
- **Real analytics service**: `NdjsonAnalyticsSink` writes to local storage only; Phase 2 wires Amplitude or PostHog behind the `IAnalyticsSink` interface. The bus/envelope split is already in place.
- **DEBUG_BATTLE preprocessor define**: brief WP-07 mentions `DEBUG_BATTLE` for the reward screen debug block; current impl uses `Debug.isDebugBuild` as the gate. Switch to the explicit symbol once the build matrix has dev/testflight/release variants.
- **CI flake (exit 139) investigation**: Unity Test Runner intermittently exits 139 on shutdown after tests pass. Re-running the action passes. If it persists into Phase 2, file an issue against `unity-test-runner@v4` or pin a different Unity image.

### Battle behaviour (touched by spec but kept Phase-1-narrow)
- **Phase 1 full-clear bonus**: spec §2 mentions a "+10% Phase 2 timer" Clarity-early bonus when beads extinguish before 90 s. Not implemented in WP-04 — Phase 2 builds on this.
- **Phase 2 Counter/Agree resonance side-effects**: spec §3 lists Counter `+0.1 Clarity` and Agree `+1 bead (regen)`. WP-05 emits the dialogue_choice event but doesn't apply the bead/clarity effect — Phase 2 wires it up alongside the Clarity buff system.
- **Sub-threshold Resume sheet UI**: `BattleStateMachine.HandleBackgroundResume` correctly Abandons after 60 s, but the `< 60 s → show "Resume or Abandon?" sheet` UI is brief WP-04 deferred to WP-08 / Phase 2.
- **Phase 1 `app_backgrounded` foreground duration accuracy**: current impl counts from `Time.realtimeSinceStartup` at scene start; should use `app_opened` timestamp once we have a real timestamp store.

### Dialogue
- **Per-archetype mid-battle switching**: archetype-doc §5 — let the player name their Demon and tie archetypes to Chain types.
- **Variation in opening lines**: 2-3 alternate Node_0 lines per archetype to reduce week-2 repetition.
- **Personalization** of demon names.

### Analytics taxonomy alignment
- **Audit emitted parameters** against `docs/analytics-taxonomy.md` per-event tables once Phase 2 wiring lands. Phase 1 emissions are reasonable but haven't been line-by-line compared.

### Brief / spec corrections to file
- "Each archetype tree → **8** nodes" (brief WP-05 currently says 7) — see WP-05 commit message.
- Phase 3 hold-release tolerance test cases — brief WP-06 examples contradict the canonical `800 ± 120` rule. Pick one.
- "exactly 8 events" in WP-08 — actually emits 11+. Update brief to match implementation.
