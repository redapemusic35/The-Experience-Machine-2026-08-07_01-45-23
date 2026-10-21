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
| W1 | review | auto | S | Read `Assets/Dialogue/DialogueTrigger.cs` (Socrates' walker), `Assets/Dialogue/PhilosopherNPC.cs` (stationary philosophers), and whichever component drives the Chalmers figure (search the `.unity` scene files for its GameObject and component GUIDs; read-only). Write `Docs/walk-system-audit.md` listing what each does, what they duplicate, and what is hard-coded to one NPC. No code changes. | — | Audit file exists and names every hard-coded reference |
| W2 | todo | auto | S | Write `PhilosopherWalker.cs`: one reusable component (follow offset, walk speed, stop-when-player-lags, IsWalking animator param). Uses NavMeshAgent so NPCs path around trees instead of walking in straight lines. Leaves the old scripts untouched. | W1 | Script compiles in principle (no undeclared types); public fields documented in a header comment |
| W3 | todo | auto | S | Write `YarnWalkCommands.cs`: Yarn commands `<<walk_to DESTINATION>>`, `<<stop_walk>>`, `<<handoff PHILOSOPHER>>`, `<<resume_walk>>`. Handoff walks the player to the named philosopher and starts that philosopher's entry node with `$came_from` set. | W2 | Commands registered; a sample `.yarn` snippet in the header comment shows usage |
| W4 | todo | auto | S | Write `Editor/MIHLabSetup.cs` with a menu item **MIHLab → Convert selected NPC to Walker** that adds `PhilosopherWalker` + `NavMeshAgent` to the selected GameObject and copies settings from the old script if present. (Editor script, so Claude never edits `.unity` scene files directly.) | W2 | Menu item exists; instructions in `Docs/how-to-convert-npc.md` |
| W5 | todo | jim | S | In Unity: bake a NavMesh for the forest, run the W4 menu item on Socrates, play-test the plane-tree walk. Report anything off in the Notes column below. | W4 | Socrates walk behaves as before |
| W6 | todo | jim | S | Same as W5 for the Chalmers figure; then delete or disable the old walker scripts. | W5 | Both figures run on the shared system |

## Track 2 — Spread the philosophers out and connect them (issue #2)

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| P1 | todo | jim | S | Decide the roster: which 2–3 philosophers stand in each area (Metaphysics, Epistemology, Value Theory), and which pairs should be able to hand off to each other. A rough list in this file is enough. | — | Roster table filled in below |
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

## Track 4 — Platform (issues #4 web, #5 Quest, #6 privacy/support, #7 Roblox)

| ID | Status | Who | Size | Task | Depends on | Done when |
|---|---|---|---|---|---|---|
| D1 | review | auto | S | Write a GitHub Actions workflow + `Docs/deploy-webgl.md` to publish a WebGL build to mihlab.org (GitHub Pages) at `/play`, with the compression fallback Pages needs. Claude writes it; Jim runs the first build. | — | Workflow file + doc — done 2026-10-21: workflow, doc, and Assets/Editor/WebGLBuild.cs (sets Gzip + decompression fallback per build). Unblocks D2. 2026-10-21: retargeted to push into MIHLab-Org/mihlab-org.github.io/play (needs SITE_DEPLOY_TOKEN). |
| D2 | todo | jim | S | Make a WebGL build, push it, confirm students can load mihlab.org/play on a lab computer and a phone. | D1 | Link works |
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
| X5 | review | auto | S | Bug #13 (B5): add `New Terrain*.asset binary` under the `LightingData.asset binary` line in `.gitattributes`. | — | Fresh clone shows a clean `git status` — done 2026-10-21 (interactive, Jim): pattern is `New[[:space:]]Terrain*.asset binary` because gitattributes patterns cannot contain spaces. Was blocking the WebGL CI build ("Branch is dirty"). |

---

## Roster (filled in by Jim — task P1)

| Area | Philosophers | Can hand off to |
|---|---|---|
| Plane tree | Socrates | (area guides) |
| Metaphysics | | |
| Epistemology | | |
| Value Theory | | |

## Notes from Jim

(Play-test observations, reordering decisions, anything a run should know.)
