using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Yarn commands that steer the walking philosophers (backlog W3). Put this component on
/// the "Dialogue System" object (next to the DialogueRunner). Each command acts on the
/// philosopher currently walking with the player (PhilosopherWalker.Active), or, if none
/// is, the walker nearest the player.
///
///   <<walk_to DESTINATION>>              walk with the player to a place
///   <<walk_to DESTINATION ARRIVAL_NODE>> ...and run ARRIVAL_NODE when the player gets there
///   <<stop_walk>>                        stop and face the player
///   <<resume_walk>>                      carry on toward the current destination
///   <<handoff PHILOSOPHER>>              walk the player to another philosopher; on arrival
///                                        $came_from is set to this philosopher's Id and the
///                                        other one's entry node runs
///
/// Commands take effect when the current node ends, so put them at the end of a node or
/// just before the player leaves it.
///
/// DESTINATION is looked up in Places (below) by name, then as a GameObject name.
/// PHILOSOPHER is looked up in Philosophers (below), then as a PhilosopherWalker Id, then
/// as a GameObject name ("Leibniz" matches "Gottfried Leibniz"; use quotes for spaces:
/// <<handoff "G. E. Moore">>). The entry node is, in order: the Philosophers entry, the
/// walker's Join Node, the PhilosopherNPC's Start Node, or Talk_<PHILOSOPHER>.
///
/// Sample (any node that uses $came_from must have it declared once somewhere):
///
///   <<declare $came_from = "">>
///
///   title: Talk_Leibniz
///   ---
///   <<if $came_from == "Socrates">>
///   Leibniz: Socrates sent you? Then you have already begun to doubt.
///   <<endif>>
///   Leibniz: Why is there something rather than nothing?
///   -> Walk on with me to the grove.
///       <<walk_to Oak_Grove Leibniz_Grove>>
///   -> I'd rather meet Lewis.
///       <<handoff Lewis>>
///   -> Wait here a moment.
///       <<stop_walk>>
///   ===
/// </summary>
public class YarnWalkCommands : MonoBehaviour
{
    [Serializable]
    public class Place
    {
        public string name;
        public Transform transform;
    }

    [Serializable]
    public class Philosopher
    {
        [Tooltip("Name used in <<handoff NAME>>.")]
        public string name;
        public Transform transform;
        [Tooltip("Optional. Blank = the walker's Join Node / the NPC's Start Node / Talk_<name>.")]
        public string entryNode;
    }

    [SerializeField] private DialogueRunner dialogueRunner;
    [Tooltip("Optional named destinations for <<walk_to>>. Anything not listed is found by GameObject name.")]
    [SerializeField] private List<Place> places = new List<Place>();
    [Tooltip("Optional handoff targets. Anything not listed is found automatically.")]
    [SerializeField] private List<Philosopher> philosophers = new List<Philosopher>();

    private static readonly FieldInfo NpcStartNodeField =
        typeof(PhilosopherNPC).GetField("startNode", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GetComponent<DialogueRunner>();
        if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        if (dialogueRunner == null)
        {
            Debug.LogError("[YarnWalkCommands] No DialogueRunner found; walk commands are not registered.", this);
            return;
        }

        dialogueRunner.AddCommandHandler("walk_to", (Action<string, string>)WalkTo);
        dialogueRunner.AddCommandHandler("stop_walk", (Action)StopWalk);
        dialogueRunner.AddCommandHandler("resume_walk", (Action)ResumeWalk);
        dialogueRunner.AddCommandHandler("handoff", (Action<string>)Handoff);
    }

    private void OnDestroy()
    {
        if (dialogueRunner == null) return;
        dialogueRunner.RemoveCommandHandler("walk_to");
        dialogueRunner.RemoveCommandHandler("stop_walk");
        dialogueRunner.RemoveCommandHandler("resume_walk");
        dialogueRunner.RemoveCommandHandler("handoff");
    }

    // ---------------------------------------------------------------- commands

    // arrivalNode is optional in Yarn: Yarn Spinner fills missing optional parameters.
    private void WalkTo(string destination, string arrivalNode = null)
    {
        PhilosopherWalker walker = CurrentWalker("walk_to");
        if (walker == null) return;

        Transform target = FindPlace(destination);
        if (target == null)
        {
            Debug.LogWarning($"[YarnWalkCommands] <<walk_to {destination}>>: no place or GameObject with that name.", this);
            return;
        }

        walker.WalkTo(target, arrivalNode);
    }

    private void StopWalk()
    {
        PhilosopherWalker walker = CurrentWalker("stop_walk");
        if (walker != null) walker.StopWalk();
    }

    private void ResumeWalk()
    {
        PhilosopherWalker walker = CurrentWalker("resume_walk");
        if (walker != null) walker.ResumeWalk();
    }

