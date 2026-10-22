# Experience Machine — Backlog

Unity project: `C:\Users\Administrator\Desktop\MIHLab\My project`
GitHub: `redapemusic35/The-Experience-Machine-2026-08-07_01-45-23` (scheduled runs work here, on branch `claude/auto`)
Dialogue: `Assets/Dialogue` (Yarn Spinner 3.x)

How to read this file:
- **Status**: `todo` · `doing` · `review` (Claude finished, Jim must check) · `done` · `blocked`
- **Who**: `auto` = a scheduled run may pick it up · `jim` = needs the Unity Editor, a headset, an account, or a judgment call
- **Size**: `S` fits in one run. Anything `M` or `L` must be split into `S` tasks before a scheduled run touches it.
- Scheduled runs take the **first** `todo / auto / S` task whose dependencies are `done`. Jim reorders by moving lines.
- **Issue**: each track (and each bug) has a GitHub issue with one checkbox per task. Runs tick the box when a task reaches `review`; Jim checks it off in Notion when he merges it, and closes the issue when every box is done.

---

## Track 1 — Shared walk-and-talk system (issue #1)

The goal: every philosopher walks with the player the way Socrates and the Chalmers figure already do, and can hand the player off to another philosopher depending on their choices.

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| W1 | done | auto | S | Read `Assets/Dialogue/DialogueTrigger.cs` (Socrates' walker), `Assets/Dialogue/PhilosopherNPC.cs` (stationary philosophers), and whichever component drives the Chalmers figure (search the `.unity` scene files for its GameObject and component GUIDs; read-only). Write `Docs/walk-system-audit.md` listing what each does, what they duplicate, and what is hard-coded to one NPC. No code changes. | — | Audit file exists and names every hard-coded reference Approved by Jim 2026-10-22. |
| W2 | todo | auto | S | Write `PhilosopherWalker.cs`: one reusable component (follow offset, walk speed, stop-when-player-lags, IsWalking animator param). Uses NavMeshAgent so NPCs path around trees instead of walking in straight lines. Leaves the old scripts untouched. | W1 | Script compiles in principle (no undeclared types); public fields documented in a header comment |
| W3 | todo | auto | S | Write `YarnWalkCommands.cs`: Yarn commands `<<walk_to DESTINATION>>`, `<<stop_walk>>`, `<<handoff PHILOSOPHER>>`, `<<resume_walk>>`. Handoff walks the player to the named philosopher and starts that philosopher's entry node with `$came_from` set. | W2 | Commands registered; a sample `.yarn` snippet in the header comment shows usage |
| W4 | todo | auto | S | Write `Editor/MIHLabSetup.cs` with a menu item **MIHLab → Convert selected NPC to Walker** that adds `PhilosopherWalker` + `NavMeshAgent` to the selected GameObject and copies settings from the old script if present. (Editor script, so Claude never edits `.unity` scene files directly.) | W2 | Menu item exists; instructions in `Docs/how-to-convert-npc.md` |
| W5 | todo | jim | S | In Unity: bake a NavMesh for the forest, run the W4 menu item on Socrates, play-test the plane-tree walk. Report anything off in the Notes column below. | W4 | Socrates walk behaves as before |
| W6 | todo | jim | S | Same as W5 for the Chalmers figure; then delete or disable the old walker scripts. | W5 | Both figures run on the shared system |

## Track 2 — Spread the philosophers out and connect them (issue #2)

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| P1 | done | jim | S | Decide the roster: which 2–3 philosophers stand in each area (Metaphysics, Epistemology, Value Theory), and which pairs should be able to hand off to each other. A rough list in this file is enough. | — | Roster table filled in below 2026-10-22: draft roster below (Claude), built from the areas and leads already in `RealityPlus_Map.yarn`, checked against PhilPapers and the SEP entries Jim named; handoffs E1-E8 each cite an SEP section. Jim: confirm or edit, then mark done. Confirmed by Jim 2026-10-22, adding Hume, Lewis, Mill and Gettier (N1-N4). |
| P2 | todo | auto | S | From the roster, write `Docs/layout-plan.md`: suggested distance between each connected pair, sized so the walk lasts about as long as the dialogue segment spoken during it (estimate words ÷ reading speed). | P1 | Plan lists every edge with a target distance in metres |
| P3 | todo | jim | M | Place the philosophers in the scene following the layout plan. | P2, W6 | All figures placed and reachable on the NavMesh |
| P4 | todo | auto | S | Write a handoff dialogue node for ONE philosopher pair (first edge in the roster): the player's choice decides whether the walk continues to philosopher B or stays. Follows existing conventions in `technology.yarn`. No copyrighted text. | W3, P1, Y1 | Node added in its own `.yarn` file; passes Y1 validation |
| P5 | todo | auto | S | Repeat P4 for the next edge in the roster. (A run copies this row for each remaining edge.) | P4 | — |

## Track 3 — Carryable map / study-guide notebook (issue #3)

Students' assignments cited the examples used in the game, so the map should double as a record of what they encountered.

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| M1 | todo | auto | S | Write `LocationRegistry` ScriptableObject: areas, philosophers, positions, and the example/thought experiment each one discusses. This is the single source of truth the map and walk system both read. | P1 | Script + one sample asset definition in a comment |
| M2 | todo | auto | S | Write `MapUI.cs`: toggle with **M** (and a mobile button), a static top-down map image, pins generated from the registry, "you are here" marker, pins greyed until the philosopher has been met. | M1 | Script compiles in principle; setup steps in `Docs/how-to-add-map.md` |
| M3 | todo | auto | S | Add a notebook panel: tapping a met philosopher's pin shows the examples and questions from that conversation (pulled from Yarn variables set during dialogue). | M2 | Panel script + list of Yarn variables each node must set |
| M4 | todo | jim | S | Decide fast travel: none / only to places already visited / anywhere. (Fast travel to anywhere removes the walk as reward; visited-only keeps it.) | — | Decision written here |
| M5 | todo | jim | S | In Unity: capture or draw the map image, create the registry asset, wire up the UI. | M3 | Map opens in play mode |
| M6 | todo | auto | S | Write a "notebook export" feature: one button that copies the notebook as plain text so students can paste it into their notes. | M3 | — |

## Track 2b — New philosophers from the SEP check (issue #2)

New dialogue goes in its own file, `Assets/Dialogue/SEP_Additions.yarn`, so Jim's `RealityPlus_Map.yarn` is not altered. Paraphrase only; no quoted text.

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| N1 | todo | auto | S | `Talk_Hume` in `SEP_Additions.yarn`: causation as constant conjunction, no impression of substance, doubts about metaphysics (SEP Metaphysics §2.3, §3.4, §5). Ends with the E9 handoff choice (-> Lewis) using W3's `<<handoff>>`, or a `TODO(handoff)` comment if W3 isn't done. | Y1 | Node passes Y1; one line in Jim's `Menu_Metaphysics` is listed for Jim to add (runs don't edit his file) |
| N2 | todo | auto | S | `Talk_Lewis`: modal realism and counterfactual causation (SEP Metaphysics §3.1, §3.4), tied to the simulation argument for the Probability area. E10 handoff (-> Leibniz). | Y1 | As N1, for `Menu_Probability` |
| N3 | todo | auto | S | `Talk_Mill`: higher and lower pleasures, pushing back on Bentham and on the experience machine (SEP Value Theory §2.2). E12 handoff (-> Moore). | Y1 | As N1, for `Menu_WellBeing` |
| N4 | todo | auto | S | `Talk_Gettier`: a Gettier case built from in-game events (for example, a true belief about where Socrates is, reached by luck). E11 handoff (-> Plato). | Y1 | As N1, for `Menu_Epistemology` |
| N5 | todo | jim | S | Add the four NPCs to the scene (PhilosopherNPC with `startNode` Talk_Hume etc.) during P3 placement, add the menu lines from N1-N4, then play-test. | N1-N4, C2 | All four reachable and talking in play mode |

