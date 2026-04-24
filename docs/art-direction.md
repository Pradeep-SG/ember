# Art direction

**Minimalist geometric.** Silhouette-first, palette-first, motion-first. We do not draw characters; we compose them out of shapes and light.

One-paragraph thesis: Kindrith is a warm, stylized world where the visual fidelity comes from *composition and animation*, not illustration. Everything on screen must read at a glance, work as a pure silhouette against any background, and hold up without facial detail. The budget that would have gone to character art goes into timing, particles, and the hearth.

---

## References

Three reference images live in `art-direction/references/` — Alto's Odyssey, Monument Valley, and one additional chosen by Pradeep (see README there). Look to them for:

- **Alto's Odyssey** — atmospheric color, silhouette-against-sky framing, parallax depth.
- **Monument Valley** — flat-shaded geometry, clear silhouettes, calm palette discipline.
- **Third (TBD)** — stakes the style we take from outside the obvious (see references README for candidates).

---

## Palette

Core palette: **five colors**. Memorize these. Every screen uses at most three of them at a time.

| Name | Hex | Use |
| --- | --- | --- |
| Hearth | `#F25A1C` | The hearth, Warden warmth, Clarity buff pulses. The only genuinely saturated color in the game. |
| Duskwine | `#3B1F3F` | Shadow Battle backdrop, negative-habit territory, map-shadow encroachment. |
| Bone | `#EDE1CC` | Warden silhouette highlights, UI text on dark, rest states. |
| Graphite | `#1A1822` | Deep background, negative space, Demon core. |
| Moss | `#6E8B5C` | Positive realm, Oath halos, XP sparks, quest markers. |

Hex values are stored as a Unity `Colors` swatch library at `Assets/Settings/Kindrith_Palette.colors` — Pradeep creates that asset inside Unity from these hex codes (see Phase 0 checklist §2). The doc is the source of truth until that asset lands.

### Usage rules

- **Hearth (`#F25A1C`) is sacred.** Use it only for the hearth and directly hearth-adjacent feedback. Never for UI chrome, never for generic accents. It has to *mean something* every time it appears or its punch is gone.
- **Duskwine and Graphite never touch each other without a Bone or Moss edge** — they turn to mud together.
- **Moss is the only "success" color.** Do not introduce green variants for quests, streaks, or confirmations.
- One color can shift in value (lighter / darker) within a frame; we do not introduce colors outside the palette without a pillar review.

---

## Silhouette rules

1. **Every character, structure, and Demon must be recognizable in pure black at 48×48 px.** If it's not, redesign the shape, not the shading.
2. **No facial detail on Demons.** Ever. A Demon is silhouette + glow. Eyes are optional, implied by negative space or a single point of Hearth light. This preserves the projective quality — the player projects their specific craving onto the shape.
3. **Warden has a silhouette, not a face.** A hood, a posture, a color accent per class. Never a drawn face. This supports identity-first — the player is the Warden.
4. **Realm structures are geometric primitives plus one expressive asymmetry.** Temples, towers, hearths: built from rectangles, triangles, arcs, with one off-angle or broken element that gives character.
5. **Depth is parallax, not perspective.** Three-layer parallax minimum on any realm scene: foreground silhouette, midground structures, background sky/mist.

---

## Motion

More important than illustration in this style.

- **Hearth breathing.** The hearth at center of the realm pulses on the 4-7-8 cadence — always, even when the app is idle. This anchors the visual language of the whole game.
- **Habit-log impact.** Logging a positive habit triggers a 300 ms light-bloom animation on the hearth and a single particle burst. Not confetti. Warm, economical.
- **Shadow decay.** When a negative habit is fed (relapse), a vignette of Duskwine creeps in from the nearest edge over ~800 ms and settles. Never sudden. Never jarring. The realm doesn't punish — it grieves.
- **Demon entrance.** Smoke accumulation, not a fade-in. The Demon materializes out of Duskwine mist over ~1.2 s, with a Hearth glow at its core that slowly stabilizes.

---

## Forbidden elements

- **No hand-drawn characters.** No illustrated faces, no anime-style portraits, no photo-real humans.
- **No realistic textures.** No wood grain, no stone lichen, no cloth weave. Flat shading, subtle gradients only.
- **No blood, gore, or literal violence.** Shadow Battle is *visibly* a contest of light versus shadow, not a sword fight.
- **No religious iconography.** Crosses, pentagrams, chakras, ankhs — out. Kindrith borrows the *language* of ritual (Oath, Hearth, Warden) but never its symbols.
- **No countdown timers visible to the player.** The 4-7-8 cadence is communicated via the pulsing circle, not digits. Numbers kill immersion.
- **No progress bars longer than the hearth is tall.** Kindrith is not a tracker; we don't look like one.

---

## Shadow Battle visual language

This is the screen that has to beat a cigarette. It gets its own rules.

### Layout

- **Full-bleed Duskwine background** with a slow Graphite vignette at the edges. The craving is present in the frame.
- **Centered breathing circle** on the Hearth → Bone → Hearth → Bone loop corresponding to 4-7-8 inhale / hold / exhale phases.
- **Demon silhouette** upper-center, ~40% frame height, never fully in focus — always softened by Duskwine mist.
- **Health beads, not bars.** Demon HP is 5 beads that extinguish one at a time on clean hits. Bars are tracker-language; beads are combat-language.

### Color behavior

- Breathing circle shifts Hearth → Bone → Hearth → Bone across the inhale, hold, exhale, hold beats. The color shift *is* the cadence cue.
- A clean tap lands a Hearth pulse on the Demon silhouette, briefly illuminating part of it.
- The Clarity buff on victory is a Hearth bloom that fills the screen for 800 ms and leaves a lasting Hearth tint on the home screen for 24 h.

### Demon presentation

- Silhouette + internal glow + drifting Duskwine mist. Nothing else.
- The Demon "speaks" by typing one short line at the bottom of the frame over 500–800 ms, in Bone text on a Graphite plate. The voice is slow, unhurried, almost tender — the worst rationalizations sound reasonable.
- No shouting, no ALL CAPS, no exclamation points. The Demon is patient. That's what makes it dangerous.

### What we never show here

- The player's Oath list. Irrelevant in this moment.
- XP numbers or level-ups mid-battle. Aftermath only.
- The Warden. The Warden *is* the player here — represented only by the tap, the breath, and the Hearth pulse.

---

## Budget and scope for Phase 1

- Palette asset: one `.colors` file.
- Shadow Battle: Duskwine backdrop, breathing circle shader, one Demon silhouette, particle pulse on tap, beads UI. No art beyond this.
- Everything else (realm, Warden, structures) is deferred to Phase 2.

If we have spare cycles in Phase 1, they go into **tuning the breathing circle's Hearth-to-Bone shift and the Demon's silhouette animation**, not into new assets.
