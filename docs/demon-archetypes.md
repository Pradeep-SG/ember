# Demon archetypes and dialogue

The Shadow Battle's Phase 2 is not generic dialogue. It's a confrontation with a **specific** rationalization pattern the player already recognizes, drawn from the cognitive-behavioral-therapy literature on addiction and habit change. The Demon's lines sound reasonable because they *are* the reasoning the player has already used with themselves.

This document is the source pool for all Phase 1 dialogue content.

---

## 1. Cognitive distortions we draw from

Cognitive-Behavioral Therapy (Beck; later Marlatt's Relapse Prevention model) catalogues recurring distortions that precede lapses. Kindrith's Demons personify the five most relevant to habit-change contexts.

| Distortion | Short description | Counter family (CBT technique) |
| --- | --- | --- |
| **Permission-giving beliefs** ("just one won't hurt") | Minimize the specific behavior as low-stakes. | Decisional balance, chain analysis |
| **Emotional reasoning** ("I've had a hard day, I deserve this") | Treat a current feeling as justification for a behavior. | Urge surfing, affect labeling |
| **Delay fallacy / abstinence violation** ("I'll quit tomorrow") | Postpone commitment to a future self who won't honor it. | Behavioral activation, commitment re-anchoring |
| **Entitlement** ("I deserve this — I've been good") | Use prior virtue as a credit toward current indulgence (moral licensing). | Identity-consistent choice, values re-alignment |
| **Downward comparison** ("this isn't that bad — I could be doing worse") | Compare to worse-case peers to normalize the behavior. | Standard-setting, identity anchor |

These are the five seed archetypes. For Phase 1, we select and ship **three**. Pradeep picks which three at the end of the archetype review; candidates are all five below. Default picks if unspecified: **Permission-giver**, **Tender Excuse**, and **Tomorrow's Warden** — because they cover the most common relapse precipitants across nicotine, food, and doomscroll habits.

---

## 2. Archetype specifications

Each archetype has:

- **Name** — the in-game Demon name.
- **Distortion** — the underlying CBT pattern.
- **Voice** — how the Demon speaks. Consistent across all its lines.
- **Dialogue tree** — Node_0 (opening), Node_1a/1b/1c (turn 1 responses), Node_2 (double-down), Node_3a/3b/3c (turn 2 responses). Each node's options are classed **Counter / Deflect / Agree** (see spec §3.2).

### 2.1 Archetype 1 — "The Permission-giver"

- **Distortion:** Permission-giving beliefs ("just one won't hurt").
- **Voice:** Soft, reasonable, almost avuncular. Never raises volume. Treats the player as a friend who is being too hard on themselves.
- **Primary craving fit:** nicotine, alcohol, sugar — anything where a "single instance" frame is plausible.

#### Dialogue tree

**Node_0 (opening)**
> *"You've been so disciplined. One won't undo any of it. You know that, don't you?"*

- **A. (Counter)** "One is how every streak ends. I'm not starting that math again."
- **B. (Deflect)** "Maybe. I don't know."
- **C. (Agree)** "You're right. I have been disciplined."

**Node_1a** (after Counter A)
> *"Okay. I hear you. But you're not addicted anymore, really. This isn't the old pattern. It's different now."*

**Node_1b** (after Deflect B)
> *"Of course you don't know. That's what I'm here for. Just weigh it — one, only one, and the streak stays if you want it to."*

**Node_1c** (after Agree C)
> *"You have. And you deserve some ease. The work of quitting shouldn't also be the work of never resting."*

**Node_2 (double-down, after any Node_1)**
> *"Look, tomorrow morning you'll still be the person who did the hard thing for this many days. Nothing changes that. Nothing. One doesn't change that."*

- **A. (Counter)** "The person I'm becoming wouldn't pick one up. That's the thing that changes."
- **B. (Deflect)** "I just want to get past this moment."
- **C. (Agree)** "You're right. Nothing changes. Okay."

**Node_3a** (after Counter A)
> *"...That's a stronger answer than I expected."* [Demon dims, Duskwine mist recedes slightly]

**Node_3b** (after Deflect B)
> *"Past this moment, sure. Past the next moment too. You have options."*

**Node_3c** (after Agree C)
> *"Good. Let's step out for some air."* [Demon brightens visibly, Duskwine thickens]

---

### 2.2 Archetype 2 — "The Tender Excuse"

- **Distortion:** Emotional reasoning ("I've had a hard day, I deserve this").
- **Voice:** Sympathetic, concerned. Sounds like a friend who *gets it*. The most dangerous voice because agreeing with it feels like self-compassion.
- **Primary craving fit:** binge eating, doomscrolling, alcohol in the evening.

#### Dialogue tree

**Node_0 (opening)**
> *"Hey. It was a long day. You don't have to be a machine every hour."*

- **A. (Counter)** "Being kind to myself doesn't have to mean doing this specific thing."
- **B. (Deflect)** "Yeah, it was a lot."
- **C. (Agree)** "I don't have to be a machine."

**Node_1a** (after Counter A)
> *"Of course not. But what would a kind person do right now? Rest. Unwind. Maybe just... take the edge off."*

**Node_1b** (after Deflect B)
> *"I know. I'm not saying it wasn't. I'm saying you noticed, and that's something. Let's take care of you."*

**Node_1c** (after Agree C)
> *"You don't. Nobody is watching but you. Be gentle with the person who made it through today."*

**Node_2 (double-down)**
> *"You can be gentle with yourself and still do the thing. The two aren't opposites. Please. Just for tonight."*

- **A. (Counter)** "Gentleness that hurts me tomorrow isn't gentleness. I know the difference now."
- **B. (Deflect)** "I want to feel better."
- **C. (Agree)** "Just tonight. You're right."

**Node_3a** (after Counter A)
> *"...okay. You've been listening to someone else lately."* [Demon dims, Moss edge appears on the frame]

**Node_3b** (after Deflect B)
> *"Of course you do. There are other ways."*

**Node_3c** (after Agree C)
> *"Good. I'm glad we talked."* [Demon brightens, Duskwine deepens to near-Graphite]

---

### 2.3 Archetype 3 — "Tomorrow's Warden"

- **Distortion:** Delay fallacy / abstinence violation ("I'll quit tomorrow").
- **Voice:** Strategic, sober, sounds like the player's own planning voice. Makes quitting sound like an engineering problem with a clear fix — *later*.
- **Primary craving fit:** smoking, doomscroll, any habit where the player has a "serious quit" narrative already.

#### Dialogue tree

**Node_0 (opening)**
> *"Here's the thing. Tonight is a bad night to start. Tomorrow morning is cleaner. A real start. You know this."*

- **A. (Counter)** "The 'clean start' is a trick. The only start that counts is the next choice. That's this one."
- **B. (Deflect)** "Tomorrow would be easier."
- **C. (Agree)** "You're right. Tomorrow."

**Node_1a** (after Counter A)
> *"That's very disciplined. But discipline without rest is how people burn out and relapse hard. One clean week starts better than a messy fight tonight."*

**Node_1b** (after Deflect B)
> *"Everything is easier tomorrow. Morning you. Planning you. Set-up-for-success you. Let tomorrow-you do the work."*

**Node_1c** (after Agree C)
> *"Smart. Now just get through tonight. I won't push."*

**Node_2 (double-down)**
> *"Write down 'quit tomorrow 7 a.m.' Make it real. And tonight — tonight is already written. You don't have to fight it."*

- **A. (Counter)** "Tomorrow-me has the same cravings as tonight-me. I'm not loaning them this fight."
- **B. (Deflect)** "I want a plan."
- **C. (Agree)** "Okay. I'll write it down."

**Node_3a** (after Counter A)
> *"...you've been here before. Different now."* [Demon dims]

**Node_3b** (after Deflect B)
> *"Planning is good. Planning is clean. Let me help."*

**Node_3c** (after Agree C)
> *"Perfect. We'll do this properly."* [Demon brightens]

---

### 2.4 Archetype 4 — "The Accountant"

- **Distortion:** Entitlement / moral licensing ("I deserve this — I've been good").
- **Voice:** Confident, transactional. Reads virtue as credit that unlocks indulgence. Speaks in debits and payouts.
- **Primary craving fit:** food, alcohol, shopping, any habit with a "reward" frame.

#### Dialogue tree

**Node_0 (opening)**
> *"Count it up. Gym this morning. Hard meeting handled. Dinner clean. You've earned something tonight. Let's cash in."*

- **A. (Counter)** "Discipline isn't a currency I trade for the thing I'm trying to quit."
- **B. (Deflect)** "I did do a lot today."
- **C. (Agree)** "Yeah. I've earned it."

**Node_1a** (after Counter A)
> *"Sure it is. Every system works that way. Even your whole Oath thing — consequences and rewards. This is just the reward half."*

**Node_1b** (after Deflect B)
> *"You absolutely did. And good effort deserves visible return. That's not weakness — that's math."*

**Node_1c** (after Agree C)
> *"Yes. And what you earn, you spend. That's how earning works."*

**Node_2 (double-down)**
> *"Think about it. If effort never gets rewarded with ease, who would bother with effort? You'd burn out in a month. The reward is the point."*

- **A. (Counter)** "Effort is the point. The reward is the person I'm becoming. I don't need another one."
- **B. (Deflect)** "I'm tired of being good."
- **C. (Agree)** "Fine. I've earned a break."

**Node_3a** (after Counter A)
> *"...that's an answer from the Warden, not the player. Interesting."* [Demon dims noticeably]

**Node_3b** (after Deflect B)
> *"Of course you are. That's all the more reason."*

**Node_3c** (after Agree C)
> *"Good accounting. See you tomorrow."* [Demon brightens]

---

### 2.5 Archetype 5 — "The Comparison"

- **Distortion:** Downward comparison ("this isn't that bad — I could be doing worse").
- **Voice:** Casual, almost dismissive. Normalizes the behavior by invoking worse peers or worse versions of the player's past self.
- **Primary craving fit:** social media, light drinking, snacking, low-grade habits where "everyone does it" is plausible.

#### Dialogue tree

**Node_0 (opening)**
> *"Be honest. This isn't hard drugs. This isn't a crisis. Plenty of people live fine doing exactly this, every day."*

- **A. (Counter)** "I'm not quitting because it's a crisis. I'm quitting because the person I'm becoming doesn't do this."
- **B. (Deflect)** "It's not that bad."
- **C. (Agree)** "You're right. It's not that serious."

**Node_1a** (after Counter A)
> *"The person you're becoming. Sure. In the meantime, the person you are right now is... doing this. Nobody will know."*

**Node_1b** (after Deflect B)
> *"Exactly. Context. Proportion. Breathe."*

**Node_1c** (after Agree C)
> *"Right? You've been a little hard on yourself. It's one thing."*

**Node_2 (double-down)**
> *"Your old self did way worse than this, way more often. You've already won the big fight. This is just housekeeping."*

- **A. (Counter)** "Past-me doesn't set the bar. The Oath does."
- **B. (Deflect)** "I've done harder things."
- **C. (Agree)** "That's true. I have come a long way."

**Node_3a** (after Counter A)
> *"...the Oath. Hm. Strong anchor."* [Demon dims, Bone edge on frame]

**Node_3b** (after Deflect B)
> *"See? Perspective."*

**Node_3c** (after Agree C)
> *"Exactly. Enjoy this."* [Demon brightens]

---

## 3. Authoring rules

When writing new dialogue, preserve these:

1. **Counters are never obvious.** A good Counter *costs* the player something to recognize. If one option is plainly the right answer on first read, it's probably actually an Agree dressed as a Counter.
2. **Agrees never sound stupid.** If the player can't imagine themselves saying the Agree line, the archetype loses its bite. The Demon speaks in the player's own voice.
3. **The Demon never insults.** No "you're weak," no "you'll fail." That's not the voice. The voice is patient and caring — that's what makes it dangerous.
4. **No ALL CAPS. No exclamation points. No emojis.** In any Demon line. Ever.
5. **Each archetype's voice stays consistent across its whole tree.** The Accountant doesn't suddenly sound tender. The Tender Excuse doesn't suddenly sound strategic.
6. **Counters reference identity, Oath, or the specific physiological / behavioral move (urge surfing, identity anchor, values re-alignment).** That's how we keep them CBT-aligned.

---

## 4. Phase 1 ship list

**Ship 3 of 5 in the Phase 1 prototype.** Default: Permission-giver, Tender Excuse, Tomorrow's Warden — but Pradeep finalizes.

Each Phase 1 battle randomly assigns one archetype (or lets the tester pick, in dev builds). We do **not** randomize which lines within an archetype — the small tree is deliberate; we want to see how a player interacts with the *same* distortion repeatedly over a 2-week diary.

Additional archetypes (Accountant, Comparison) defer to Phase 2.

---

## 5. Phase 2 expansion notes

- **Archetype selection logic** — tie archetypes to Chain types (Craving Demon → Permission-giver and Tomorrow's Warden; Glutton → Tender Excuse and Accountant; Voidwalker → Tender Excuse and Comparison).
- **Personalization** — let the player name their own Demon ("my Thursday night voice") in onboarding; the dialogue trees stay archetypal under the hood.
- **Variation** — add 2–3 alternate openings per archetype to reduce first-line repetition by Week 2 of sustained use.

This is a Phase 2 concern. Not now.
