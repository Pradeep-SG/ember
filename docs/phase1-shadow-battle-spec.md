# Phase 1 — Shadow Battle Prototype Specification

**Scope:** one scene, one Demon archetype at a time (swappable), three mechanical phases, placeholder rewards. No realm, no Oaths, no Warden progression, no cosmetics. Programmer-art acceptable.

**Thesis under test:** a 3–5 minute Shadow Battle is more engaging than the craving's alternative (a cigarette, a cookie, a scroll), measured under real-craving field conditions.

**Success is not "polish." Success is timing, feel, and the subjective report of the craver.**

---

## 1. Overview

A Shadow Battle is a single linear session with three phases, gated by time and input quality. The player always starts in Phase 1, advances to 2 on Phase 1 completion, advances to 3 on Phase 2 completion, and exits into a Win, Lose, or Abandon outcome.

Total intended duration: **3 min 00 s ± 30 s** for a successful run.

| Phase | Target duration | Core mechanic |
| --- | --- | --- |
| 1. Rhythm Breathing | 90 s (~5.5 breath cycles) | Tap on 4-7-8 cadence |
| 2. Choice | 60 s | Branching dialogue, pick counter-lines |
| 3. Finisher | 30 s | Reflex input pattern |

---

## 2. Phase 1 — Rhythm Breathing

### 2.1 The 4-7-8 cadence

One breath cycle is **19 000 ms** total, divided into four beats:

| Beat | Name | Duration (ms) | Visual state |
| --- | --- | --- | --- |
| 1 | Inhale | 4 000 | Circle grows from 40% → 100% radius, color shifts Bone → Hearth |
| 2 | Hold top | 7 000 | Circle holds 100% radius, color holds Hearth, subtle shimmer |
| 3 | Exhale | 8 000 | Circle shrinks 100% → 40% radius, color shifts Hearth → Bone |
| 4 | (No fourth hold) | — | Immediately loops to Inhale |

Standard 4-7-8 prescribes 4 s in / 7 s hold / 8 s out with no post-exhale hold. We respect the clinical timing exactly.

One Shadow Battle Phase 1 is **~4.7 cycles** (90 000 ms / 19 000 ms).

### 2.2 Tap mechanics

The player taps **once per breath cycle**, on the transition from **Hold top → Exhale** (i.e., at the 11 000 ms mark within each cycle, the start of the exhale).

Why that beat: it's the neurochemically-active moment of 4-7-8 (the release after hold). Anchoring the tap there reinforces the physiological effect the exercise already produces.

### 2.3 Tap tolerance windows

Relative to the intended tap moment (0 ms = start of exhale):

| Grade | Window (ms) | Damage | Feedback |
| --- | --- | --- | --- |
| **Perfect** | −150 to +150 | 2 beads of damage | Hearth pulse fills circle briefly, clean *thrum* SFX |
| **Clean** | −400 to +400 (excluding Perfect) | 1 bead of damage | Short Hearth flare, soft *tap* SFX |
| **Loose** | −900 to +900 (excluding Clean) | 0 beads, no penalty | Dim flicker, no SFX |
| **Miss** | outside ±900, or no tap in cycle | 0 beads, Demon gains small intensity | No pulse, Demon silhouette briefly brightens |

Demon HP starts at **5 beads**. Phase 1 ends the moment the 5th bead extinguishes OR the 90-second timer elapses, whichever comes first.

### 2.4 Phase 1 outcomes

- **Full clear (5 beads before 90 s)** — skip the remainder of the phase timer, advance to Phase 2 immediately with a small Clarity-early bonus (+10 % Phase 2 timer).
- **Partial clear (≥3 beads at 90 s)** — advance to Phase 2 at baseline.
- **Weak performance (0–2 beads at 90 s)** — advance to Phase 2 with a Demon-strength modifier (fewer valid counter options in dialogue).
- **Abandon** — player taps exit. Route to Abandon outcome (see §5).

### 2.5 Feedback in Phase 1

- Circle pulse animation: always on, always accurate to the 4-7-8 timing. The visual *is* the metronome.
- No numeric timer displayed. The only "countdown" visible is the 5 HP beads on the Demon.
- Audio: soft ambient drone, pitch-shifted on inhale/exhale to reinforce the cadence. No music.
- Haptic: iOS `UIImpactFeedbackGenerator` medium on Perfect taps only. Nothing on Clean/Loose/Miss — avoid haptic noise.

