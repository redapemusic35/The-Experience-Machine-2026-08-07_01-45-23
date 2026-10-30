using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Yarn.Unity;

/// <summary>
/// MIHLab > Convert selected NPC to Walker (backlog W4).
///
/// For each selected GameObject: adds a NavMeshAgent (sized from the figure's renderers)
/// and a PhilosopherWalker, copies the settings from an existing DialogueTrigger or
/// PhilosopherNPC, and makes sure the scene has one YarnWalkCommands next to the
/// DialogueRunner. Nothing is deleted:
///   - a DialogueTrigger is disabled (its walk is now done by PhilosopherWalker);
///   - a CharacterController is disabled (it fights the NavMeshAgent);
///   - a PhilosopherNPC stays enabled, so "Press E to talk" still works; the walker on a
///     stationary philosopher only starts walking when Yarn says so (<<walk_to>>, or a
///     <<handoff>> from another philosopher).
/// Everything goes through Undo, so Ctrl+Z reverts a conversion. Save the scene afterwards.
/// See Docs/how-to-convert-npc.md.
/// </summary>
public static class MIHLabSetup
{
    private const string ConvertMenu = "MIHLab/Convert selected NPC to Walker";

    // DialogueTrigger field -> PhilosopherWalker field (W1 audit, section 5).
    private static readonly string[,] FromDialogueTrigger =
    {
        { "dialogueRunner", "dialogueRunner" },
        { "callOutNode", "callOutNode" },
        { "callOutRadius", "callOutRadius" },
        { "joinNode", "joinNode" },
        { "walkingNodes", "walkingNodes" },
        { "arrivalNode", "arrivalNode" },
        { "revisitNode", "revisitNode" },
        { "playerOverride", "playerOverride" },
        { "triggerRadius", "triggerRadius" },
        { "destination", "destination" },
        { "arriveDistance", "arriveDistance" },
        { "sitSpot", "sitSpot" },
        { "dialogueAnimator", "animator" },
        { "followSpeed", "walkSpeed" },
        { "stopDistance", "stopDistance" },
        { "turnSpeed", "turnSpeed" },
        { "sideOffset", "sideOffset" },
        { "forwardOffset", "forwardOffset" },
        { "walkBoolParam", "walkBoolParam" },
        { "playerStoppedSpeed", "playerStoppedSpeed" },
        { "stoppedDwellTime", "stoppedDwellTime" },
        { "fallBehindDistance", "fallBehindDistance" },
        { "checkpointCooldown", "checkpointCooldown" },
        { "lineOfSightDistance", "lineOfSightDistance" },
        { "useSeatedAnimations", "useSeatedAnimations" },
        { "seatedBoolParam", "seatedBoolParam" },
        { "standUpTriggerParam", "standUpTriggerParam" },
        { "sitDownTriggerParam", "sitDownTriggerParam" },
        { "standUpDuration", "standUpDuration" },
        { "sitDownDuration", "sitDownDuration" },
        { "showDebug", "showDebug" },
    };

    // PhilosopherNPC field -> PhilosopherWalker field.
    private static readonly string[,] FromPhilosopherNPC =
    {
        { "dialogueRunner", "dialogueRunner" },
        { "playerOverride", "playerOverride" },
        { "turnSpeed", "turnSpeed" },
        { "startNode", "joinNode" },
        { "talkRadius", "triggerRadius" },
    };

    [MenuItem(ConvertMenu, true)]
    private static bool ValidateConvert()
    {
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem(ConvertMenu)]
    private static void ConvertSelected()
    {
        Undo.SetCurrentGroupName("Convert NPC to Walker");
        int group = Undo.GetCurrentGroup();

        var report = new List<string>();
        foreach (GameObject go in Selection.gameObjects)
        {
            if (EditorUtility.IsPersistent(go))
            {
                report.Add($"- {go.name}: skipped (a prefab asset; select the object in the scene instead).");
                continue;
            }
            report.Add(Convert(go));
        }

        EnsureWalkCommands(report);
        Undo.CollapseUndoOperations(group);

        Debug.Log("[MIHLab] Convert to Walker:\n" + string.Join("\n", report));
    }

    private static string Convert(GameObject go)
    {
        var trigger = go.GetComponent<DialogueTrigger>();
        var npc = go.GetComponent<PhilosopherNPC>();

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        bool newAgent = agent == null;
        if (newAgent) agent = Undo.AddComponent<NavMeshAgent>(go);
        Undo.RecordObject(agent, "Configure NavMeshAgent");
        SizeAgent(go, agent);

        PhilosopherWalker walker = go.GetComponent<PhilosopherWalker>();
        if (walker == null) walker = Undo.AddComponent<PhilosopherWalker>(go);

        var dst = new SerializedObject(walker);
        dst.FindProperty("agent").objectReferenceValue = agent;
        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null) dst.FindProperty("animator").objectReferenceValue = anim;

