# How to convert an NPC to the shared walker (W4, used in W5 and W6)

The new walk system is three scripts:

- `Assets/Dialogue/PhilosopherWalker.cs` (W2): walks beside the player with a NavMeshAgent, so the philosopher goes around trees instead of through them. It does the same job as `DialogueTrigger`: greeting, checkpoint questions, arrival, revisit.
- `Assets/Dialogue/YarnWalkCommands.cs` (W3): the Yarn commands `<<walk_to>>`, `<<stop_walk>>`, `<<resume_walk>>` and `<<handoff>>`.
- `Assets/Editor/MIHLabSetup.cs` (W4): the menu item below.

## 1. Bake a NavMesh (once)

The project already has the AI Navigation package (2.0.14).

1. In the Hierarchy, create an empty GameObject named `NavMesh` (or select the terrain).
2. **Add Component → NavMesh Surface**.
3. Leave Agent Type on *Humanoid*. Set **Collect Objects** to *All Game Objects*, and **Use Geometry** to *Physics Colliders* (that way trees with colliders become holes in the NavMesh).
4. Click **Bake**. The walkable area shows up as a blue overlay in the Scene view. Trees and rocks should be cut out of it.
5. Save the scene.

If a philosopher stands off the NavMesh, he still walks, in a straight line, and the Console says `not on a NavMesh`.

## 2. Convert a figure

1. Select the figure in the Hierarchy (Socrates is `Commission Mr_ John Herbert Family Guy Realistic 1_0 (1)`; the Chalmers figure is `shorts-shirt-guy`). You can select several at once.
2. Menu **MIHLab → Convert selected NPC to Walker**.
3. Read the Console line for each figure. The menu:
   - adds a **NavMeshAgent**, sized from the figure;
   - adds **PhilosopherWalker** and copies every setting from `DialogueTrigger` (nodes, destination, sit spot, speeds, offsets, seated animations) or, for a stationary philosopher, from `PhilosopherNPC` (start node, radius);
   - **disables** the old `DialogueTrigger` and any `CharacterController`. Nothing is deleted;
   - adds **YarnWalkCommands** to the `Dialogue System` object if the scene doesn't have one yet.
4. Check **Philosopher Id** on the new component. It is what `$came_from` will say after a handoff. Socrates gets `Socrates` automatically (from `Talk_Socrates`). The Chalmers figure has no revisit node, so type `Chalmers`.
5. Save the scene. **Ctrl+Z** undoes a conversion.

## 3. Play-test (W5 for Socrates, W6 for Chalmers)

Walk the plane-tree route and check:

- he stands, greets, and walks on your left as before;
- he goes **around** trees instead of clipping through them;
- stopping or falling behind still triggers the checkpoint questions;
- arriving at the plane tree still sits him down and starts `plane_tree`;
- the Walk animation plays while he moves and stops when he stops.

Write anything off in the Notes column of W5 or W6. To compare with the old behaviour, disable `PhilosopherWalker` and re-enable `DialogueTrigger`.

Once both figures behave, W6 says you can delete or disable the old `DialogueTrigger` components.

## Stationary philosophers

Converting a stationary philosopher keeps its `PhilosopherNPC`, so "Press E to talk" still works. **Join On Approach** is off, so the philosopher only starts walking when Yarn tells it to:

- `<<walk_to Oak_Grove>>` inside its own conversation (it walks with you to the GameObject named `Oak_Grove`);
- `<<handoff Leibniz>>` from another philosopher (the previous one walks you over, and Leibniz's conversation starts with `$came_from` set).

Names in `<<handoff>>` can be short: `Leibniz` finds `Gottfried Leibniz`, and `Kant` finds `Immanuel Kant (lead)`. Use quotes for names with spaces, e.g. `<<handoff "G. E. Moore">>`.

Any Yarn file that reads `$came_from` needs `<<declare $came_from = "">>` once somewhere in the project.
