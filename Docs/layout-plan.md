# Layout plan (P2)

The goal: each handoff walk lasts about as long as the dialogue spoken during it, so the conversation doesn't run out halfway and the player isn't left reading while standing still at the destination. This plan gives a target distance for every connected pair in the P1 roster, so you can place the figures (P3).

## How the distances are worked out

- **Words spoken on the walk.** The handoff conversation (P4/P5 nodes, not written yet) carries on the conversation the player was having with the first philosopher. So the estimate is **half the words of the first philosopher's existing `Talk_` node** in `RealityPlus_Map.yarn`. That is roughly one branch's worth, since a player doesn't see every option. Hume, Lewis, Mill and Gettier have no dialogue yet, so they get 100 words, about half the average existing node (~205 words).
- **Reading speed: 150 words a minute.** That's slower than silent reading (~200–250) because the player also advances lines and picks options.
- **Walking pace with a philosopher: 1.5 m/s on average.** The walker's top speed is 2–3 m/s, but he eases off near the player and stops for checkpoint questions. The player's own top speed is 4 m/s.
- **Distance** = words ÷ 150 × 60 s × 1.5 m/s, rounded to 5 m. That works out to about **0.6 m per word**, or 60 m per 100 words.

When the P4/P5 nodes exist, re-measure: count their words and use 0.6 m per word.

## Where things stand now

From `SampleScene.unity`:

- **Areas.** The 12 areas are already spread over about 1 km × 0.7 km of the 3 km × 3 km terrain (area centres from (35, 915) to (945, 225)).
- **Inside an area.** Philosophers stand in a tight triangle about **7 m** from the area centre: the lead at (0, 7), the others at (±6, −3.5) or (0, −7). That is too close for any walk; a handoff would end before the first line was read.

## 1. Inside each area: lead → each member

Place the members on a ring around the lead at about this radius. Members on the same ring end up roughly radius × 1.7 apart from each other, which is fine because only the lead hands off to them.

| Area | Lead (`Talk_` words) | Words on the walk | Walk time | **Target radius** | Members on the ring |
|---|---|---|---|---|---|
| Metaphysics | Heim (155) | 78 | 31 s | **45 m** | Plato, Berkeley, Hume |
| Epistemology | Descartes (279) | 140 | 56 s | **85 m** | Moore, Zhuangzi, Gettier |
| Philosophy of Mind | Elisabeth (261) | 130 | 52 s | **80 m** | Nagel, Clark |
| Philosophy of Language | Putnam (256) | 128 | 51 s | **75 m** | Carnap |
| Philosophy of Religion | Anselm (211) | 106 | 42 s | **65 m** | Leibniz, Narada |
| Computing and Information | Turing (189) | 94 | 38 s | **55 m** | Searle, Wheeler |
| Physical Science | Russell (214) | 107 | 43 s | **65 m** | Sellars, Galileo |
| Probability | Bostrom (219) | 110 | 44 s | **65 m** | Boltzmann, Lewis |
| Well-Being | Nozick (217) | 108 | 43 s | **65 m** | Bentham, Mill |
| Normative Ethics | Kant (156) | 78 | 31 s | **45 m** | Aristotle |
| Applied Ethics | Singer (206) | 103 | 41 s | **60 m** | Slater |
| Social and Political | Hobbes (152) | 76 | 30 s | **45 m** | Mozi, Rawls |

Practical notes for P3:

- Put each member where a path from the lead naturally runs, e.g. down a slope or round a rock, rather than in open ground. The NavMesh walker (W2) follows paths around obstacles, so a winding route can be shorter as the crow flies than the target: aim for the target *along the route*.
- Members who also have a cross-area handoff (table 2) are best placed on the side of the ring that faces the other area.

## 2. Across areas: the twelve handoff edges

| Edge | From → To | Areas | Words on the walk | Walk time | **Target distance** | Distance today (area centre to centre) |
|---|---|---|---|---|---|---|
| E1 | Descartes → Elisabeth | Epistemology → Mind | 140 | 56 s | **85 m** | 294 m |
| E2 | Plato → Descartes | Metaphysics → Epistemology | 130 | 52 s | **80 m** | 335 m |
| E3 | Anselm → Kant | Religion → Normative | 106 | 42 s | **65 m** | 332 m |
| E4 | Aristotle → Plato | Normative → Metaphysics | 92 | 37 s | **55 m** | 718 m |
| E5 | Moore → Nozick | Epistemology → Well-Being | 104 | 42 s | **60 m** | 660 m |
| E6 | Leibniz → Heim | Religion → Metaphysics | 97 | 39 s | **60 m** | 524 m |
| E7 | Nagel → Bentham | Mind → Well-Being | 150 | 60 s | **90 m** | 466 m |
| E8 | Bentham → Kant | Well-Being → Normative | 74 | 30 s | **45 m** | 275 m |
| E9 | Hume → Lewis | Metaphysics → Probability | 100 (est.) | 40 s | **60 m** | 641 m |
| E10 | Lewis → Leibniz | Probability → Religion | 100 (est.) | 40 s | **60 m** | 188 m |
| E11 | Gettier → Plato | Epistemology → Metaphysics | 100 (est.) | 40 s | **60 m** | 335 m |
| E12 | Mill → Moore | Well-Being → Epistemology | 100 (est.) | 40 s | **60 m** | 660 m |

**The gap.** Every cross-area edge is 3 to 13 times longer today than its dialogue would fill. You can't close the gap by moving areas: the twelve edges link the areas in a web (Metaphysics alone connects to Epistemology, Religion, Normative and Probability), so pulling some areas together pushes others apart. You need to choose one of these (a P3 decision):

- **A. Meeting points (recommended).** For each edge, put an empty GameObject such as `Meet_E1` at the target distance from philosopher A, on the way to B's area. The handoff node uses `<<walk_to Meet_E1>>`. At the meeting point, A points the way and the player walks the rest alone, which is the same exploring they do today between areas. B's conversation starts when the player reaches B. No dialogue needs padding, and the areas stay where they are.
- **B. Escort all the way.** Use `<<handoff B>>` directly and fill the extra distance with checkpoint questions (W2 asks the next one whenever the player stops). That costs about **1.7 words per extra metre**, e.g. ~350 extra words for E1 and ~1,100 for E4. That is a lot of new writing, and long escorted walks risk feeling slow.
- **C. Pull the closest pairs in.** Move Probability toward Religion (E10, today 188 m) and Well-Being toward Normative (E8, 275 m) so those two edges can be escorted the whole way. Use A for the rest.

Whichever you choose, the target distances above are what the *escorted* part of each walk should measure.

## 3. Hub (Socrates)

Socrates sends the player to every area's lead via the existing `Gate_` nodes. The player walks those routes alone, so they aren't sized to dialogue. Nothing to change here.
