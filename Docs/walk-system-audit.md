# Walk-system audit (backlog task W1)

Read-only audit of the three NPC drivers, written to inform W2 (`PhilosopherWalker`) and W3 (`YarnWalkCommands`). No code or scene was changed.

Sources read: `Assets/Dialogue/DialogueTrigger.cs`, `Assets/Dialogue/PhilosopherNPC.cs`, `Assets/Scenes/SampleScene.unity` (searched for the script GUIDs below), `Assets/Dialogue/technology.yarn` (node titles and jumps only).

| Script | GUID |
|---|---|
| `DialogueTrigger.cs` | `4c95a950c306dd34d91473106095d00a` |
| `PhilosopherNPC.cs` | `038d82935c5bda34db1bd1e6665f263c` |

## 1. Who uses what

**There is no separate Chalmers script.** Both walking figures run `DialogueTrigger` with different Inspector values. `SampleScene.unity` contains exactly two `DialogueTrigger` components and 31 `PhilosopherNPC` components.

| Figure | GameObject (prefab instance) | Component | Key settings |
|---|---|---|---|
| Socrates | `Commission Mr_ John Herbert Family Guy Realistic 1_0 (1)` (prefab `ab56f2aa…`) | `DialogueTrigger` | join `Start`, walking `technology_greeting`, `Walk_Checkpoint_1`, arrival `plane_tree`, revisit `Talk_Socrates`, destination/sit = `PT_Fruit_Tree_01_green`, followSpeed 3, sideOffset −1, forwardOffset 1.9, seated anims **off** |
| Chalmers | `shorts-shirt-guy` (prefab `252bed16…`) | `DialogueTrigger` | join `real_life`, walking `real_life_first_walk`, arrival `real_value_arrival`, no revisit, destination/sit = `PT_Ore_Rock_01`, triggerRadius 6, followSpeed 2, stopDistance 2.5, sideOffset +1.5, forwardOffset 0.5, seated anims **on** |
| 31 stationary philosophers and area leads (Leibniz, Clark, Searle, Kant (lead), Descartes (lead), …) | one GameObject each | `PhilosopherNPC` | `startNode` = `Talk_<Name>` or `Gate_<Area>` |

Both walkers share the same `DialogueRunner` (`Dialogue System` prefab) and the same `playerOverride` (a Transform inside the `NestedParent_Unpack` prefab, i.e. the player rig).

## 2. What each script does

### `DialogueTrigger` (the walker)

A state machine: `Seated → Rising → Greeting → Walking ⇄ Checkpoint → SittingDown → Arrived`.

- **Seated**: optional call-out node at `callOutRadius`; at `triggerRadius` stands up (optional `StandUp` trigger / `IsSeated` bool).
- **Greeting**: runs `joinNode`, waits for it to end.
- **Walking**: holds a slot beside the player (`sideOffset`, `forwardOffset`), eases speed by distance, sets `IsWalking`. Moves in a **straight line** toward the slot through `CharacterController.Move` (or raw transform), so it cannot path around trees.
- **Checkpoint**: when the player stands still for `stoppedDwellTime` or falls more than `fallBehindDistance` behind, steps `lineOfSightDistance` in front of the player and runs the next `walkingNodes` entry. Cooldown between checkpoints.
- **SittingDown / Arrived**: when the *player* reaches `destination` within `arriveDistance`, walks to `sitSpot`, runs `arrivalNode`; optional `revisitNode` when the player leaves and returns.
- **Grounding**: raycast ground-snap every frame plus a "buried" recovery probe from 300 m up; bounded fall only when no ground is found.
- Dialogue end is detected by polling `DialogueRunner.IsDialogueRunning` (with 0.3 s grace), not events. If another dialogue is already running when it wants to start one, it adopts that one and waits.
- Player lookup: `playerOverride` → tag `Player` → `GameObject.Find("XR Origin")` → tag `MainCamera`.
- Debug `OnGUI` overlay (on for both figures in the scene) and gizmos.

### `PhilosopherNPC` (stationary)

- Finds a `DialogueRunner` (`FindAnyObjectByType` fallback) and player (`playerOverride` → tag `Player`).
- Within `talkRadius`, turns to face the player and shows an `OnGUI` prompt; on **E** / gamepad south (new Input System) — or automatically once per visit if `requireKeyPress` is off — runs `startNode`.
- Never starts while another dialogue is running.

## 3. Duplication between the two