### 2.6 Edge cases

- **App backgrounded mid-phase**: pause the battle, freeze timers and HP at the moment of backgrounding. On foreground, show a "Resume or Abandon?" sheet. No silent resume — foregrounding should feel intentional.
- **Phone silent mode**: all audio mutes as expected; haptics still fire if system haptics are enabled.
- **VoiceOver on**: announce phase entry ("Breathing phase, five beads remain") and each bead extinguishing. No per-tap announcements (would overwhelm).

---

## 3. Phase 2 — Choice (Dialogue)

### 3.1 State machine

```
Phase2Entry
  └── Node_0 (Demon opening line)
        ├── Option A → Node_1a (Demon responds) ──┐
        ├── Option B → Node_1b (Demon responds) ──┤
        └── Option C → Node_1c (Demon responds) ──┤
                                                   │
                                                   ▼
                                    Node_2 (Demon doubles down)
                                      ├── Option A → Node_3a ──┐
                                      ├── Option B → Node_3b ──┤
                                      └── Option C → Node_3c ──┤
                                                               ▼
                                                   Phase2Exit (routes to Phase 3)
```

**Depth:** 2 turns (Node_0 → Node_2 → exit). Each turn offers **3 options**. **Total nodes per tree: 8** (Node_0, Node_1a, Node_1b, Node_1c, Node_2, Node_3a, Node_3b, Node_3c). The tree is **archetype-specific** — the summoned Demon (e.g., Craving Demon for smoking) picks from its own dialogue pool. See `demon-archetypes.md` for the dialogue pools. (Earlier brief drafts said 7 nodes; the canonical count is 8.)

### 3.2 Option evaluation

Each option is classed as one of:

| Class | Effect on Demon HP | Effect on Clarity buff | Player feedback |
| --- | --- | --- | --- |
| **Counter** (CBT-aligned reframe) | −1 bead or adds re-lit bead pool for Phase 3 | +0.1 Clarity | Hearth pulse on Demon, Bone shimmer on text plate |
| **Deflect** (neutral, non-engaging) | 0 | 0 | Minimal feedback, Demon responds flatly |
| **Agree** (player-surface rationalization) | +1 bead (Demon partially regenerates) | −0.1 Clarity | Demon silhouette brightens, Duskwine vignette deepens |

The "right" answer is always a Counter. Counters are CBT-informed — mapping to cognitive restructuring, urge surfing, or behavioral activation depending on Demon archetype. They are **not** obvious — Agree lines are plausible and sometimes feel kind to yourself.

### 3.3 Timing

- **60 s** soft cap for the whole phase.
- Within a turn, the player has **20 s** to pick an option. No pick → treated as a **Deflect**.
- Demon "types" each line at 50 ms per character (Bone text on Graphite plate). Player can tap to instant-complete the line; the tap does not count as a dialogue pick.

### 3.4 Phase 2 outcomes

- **Two Counters** — advance to Phase 3 with full Clarity pool.
- **One Counter, one non-Counter** — advance to Phase 3 at baseline.
- **Two non-Counters** — advance to Phase 3 with reduced Clarity pool (shorter finisher window).
- **Abandon** — Abandon outcome (§5).

### 3.5 Feedback in Phase 2

- Duskwine mist slowly clears from around the Demon silhouette on Counter, slowly thickens on Agree.
- HP beads update live.
- No timer visible. A subtle Bone-line "pulse" indicator under the options indicates remaining time.

---

## 4. Phase 3 — Finisher

### 4.1 The reflex pattern

After 2 s of silence and a widening Hearth glow, a **three-beat prompt** appears:

- Beat 1: a Bone ring expands from the Demon's core; the player must tap when it reaches the edge of the frame. Window: ±180 ms.
- Beat 2: immediately after, a Duskwine ring contracts inward; tap when it reaches the Demon's core. Window: ±180 ms.
- Beat 3: a final Hearth ring expands and must be **held** (touch held) until it reaches the frame edge, then released. Hold duration: **800 ms ±120 ms** — the canonical figure tracked by `FinisherSequencer.HoldDurationMs` / `HoldToleranceMs`. (Earlier brief drafts mentioned 700 ms; treat 800±120 as authoritative.)

