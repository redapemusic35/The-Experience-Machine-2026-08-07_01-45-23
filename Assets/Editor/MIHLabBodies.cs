using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MIHLab > Give selected philosophers a body (backlog C2).
///
/// Opens a small window: pick a body model, an animator controller and (optionally) a
/// look preset, select capsule philosophers in the Hierarchy, press the button. For each
/// selected philosopher it:
///   - hides the capsule's MeshRenderer (disabled, not deleted);
///   - adds the body as a child named "Body", feet on the capsule's bottom, scale undone
///     so a stretched capsule doesn't stretch the body, with the animator controller;
///   - adds NPCLook pointing at that body, with the chosen preset, or, if "Find preset by
///     name" is on, the NPCLookPreset asset named Look_<Name> (Look_Leibniz, ...).
/// PhilosopherNPC, the collider and every other component are left as they are. Undo
/// (Ctrl+Z) removes the lot. Save the scene afterwards. See Docs/how-to-give-npc-a-body.md.
/// </summary>
public class MIHLabBodies : EditorWindow
{
    private const string HerbertPath = "Assets/models/mr_herbert_charater/Commission Mr_ John Herbert Family Guy Realistic 1_0.fbx";
    private const string ControllerPath = "Assets/models/mr_herbert_charater/Character.controller";
    private const string BodyName = "Body";

    private GameObject body;
    private RuntimeAnimatorController controller;
    private NPCLookPreset preset;
    private bool findPresetByName = true;

    [MenuItem("MIHLab/Give selected philosophers a body")]
    private static void Open()
    {
        var w = GetWindow<MIHLabBodies>(true, "Give philosophers a body");
        w.minSize = new Vector2(380, 230);
        w.Show();
    }

    private void OnEnable()
    {
        body = Load<GameObject>("MIHLab.Body.Model", HerbertPath);
        controller = Load<RuntimeAnimatorController>("MIHLab.Body.Controller", ControllerPath);
        findPresetByName = EditorPrefs.GetBool("MIHLab.Body.FindPreset", true);
        Selection.selectionChanged += Repaint;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Select capsule philosophers in the Hierarchy, choose a body, then press the button. Bodies already in the build: Mr Herbert, shorts-shirt-guy.", MessageType.Info);

        body = (GameObject)EditorGUILayout.ObjectField("Body model", body, typeof(GameObject), false);
        controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator controller", controller, typeof(RuntimeAnimatorController), false);
        preset = (NPCLookPreset)EditorGUILayout.ObjectField("Look preset (optional)", preset, typeof(NPCLookPreset), false);
        findPresetByName = EditorGUILayout.Toggle(new GUIContent("Find preset by name", "Use the NPCLookPreset asset named Look_<Name> when one exists, e.g. Look_Leibniz."), findPresetByName);

        int count = SceneSelection().Count;
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(body == null || count == 0))
        {
            if (GUILayout.Button($"Give {count} selected philosopher{(count == 1 ? "" : "s")} this body", GUILayout.Height(30)))
            {
                Save();
                GiveBodies();
            }
        }
    }

    private void GiveBodies()
    {
        Undo.SetCurrentGroupName("Give philosophers a body");
        int group = Undo.GetCurrentGroup();
        var report = new List<string>();

        foreach (GameObject go in SceneSelection())
        {
            report.Add(GiveBody(go));
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log("[MIHLab] Give a body:\n" + string.Join("\n", report));
    }

    private string GiveBody(GameObject go)
    {
        if (go.transform.Find(BodyName) != null)
        {
            return $"- {go.name}: skipped (already has a '{BodyName}' child).";
        }

        // Where are the capsule's feet? Use the visible mesh before hiding it.
        float feetY = go.transform.position.y;
        var capsuleRenderers = new List<MeshRenderer>();
        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            bool isCapsule = mr.gameObject == go || (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Capsule"));
            if (!isCapsule) continue;
            capsuleRenderers.Add(mr);
            feetY = Mathf.Min(feetY, mr.bounds.min.y);
        }

        foreach (MeshRenderer mr in capsuleRenderers)
        {
            Undo.RecordObject(mr, "Hide capsule");
            mr.enabled = false;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(body, go.scene);
        Undo.RegisterCreatedObjectUndo(instance, "Add body");
        instance.name = BodyName;
        Undo.SetTransformParent(instance.transform, go.transform, "Parent body");

        Transform t = instance.transform;
        t.localRotation = Quaternion.identity;
        t.position = new Vector3(go.transform.position.x, feetY, go.transform.position.z);
        Vector3 s = go.transform.lossyScale;
        t.localScale = new Vector3(SafeInverse(s.x), SafeInverse(s.y), SafeInverse(s.z));

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(instance);
        animator.applyRootMotion = false;
        if (controller != null) animator.runtimeAnimatorController = controller;

        NPCLook look = go.GetComponent<NPCLook>();
        if (look == null) look = Undo.AddComponent<NPCLook>(go);
        Undo.RecordObject(look, "Set look");
        look.bodyRoot = t;
        NPCLookPreset chosen = findPresetByName ? FindPreset(go) : null;
        look.preset = chosen != null ? chosen : preset;
        look.Apply();
        EditorUtility.SetDirty(look);

        string lookNote = look.preset != null ? $"look {look.preset.name}" : "no look preset yet";
        return $"- {go.name}: {body.name}, {lookNote}" + (capsuleRenderers.Count == 0 ? " (no capsule renderer found to hide)" : "");
    }

    /// <summary>
    /// Look_<Name>. Name candidates, in order: from Talk_<Name>, then the last word of the
    /// object name, then the first ("Elisabeth of Bohemia (lead)" -> Look_Elisabeth).
    /// </summary>
    private static NPCLookPreset FindPreset(GameObject go)
    {
        var keys = new List<string>();
        var npc = go.GetComponent<PhilosopherNPC>();
        if (npc != null)
        {
            string start = new SerializedObject(npc).FindProperty("startNode").stringValue;
            if (!string.IsNullOrEmpty(start) && start.StartsWith("Talk_")) keys.Add(start.Substring(5));
        }
        string[] words = go.name.Replace("(lead)", "").Trim().Split(' ');
        if (words.Length > 0)
        {
            keys.Add(words[words.Length - 1]);
            keys.Add(words[0]);
        }

        foreach (string key in keys)
        {
            foreach (string guid in AssetDatabase.FindAssets("Look_" + key + " t:NPCLookPreset"))
            {
                var p = AssetDatabase.LoadAssetAtPath<NPCLookPreset>(AssetDatabase.GUIDToAssetPath(guid));
                if (p != null && string.Equals(p.name, "Look_" + key, System.StringComparison.OrdinalIgnoreCase)) return p;
            }
        }
        return null;
    }

    private static List<GameObject> SceneSelection()
    {
        var list = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
        {
            if (!EditorUtility.IsPersistent(go)) list.Add(go);
        }
        return list;
    }

    private static float SafeInverse(float v)
    {
        return Mathf.Abs(v) < 0.0001f ? 1f : 1f / v;
    }

    private void Save()
    {
        EditorPrefs.SetString("MIHLab.Body.Model", body != null ? AssetDatabase.GetAssetPath(body) : "");
        EditorPrefs.SetString("MIHLab.Body.Controller", controller != null ? AssetDatabase.GetAssetPath(controller) : "");
        EditorPrefs.SetBool("MIHLab.Body.FindPreset", findPresetByName);
    }

    private static T Load<T>(string prefKey, string fallbackPath) where T : Object
    {
        string path = EditorPrefs.GetString(prefKey, "");
        T asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        return asset != null ? asset : AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
    }
}
