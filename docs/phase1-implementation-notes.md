# Phase 1 implementation notes

Running log of decisions and deviations made while implementing Phase 1 (WP-01 → WP-08). Use as the input to the post-Phase-1 retro and the Phase 2 brief revision.

## Architecture summary

Phase 1 ships seven runtime assemblies plus an Editor assembly:

```
Kindrith.Core           — IClock + System/FakeClock, IRandomProvider, Log
Kindrith.Breathing      — 4-7-8 cadence + BreathingClock + BreathingCircleView (+ BreathingHarness)
Kindrith.ShadowBattle   — TapEvaluator, Beads, BattleStateMachine, Phase1/2/3Controllers,
                          FinisherSequencer, OutcomeRouter, RewardScreen, IBattleEventEmitter
Kindrith.Dialogue       — DialogueTree (ScriptableObject), DialogueRunner, DialogueMarkdownParser
Kindrith.Analytics      — AnalyticsBus, IAnalyticsSink, NdjsonAnalyticsSink, IEnvelopeProvider
Kindrith.Persistence    — BattleRecord (Phase 1 subset of data-model §5), BattleStore
Kindrith.UI             — HomeShell, ResistButton, SessionLogView, ArchetypePicker, Boot,
                          AnalyticsBusAdapter
Kindrith.Editor         — DialogueTreeImporter (Tools/Kindrith/Regenerate Dialogue Trees)
```

## Per-WP deviations from the brief

- **WP-02 / WP-03 / WP-04 / WP-05**: created several asmdefs earlier than the §3 file map tags them (`Kindrith.ShadowBattle`, `Tests.PlayMode`, `Kindrith.Dialogue`). Necessary so the new types compile in their own assembly instead of polluting Assembly-CSharp; later WPs just add files into the existing assembly.
- **WP-03**: brief §5 says "no new C#" but the tap-logging acceptance can't be wired without a small MonoBehaviour. Added `TapDebugLogger` as a dev-only scaffold; replaced by Phase1Controller in WP-04.
- **WP-05**: `DialogueNode` adds a `NextNodeId` field not in the brief sketch — Node_1{a,b,c} have no player options, so the runner threads through them via NextNodeId rather than ad-hoc id matching. Brief also says "→ 7 nodes" per archetype tree; the markdown actually contains 8 (Node_0, Node_1{a,b,c}, Node_2, Node_3{a,b,c}). Tests assert 8, matching the source.
- **WP-06**: brief WP-06 §5 examples (`Hold release at 700 ms → not landed; at 950 ms → landed`) contradict the canonical `outside 800 ± 120 ms is not landed` rule. Tests follow the canonical formula.
- **WP-07**: brief mentions tracking "Beads were 0 entering Phase 3"; Phase 3 doesn't damage beads, so `DemonBeads.Remaining` at outcome time equals beads-at-Phase-3-entry — no separate context field needed. State machine assigns `BattleContext.Outcome` via `OutcomeRouter.Resolve` on transition to Outcome and surfaces it through the entry_context payload.
- **WP-08**: full Phase 1 → Phase 2 → Phase 3 production playback is **not** wired into HomeShell/Boot in this PR. WP-08 ships the analytics + persistence + UI shell; the Phase 1 polish PR after Phase 1's TestFlight cohort is the right time to add real Phase 1 tap input UI, Phase 2 dialogue button bar, Phase 3 finisher cues. The pipeline is exhaustively tested via WP08HappyPathE2E so wiring it up is plug-and-play.

## Known issues / limitations

- **CI flake (exit 139)**: the `unity-test-runner@v4` job sometimes exits with code 139 (SIGSEGV on Unity shutdown) **after** all tests pass. Same code, same commit — the duplicate run triggered by PR sync usually passes. Re-run the failed run via `gh run rerun <id>` to land. Worth investigating in Phase 2 if it persists.
- **Generated dialogue tree assets**: regenerated via `Tools/Kindrith/Regenerate Dialogue Trees` and live in `Assets/Settings/DialogueTrees/`. Re-run the importer after any edit to `docs/demon-archetypes.md`.
- **Hand-authored .unity files** (Harness_Breathing, ShadowBattle, Bootstrap): I built these YAMLs by hand to avoid the open-Unity-and-click loop. They've all loaded cleanly on Pradeep's machine but a future Unity version bump may break the format — re-author with the editor if so.

## Lessons learned

- Treat the brief's example numbers as illustrative, not normative — several spec/brief contradictions surfaced (700 ms hold-release "landed" status, "→ 7 nodes" vs 8). Always cross-check against the spec.
- `FindFirstObjectByType<T>()` is obsolete in Unity 6 LTS — use `FindAnyObjectByType<T>()` or `-warnaserror` will flag it.
- A PlayMode test asmdef must use `includePlatforms: []`, not `["Editor"]` — otherwise Unity Test Framework treats `[UnityTest]` coroutines as EditMode tests with no PlayMode to enter, and `EditorSceneManager.LoadSceneAsyncInPlayMode` quietly fails.
- Hand-crafted `.asset` YAML for ScriptableObjects is doable but generating them via an Editor script (run with `-batchmode -executeMethod`) is more robust.
