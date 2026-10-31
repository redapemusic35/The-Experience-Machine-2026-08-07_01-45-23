# How to give the philosophers bodies (C2, used in C4)

Two scripts do this:

- `Assets/Dialogue/NPCLook.cs` and `NPCLookPreset.cs` (C1) set clothing colours, which hair meshes show, and height. They never create new textures or meshes.
- `Assets/Editor/MIHLabBodies.cs` (C2) adds the menu item below.

`Docs/character-looks.md` (C3) gives each philosopher a body and look.

## 1. Make the look presets (once per philosopher)

1. In the Project window, make a folder such as `Assets/Looks`.
2. **Right-click → Create → MIHLab → NPC Look Preset**. Name it `Look_<Name>` using the name from the C3 table, e.g. `Look_Leibniz`, `Look_Descartes`, `Look_Elisabeth`.
3. Fill it in from the C3 row:
   - **Colours**: one entry per clothing renderer (e.g. `robe`, or `Ch42_Shirt` and `Ch42_Shorts`) with its colour. Leave **Material Index** at -1 to tint the whole renderer.
   - **Show** / **Hide**: hair objects (e.g. Hide `RL_G6_Hair` for long hair only).
   - **Height**: 0.92 to 1.08.

The menu finds a preset by name, so with the presets in place you can give a whole area its bodies in one click.

## 2. Give the capsules a body

1. **MIHLab → Give selected philosophers a body**. A small window opens.
2. **Body model** defaults to Mr Herbert; **Animator controller** defaults to `Character.controller` (idle, plus walk on `IsWalking`). Change the body to `shorts-shirt-guy` for the modern figures.
3. Leave **Find preset by name** on.
4. In the Hierarchy, select every capsule that should get this body. Do one body at a time, using the C3 table.
5. Press **Give N selected philosophers this body**.

For each one the menu:

- hides the capsule's MeshRenderer (disabled, not deleted);
- adds the body as a child called `Body`, standing on the capsule's bottom, with the animator controller;
- adds `NPCLook` with the matching `Look_<Name>` preset, if one exists.

`PhilosopherNPC`, the collider and every other component stay as they are, so talking works as before. **Ctrl+Z** undoes it. Save the scene.

The Console prints one line per philosopher. "no look preset yet" means the body is there, untinted. Make the preset, then drag it onto the philosopher's `NPCLook` component.

## 3. Check (C4)

- Press Play. Each philosopher should stand on the ground, play the idle animation, and look different from the others in the same area.
- Run **Deploy WebGL** and compare the data file size with today's (~54 MB). Note it in C4.
- Elisabeth uses `business-lady`, which isn't in the build yet. Expect the size to go up when you add her. If it goes up too much, give her Mr Herbert in a dark robe instead.

## Tips

- Tints multiply the texture, so they can only darken or shift a colour, never lighten it. Mid tones work best. White means "unchanged".
- A body that stands too high or too low means the capsule's pivot isn't where the menu expected. Move the `Body` child up or down.
- To convert a philosopher to a walker as well (W4), run the walker menu after this one, so the NavMeshAgent is sized to the body.
