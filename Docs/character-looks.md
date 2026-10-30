# Character looks (C3)

One row per roster philosopher (P1 roster plus Hume, Lewis, Mill and Gettier from N1–N4). Each row gives a body, clothing colours, hair and height, chosen so that philosophers in the same area look clearly different. These are game-art choices for telling figures apart, not likenesses.

- **Bodies.** Only the two bodies already in the build are used, plus `business-lady` for Elisabeth of Bohemia, the one woman on the roster.
  - **Mr Herbert** (robe and slippers) goes to ancient, medieval and pre-1900 figures, with a dark robe as a cheap "coat" hint for the 17th–19th centuries.
  - **shorts-shirt-guy** goes to 20th–21st-century figures.
- **Preset** is the `NPCLookPreset` asset name. The C2 menu finds it automatically.
- **Colours** are tints (multiplied with the texture), written as hex. White `#FFFFFF` means "leave as is".
- **Renderer names.** The C1 header in `NPCLook.cs` lists them for every body.
  - Herbert: tint `robe` (all of it) and optionally `Slippers_Socks`. Hair is `Old_Long` (long) and `RL_G6_Hair` (short layer).
  - shorts-shirt-guy: tint `Ch42_Shirt`, `Ch42_Shorts` and `Ch42_Sneakers`. Hair is `Ch42_Hair1` (on or off; hair tints can only darken, so it is left untinted).
  - business-lady: tint `Ch22_Shirt`, `Ch22_Pants` and `Ch22_Sneakers`. Hair is `Ch22_Hair`.
- **Height**: 0.92–1.08 (`NPCLook` clamps to that range).

## Hub (no change)

| Philosopher | Preset | Body | Colours | Hair | Height |
|---|---|---|---|---|---|
| Socrates | — (keep as is) | Mr Herbert | as imported | as imported | 1.00 |
| Chalmers figure | — (keep as is) | shorts-shirt-guy | as imported | as imported | 1.00 |

## Metaphysics and Epistemology

| Area | Philosopher | Preset | Body | Colours | Hair | Height |
|---|---|---|---|---|---|---|
| Metaphysics | **Heim** (lead) | Look_Heim | shorts-shirt-guy | shirt slate blue `#5B7DB1`, shorts charcoal `#3A3A3A` | on | 1.02 |
| Metaphysics | Plato | Look_Plato | Mr Herbert | robe cream `#EDE6D6` | long + short | 1.06 |
| Metaphysics | Berkeley | Look_Berkeley | Mr Herbert | robe navy coat `#2B3550` | short only | 0.98 |
| Metaphysics | Hume (N1) | Look_Hume | Mr Herbert | robe bottle green `#3E4A33` | long only | 1.04 |
| Epistemology | **Descartes** (lead) | Look_Descartes | Mr Herbert | robe black coat `#262626` | long only | 0.96 |
| Epistemology | Moore | Look_Moore | shorts-shirt-guy | shirt cream `#E8DFC8`, shorts brown `#6B4F35` | on | 1.04 |
| Epistemology | Zhuangzi | Look_Zhuangzi | Mr Herbert | robe ochre `#C9A24A` | long + short | 0.95 |
| Epistemology | Gettier (N4) | Look_Gettier | shorts-shirt-guy | shirt maroon `#7A2E35`, shorts navy `#2B3550` | on | 0.97 |
| Philosophy of Mind | **Elisabeth of Bohemia** (lead) | Look_Elisabeth | business-lady | shirt deep purple `#4B2E5E`, pants black `#262626`, sneakers dark grey `#4A4A4A` | on | 0.96 |
| Philosophy of Mind | Nagel | Look_Nagel | shorts-shirt-guy | shirt mid grey `#8A8A8A`, shorts black `#262626` | on | 1.00 |
| Philosophy of Mind | Clark | Look_Clark | shorts-shirt-guy | shirt teal `#2E7D7A`, shorts khaki `#B8A27A` | off | 1.05 |
| Philosophy of Language | **Putnam** (lead) | Look_Putnam | shorts-shirt-guy | shirt olive `#6B7340`, shorts grey `#7D7D7D` | on | 1.03 |
| Philosophy of Language | Carnap | Look_Carnap | shorts-shirt-guy | shirt white `#FFFFFF`, shorts navy `#2B3550` | off | 0.99 |
| Philosophy of Religion | **Anselm** (lead) | Look_Anselm | Mr Herbert | robe dark monk's brown `#3B2F2A` | none (both hidden) | 0.97 |
| Philosophy of Religion | Leibniz | Look_Leibniz | Mr Herbert | robe deep red coat `#7A2420` | long only (wig) | 0.95 |
| Philosophy of Religion | Narada | Look_Narada | Mr Herbert | robe saffron `#D9822B` | long + short | 1.02 |

## Science, Logic, and Mathematics

