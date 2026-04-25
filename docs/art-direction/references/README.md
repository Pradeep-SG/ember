# Art direction references

Three original mood-board images that anchor Kindrith's visual language. Generated with Nano Banana Pro from prompts pinned to the five-color palette in `docs/art-direction.md`. Owned outright — no third-party licensing concerns.

These are the reference images we check every new asset against. When the team disagrees about a visual call, we point at one of these and say "more like this, less like that." Their job is alignment, not perfection.

---

## The three references

### `01-realm.png` — Realm at rest

Anchors environment color, mood, and composition.

A small tended valley at dusk: moss-green hills, a bone-stepping-stone path winding toward a small modest pointed-roof shrine glowing ember orange from within, layered duskwine mountains, ember-orange firefly motes scattered through warm dusk sky.

What we extract:
- **Mood for any realm scene** — quiet, intentional, loved, the place you came home to.
- **Scale of the focal structure** — small enough to be intimate, never monumental.
- **Use of ember motes** — the realm should always feel quietly alive. Stillness is not emptiness.
- **Path-as-progress** — the user's journey across the realm reads as a path of stepping stones, never a road or trail.

Known imperfections (file for actual asset production, not for re-generation): the sky is gradient rather than banded; the shrine silhouette is slightly generic-cabin. Both will be sharper when we produce the real assets.

### `02-shadow-battle.png` — Shadow Battle confrontation

Anchors the visual relationship between Warden and Demon, and the staging of combat.

The Warden in profile on the left as a hooded graphite silhouette with an ember-orange edge glow, facing a larger amorphous duskwine smoke Demon on the right with two faint glowing slits where eyes would be. Between them, a bone-colored circular arena floor with concentric ember-orange ring patterns. Background fades from duskwine to graphite.

What we extract:
- **Who-is-who readability** — at half a second, the player must know which silhouette is theirs and which is the threat.
- **Demon shape language** — amorphous, smoky, formless. No mouth, no body, no detail you can sympathize with. Eyes only as glowing slits, and only when needed.
- **Warden language** — hooded silhouette, ember edge-glow as the only color signal. The player projects identity onto the silhouette.
- **Arena as ritual stage** — concentric rings, geometric pattern, bone-color floor. The stage will pulse with the 4-7-8 cadence in the actual screen.
- **No weapons drawn, ever.** Shadow Battle is a contest of light and breath, not blades.

Known imperfection: the Warden has a faint nose/profile hint. When producing the actual Warden art, specify "pure silhouette, no profile features."

### `03-hearth.png` — Hearth interior

Anchors the user's identity and home space.

An intimate stone-block hearth set in a bone-walled room at night, with a generous ember orange flame casting visible warmth onto the surrounding walls and a clear pool of light on the floor. Ember motes drift upward from the fire. A moss-green rug sits in the firelit zone. A pointed-arch window upper-right shows duskwine sky with one star.

What we extract:
- **The hearth as the room's source of meaning** — fire is the only saturated color, and the room visibly warms from it.
- **Stepped stone-block construction** — the hearth reads as ancient, ritual, made by hand. Not furniture.
- **Ember motes as breathing** — the warmth is alive. The room exhales.
- **Pointed-arch window as visual continuity** — the same arch shape appears on the shrine in `01-realm.png`. Cross-asset language; do not break it.
- **Unfilled walls are the opportunity** — the room is intentionally spartan. Cosmetics earned by habit change will fill it over time.

Known imperfection: the rug could be larger/closer to the hearth in the actual asset (image 2 of the regeneration pair did this better — refer back to that when producing the real version).

---

## Rules for using these references

- These are **mood references**, not assets. Do not extract sprites or art directly from them. Their job is alignment.
- When producing a new asset, write down which reference is anchoring it and what specifically you're taking from it. If you can't name it, you're not using it.
- If a future asset can't be reconciled with these references, we discuss whether the reference needs updating *before* committing the asset. References are slow-moving; assets are fast-moving.
- These images are owned by the project (generated, not borrowed) and may be used in pitch decks or press without external licensing concerns.

---

## Updating

If we ever update these references, update both:

1. The image file in this folder.
2. The "What we extract" section above for that image.
3. The corresponding line in `docs/art-direction.md` under "References."

References move together with the doc, never independently.