This pattern is memorizable but punishing — it's a skill check on attention, which is the state the player just spent 2.5 minutes cultivating.

### 4.2 Outcomes

| Beats landed | Outcome |
| --- | --- |
| **3 of 3** | **Critical Win.** Big Hearth bloom, guaranteed rare drop, full 24h Clarity buff. |
| **2 of 3** | **Win.** Standard Hearth bloom, standard drop, Clarity buff. |
| **1 of 3 with Demon at 0 beads from earlier phases** | **Win.** Same as 2-of-3 win. |
| **Demon HP > 0 at end of Phase 3** | **Loss.** See §5. |

### 4.3 Phase 3 duration

Maximum 30 s from prompt start to final release. If the player flubs all three beats, route immediately to Loss — do not drag out the failure.

### 4.4 Feedback in Phase 3

- Screen-wide Duskwine → Kindrith transition on final release (Win).
- Screen dim to Graphite and a slow Demon laugh — a single long Duskwine exhale sound — on Loss.
- Haptic: strong on Critical, medium on Win, none on Loss. Loss should feel *quiet*, not punishing.

---

## 5. Win / Lose / Abandon state transitions

```
             ┌──────────────────┐
             │  Home (Resist)   │
             └────────┬─────────┘
                      │ tap Resist
                      ▼
             ┌──────────────────┐
             │    Phase 1       │──abandon──┐
             └────────┬─────────┘           │
                      │                     │
                      ▼                     ▼
             ┌──────────────────┐    ┌────────────┐
             │    Phase 2       │───►│  Abandon   │
             └────────┬─────────┘    │  outcome   │
                      │              └─────┬──────┘
                      ▼                    │
             ┌──────────────────┐          │
             │    Phase 3       │──abandon─┤
             └────┬─────────────┘          │
                  │                         │
          ┌───────┴────────┐                │
          ▼                ▼                │
       ┌──────┐       ┌──────┐              │
       │ Win  │       │ Loss │              │
       └──┬───┘       └──┬───┘              │
          │              │                   │
          └──────┬───────┴───────────────────┘
                 ▼
        ┌─────────────────┐
        │ Reward / debrief│
        │    screen       │
        └─────────────────┘
```

### 5.1 Win state

- Displays a **Critical Win** vs **Win** header (see §4.2).
- Placeholder reward: "You earned: [Rare Shard placeholder] and a 24-hour Clarity buff." (Phase 1 renders text only; no actual item art.)
- CTA: "Return to the Realm" (Phase 1 just closes the battle).
- 3 s auto-advance available; player can also tap through.

### 5.2 Loss state

- Header: **"The Demon is strong today."**
- Body copy: "You showed up. That is the hardest part. Tomorrow you fight again."
- **No shaming copy. Ever.** See pillar 2.
- Placeholder reward: "You earned: small Clarity token." (Yes, losing still gives something. The goal is that *opening during a craving* is the win condition.)
- CTA: "Return to the Realm."

### 5.3 Abandon state

Triggered by:
- Tapping the exit button during any phase.
- OS-level force-quit.
- 60 s of background inactivity after a pause.

- Header: **"Stepped back."**
- Body copy: "The battle waits. Come back when you're ready."
- Placeholder reward: none. We don't reward abandoning.
- But: this is still logged as an open — per thesis, opening is a win the craving didn't get.

---

## 6. Placeholder reward screen

Phase 1 prototype does not have real rewards (no realm, no Warden, no cosmetics). The reward screen displays:

```
┌────────────────────────────────┐
│         [Outcome header]       │
│                                │
│         Clarity +1             │
│         Shard of Bone          │
│         [debug info if on]     │
│                                │
│    [Return to the Realm]       │
└────────────────────────────────┘
```

Where:

- **Clarity +1** is an integer token displayed on the home screen post-battle, for the duration of a 24h debug-timer (visible when debug flag on). In Phase 2+, Clarity becomes the real buff.
- **Shard of Bone** is a placeholder item name. One variety only in Phase 1.
- **[debug info if on]**: in builds with the `DEBUG_BATTLE` flag, show per-phase grade, tap accuracy stats, time to complete, Demon archetype ID. Never in production.

