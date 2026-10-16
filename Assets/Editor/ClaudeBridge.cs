// ClaudeBridge — a file-based command queue so an assistant working on this project
// from outside Unity can perform Editor operations it otherwise cannot: instantiating
// prefabs, wiring serialized references, adding components, reading mesh statistics,
// and tuning terrain.
//
// HOW IT WORKS
//   Enable it from  Tools > Claude Bridge > Enabled.
//   While enabled, it watches  <project>/ClaudeBridge/in.json  a few times a second.
//   When a command file appears it executes it, writes  <project>/ClaudeBridge/out.json,
//   and deletes the input so the command runs exactly once.
//
// SAFETY
//   * Editor-only. It lives in an Editor folder and is never compiled into a build.
//   * Off by default. Nothing is watched until you switch it on.
//   * Every mutation is registered with Undo, so Ctrl+Z works normally.
//   * Scenes are marked dirty but never saved unless you ask for scene.save.
//   * The watch folder sits outside Assets/ so it never triggers an asset reimport.
//
// To remove it entirely: delete this file and the ClaudeBridge folder.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClaudeBridgeTools
{
    [Serializable]
    public class BridgeCommand
    {
        public string id;
        public string op;
        public string a;
        public string b;
        public string c;
        public string d;
    }

    [Serializable]
    public class BridgeResult
    {
        public string id;
        public string op;
        public bool ok;
        public string message;
        public string data;
    }

    [InitializeOnLoad]
    public static class ClaudeBridge
    {
        private const string EnabledKey = "ClaudeBridge.Enabled";
        private const double PollSeconds = 0.4;

        private static double _nextPoll;

        static ClaudeBridge()
        {
            EditorApplication.update += Tick;
        }

        // ---------------------------------------------------------------- paths

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string FolderPath
        {
            get { return Path.Combine(ProjectRoot, "ClaudeBridge"); }
        }

        private static string InPath
        {
            get { return Path.Combine(FolderPath, "in.json"); }
        }

        private static string OutPath
        {
            get { return Path.Combine(FolderPath, "out.json"); }
        }

        // ---------------------------------------------------------------- menu

        public static bool Enabled
        {
            get { return EditorPrefs.GetBool(EnabledKey, false); }
            set { EditorPrefs.SetBool(EnabledKey, value); }
        }

        // Two explicit verbs rather than one checkbox labelled "Enabled" - that read
        // as a status rather than a switch, and cost someone a confused ten minutes.
        [MenuItem("Tools/Claude Bridge/Start Listening", false, 1)]
        private static void StartListening()
        {
            Enabled = true;
            Directory.CreateDirectory(FolderPath);
            Debug.Log("[ClaudeBridge] Now LISTENING. Watching " + InPath);
        }

        [MenuItem("Tools/Claude Bridge/Start Listening", true)]
        private static bool StartListeningValidate()
        {
            return !Enabled;
        }

        [MenuItem("Tools/Claude Bridge/Stop Listening", false, 2)]
        private static void StopListening()
        {
            Enabled = false;
            Debug.Log("[ClaudeBridge] Stopped. No longer watching for commands.");
        }

        [MenuItem("Tools/Claude Bridge/Stop Listening", true)]
        private static bool StopListeningValidate()
        {
            return Enabled;
        }

        [MenuItem("Tools/Claude Bridge/Status", false, 3)]
        private static void LogStatus()
        {
            Debug.Log("[ClaudeBridge] " + (Enabled ? "LISTENING on " + InPath
                                                   : "STOPPED. Use Start Listening to enable."));
        }

        [MenuItem("Tools/Claude Bridge/Reveal Watch Folder", false, 20)]
        private static void RevealFolder()
        {
            Directory.CreateDirectory(FolderPath);
            EditorUtility.RevealInFinder(FolderPath);
        }

        // ---------------------------------------------------------------- loop

        private static void Tick()
        {
            if (!Enabled) return;
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            if (!File.Exists(InPath)) return;

            string raw;
            try
            {
                raw = File.ReadAllText(InPath);
            }
            catch (IOException)
            {
                return; // still being written; try again next tick
            }

            // Consume the command before running it, so a command that throws
            // cannot loop forever.
            try { File.Delete(InPath); } catch (IOException) { }

            BridgeResult result = new BridgeResult();

            try
            {
                BridgeCommand cmd = JsonUtility.FromJson<BridgeCommand>(raw);
                if (cmd == null || string.IsNullOrEmpty(cmd.op))
                {
                    result.ok = false;
                    result.message = "Could not parse a command with an 'op' field.";
                }
                else
                {
                    result.id = cmd.id;
                    result.op = cmd.op;
                    Execute(cmd, result);
                }
            }
            catch (Exception e)
            {
                result.ok = false;
                result.message = e.GetType().Name + ": " + e.Message;
            }

            try
            {
                Directory.CreateDirectory(FolderPath);
                File.WriteAllText(OutPath, JsonUtility.ToJson(result, true));
            }
            catch (IOException e)
            {
                Debug.LogError("[ClaudeBridge] Could not write result: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- ops

        private static void Execute(BridgeCommand cmd, BridgeResult res)
        {
            switch (cmd.op)
            {
                case "ping":
                    res.ok = true;
                    res.message = "Unity " + Application.unityVersion + ", scene '" +
                                  SceneManager.GetActiveScene().name + "'";
                    break;

                case "scene.list":
                    res.ok = true;
                    res.data = ListHierarchy();
                    res.message = "Hierarchy of the active scene.";
                    break;

                case "assets.refresh":
                    // Unity only refreshes the asset database when the Editor regains
                    // focus. That blocks script compiles, model imports, and any other
                    // tooling that depends on them while the window sits in the
                    // background. This lets the refresh be requested from outside.
                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                    // Refresh alone imports assets but does NOT recompile scripts while the
                    // Editor is backgrounded. Ask for compilation explicitly.
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                    res.ok = true;
                    res.message = "Asset database refreshed and script compilation requested.";
                    break;

                case "assets.import":
                    if (string.IsNullOrEmpty(cmd.a))
                    {
                        res.ok = false;
                        res.message = "assets.import needs an asset path in 'a'.";
                        break;
                    }
                    AssetDatabase.ImportAsset(cmd.a, ImportAssetOptions.ForceUpdate);
                    res.ok = true;
                    res.message = "Imported " + cmd.a;
                    break;

                case "scene.save":
                    EditorSceneManager.SaveOpenScenes();
                    res.ok = true;
                    res.message = "Saved open scenes.";
                    break;

                case "prefab.instantiate":
                    InstantiatePrefab(cmd, res);
                    break;

                case "component.add":
                    AddComponent(cmd, res);
                    break;

                case "ref.set":
                    SetReference(cmd, res);
                    break;

                case "prop.set":
                    SetProperty(cmd, res);
                    break;

                case "component.dump":
                    ComponentDump(cmd, res);
                    break;

                case "mesh.stats":
                    MeshStats(cmd, res);
                    break;

                case "model.info":
                    ModelInfo(cmd, res);
                    break;

                case "model.extractTextures":
                    ExtractTextures(cmd, res);
                    break;

                case "terrain.set":
                    TerrainSet(cmd, res);
                    break;

                default:
                    res.ok = false;
                    res.message = "Unknown op '" + cmd.op + "'. Known ops: ping, scene.list, " +
                                  "scene.save, prefab.instantiate, component.add, ref.set, " +
                                  "prop.set, component.dump, mesh.stats, model.info, model.extractTextures, terrain.set";
                    break;
            }
        }

        // a = prefab asset path, b = optional parent hierarchy path, c = optional name
        private static void InstantiatePrefab(BridgeCommand cmd, BridgeResult res)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(cmd.a);
            if (asset == null)
            {
                res.ok = false;
                res.message = "No prefab at '" + cmd.a + "'.";
                return;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, SceneManager.GetActiveScene());
            if (go == null)
            {
                res.ok = false;
                res.message = "InstantiatePrefab returned null.";
                return;
            }

            Undo.RegisterCreatedObjectUndo(go, "Claude Bridge: instantiate " + asset.name);

            if (!string.IsNullOrEmpty(cmd.b))
            {
                GameObject parent = FindByPath(cmd.b);
                if (parent == null)
                {
                    res.ok = false;
                    res.message = "Created it, but no parent found at '" + cmd.b + "'.";
                    return;
                }
                Undo.SetTransformParent(go.transform, parent.transform, "Claude Bridge: reparent");
            }

            if (!string.IsNullOrEmpty(cmd.c)) go.name = cmd.c;

            EditorSceneManager.MarkSceneDirty(go.scene);
            res.ok = true;
            res.message = "Instantiated '" + go.name + "'.";
            res.data = PathOf(go);
        }

        // a = object path, b = component type name
        private static void AddComponent(BridgeCommand cmd, BridgeResult res)
        {
            GameObject go = FindByPath(cmd.a);
            if (go == null) { res.ok = false; res.message = "No object at '" + cmd.a + "'."; return; }

            Type t = FindComponentType(cmd.b);
            if (t == null) { res.ok = false; res.message = "No component type named '" + cmd.b + "'."; return; }

            if (go.GetComponent(t) != null)
            {
                res.ok = true;
                res.message = "'" + cmd.b + "' was already on '" + go.name + "'. Nothing to do.";
                return;
            }

            Undo.AddComponent(go, t);
            EditorSceneManager.MarkSceneDirty(go.scene);
            res.ok = true;
            res.message = "Added " + cmd.b + " to " + go.name + ".";
        }

        // a = object path, b = component type, c = property, d = target object path
        private static void SetReference(BridgeCommand cmd, BridgeResult res)
        {
            GameObject go = FindByPath(cmd.a);
            if (go == null) { res.ok = false; res.message = "No object at '" + cmd.a + "'."; return; }

            Component comp = FindComponent(go, cmd.b);
            if (comp == null) { res.ok = false; res.message = "No '" + cmd.b + "' on '" + cmd.a + "'."; return; }

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty(cmd.c);
            if (prop == null)
            {
                res.ok = false;
                res.message = "No serialized property '" + cmd.c + "' on " + cmd.b + ". Available: " + ListProperties(so);
                return;
            }

            if (prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                res.ok = false;
                res.message = "'" + cmd.c + "' is " + prop.propertyType + ", not an object reference. Use prop.set.";
                return;
            }

            UnityEngine.Object target = null;
            if (!string.IsNullOrEmpty(cmd.d))
            {
                GameObject targetGo = FindByPath(cmd.d);
                if (targetGo == null)
                {
                    res.ok = false;
                    res.message = "No target object at '" + cmd.d + "'.";
                    return;
                }

                // The field may want a component rather than the GameObject itself.
                Type want = GetFieldObjectType(prop);
                if (want != null && typeof(Component).IsAssignableFrom(want))
                {
                    target = targetGo.GetComponent(want);
                    if (target == null)
                    {
                        res.ok = false;
                        res.message = "'" + cmd.d + "' has no " + want.Name + " component.";
                        return;
                    }
                }
                else
                {
                    target = targetGo;
                }
            }

            Undo.RecordObject(comp, "Claude Bridge: set " + cmd.c);
            prop.objectReferenceValue = target;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(go.scene);

            res.ok = true;
            res.message = "Set " + cmd.b + "." + cmd.c + " = " +
                          (target == null ? "None" : target.name + " (" + target.GetType().Name + ")");
        }

        // a = object path, b = component type, c = property, d = value
        private static void SetProperty(BridgeCommand cmd, BridgeResult res)
        {
            GameObject go = FindByPath(cmd.a);
            if (go == null) { res.ok = false; res.message = "No object at '" + cmd.a + "'."; return; }

            Component comp = FindComponent(go, cmd.b);
            if (comp == null) { res.ok = false; res.message = "No '" + cmd.b + "' on '" + cmd.a + "'."; return; }

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty(cmd.c);
            if (prop == null)
            {
                res.ok = false;
                res.message = "No serialized property '" + cmd.c + "'. Available: " + ListProperties(so);
                return;
            }

            Undo.RecordObject(comp, "Claude Bridge: set " + cmd.c);

            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = cmd.d == "1" || cmd.d.ToLowerInvariant() == "true";
                        break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                        prop.intValue = int.Parse(cmd.d);
                        break;
                    case SerializedPropertyType.Enum:
                        prop.enumValueIndex = int.Parse(cmd.d);
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = float.Parse(cmd.d, System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = cmd.d;
                        break;
                    default:
                        res.ok = false;
                        res.message = "Unsupported property type " + prop.propertyType + ".";
                        return;
                }
            }
            catch (FormatException)
            {
                res.ok = false;
                res.message = "Could not parse '" + cmd.d + "' as " + prop.propertyType + ".";
                return;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(go.scene);

            res.ok = true;
            res.message = "Set " + cmd.b + "." + cmd.c + " = " + cmd.d;
        }

        // a = object path, b = component type (optional; omit to list all components)
        private static void ComponentDump(BridgeCommand cmd, BridgeResult res)
        {
            GameObject go = FindByPath(cmd.a);
            if (go == null) { res.ok = false; res.message = "No object at '" + cmd.a + "'."; return; }

            StringBuilder sb = new StringBuilder();
            int dumped = 0;

            foreach (Component comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string tn = comp.GetType().Name;
                if (!string.IsNullOrEmpty(cmd.b) && tn != cmd.b) continue;

                dumped++;
                sb.Append("--- ").Append(tn).Append(" ---\n");

                SerializedObject so = new SerializedObject(comp);
                SerializedProperty it = so.GetIterator();
                bool enterChildren = true;
                int guard = 0;

                while (it.NextVisible(enterChildren) && guard++ < 200)
                {
                    enterChildren = false;
                    string val;

                    switch (it.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            val = it.objectReferenceValue == null
                                ? "None"
                                : it.objectReferenceValue.name + " (" + it.objectReferenceValue.GetType().Name + ")";
                            break;
                        case SerializedPropertyType.Boolean: val = it.boolValue ? "true" : "false"; break;
                        case SerializedPropertyType.Integer: val = it.intValue.ToString(); break;
                        case SerializedPropertyType.Float:   val = it.floatValue.ToString("0.###"); break;
                        case SerializedPropertyType.String:  val = "\"" + it.stringValue + "\""; break;
                        case SerializedPropertyType.Enum:
                            val = it.enumValueIndex >= 0 && it.enumValueIndex < it.enumNames.Length
                                ? it.enumNames[it.enumValueIndex] : it.enumValueIndex.ToString();
                            break;
                        case SerializedPropertyType.Vector3: val = it.vector3Value.ToString("0.###"); break;
                        case SerializedPropertyType.Generic:
                            val = it.isArray ? "[" + it.arraySize + " items]" : "(generic)";
                            break;
                        default: val = it.propertyType.ToString(); break;
                    }

                    sb.Append("  ").Append(it.name).Append(" = ").Append(val).Append("\n");

                    if (it.isArray && it.propertyType == SerializedPropertyType.Generic)
                    {
                        for (int i = 0; i < it.arraySize && i < 20; i++)
                        {
                            SerializedProperty el = it.GetArrayElementAtIndex(i);
                            string ev = el.propertyType == SerializedPropertyType.String
                                ? el.stringValue
                                : (el.propertyType == SerializedPropertyType.ObjectReference
                                    ? (el.objectReferenceValue == null ? "None" : el.objectReferenceValue.name)
                                    : el.propertyType.ToString());
                            sb.Append("      [").Append(i).Append("] ").Append(ev).Append("\n");
                        }
                    }
                }
            }

            if (dumped == 0)
            {
                res.ok = false;
                res.message = "No matching component on '" + cmd.a + "'.";
                return;
            }

            res.ok = true;
            res.message = "Dumped " + dumped + " component(s) from " + go.name + ".";
            res.data = sb.ToString();
        }

        // a = model asset path. Reports geometry AND embedded textures, because file
        // size on disk says almost nothing about what a model costs at runtime.
        private static void ModelInfo(BridgeCommand cmd, BridgeResult res)
        {
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(cmd.a);
            if (all == null || all.Length == 0)
            {
                res.ok = false; res.message = "Nothing loadable at '" + cmd.a + "'."; return;
            }

            int tris = 0, verts = 0, texCount = 0, matCount = 0;
            long texBytes = 0;
            StringBuilder sb = new StringBuilder();

            foreach (UnityEngine.Object o in all)
            {
                Mesh m = o as Mesh;
                if (m != null) { tris += m.triangles.Length / 3; verts += m.vertexCount; continue; }

                Material mat = o as Material;
                if (mat != null) { matCount++; sb.Append("  material: ").Append(mat.name).Append("\n"); continue; }

                Texture2D t = o as Texture2D;
                if (t != null)
                {
                    texCount++;
                    long b = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                    texBytes += b;
                    sb.Append("  texture:  ").Append(t.name).Append("  ")
                      .Append(t.width).Append("x").Append(t.height)
                      .Append("  ~").Append(b / 1024 / 1024).Append(" MB\n");
                }
            }

            res.ok = true;
            res.message = "tris " + tris + ", verts " + verts +
                          ", embedded textures " + texCount + " (~" + (texBytes / 1024 / 1024) +
                          " MB runtime), materials " + matCount;
            res.data = sb.ToString();
        }

        // a = model asset path, b = optional output folder (defaults to <model dir>/Textures)
        private static void ExtractTextures(BridgeCommand cmd, BridgeResult res)
        {
            ModelImporter imp = AssetImporter.GetAtPath(cmd.a) as ModelImporter;
            if (imp == null) { res.ok = false; res.message = "No model importer at '" + cmd.a + "'."; return; }

            string dir = string.IsNullOrEmpty(cmd.b)
                ? Path.GetDirectoryName(cmd.a).Replace("\\", "/") + "/Textures"
                : cmd.b;

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool ok = imp.ExtractTextures(dir);
            AssetDatabase.Refresh();

            res.ok = true;
            res.message = "ExtractTextures -> " + (ok ? "extracted into " + dir
                                                     : "nothing extracted (no embedded textures in this file)");
        }

        // a = asset path of a model
        private static void MeshStats(BridgeCommand cmd, BridgeResult res)
        {
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(cmd.a);
            if (all == null || all.Length == 0)
            {
                res.ok = false;
                res.message = "Nothing loadable at '" + cmd.a + "'.";
                return;
            }

            int meshes = 0, tris = 0, verts = 0, subMeshes = 0;
            StringBuilder sb = new StringBuilder();

            foreach (UnityEngine.Object o in all)
            {
                Mesh m = o as Mesh;
                if (m == null) continue;
                meshes++;
                int t = m.triangles.Length / 3;
                tris += t;
                verts += m.vertexCount;
                subMeshes += m.subMeshCount;
                sb.Append(m.name).Append(": ").Append(t).Append(" tris, ")
                  .Append(m.vertexCount).Append(" verts, ")
                  .Append(m.subMeshCount).Append(" submeshes\n");
            }

            res.ok = true;
            res.message = meshes + " mesh(es), " + tris + " triangles, " + verts +
                          " vertices, " + subMeshes + " submeshes total";
            res.data = sb.ToString();
        }

        // a = terrain object path, b = field, c = value
        private static void TerrainSet(BridgeCommand cmd, BridgeResult res)
        {
            GameObject go = FindByPath(cmd.a);
            if (go == null) { res.ok = false; res.message = "No object at '" + cmd.a + "'."; return; }

            Terrain terrain = go.GetComponent<Terrain>();
            if (terrain == null) { res.ok = false; res.message = "'" + cmd.a + "' has no Terrain component."; return; }

            Undo.RecordObject(terrain, "Claude Bridge: terrain " + cmd.b);

            float f;
            float.TryParse(cmd.c, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out f);

            switch (cmd.b)
            {
                case "pixelError":           terrain.heightmapPixelError = f; break;
                case "basemapDistance":      terrain.basemapDistance = f; break;
                case "detailObjectDistance": terrain.detailObjectDistance = f; break;
                case "detailObjectDensity":  terrain.detailObjectDensity = f; break;
                case "treeDistance":         terrain.treeDistance = f; break;
                case "treeBillboardDistance":terrain.treeBillboardDistance = f; break;
                case "castShadows":
                    terrain.shadowCastingMode = (cmd.c == "1" || cmd.c.ToLowerInvariant() == "true")
                        ? UnityEngine.Rendering.ShadowCastingMode.On
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                    break;
                case "drawInstanced":        terrain.drawInstanced = (cmd.c == "1"); break;
                default:
                    res.ok = false;
                    res.message = "Unknown terrain field '" + cmd.b + "'. Known: pixelError, " +
                                  "basemapDistance, detailObjectDistance, detailObjectDensity, " +
                                  "treeDistance, treeBillboardDistance, castShadows, drawInstanced";
                    return;
            }

            EditorUtility.SetDirty(terrain);
            EditorSceneManager.MarkSceneDirty(go.scene);
            res.ok = true;
            res.message = "Terrain '" + go.name + "': " + cmd.b + " = " + cmd.c;
        }

        // ---------------------------------------------------------------- helpers

        private static string ListHierarchy()
        {
            StringBuilder sb = new StringBuilder();
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
                Walk(root.transform, 0, sb);
            return sb.ToString();
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            sb.Append(new string(' ', depth * 2)).Append(t.name);

            Component[] comps = t.GetComponents<Component>();
            if (comps.Length > 1)
            {
                sb.Append("  [");
                bool first = true;
                foreach (Component c in comps)
                {
                    if (c == null || c is Transform) continue;
                    if (!first) sb.Append(", ");
                    sb.Append(c.GetType().Name);
                    first = false;
                }
                sb.Append("]");
            }

            sb.Append("\n");
            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1, sb);
        }

        private static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            GameObject direct = GameObject.Find(path);
            if (direct != null) return direct;

            // Fall back to a name match anywhere in the active scene, so a bare
            // name works without the caller knowing the full path.
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject hit = FindRecursive(root.transform, path);
                if (hit != null) return hit;
            }
            return null;
        }

        private static GameObject FindRecursive(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;
            for (int i = 0; i < t.childCount; i++)
            {
                GameObject hit = FindRecursive(t.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static string PathOf(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        private static Component FindComponent(GameObject go, string typeName)
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                if (c.GetType().Name == typeName) return c;
            }
            return null;
        }

        private static Type FindComponentType(string typeName)
        {
            foreach (Type t in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (t.Name == typeName) return t;
            }
            return null;
        }

        private static Type GetFieldObjectType(SerializedProperty prop)
        {
            // "PPtr<$StarterAssetsInputs>" -> StarterAssetsInputs
            string raw = prop.type;
            int lt = raw.IndexOf('$');
            int gt = raw.IndexOf('>');
            if (lt < 0 || gt < 0 || gt <= lt) return null;

            string name = raw.Substring(lt + 1, gt - lt - 1);
            foreach (Type t in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (t.Name == name) return t;
            }
            return null;
        }

        private static string ListProperties(SerializedObject so)
        {
            List<string> names = new List<string>();
            SerializedProperty it = so.GetIterator();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                names.Add(it.name);
                if (names.Count > 40) break;
            }
            return string.Join(", ", names.ToArray());
        }
    }
}