## Track 2c — Bodies for the philosophers, reusing existing models (issue #2)

All 31 stationary philosophers are grey capsules today. Jim wants no new character models, to keep the build small. Reuse the four Mixamo bodies already in the project, and make each figure distinct with colour, hair and height. NPCs sharing a body share its mesh and textures in memory; recolours use `MaterialPropertyBlock`, so they add no new files.

Bodies: Mr Herbert (`Ch42`, 24 MB; already in the build as Socrates), shorts-shirt-guy (51 MB; in the build), business-lady (56 MB; not in the build yet), hiker-lady (50 MB; not in the build). Prefer the two already in the build; use one of the lady models only for women in the roster (Elisabeth of Bohemia today), and check the build size when it's added.

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| C1 | todo | auto | S | Write `Assets/Dialogue/NPCLook.cs`: sets per-renderer colours with `MaterialPropertyBlock` (clothing slots; don't recolour skin), switches between existing hair/child meshes by name, and sets height (0.92-1.08 scale). Settings come from a small `NPCLookPreset` ScriptableObject so looks can be reused. No new textures or meshes. | — | Compiles in principle; header comment lists each body's renderer and hair slot names (read from the prefab/FBX meta files) |
| C2 | todo | auto | S | Editor menu **MIHLab → Give selected philosophers a body**: for each selected capsule NPC, hides the capsule mesh, adds the chosen body prefab as a child with the idle animator, and adds `NPCLook`. `PhilosopherNPC` and its collider are untouched. Editor script, so runs never edit the scene. | C1 | Menu item exists; steps in `Docs/how-to-give-npc-a-body.md` |
| C3 | todo | auto | S | `Docs/character-looks.md`: for every roster philosopher (35 + N1-N4), a body, a clothing palette, hair and height, chosen so area-mates look clearly different. Period hints where cheap (for example, dark coat for 17th-18th century figures). | P1 | Table covers every roster name |
| C4 | todo | jim | S | Run C2 on the capsules using C3's table, look at it in play mode, run Deploy WebGL, and compare the build size with today's (~54 MB data file). | C2, C3 | Every philosopher has a body; build size noted here |

## Track 4 — Platform (issues #4 web, #5 Quest, #6 privacy/support, #7 Roblox)

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| D1 | done | auto | S | Write a GitHub Actions workflow + `Docs/deploy-webgl.md` to publish a WebGL build to mihlab.org (GitHub Pages) at `/play`, with the compression fallback Pages needs. Claude writes it; Jim runs the first build. | — | Workflow file + doc — done 2026-10-21: workflow, doc, and Assets/Editor/WebGLBuild.cs (sets Gzip + decompression fallback per build). Unblocks D2. 2026-10-21: retargeted to push into MIHLab-Org/mihlab-org.github.io/play (needs SITE_DEPLOY_TOKEN). 2026-10-22: live at mihlab.org/play; workflow builds desktop (DXT) + phone (ASTC) textures, see X8. |
| D2 | done | jim | S | Make a WebGL build, push it, confirm students can load mihlab.org/play on a lab computer and a phone. | D1 | Link works — 2026-10-22: works on a computer. Phone split out to D7. |
| D7 | blocked | jim | S | Phone check: load mihlab.org/play on a phone after X8's fix is deployed. Jim's iPhone 11 failed on 2026-10-22, but that build had no real ASTC textures (X8), so the phone itself is untested. Parked until a deploy with X8 and, if the iPhone 11 still fails, another device to compare. | X8 | Loads on at least one current iPhone and one Android phone 2026-10-22: X8 is live, so this is unblocked; retest the iPhone 11 when convenient. 2026-10-22: retested on iPhone 11 with real ASTC textures; Safari keeps reloading the page, which usually means iOS killed the tab for memory (scene needs ~1 GB). Parked until another device is available to compare; fixing it means cutting memory use. |
| D3 | todo | auto | S | Write `Docs/quest-readiness.md`: checklist against Meta's current store requirements (IL2CPP, ARM64, frame rate, 45-minute no-crash playthrough, locomotion options, privacy policy URL, website). Mark each item met / unmet / unknown for this project. | — | Checklist exists |
| D4 | todo | auto | S | Draft the privacy policy and support page text for mihlab.org (overlaps with Gate C). Draft only; Jim reviews before anything is published. | — | Drafts in `Docs/legal-drafts/` |
| D5 | todo | jim | M | Add the Meta XR / OpenXR packages and a VR rig; first Quest build to a private release channel for testers. | D3 | Build runs on a Quest |
| D6 | todo | jim | L | Roblox edition — **parked**. Full rewrite in Luau; nothing carries over except design and dialogue text. Revisit after the Quest build. | — | — |

## Track 5 — Tooling that makes the runs safer (issue #8)

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| Y1 | todo | auto | S | Write `Tools/validate_yarn.py`: checks every `.yarn` file for missing jump targets, orphaned nodes, duplicate node titles, and the colon-dash parser issue found earlier. Runs with plain Python, no Unity needed. | — | Script runs clean on current dialogue or reports real issues |

**Suggested first order:** Y1 → W1 → W2 → W3 → W4 → D1 → D3 → (Jim: P1, W5) → M1 …

## Track 6 — Bugs

Each bug has its own GitHub issue written with the bug template.

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| X1 | todo | jim | S | Bug #9 (B1): play-test the Chalmers walk and decide how `state_ambivalence` should end, so `real_value_arrival` plays once. Jim's dialogue, so runs don't edit it. | — | Arrival plays once |
| X2 | todo | auto | S | Bug #10 (B2): in `DialogueTrigger.OnGUI`, also require `Debug.isDebugBuild` so the debug overlay never shows in release builds. Code only; no scene edits. | — | Overlay hidden in release builds |
| X3 | todo | auto | S | Bug #11 (B3): empty `DialogueTrigger`'s default node names and log a warning naming any required node field left blank. Low priority; skip (mark `done`, note "superseded") if W6 is already `done`. | — | No default points at a missing node |
| X4 | todo | auto | S | Bug #12 (B4): `PhilosopherNPC` reads talk input from an `InputActionReference` (keyboard E, gamepad south, XR primary button) with the current keys as fallback, plus an optional on-screen Talk button for touch; prompt names the right control. | — | Talk works without a keyboard; setup steps in the header comment |
| X4b | done | jim | S | Dialogue advances on **F**, not Escape (2026-10-22, Jim). In the browser, Escape also exits fullscreen and releases the mouse, and a web page can't intercept it, so players had to press it twice and lost mouse look. In Unity: select **Dialogue System**, then in its **Line Advancer Input** (Key Codes) component set **Next Line Key Code** to **F** (it's Escape now). The other keys there are Keypad 0/1/3 (hurry up, hurry up options, cancel); leave them or set Hurry Up to F as well if one key should do both. Jim's because it's a scene setting and runs don't edit `.unity` files. | — | F advances dialogue in a WebGL build; Escape no longer needed; fullscreen and mouse look stay on while reading Done 2026-10-22 (Jim): Hurry Up, Next Line and Hurry Up Options all on F; works in the web build. |
| X4c | todo | auto | S | Arrow keys turn, A/D strafe (2026-10-22, Jim). Today the arrow keys duplicate WASD (left/right strafe). In `Assets/StarterAssets/InputSystem/StarterAssets.inputactions`: remove leftArrow/rightArrow from the Move composite (keep up/down arrows for forward/back) and add a `Turn` action (1D axis: leftArrow negative, rightArrow positive). In `StarterAssetsInputs.cs` add `turn` + `OnTurn`; in `FirstPersonController.cs` CameraRotation add `turn * TurnSpeed * Time.deltaTime` to the yaw (new public `TurnSpeed`, default 120 deg/s), alongside mouse look. Script/asset edits only; no scene edits. | — | Left/right arrows turn the player; A/D strafe; up/down arrows and W/S move forward/back; mouse look unchanged |
| X5 | done | auto | S | Bug #13 (B5): add `New Terrain*.asset binary` under the `LightingData.asset binary` line in `.gitattributes`. | — | Fresh clone shows a clean `git status` — done 2026-10-21 (interactive, Jim): pattern is `New[[:space:]]Terrain*.asset binary` because gitattributes patterns cannot contain spaces. Was blocking the WebGL CI build ("Branch is dirty"). |
| X6 | done | auto | S | Ground missing in the web build (player fell through the sky). | — | Done 2026-10-21 (interactive): `New Terrain.asset` (centre tile), `UNS_Terrain.asset` and one Polytope TerrainData were binary files damaged by the `*.asset` eol=lf rule (CR bytes stripped). Restored from Jim's local copies byte-for-byte; added `TerrainData*.asset` / `UNS_Terrain.asset binary`. Note: 8 of SampleScene's 9 terrain tiles point at data that exists nowhere, locally or in git (empty tiles; not a bug in play, but worth deleting or re-creating). |
| X7 | done | auto | S | Web build crashed with out-of-memory once the terrain loaded. | X6 | Done 2026-10-21: `WebGLBuild.cs` raises Maximum Memory to 2048 MB for the build only (project setting stays 512). Scene uses ~1 GB. |
| X8 | done | auto | S | Phone build actually uses ASTC textures. | X7 | 2026-10-22: the first dual build shipped DXT textures in both data files (`BuildPlayerOptions.subtarget` left at default overrides `webGLBuildSubtarget`). Fixed in `WebGLBuild.cs`; `Tools/webgl_add_phone_build.py` now refuses to publish unless the phone data file contains ASTC textures. Jim: merge, rerun Deploy WebGL, check the run log says "OK: N ASTC textures". Verified 2026-10-22 on the deploy from 86860d6: phone file has 54 ASTC textures (desktop file: DXT). |