        string source;
        if (trigger != null)
        {
            CopyFields(new SerializedObject(trigger), dst, FromDialogueTrigger);
            dst.FindProperty("joinOnApproach").boolValue = true;
            Undo.RecordObject(trigger, "Disable DialogueTrigger");
            trigger.enabled = false;
            source = "DialogueTrigger (now disabled)";
        }
        else if (npc != null)
        {
            CopyFields(new SerializedObject(npc), dst, FromPhilosopherNPC);
            // Stationary philosophers keep "Press E to talk"; they walk only when Yarn asks.
            dst.FindProperty("joinOnApproach").boolValue = false;
            source = "PhilosopherNPC (kept enabled for Press-E talk)";
        }
        else
        {
            source = "nothing to copy (defaults used)";
        }

        // Philosopher Id: what $came_from will say. Prefer a short, readable name.
        // Talk_Leibniz -> "Leibniz"; otherwise the NPC's display name without "(lead)".
        SerializedProperty id = dst.FindProperty("philosopherId");
        if (string.IsNullOrEmpty(id.stringValue))
        {
            string revisit = dst.FindProperty("revisitNode").stringValue;
            string join = dst.FindProperty("joinNode").stringValue;
            if (!string.IsNullOrEmpty(revisit) && revisit.StartsWith("Talk_")) id.stringValue = revisit.Substring(5);
            else if (npc != null && !string.IsNullOrEmpty(join) && join.StartsWith("Talk_")) id.stringValue = join.Substring(5);
            else if (npc != null)
            {
                string display = new SerializedObject(npc).FindProperty("displayName").stringValue;
                if (!string.IsNullOrEmpty(display)) id.stringValue = display.Replace("(lead)", "").Trim();
            }
        }

        dst.ApplyModifiedProperties();

        // The agent moves him now; a CharacterController on the same object fights it.
        var cc = go.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            Undo.RecordObject(cc, "Disable CharacterController");
            cc.enabled = false;
        }

        EditorUtility.SetDirty(walker);
        string idNote = string.IsNullOrEmpty(walker.Id) || walker.Id == go.name
            ? "  ! set Philosopher Id (e.g. Chalmers) so $came_from reads well"
            : $"  Id '{walker.Id}'";
        return $"- {go.name}: walker added{(newAgent ? " + NavMeshAgent" : "")}, settings from {source}.{idNote}";
    }

    private static void SizeAgent(GameObject go, NavMeshAgent agent)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float height = Mathf.Clamp(b.size.y, 1.2f, 2.4f);
        agent.height = height;
        agent.radius = 0.35f;
        // Mixamo bodies have their pivot at the feet (offset ~0); capsules at their centre.
        agent.baseOffset = Mathf.Max(0f, go.transform.position.y - b.min.y);
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.autoBraking = true;
        agent.updateRotation = false;
    }

    private static void CopyFields(SerializedObject src, SerializedObject dst, string[,] map)
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            SerializedProperty a = src.FindProperty(map[i, 0]);
            SerializedProperty b = dst.FindProperty(map[i, 1]);
            if (a == null || b == null) continue;

            switch (a.propertyType)
            {
                case SerializedPropertyType.ObjectReference: b.objectReferenceValue = a.objectReferenceValue; break;
                case SerializedPropertyType.String:          b.stringValue = a.stringValue; break;
                case SerializedPropertyType.Float:           b.floatValue = a.floatValue; break;
                case SerializedPropertyType.Integer:         b.intValue = a.intValue; break;
                case SerializedPropertyType.Boolean:         b.boolValue = a.boolValue; break;
                case SerializedPropertyType.Generic:
                    if (a.isArray && b.isArray)
                    {
                        b.arraySize = a.arraySize;
                        for (int j = 0; j < a.arraySize; j++)
                        {
                            b.GetArrayElementAtIndex(j).stringValue = a.GetArrayElementAtIndex(j).stringValue;
                        }
                    }
                    break;
            }
        }
    }

    private static void EnsureWalkCommands(List<string> report)
    {
        if (Object.FindAnyObjectByType<YarnWalkCommands>() != null) return;

        DialogueRunner runner = Object.FindAnyObjectByType<DialogueRunner>();
        if (runner == null)
        {
            report.Add("! No DialogueRunner in the scene, so YarnWalkCommands was not added.");
            return;
        }

        Undo.AddComponent<YarnWalkCommands>(runner.gameObject);
        report.Add($"- YarnWalkCommands added to '{runner.gameObject.name}'.");
    }
}