---

## 6.1 Phase 1 analytics events (canonical count)

Phase 1 emits **9 distinct event names** through the run:

1. `app_opened` — cold start or foreground resume
2. `app_backgrounded` — app pause
3. `shadow_battle_started`
4. `shadow_battle_phase_entered` (×3 per battle: phase1, phase2, phase3)
5. `shadow_battle_tap_registered` (one per Phase-1 tap)
6. `shadow_battle_dialogue_choice` (one per Phase-2 option pick)
7. `shadow_battle_finisher_beat` (×3 per battle)
8. `shadow_battle_completed` (terminal — carries `outcome=abandon`+`abandon_reason` for Abandon flows; no separate `shadow_battle_abandoned` in Phase 1)
9. `feedback_submitted` (Phase 1 feedback button — when wired)

A clean win flow emits 11+ rows total: 1 started + 3 phase_entered + N tap_registered + 2 dialogue_choice + 3 finisher_beat + 1 completed. (Earlier brief drafts said "exactly 8 events" — that figure was wrong; the canonical contract is in `docs/analytics-taxonomy.md` §2.5 and pinned by `EditMode/P1ParameterAuditTests.cs`.)

---

## 7. Home screen (minimal Phase 1 surface)

Outside the Shadow Battle, the app is a single home screen with:

- **Hearth visual** (centered, animated at 4-7-8 ambient breathing — but NOT at full Shadow Battle intensity).
- **Resist button** — large, persistent, warm-bordered. Tapping launches a Shadow Battle.
- **Demon archetype picker** (dev / playtester build only) — lets tester choose which archetype to face, for diary study variety.
- **Session log** — last 3 battle outcomes with grade and timestamp.

Nothing else. No menus, no settings beyond debug flags, no cosmetics gallery.

---

## 8. Pre-committed success bars

We commit to these now so we can't move the goalposts after the data comes in.

### Quantitative

- **Engagement vs. alternative: ≥ 60 %** of testers, when asked *"Was the Shadow Battle more engaging than what you would have done instead during the craving?"*, answer yes.
- **Mean session length: ≥ 3 min 00 s** across all battles in the 2-week diary study (excluding Abandon sessions under 10 s).
- **Completion rate: ≥ 70 %** of started battles reach a Win or Loss outcome (i.e., not Abandon).

### Qualitative

- **Combat framing** — testers, when describing the experience unprompted in interview, use combat language ("fight," "beat," "took down the Demon") more than tracker language ("logged," "tracked," "did the exercise"). Scored by two independent coders on recorded interviews, inter-rater agreement ≥ 0.7.
- **Return intent** — at least 50% of testers indicate they opened the app during a real craving at least 3 separate times during the 2-week study.

---

## 9. Kill criteria

If, after **9 cumulative weeks on Phase 1** (the initial 4-week sprint plus up to 2 iteration rounds of 2–3 weeks each), the quantitative bars are still not cleared, reconsider the core thesis before building Phase 2.

Concretely: stop development, run a post-mortem, and decide whether to:

1. **Pivot the mechanic** — keep the thesis, redesign the battle (e.g., replace 4-7-8 breathing with a different clinically-backed technique).
2. **Pivot the product** — drop the Shadow Battle concept; Kindrith becomes something else or does not ship.
3. **Continue with a documented exception** — very rare, requires an explicit argument for why current signal is stronger than the success bar despite the numbers.

Better to kill here than at Month 6.

---

## 10. Build plan summary (companion, not authoritative)

This doc is the *spec*. Sequencing lives in the roadmap. In brief, the Phase 1 build is:

1. Breathing-circle primitive with exact 4-7-8 timing (Week 1).
2. Tap detection with tolerance windows and HP beads (Week 1).
3. Dialogue state machine with 3 archetypes from `demon-archetypes.md` (Week 2).
4. Phase 3 finisher reflex mechanic (Week 2–3).
5. Win/Lose/Abandon routing and placeholder reward screen (Week 3).
6. TestFlight-able build for the diary study (Week 4).

Any divergence from this spec during build gets redlined here with a dated amendment, not silently fixed in code.
