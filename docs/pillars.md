# Kindrith — The Four Pillars

Every design, copy, and feature decision passes through these four filters. If a proposal fails any one of them, it doesn't ship.

Reviewed weekly (Monday, 15 min). When a pillar feels wrong, change the pillar deliberately — don't quietly violate it.

---

## 1. Identity-first

**We are building a world in which the player becomes someone, not a tracker in which the player records behavior.**

Habit change is an identity problem. Behavior follows self-story. Kindrith treats every onboarding ritual, class choice, and Oath as a declaration of who the player wants to be, and uses the realm to reflect whether they're living it.

- **We will** use the language of commitment (Oath, Chain, Warden, Demon) over the language of measurement (task, completion, streak count).
- **We will** make the Warden class the player's avatar — Warrior, Scholar, Monk, Ranger — and let real-world action shape who that Warden becomes.
- **We will not** ship a "quick add habit" shortcut that bypasses the Oath ritual. The friction is the product.
- **We will not** show raw numbers where a visible change in the realm can carry the signal instead.

---

## 2. Loss-averse but kind

**We use loss aversion for motivation, never for punishment.**

Consequences are real and visible — the hearth dims, a structure fades, a Chain gains territory. But every setback has a clearly lit, 24–48h path back, and the app never shames. The single biggest predictor of full relapse is shame about a lapse (Marlatt). We refuse to be the thing that causes that.

- **We will** show visible decay when the player slips: dimmer hearth, Demon regains HP, a structure darkens.
- **We will** always surface a Return Quest — small, almost-guaranteed — the next morning after any lapse.
- **We will not** reset progress on a slip. Resonance decays, never zeroes; relapse HP-regen is proportional, never full reset.
- **We will not** use language or imagery that blames. "The hearth burned low — it is waiting" over "You failed."

---

## 3. Cosmetic-dominant

**Rewards express identity, they don't gate capability.**

Cosmetics are pure expressive rewards and preserve intrinsic motivation (Deci & Ryan: extrinsic functional rewards displace intrinsic). We hold the 80/20 cosmetic-to-functional ratio until evidence forces otherwise, and we build it as a server-tunable value so we can adjust without a client release.

- **We will** make ~80% of loot cosmetic: realm decor, Warden outfits, title flavors.
- **We will** keep functional rewards (potions, buffs) scoped to Shadow Battles — they modulate the combat layer, not the habit layer.
- **We will not** sell capability. No paid XP, no paid Resonance, no paid Shadow Battle wins. Ever.
- **We will not** use loot boxes, gacha, or pay-for-gems-that-buy-cosmetics indirection. Direct cosmetic purchase only.

---

## 4. Craving-competitive

**When a craving hits, the Shadow Battle must be a more compelling 3–5 minutes than the thing the craving wants.**

This is the thesis and the risk. If Shadow Battle loses that contest, Kindrith is just another tracker. Every UI decision on the battle screen, every dialogue line, every animation budget gets spent here before anywhere else.

- **We will** spend disproportionate polish, animation, and tuning time on Shadow Battle relative to any other screen.
- **We will** make the Resist button reachable in ≤1 tap from the home screen. Always visible, always warm.
- **We will** dress the breathing exercise as combat because the neurochemistry needs to compete with the substance — clinical framing loses.
- **We will not** ship a passive "take a deep breath" fallback or a boring text-based intervention. If we can't make it combat, we don't ship it.
- **We will not** build the realm, Oaths, or onboarding until a playable Shadow Battle beats its success bar.

---

## Weekly review cadence

Monday, 15 minutes. Two questions only:

1. Did any decision this week violate a pillar? Name it; decide to roll back, amend the pillar, or accept as an explicit exception.
2. Is any pillar becoming a cliché we repeat without checking? Kill jargon before it kills judgement.

---

*Ratified Phase 0. Change requires a dated amendment here and a note in the weekly review log.*