| Area | Philosopher | Preset | Body | Colours | Hair | Height |
|---|---|---|---|---|---|---|
| Computing and Information | **Turing** (lead) | Look_Turing | shorts-shirt-guy | shirt white `#F2F2F2`, shorts flannel grey `#7D7D7D` | on | 1.00 |
| Computing and Information | Searle | Look_Searle | shorts-shirt-guy | shirt navy `#2B3550`, shorts tan `#B8A27A` | off | 1.04 |
| Computing and Information | Wheeler | Look_Wheeler | shorts-shirt-guy | shirt light blue `#9DB8D9`, shorts charcoal `#3A3A3A` | on | 0.95 |
| Physical Science | **Russell** (lead) | Look_Russell | shorts-shirt-guy | shirt dark grey `#4A4A4A`, shorts black `#262626` | on | 0.97 |
| Physical Science | Sellars | Look_Sellars | shorts-shirt-guy | shirt brick `#A0522D`, shorts navy `#2B3550` | off | 1.04 |
| Physical Science | Galileo | Look_Galileo | Mr Herbert | robe dark green-black coat `#2F3A2F` | long + short | 1.00 |
| Probability | **Bostrom** (lead) | Look_Bostrom | shorts-shirt-guy | shirt black `#262626`, shorts denim `#3A4A6A` | off | 1.02 |
| Probability | Boltzmann | Look_Boltzmann | Mr Herbert | robe charcoal frock coat `#3A3A3A` | long + short | 1.06 |
| Probability | Lewis (N2) | Look_Lewis | shorts-shirt-guy | shirt mustard `#C9A227`, shorts brown `#6B4F35` | on | 0.98 |

## Value Theory

| Area | Philosopher | Preset | Body | Colours | Hair | Height |
|---|---|---|---|---|---|---|
| Well-Being | **Nozick** (lead) | Look_Nozick | shorts-shirt-guy | shirt burgundy `#6E1F2E`, shorts grey `#7D7D7D` | on | 1.00 |
| Well-Being | Bentham | Look_Bentham | Mr Herbert | robe plum coat `#5A3A4A` | long only | 0.96 |
| Well-Being | Mill (N3) | Look_Mill | Mr Herbert | robe black coat `#262626` | short only | 1.04 |
| Normative Ethics | **Kant** (lead) | Look_Kant | Mr Herbert | robe slate blue coat `#34405A` | long only (wig) | 0.92 |
| Normative Ethics | Aristotle | Look_Aristotle | Mr Herbert | robe pale blue-grey `#A9B8C9` | short only | 1.03 |
| Applied Ethics | **Singer** (lead) | Look_Singer | shorts-shirt-guy | shirt leaf green `#4F7F4F`, shorts khaki `#B8A27A` | off | 1.02 |
| Applied Ethics | Slater | Look_Slater | shorts-shirt-guy | shirt orange `#D2691E`, shorts navy `#2B3550` | on | 0.97 |
| Social and Political | **Hobbes** (lead) | Look_Hobbes | Mr Herbert | robe black coat `#262626` | long + short | 1.04 |
| Social and Political | Mozi | Look_Mozi | Mr Herbert | robe undyed hemp `#B5A88A` (Mohists wore plain, coarse cloth) | short only | 0.96 |
| Social and Political | Rawls | Look_Rawls | shorts-shirt-guy | shirt light blue `#9DB8D9`, shorts khaki `#B8A27A` | on | 1.00 |

## How to read "Hair"

| Body | Value | Show | Hide |
|---|---|---|---|
| Mr Herbert | long + short | `Old_Long`, `RL_G6_Hair` | — |
| Mr Herbert | long only | `Old_Long` | `RL_G6_Hair` |
| Mr Herbert | short only | `RL_G6_Hair` | `Old_Long` |
| Mr Herbert | none | — | `Old_Long`, `RL_G6_Hair` |
| shorts-shirt-guy | on / off | `Ch42_Hair1` / — | — / `Ch42_Hair1` |
| business-lady | on | `Ch22_Hair` | — |

`RL_G6_Hair` is a short-hair layer; what it covers exactly is only visible in Unity. If "short only" looks odd on Mr Herbert, swap to "none" or "long only" and note it here.

## Counts

- Mr Herbert: 17 (16 new presets plus Socrates).
- shorts-shirt-guy: 19 (18 new presets plus the Chalmers figure).
- business-lady: 1 (Elisabeth). This is the only body not yet in the build, so check the build size after adding it (C4).
- 35 new presets in total: the 31 stationary philosophers already in the scene plus the 4 from N1–N4.

Same-area check: in every area, no two figures share a body *and* a colour family. Where two or more Herberts share an area (Metaphysics, Epistemology, Religion, Well-Being, Normative, Political), they differ in robe colour and in hair or height, so they still read apart in the WebGL build's simpler lighting. In Metaphysics, Berkeley and Hume both wear dark coats, so they differ in hair (short / long) and height (0.98 / 1.04).