| Concern | `DialogueTrigger` | `PhilosopherNPC` |
|---|---|---|
| Player lookup | `FindPlayerTransform()` (4 fallbacks) | inline, 2 fallbacks (no XR Origin, no camera) |
| Flat (XZ) distance | `FlatDistance()` | inline `a.y = 0; b.y = 0` |
| Face the player | `FaceThePlayer` / `FaceDirection` (Slerp × `turnSpeed`) | inline Slerp × `turnSpeed` |
| "Don't start if runner busy" | in `RunNode` | in `Update` |
| Detect dialogue end | polls `IsDialogueRunning` | polls `IsDialogueRunning` (`talking` flag) |
| Range trigger with re-arm on leave | `revisitArmed` logic | `firedThisVisit` logic |
| `OnGUI` + `OnDrawGizmosSelected` radius sphere | yes | yes |

These are the natural pieces for `PhilosopherWalker` (or a small shared helper) to own.

## 4. Hard-coded references

**In `DialogueTrigger.cs`:**

1. Class doc and comments are written for Socrates and the Phaedrus walk ("Socrates NPC controller", "plane tree", "Yarn sends the player on to Chalmers"); log messages say "Socrates".
2. Default node names in code: `joinNode = "Part_Intro_Technophilosophy"`, `walkingNodes = Walk_Checkpoint_1..4`, `arrivalNode = "PlaneTree"`. None of `Part_Intro_Technophilosophy`, `Walk_Checkpoint_2..4` or `PlaneTree` exists in `technology.yarn` (the scene overrides them, so this only bites a freshly-added component).
3. The state enum is named `SocState`.
4. Player fallback `GameObject.Find("XR Origin")` (exact object name).
5. Tags `"Player"` and `"MainCamera"`.
6. Animator parameter names default to `IsWalking`, `IsSeated`, `StandUp`, `SitDown` (serialized, so overridable).
7. Magic numbers in code, not exposed: speed-easing curve `followSpeed * (dist / 2f) + 0.4f`; dialogue grace `0.3f`; revisit re-arm margin `triggerRadius + 2f`; ground-snap threshold `2f`; player-speed smoothing `0.35f`; debug box size `380×156`.
8. One `destination` per component — a walker can only ever go to one place, which is why handoff (W3) needs a new component.

**In `PhilosopherNPC.cs`:**

9. Default `startNode = "Talk_Descartes"`, `displayName = "Descartes"`.
10. Talk input hard-wired to `Keyboard.current[talkKey]` and `Gamepad.current.buttonSouth` (no touch / XR input — relevant to mobile and Quest).
11. Tag `"Player"`; prompt text format, font size 18 and box size 360×40.

**In the scene (`SampleScene.unity`, values on the two walkers):**

12. Socrates' destination and sit spot are the same object, `PT_Fruit_Tree_01_green`; Chalmers' are both `PT_Ore_Rock_01`. Renaming or deleting either prop silently breaks the walk.
13. Both walkers have `showDebug: 1`, so the debug overlay shows in builds.

**In `technology.yarn` (flow, not code):**

14. The checkpoint chain `real_life_first_walk` → … → `state_ambivalence` ends in `<<jump real_value_arrival>>` (line ~320), so Chalmers' arrival node can play during a mid-walk checkpoint and then play **again** when the player reaches the rock. `real_life_second_walk` is reached only by jumps, not by the walker's checkpoint list. Worth a play-test check during W6.
15. No custom Yarn commands exist yet (`YarnCommand` / `AddCommandHandler` not used anywhere outside plugins), so W3 starts from a clean slate.

## 5. Implications for W2–W4

- `PhilosopherWalker` should take a list of destinations (or a current-destination field set by `<<walk_to>>`) rather than one fixed `destination`.
- Replacing straight-line `CharacterController.Move` with `NavMeshAgent` means the ground-snap code can mostly go; the agent keeps itself on the baked NavMesh. Keep `stopDistance`, `sideOffset`/`forwardOffset`, `fallBehindDistance`, `playerStoppedSpeed` as the "stop-when-player-lags" settings.
- W4's converter should copy, from `DialogueTrigger`: `dialogueRunner`, `playerOverride`, `followSpeed`, `turnSpeed`, `stopDistance`, `sideOffset`, `forwardOffset`, `walkBoolParam`, `fallBehindDistance`, `playerStoppedSpeed`, `stoppedDwellTime`, `dialogueAnimator`, `destination`. `PhilosopherNPC` has nothing walk-related to copy beyond `dialogueRunner`, `playerOverride`, `turnSpeed`, `startNode`.
- The Chalmers settings (stopDistance 2.5, sideOffset +1.5, seated animations on) differ enough from Socrates' that the converter must copy per-NPC values, not apply one preset.
