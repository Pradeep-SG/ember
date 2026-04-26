# Onboarding specification

**Status:** authored in WP-14. First-run flow drives the player from a cold install to "I have an Oath, a Chain, a Demon, and I just won my first battle." Idempotent on app reopen mid-flow per Pillar 1 ("the friction is the product").

## 1. Steps

Six steps, strictly serial:

| # | Step | Stores written | Acceptance |
| --- | --- | --- | --- |
| 1 | `WardenName` | WardenStore | DisplayName 2–24 chars, trimmed of leading whitespace |
| 2 | `ClassSelect` | WardenStore | Phase 2 ships only Scholar; the other three render as "Coming in v1.1" disabled cards |
| 3 | `FirstOath` | OathStore | Pre-fill "Read 20 minutes" / "Because the person I'm becoming reads."; player can edit before taking |
| 4 | `FirstChain` | ChainStore | Pre-fill "Scrolling after 10pm"; severity hint defaults to `moderate` |
| 5 | `DemonSummon` | DemonStore | The Voidwalker — manifests from the Chain. archetype_primary = `permission_giver` (the only Phase-1-shipped tree the tutorial can use). chain_id back-stamped onto the Chain. |
| 6 | `TutorialBattle` | BattleStore | Phase 1 capped at 45 s soft cap. archetype forced to `permission_giver`. Outcome screen branches to "Welcome to your realm" instead of returning to Home. |

Skip rules: **none**. Onboarding cannot be skipped in v1; only paused via app-quit. Phase 3 may add a "skip — I've done this before" path tied to migrated accounts.

## 2. Idempotency

`OnboardingFlow.CurrentStep` is **derived** from store contents on each access — there is no completion flag. The stores are the source of truth.

```
WardenStore.LoadOrCreate().display_name empty  → WardenName
class_id empty                                 → ClassSelect
OathStore.ListIds() empty                      → FirstOath
ChainStore.ListIds() empty                     → FirstChain
DemonStore.ListIds() empty                     → DemonSummon
BattleStore.AnyForChain(OnboardingChainId) false → TutorialBattle
otherwise                                      → Complete
```

`OnboardingFlow.OnboardingChainId` returns the lex-first ULID from `ChainStore.ListIds()` — ULIDs sort by time, so this is the first Chain the player created (which is always the onboarding Chain in v1, since v1 doesn't surface a "create another chain" affordance until WP-19).

`Resume()` is a no-op past `EmitStartedIfNeeded()`. Submitting a step that doesn't match `CurrentStep` is a silent no-op (defends against double-submits and replayed UI events).

## 3. Validation

| Step | Rule |
| --- | --- |
| WardenName | 2..24 chars after `Trim()`. ArgumentException otherwise. |
| ClassSelect | Must be exactly `"scholar"` in v1. Any other value is rejected. |
| FirstOath | Title required (non-empty after trim). `why` optional. `class_id` defaults to `"scholar"` if blank. |
| FirstChain | Title required. SeverityHint defaults to `"moderate"` if blank. |
| DemonSummon | No payload fields. Generated record links to `OnboardingChainId`. |
| TutorialBattle | Payload optional; the flow detects completion from BattleStore.AnyForChain. |

## 4. Analytics

Per `analytics-taxonomy.md` §2.2:

- `onboarding_started` — emitted once per `OnboardingFlow` instance, only if there's actual onboarding work to do (returning players don't re-fire).
- `onboarding_step_completed` — `step_id` ∈ `warden_name | class_select | first_oath | first_chain | demon_summon | tutorial_battle`.
- `onboarding_completed` — `total_duration_ms` (UTC since flow construction), `skipped_steps` (always empty in v1).
- `onboarding_abandoned` — emitted on cold-start with an incomplete flow if the previous step's timestamp is ≥ 2 days old. **Phase 2.5 work** — the abandonment timer is plumbed but not surfaced; Boot computes elapsed-since-last-step from the most recent store write. WP-14 implements emit-on-Resume only when the flow has been idle for the cutoff. (See implementation-notes for the `last_step_at` heuristic.)

## 5. Tutorial-battle integration

`Boot.StartTutorialBattle` calls `BattleRunner.StartBattle(ArchetypeId.PermissionGiver, chainId: OnboardingChainId, demonId: <first demon>, phase1MaxDurationMs: 45_000, trigger: "onboarding_tutorial")`. The shorter Phase 1 is enforced via `Phase1Controller.MaxDurationMs` (instance property added in WP-14; default still 90 s for non-tutorial battles).

`BattleRunner.BuildBattleRecord` stamps `chain_id` and `demon_id` onto the persisted record, which is what `BattleStore.AnyForChain(OnboardingChainId)` reads to detect tutorial completion. The record's `outcome` doesn't matter — even a Loss counts as the tutorial having happened.

On `BattleRunner.BattleEnded`, `Boot.OnBattleEnded` re-activates `OnboardingShell` and submits the `TutorialBattle` step. The flow then derives `Complete` and the shell renders its final "Welcome to your realm" body before handing off to `HomeShell`.

## 6. Out of scope

- **Multi-language copy** — Phase 3.
- **Settings screen / privacy disclosure modal** — Phase 3 (tied to lawyer-drafted privacy policy).
- **The other three classes** — locked in v1 with "Coming in v1.1" affordance.
- **Ability to delete the onboarding Chain / Oath** — Phase 2 doesn't surface a delete affordance; Phase 3 does.
- **Onboarding analytics test** — covered by EditMode `OnboardingFlowTests` for state machine logic; PlayMode E2E is deferred (see implementation-notes; programmatic UI coverage is sufficient at this stage).