---

## Roster (task P1) — DRAFT for Jim to confirm

Sources (Jim, 2026-10-22): areas follow the PhilPapers taxonomy (philpapers.org: top-level *Metaphysics and Epistemology*, *Value Theory*, *Science, Logic, and Mathematics*, with their subcategories). Handoffs are drawn from the SEP entries [Metaphysics](https://plato.stanford.edu/entries/metaphysics/), [Epistemology](https://plato.stanford.edu/entries/epistemology/) and [Value Theory](https://plato.stanford.edu/entries/value-theory/).

The areas, philosophers and leads (**bold**, where each area's `Gate_` node lands) are the ones Jim already wrote in `Assets/Dialogue/RealityPlus_Map.yarn`; nothing here changes that file. "SEP" marks who the three entries cite.

| Region (PhilPapers) | Area | Philosophers (lead first) | SEP cites | Can hand off to |
|---|---|---|---|---|
| Hub | Plane tree | **Socrates** | — | every area's lead (Gate_ nodes, already written) |
| Metaphysics and Epistemology | Metaphysics | **Heim**, Plato, Berkeley, Hume (new, N1) | Plato (forms, cave); Hume (causation, substance, can metaphysics be done) | Plato -> Descartes (E2); Hume -> Lewis (E9) |
| Metaphysics and Epistemology | Epistemology | **Descartes**, Moore, Zhuangzi, Gettier (new, N4) | Descartes, Moore, Plato; Gettier (Gettier cases) | Descartes -> Elisabeth (E1); Moore -> Nozick (E5); Gettier -> Plato (E11) |
| Metaphysics and Epistemology | Philosophy of Mind | **Elisabeth of Bohemia**, Nagel, Clark | Nagel (value theory entry) | Nagel -> Bentham (E7, optional) |
| Metaphysics and Epistemology | Philosophy of Language | **Putnam**, Carnap | — | within area only |
| Metaphysics and Epistemology | Philosophy of Religion | **Anselm**, Leibniz, Narada | Anselm, Leibniz | Anselm -> Kant (E3); Leibniz -> Heim (E6) |
| Science, Logic, and Mathematics | Computing and Information | **Turing**, Searle, Wheeler | not covered by the three entries | within area only |
| Science, Logic, and Mathematics | Physical Science | **Russell**, Sellars, Galileo | Russell | within area only |
| Science, Logic, and Mathematics | Probability | **Bostrom**, Boltzmann, Lewis (new, N2) | Lewis (modal realism, counterfactual causation; Metaphysics §3.1, §3.4) | Lewis -> Leibniz (E10) |
| Value Theory | Well-Being (Value Theory, Misc) | **Nozick**, Bentham, Mill (new, N3) | Bentham; Mill (higher and lower pleasures) | Bentham -> Kant (E8); Mill -> Moore (E12) |
| Value Theory | Normative Ethics | **Kant**, Aristotle | Kant, Aristotle | Aristotle -> Plato (E4) |
| Value Theory | Applied Ethics | **Singer**, Slater | — | within area only |
| Value Theory | Social and Political | **Hobbes**, Mozi, Rawls | Rawls | within area only |

Within every area, the lead hands off to the others listed (lead -> each). Cross-area handoffs, each with the SEP passage it rests on:

| Edge | From -> To | Shared question | SEP basis |
|---|---|---|---|
| E1 | Descartes -> Elisabeth | How can an immaterial mind move a body? | Metaphysics §3.5 (the mental and the physical) |
| E2 | Plato -> Descartes | Is what we see real, and what turns true opinion into knowledge? | Epistemology intro (Plato: knowledge vs true opinion); Metaphysics §2.2, §4 (forms, the cave) |
| E3 | Anselm -> Kant | Is existence a perfection, or not a real predicate at all? | Metaphysics §2.1 (Anselm, Descartes, Kant on being) |
| E4 | Aristotle -> Plato | Do universals exist apart from things, or only in them? | Metaphysics §2.2 (ante rem vs in re) |
| E5 | Moore -> Nozick | Is knowledge good in itself, or only for what it gets us? | Value Theory §2.2 (Moore on knowledge as intrinsically valuable) |
| E6 | Leibniz -> Heim | From possible worlds to virtual worlds | Metaphysics §3.1 (modality; Leibniz coined "possible world") |
| E7 | Nagel -> Bentham | Good from whose point of view? (optional, weakest link) | Value Theory §1.1.1 (Nagel's point-of-view theory) |
| E8 | Bentham -> Kant | Can every value be priced, or do persons have dignity? | Value Theory §2.2, §2.3.2 (monism; Kant's dignity vs price) |
| E9 | Hume -> Lewis | Is causation just constant conjunction, or counterfactual dependence? | Metaphysics §3.4 (Hume; Stalnaker and Lewis) |
| E10 | Lewis -> Leibniz | Are possible worlds as real as ours, or only possible creations? | Metaphysics §3.1 (Leibniz coined the term; Lewis's modal realism) |
| E11 | Gettier -> Plato | Is knowledge justified true belief? | Epistemology intro and §2.3 (Plato on knowledge vs true opinion; Gettier cases) |
| E12 | Mill -> Moore | Is knowledge only instrumentally good, or good in itself? | Value Theory §2.2 (Mill: instrumental; Moore: intrinsic) |

Suggested first edge for P4: **E1 (Descartes -> Elisabeth)**. Both already have dialogue, and the mind-body problem is the clearest hand-over.

Added 2026-10-22 (Jim): Hume, David Lewis, Mill and Gettier, which SEP treats as central and which had no NPC (tasks N1-N4). Metaphysics and Epistemology now have four figures each, one more than the 2-3 originally planned. Lewis sits in Probability (his work on chance and self-locating belief fits Bostrom's simulation argument) to keep Metaphysics from growing to five. Heim, Zhuangzi, Nozick, Clark, Slater, Singer, Mozi and Hobbes come from *Reality+* rather than these three SEP entries; their PhilPapers areas still fit.

## Notes from Jim

(Play-test observations, reordering decisions, anything a run should know.)

- 2026-10-22: mihlab.org/play works on a computer; phone testing parked (D7).
- 2026-10-22 (Claude): the debug overlay from X2 is visible on the live site, so X2 is worth doing soon.
- 2026-10-22: dialogue should advance on F, not Escape (X4b); arrow keys should turn, A/D strafe (X4c).
