// Cell characters pack - automatic setup for Unity 6 (URP)
//   PrisonerStanding : standing prisoner, shouting / rattling the cell bars
//   PrisonerSitting  : seated prisoner, holding his head and rocking side to side
//   SleepingPolice   : ThaiPolice asleep on the project's Chair prefab, breathing
// Creates URP Lit materials, fixes texture / model import settings, loops the animation clips,
// builds Animator Controllers and prefabs. Existing materials / controllers / prefabs are never overwritten
// (the only exception: the SleepingPolice prefab is rebuilt once to sit on the project's Chair prefab).
// Menu: Tools > Cell Characters > ...
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CellCharactersPack.EditorTools
{
    [InitializeOnLoad]
    public static class CellCharactersSetup
    {
        const string LitShader = "Universal Render Pipeline/Lit";
        const string SessionKey = "CellCharacters.AutoSetupDone.v1";
        const string UpgradeKey = "CellCharacters.SleepChairUpgrade.v2";
        const string SeatChairName = "SeatChair";   // child of SleepingPolice.prefab = nested instance of the project's Chair prefab
        const float SeatTop = 0.50f;                // seat height the sleeping animation was made for (m)
        const float BackFaceZ = -0.227f;            // z of the backrest front face in SleepingPolice space (+Z = front)
        const float FitTolerance = 0.06f;           // chair seat within +-6 cm of SeatTop keeps its original size

        class Def
        {
            public string name;       // folder / fbx / prefab name
            public string clip;       // clip name inside the fbx
            public float smoothness;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public bool capsule;
        }

        static readonly Def[] Defs =
        {
            new Def { name = "PrisonerStanding", clip = "Shout_RattleBars", smoothness = 0.12f, capsule = true,
                      colliderCenter = new Vector3(0f, 0.87f, 0f), colliderSize = new Vector3(0.26f, 1.74f, 0f) },
            new Def { name = "PrisonerSitting", clip = "Sit_HoldHeadRock", smoothness = 0.15f, capsule = false,
                      colliderCenter = new Vector3(0f, 0.38f, 0.12f), colliderSize = new Vector3(0.75f, 0.76f, 0.9f) },
            new Def { name = "SleepingPolice", clip = "Sleep_Breathe", smoothness = 0.15f, capsule = false,
                      colliderCenter = new Vector3(0f, 0.73f, 0.02f), colliderSize = new Vector3(0.8f, 1.46f, 0.98f) },
        };

        static CellCharactersSetup()
        {
            EditorApplication.delayCall += AutoSetupOnce;
            EditorApplication.delayCall += AutoUpgradeSleepingPolice;
        }

        static void AutoSetupOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetupOnce;
                return;
            }
            string root = Root;
            if (root == null) return;
            bool missing = Defs.Any(d => AssetDatabase.LoadAssetAtPath<GameObject>($"{root}/{d.name}/{d.name}.fbx") != null
                                         && AssetDatabase.LoadAssetAtPath<GameObject>($"{root}/{d.name}/{d.name}.prefab") == null);
            if (!missing) return;
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Debug.Log("[CellCharacters] First import detected - running automatic setup...");
            RunSetup();
        }

        static void AutoUpgradeSleepingPolice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoUpgradeSleepingPolice;
                return;
            }
            string root = Root;
            if (root == null) return;
            var prefab = Load<GameObject>($"{root}/SleepingPolice/SleepingPolice.prefab");
            var fbx = Load<GameObject>($"{root}/SleepingPolice/SleepingPolice.fbx");
            if (prefab == null || fbx == null) return;
            if (prefab.transform.Find(SeatChairName) != null) return;   // already sits on the project's Chair
            if (SessionState.GetBool(UpgradeKey, false)) return;
            SessionState.SetBool(UpgradeKey, true);
            Debug.Log("[CellCharacters] SleepingPolice: switching to the project's Chair prefab...");
            RebuildSleepingPolice(null);
        }

        /// <summary>Folder that contains PrisonerStanding/, PrisonerSitting/, SleepingPolice/, Editor/</summary>
        public static string Root
        {
            get
            {
                foreach (var guid in AssetDatabase.FindAssets("CellCharactersSetup t:MonoScript"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.EndsWith("/CellCharactersSetup.cs")) continue;
                    var editorDir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                    var root = Path.GetDirectoryName(editorDir)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(root)) return root;
                }
                return null;
            }
        }

        static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        [MenuItem("Tools/Cell Characters/1) Setup Materials, Animators & Prefabs", priority = 1)]
        public static void RunSetup()
        {
            string root = Root;
            if (root == null) { Debug.LogError("[CellCharacters] Cannot locate the CellCharacters folder."); return; }
            if (Shader.Find(LitShader) == null)
            {
                Debug.LogError("[CellCharacters] URP Lit shader not found. Make sure the project uses the Universal Render Pipeline.");
                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                FixTextureImporters(root);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (var d in Defs)
            {
                string fbx = $"{root}/{d.name}/{d.name}.fbx";
                if (Load<GameObject>(fbx) == null)
                {
                    Debug.LogWarning($"[CellCharacters] {fbx} not found - skipped.");
                    continue;
                }
                var mat = CreateMaterial(root, d);
                Material chairBlack = null, chairMetal = null;
                if (d.name == "SleepingPolice" && Load<GameObject>($"{root}/{d.name}/OfficeChair.fbx") != null)
                {
                    chairBlack = CreateFlatMaterial($"{root}/{d.name}/M_OfficeChair_Black.mat", new Color(0.035f, 0.035f, 0.04f), 0.35f);
                    chairMetal = CreateFlatMaterial($"{root}/{d.name}/M_OfficeChair_Metal.mat", new Color(0.36f, 0.37f, 0.39f), 0.55f);
                }
                AssetDatabase.SaveAssets();
                FixModelImporter(fbx, d, "M_" + d.name, mat);
                if (d.name == "SleepingPolice" && chairBlack != null)
                    FixPropImporter($"{root}/{d.name}/OfficeChair.fbx", chairBlack, chairMetal);
            }
            AssetDatabase.SaveAssets();

            foreach (var d in Defs)
            {
                string fbx = $"{root}/{d.name}/{d.name}.fbx";
                if (Load<GameObject>(fbx) == null) continue;
                var ctrl = CreateController(root, d, fbx);
                CreatePrefab(root, d, fbx, ctrl);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CellCharacters] Setup complete. Prefabs are in each character folder. " +
                      "Use Tools > Cell Characters > 2) Create Demo Scene to see them in a small cell.");
        }

        // ------------------------------------------------------------------------------------
        static void FixTextureImporters(string root)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool changed = false;
                if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; changed = true; }
                if (!ti.sRGBTexture) { ti.sRGBTexture = true; changed = true; }
                if (ti.alphaSource != TextureImporterAlphaSource.None) { ti.alphaSource = TextureImporterAlphaSource.None; changed = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
                if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (ti.anisoLevel < 4) { ti.anisoLevel = 4; changed = true; }
                if (ti.maxTextureSize < 1024) { ti.maxTextureSize = 1024; changed = true; }
                if (changed) ti.SaveAndReimport();
            }
        }

        static Material CreateMaterial(string root, Def d)
        {
            string path = $"{root}/{d.name}/{d.name}.mat";
            var m = Load<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(m, path);
            }
            var tex = Load<Texture2D>($"{root}/{d.name}/Textures/{d.name}_BaseColor.png");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", d.smoothness);
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_Cull", (float)CullMode.Back);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material CreateFlatMaterial(string path, Color c, float smooth)
        {
            var m = Load<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void CommonModelSettings(ModelImporter mi)
        {
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importBlendShapes = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
            mi.addCollider = false;
            mi.generateSecondaryUV = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        static void FixModelImporter(string fbx, Def d, string sourceMatName, Material mat)
        {
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceSynchronousImport);
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (mi == null) return;
            CommonModelSettings(mi);
            mi.importAnimation = true;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.optimizeGameObjects = false;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMatName), mat);
            mi.SaveAndReimport();

            // loop the clip and give it a readable name
            mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            var clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.clipAnimations;
            if (clips != null && clips.Length > 0)
            {
                var c = clips[0];
                c.name = d.clip;
                c.loopTime = true;
                c.loopPose = true;
                mi.clipAnimations = new[] { c };
                mi.SaveAndReimport();
            }
        }

        static void FixPropImporter(string fbx, Material black, Material metal)
        {
            if (Load<GameObject>(fbx) == null) return;
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (mi == null) return;
            CommonModelSettings(mi);
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_OfficeChair_Black"), black);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_OfficeChair_Metal"), metal);
            mi.SaveAndReimport();
        }

        static AnimatorController CreateController(string root, Def d, string fbx)
        {
            string path = $"{root}/{d.name}/{d.name}.controller";
            var existing = Load<AnimatorController>(path);
            if (existing != null) return existing;
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                Debug.LogWarning($"[CellCharacters] No animation clip found in {fbx}.");
                return null;
            }
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var state = ctrl.AddMotion(clip);
            state.name = d.clip;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void CreatePrefab(string root, Def d, string fbx, AnimatorController ctrl)
        {
            string prefabPath = $"{root}/{d.name}/{d.name}.prefab";
            if (Load<GameObject>(prefabPath) != null) return; // never overwrite
            string chairPath = d.name == "SleepingPolice" ? FindChairPrefabPath(root) : null;
            BuildPrefab(root, d, fbx, ctrl, prefabPath, chairPath, false);
        }

        /// <summary>Builds and saves a character prefab. For SleepingPolice, requireChair=true aborts (and keeps the existing prefab) when the chair cannot be fitted.</summary>
        static bool BuildPrefab(string root, Def d, string fbx, AnimatorController ctrl, string prefabPath, string chairPath, bool requireChair)
        {
            var model = Load<GameObject>(fbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = d.name;
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true; // bounds come from the rest pose, the animation moves far from it
                smr.shadowCastingMode = ShadowCastingMode.On;
                smr.receiveShadows = true;
            }
            if (d.capsule)
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.direction = 1;
                cc.center = d.colliderCenter;
                cc.radius = d.colliderSize.x;
                cc.height = d.colliderSize.y;
            }
            else
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = d.colliderCenter;
                bc.size = d.colliderSize;
            }
            if (d.name == "SleepingPolice")
            {
                bool ok = !string.IsNullOrEmpty(chairPath) && AddFittedChair(go, chairPath);
                if (!ok)
                {
                    if (requireChair)
                    {
                        Object.DestroyImmediate(go);
                        return false;
                    }
                    var oldChair = Load<GameObject>($"{root}/{d.name}/OfficeChair.fbx"); // pack's own chair, if still present
                    if (oldChair != null)
                    {
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(oldChair);
                        c.name = "OfficeChair";
                        c.transform.SetParent(go.transform, false);
                    }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return true;
        }

        // ------------------------------------------------------------------------------------
        //  SleepingPolice sits on the project's own Chair prefab
        // ------------------------------------------------------------------------------------
        [MenuItem("Tools/Cell Characters/3) Rebuild SleepingPolice (use the Chair prefab selected in Project)", priority = 3)]
        public static void RebuildSleepingPoliceMenu()
        {
            string chairPath = null;
            var sel = Selection.activeObject as GameObject;
            if (sel != null)
            {
                var p = AssetDatabase.GetAssetPath(sel);
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".prefab") && !p.Contains("/SleepingPolice/")) chairPath = p;
            }
            if (!RebuildSleepingPolice(chairPath))
                EditorUtility.DisplayDialog("Cell Characters", "SleepingPolice was not rebuilt. See the Console for details.\n" +
                    "Tip: select your Chair prefab in the Project window, then run this menu again.", "OK");
        }

        /// <summary>Rebuilds SleepingPolice.prefab so the officer sits on the given (or auto-found) Chair prefab.</summary>
        public static bool RebuildSleepingPolice(string chairPath)
        {
            string root = Root;
            if (root == null) return false;
            string fbx = $"{root}/SleepingPolice/SleepingPolice.fbx";
            if (Load<GameObject>(fbx) == null)
            {
                Debug.LogWarning("[CellCharacters] SleepingPolice.fbx not found - nothing to rebuild.");
                return false;
            }
            if (string.IsNullOrEmpty(chairPath)) chairPath = FindChairPrefabPath(root);
            if (string.IsNullOrEmpty(chairPath))
            {
                Debug.LogWarning("[CellCharacters] No prefab named 'Chair' or 'OfficeChair' found in Assets. " +
                                 "Select your Chair prefab in the Project window and run Tools > Cell Characters > 3) Rebuild SleepingPolice.");
                return false;
            }
            var d = Defs.First(x => x.name == "SleepingPolice");
            var ctrl = CreateController(root, d, fbx);
            string prefabPath = $"{root}/SleepingPolice/SleepingPolice.prefab";
            if (!BuildPrefab(root, d, fbx, ctrl, prefabPath, chairPath, true))
            {
                Debug.LogWarning($"[CellCharacters] Could not fit '{chairPath}' (no visible mesh found?). The existing SleepingPolice prefab was left unchanged.");
                return false;
            }
            // the chair that shipped with the pack is no longer used
            foreach (var p in new[] { $"{root}/SleepingPolice/OfficeChair.fbx", $"{root}/SleepingPolice/M_OfficeChair_Black.mat", $"{root}/SleepingPolice/M_OfficeChair_Metal.mat" })
                if (AssetDatabase.LoadMainAssetAtPath(p) != null) AssetDatabase.DeleteAsset(p);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CellCharacters] SleepingPolice now sits on '{chairPath}'.");
            return true;
        }

        static string FindChairPrefabPath(string packRoot)
        {
            string[] wanted = { "Chair", "OfficeChair" };
            foreach (var w in wanted)
            {
                foreach (var guid in AssetDatabase.FindAssets(w + " t:Prefab"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.StartsWith("Assets/") || p.StartsWith(packRoot + "/")) continue;
                    if (Path.GetFileNameWithoutExtension(p) == w) return p;
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("Chair t:Prefab"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.StartsWith("Assets/") && !p.StartsWith(packRoot + "/")) return p;
            }
            return null;
        }

        static List<Vector3> CollectVertices(Transform space, GameObject chair)
        {
            var pts = new List<Vector3>();
            foreach (var mf in chair.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                Vector3[] vs;
                try { vs = mesh.vertices; } catch { continue; }
                if (vs == null || vs.Length == 0) continue;
                Matrix4x4 m = space.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < vs.Length; i++) pts.Add(m.MultiplyPoint3x4(vs[i]));
            }
            return pts;
        }

        /// <summary>Finds floor, seat height and the direction the chair faces from its vertices (office-chair shaped meshes).</summary>
        static bool MeasureChair(List<Vector3> p, out float floorY, out float seatBotRel, out float seatTopRel, out float yaw)
        {
            floorY = 0f; seatBotRel = 0f; seatTopRel = 0f; yaw = 0f;
            if (p.Count < 200) return false;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in p) { if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y; }
            float H = maxY - minY;
            if (H < 0.05f) return false;
            const float step = 0.02f;
            int n = Mathf.CeilToInt(H / step) + 1;
            var cnt = new int[n];
            foreach (var v in p) cnt[Mathf.Min(n - 1, (int)((v.y - minY) / step))]++;
            int lo = Mathf.Max(0, (int)(0.22f * H / step));
            int hi = Mathf.Min(n, (int)(0.62f * H / step));
            if (hi <= lo) return false;
            int mx = 0;
            for (int i = lo; i < hi; i++) mx = Mathf.Max(mx, cnt[i]);
            if (mx == 0) return false;
            float th = 0.22f * mx;
            int s0 = lo;
            while (s0 < hi && cnt[s0] < th) s0++;
            int e0 = s0;
            while (e0 < hi && cnt[e0] >= th) e0++;
            if (e0 <= s0) return false;
            float sBot = s0 * step, sTop = e0 * step;

            // seat centroid, and the thin backrest plate above the arm rests (its normal = the way the chair faces)
            Vector2 sc = Vector2.zero;
            int sn = 0;
            var back = new List<Vector2>();
            foreach (var v in p)
            {
                float y = v.y - minY;
                if (y >= sBot && y < sTop) { sc += new Vector2(v.x, v.z); sn++; }
                else if (y > sTop + 0.35f && y < H - 0.02f) back.Add(new Vector2(v.x, v.z));
            }
            if (sn < 20 || back.Count < 20) return false;
            sc /= sn;
            Vector2 bc = Vector2.zero;
            foreach (var b in back) bc += b;
            bc /= back.Count;
            float a = 0f, bxy = 0f, c = 0f;
            foreach (var b in back)
            {
                Vector2 dv = b - bc;
                a += dv.x * dv.x; bxy += dv.x * dv.y; c += dv.y * dv.y;
            }
            a /= back.Count; bxy /= back.Count; c /= back.Count;
            float tr = 0.5f * (a + c);
            float dt = Mathf.Sqrt(0.25f * (a - c) * (a - c) + bxy * bxy);
            float lmin = tr - dt;
            Vector2 nrm = Mathf.Abs(bxy) > 1e-9f ? new Vector2(bxy, lmin - a) : (a < c ? new Vector2(1f, 0f) : new Vector2(0f, 1f));
            nrm.Normalize();
            if (Vector2.Dot(sc - bc, nrm) < 0f) nrm = -nrm;
            yaw = Mathf.Atan2(nrm.x, nrm.y);
            floorY = minY; seatBotRel = sBot; seatTopRel = sTop;
            return true;
        }

        /// <summary>Adds the project's Chair prefab as a child, turned to face +Z, sized so its seat is at SeatTop, with the backrest where the officer's back is.</summary>
        static bool AddFittedChair(GameObject go, string chairPath)
        {
            var asset = Load<GameObject>(chairPath);
            if (asset == null) return false;
            var chair = (GameObject)PrefabUtility.InstantiatePrefab(asset, go.transform);
            chair.name = SeatChairName;
            var ct = chair.transform;

            var pts = CollectVertices(go.transform, chair);
            float floorY, sBot, sTop, yaw;
            if (MeasureChair(pts, out floorY, out sBot, out sTop, out yaw))
            {
                float s = Mathf.Abs(sTop - SeatTop) > FitTolerance ? SeatTop / sTop : 1f;
                var rot = Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg, 0f);
                ct.localPosition = rot * (ct.localPosition * s);
                ct.localRotation = rot * ct.localRotation;
                ct.localScale = ct.localScale * s;

                pts = CollectVertices(go.transform, chair);
                float minY = float.MaxValue;
                foreach (var v in pts) if (v.y < minY) minY = v.y;
                float seatLo = minY + sBot * s, seatHi = minY + sTop * s;
                float sx = 0f; int sn = 0;
                float bandMax = float.MinValue, aboveMax = float.MinValue;
                foreach (var v in pts)
                {
                    if (v.y >= seatLo && v.y < seatHi) { sx += v.x; sn++; }
                    if (v.y >= seatHi + 0.30f && v.y <= seatHi + 0.55f) bandMax = Mathf.Max(bandMax, v.z);
                    if (v.y >= seatHi + 0.30f) aboveMax = Mathf.Max(aboveMax, v.z);
                }
                float seatX = sn > 0 ? sx / sn : 0f;
                float backFace = bandMax > float.MinValue ? bandMax : (aboveMax > float.MinValue ? aboveMax : 0f);
                ct.localPosition += new Vector3(-seatX, -minY, BackFaceZ - backFace);
                return true;
            }

            // no readable vertices: use renderer bounds and assume the chair already faces +Z
            bool any = false;
            Vector3 bmin = Vector3.zero, bmax = Vector3.zero;
            foreach (var r in chair.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 lp = go.transform.InverseTransformPoint(corner);
                    if (!any) { bmin = bmax = lp; any = true; }
                    else { bmin = Vector3.Min(bmin, lp); bmax = Vector3.Max(bmax, lp); }
                }
            }
            if (!any || bmax.y - bmin.y < 0.05f)
            {
                Object.DestroyImmediate(chair);
                return false;
            }
            float seatEst = 0.414f * (bmax.y - bmin.y);
            float k = Mathf.Abs(seatEst - SeatTop) > FitTolerance ? SeatTop / seatEst : 1f;
            ct.localPosition *= k;
            ct.localScale = ct.localScale * k;
            Vector3 c0 = (bmin + bmax) * 0.5f * k;
            ct.localPosition += new Vector3(-c0.x, -bmin.y * k, BackFaceZ + 0.16f * k - c0.z);
            return true;
        }

        // ------------------------------------------------------------------------------------
        [MenuItem("Tools/Cell Characters/2) Create Demo Scene", priority = 2)]
        public static void CreateDemoScene()
        {
            string root = Root;
            if (root == null) return;
            if (Load<GameObject>($"{root}/PrisonerStanding/PrisonerStanding.prefab") == null) RunSetup();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var concrete = DemoMat(root, "M_Demo_Concrete", new Color(0.50f, 0.50f, 0.48f), 0.05f);
            var steel = DemoMat(root, "M_Demo_Bars", new Color(0.18f, 0.19f, 0.21f), 0.45f);
            var floorMat = DemoMat(root, "M_Demo_Floor", new Color(0.36f, 0.36f, 0.35f), 0.15f);

            // cell: x -2..2, z -2.5..1.6, bars on the z = 1.6 line, corridor beyond it
            MakeBox("Floor", new Vector3(0f, -0.05f, 0.5f), new Vector3(7f, 0.1f, 8f), floorMat);
            MakeBox("Wall_Back", new Vector3(0f, 1.5f, -2.55f), new Vector3(4.2f, 3f, 0.1f), concrete);
            MakeBox("Wall_Left", new Vector3(-2.05f, 1.5f, -0.45f), new Vector3(0.1f, 3f, 4.2f), concrete);
            MakeBox("Wall_Right", new Vector3(2.05f, 1.5f, -0.45f), new Vector3(0.1f, 3f, 4.2f), concrete);
            MakeBox("Ceiling_Beam", new Vector3(0f, 2.55f, 1.6f), new Vector3(4.2f, 0.12f, 0.12f), steel);
            MakeBox("Bar_Rail_Mid", new Vector3(0f, 1.1f, 1.6f), new Vector3(4.0f, 0.05f, 0.05f), steel);
            for (int i = 0; i <= 20; i++)
            {
                var bar = MakeBox("Bar_" + i, new Vector3(-2f + i * 0.2f, 1.28f, 1.6f), new Vector3(0.04f, 2.56f, 0.04f), steel);
            }

            Place($"{root}/PrisonerStanding/PrisonerStanding.prefab", new Vector3(-0.9f, 0f, 1.12f), 0f);
            Place($"{root}/PrisonerSitting/PrisonerSitting.prefab", new Vector3(1.2f, 0f, -1.7f), -25f);
            Place($"{root}/SleepingPolice/SleepingPolice.prefab", new Vector3(0.2f, 0f, 3.6f), 180f);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(3.2f, 1.7f, 5.2f);
                cam.transform.LookAt(new Vector3(0f, 0.9f, 0.9f));
                cam.fieldOfView = 55f;
            }
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(50f, -140f, 0f);
                sun.intensity = 0.9f;
            }
            var lampGO = new GameObject("Cell Lamp");
            var lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 7f;
            lamp.intensity = 6f;
            lamp.color = new Color(1f, 0.93f, 0.78f);
            lampGO.transform.position = new Vector3(0f, 2.6f, 0f);

            EnsureFolder(root, "Scenes");
            string scenePath = $"{root}/Scenes/CellCharacters_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[CellCharacters] Demo scene saved to " + scenePath);
        }

        static void EnsureFolder(string root, string name)
        {
            if (!AssetDatabase.IsValidFolder(root + "/" + name)) AssetDatabase.CreateFolder(root, name);
        }

        static Material DemoMat(string root, string name, Color c, float smooth)
        {
            EnsureFolder(root, "Scenes");
            string path = $"{root}/Scenes/{name}.mat";
            var m = Load<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(LitShader));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject MakeBox(string name, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.position = pos;
            g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        static void Place(string prefabPath, Vector3 pos, float yaw)
        {
            var prefab = Load<GameObject>(prefabPath);
            if (prefab == null) return;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
#endif