    private void Handoff(string philosopher)
    {
        PhilosopherWalker walker = CurrentWalker("handoff");
        if (walker == null) return;

        if (!TryFindPhilosopher(philosopher, out Transform target, out string entryNode, out PhilosopherWalker targetWalker))
        {
            Debug.LogWarning($"[YarnWalkCommands] <<handoff {philosopher}>>: no philosopher with that name.", this);
            return;
        }

        if (targetWalker == walker)
        {
            Debug.LogWarning($"[YarnWalkCommands] <<handoff {philosopher}>>: that is the philosopher already walking with the player.", this);
            return;
        }

        walker.HandoffTo(target, entryNode, targetWalker);
    }

    // ---------------------------------------------------------------- lookups

    private PhilosopherWalker CurrentWalker(string command)
    {
        if (PhilosopherWalker.Active != null) return PhilosopherWalker.Active;

        // Nobody is walking: take the walker nearest the main camera / player.
        Transform reference = Camera.main != null ? Camera.main.transform : null;
        PhilosopherWalker best = null;
        float bestDistance = float.MaxValue;
        foreach (PhilosopherWalker w in FindObjectsByType<PhilosopherWalker>(FindObjectsSortMode.None))
        {
            float d = reference != null ? Vector3.Distance(w.transform.position, reference.position) : 0f;
            if (d < bestDistance) { best = w; bestDistance = d; }
        }

        if (best == null)
        {
            Debug.LogWarning($"[YarnWalkCommands] <<{command}>>: no PhilosopherWalker in the scene.", this);
        }
        return best;
    }

    private Transform FindPlace(string placeName)
    {
        if (string.IsNullOrEmpty(placeName)) return null;
        foreach (Place p in places)
        {
            if (p != null && p.transform != null && Same(p.name, placeName)) return p.transform;
        }
        GameObject go = GameObject.Find(placeName);
        return go != null ? go.transform : null;
    }

    private bool TryFindPhilosopher(string who, out Transform target, out string entryNode, out PhilosopherWalker walker)
    {
        target = null;
        entryNode = null;
        walker = null;
        if (string.IsNullOrEmpty(who)) return false;

        // 1. Listed explicitly.
        foreach (Philosopher p in philosophers)
        {
            if (p != null && p.transform != null && Same(p.name, who))
            {
                target = p.transform;
                walker = p.transform.GetComponent<PhilosopherWalker>();
                entryNode = !string.IsNullOrEmpty(p.entryNode) ? p.entryNode : EntryNodeOf(p.transform, who);
                return true;
            }
        }

        // 2. A walker with that Id.
        foreach (PhilosopherWalker w in FindObjectsByType<PhilosopherWalker>(FindObjectsSortMode.None))
        {
            if (Same(w.Id, who))
            {
                target = w.transform;
                walker = w;
                entryNode = EntryNodeOf(w.transform, who);
                return true;
            }
        }

        // 3. A stationary philosopher whose GameObject name is or contains the name
        //    ("Leibniz" -> "Gottfried Leibniz", "Kant" -> "Immanuel Kant (lead)").
        PhilosopherNPC partial = null;
        foreach (PhilosopherNPC npc in FindObjectsByType<PhilosopherNPC>(FindObjectsSortMode.None))
        {
            string n = npc.gameObject.name;
            if (Same(n, who)) { partial = npc; break; }
            if (partial == null && n.IndexOf(who, StringComparison.OrdinalIgnoreCase) >= 0) partial = npc;
        }
        if (partial != null)
        {
            target = partial.transform;
            walker = partial.GetComponent<PhilosopherWalker>();
            entryNode = EntryNodeOf(partial.transform, who);
            return true;
        }

        // 4. Any GameObject with that exact name.
        GameObject go = GameObject.Find(who);
        if (go != null)
        {
            target = go.transform;
            walker = go.GetComponent<PhilosopherWalker>();
            entryNode = EntryNodeOf(go.transform, who);
            return true;
        }

        return false;
    }

    private static string EntryNodeOf(Transform t, string who)
    {
        PhilosopherWalker w = t.GetComponent<PhilosopherWalker>();
        if (w != null && !string.IsNullOrEmpty(w.JoinNode)) return w.JoinNode;

        PhilosopherNPC npc = t.GetComponent<PhilosopherNPC>();
        if (npc != null && NpcStartNodeField != null)
        {
            string node = NpcStartNodeField.GetValue(npc) as string;
            if (!string.IsNullOrEmpty(node)) return node;
        }

        return "Talk_" + who.Replace(" ", "").Replace(".", "");
    }

    private static bool Same(string a, string b)
    {
        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
