using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// Shared primitive / material helpers used by every procedural level builder in this project.
    /// Call UseLibrary() first so materials land in the right folder for the level being built.
    /// All geometry is authored in metres at 1:1 scale.
    /// </summary>
    public static partial class LevelKit
    {
        static string _folder = "Assets/Materials";
        static string _prefix = "M_";
        static readonly Dictionary<string, Material> _mat = new Dictionary<string, Material>();

        // ───────────────────────── material library ─────────────────────────
        public static void UseLibrary(string folder, string prefix)
        {
            _folder = folder;
            _prefix = prefix;
            _mat.Clear();
            EnsureFolder(folder);
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }

        public static Material Mat(string key, Color c, float metallic, float smooth)
        {
            var m = LoadOrCreate(key);
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            _mat[key] = m;
            return m;
        }

        public static Material MatTransparent(string key, Color c, float smooth)
        {
            var m = Mat(key, c, 0f, smooth);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material MatEmissive(string key, Color baseColor, Color emission)
        {
            var m = Mat(key, baseColor, 0f, 0.5f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LoadOrCreate(string key)
        {
            string path = _folder + "/" + _prefix + key + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public static Material M(string key)
        {
            Material m;
            if (_mat.TryGetValue(key, out m) && m != null) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>(_folder + "/" + _prefix + key + ".mat");
            if (m != null) _mat[key] = m;
            return m;
        }

        // ───────────────────────── primitives ─────────────────────────
        public static Transform Group(string name, Transform parent)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g.transform;
        }

        public static GameObject Box(string name, Transform parent, Vector3 centre, Vector3 size, string mat,
                                     Vector3 euler = default(Vector3), bool collider = true, bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = size;
            Paint(g, mat);
            if (!collider) StripCollider(g);
            if (markStatic) g.isStatic = true;
            return g;
        }

        /// <summary>Cylinder with real diameter/height (Unity's cylinder is 1 wide, 2 tall).</summary>
        public static GameObject Cyl(string name, Transform parent, Vector3 centre, float diameter, float height,
                                     string mat, Vector3 euler = default(Vector3), bool collider = false, bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Paint(g, mat);
            StripCollider(g);
            if (collider)
            {
                var bc = g.AddComponent<BoxCollider>();
                bc.size = new Vector3(1f, 2f, 1f);
            }
            if (markStatic) g.isStatic = true;
            return g;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 centre, float diameter, string mat,
                                        bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localScale = Vector3.one * diameter;
            Paint(g, mat);
            StripCollider(g);
            if (markStatic) g.isStatic = true;
            return g;
        }

        /// <summary>Flat quad for floor markings / decals. Faces +Y by default.</summary>
        public static GameObject Decal(string name, Transform parent, Vector3 centre, Vector2 size, string mat, float yaw = 0f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = new Vector3(-90f, yaw, 0f);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            Paint(g, mat);
            StripCollider(g);
            g.isStatic = true;
            return g;
        }

        public static void Paint(GameObject g, string mat)
        {
            var m = M(mat);
            if (m == null) return;
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        public static void StripCollider(GameObject g)
        {
            var c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        public static GameObject Marker(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            return g;
        }

        /// <summary>World-space TextMeshPro sign, auto fitted to a box. yaw 180 = reads from -Z.</summary>
        public static GameObject Sign(string name, Transform parent, Vector3 pos, Vector2 boxSize, string text,
                                      Color colour, float yaw = 180f, bool bold = true)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var tmp = g.AddComponent<TMPro.TextMeshPro>();
            if (tmp.font == null)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (f != null) tmp.font = f;
            }
            var rt = g.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = boxSize;
            tmp.text = text;
            tmp.color = colour;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.5f;
            tmp.fontSizeMax = 300f;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.characterSpacing = 4f;
            g.transform.localPosition = pos;
            // TMP 3D text is readable from its -Z side, so a "facing" yaw needs the extra half turn
            g.transform.localEulerAngles = new Vector3(0f, yaw + 180f, 0f);
            g.isStatic = true;
            return g;
        }

        /// <summary>Capsule stand-in for a person, at roughly human proportions.</summary>
        public static GameObject PlaceHuman(string assetPath, Transform parent, Vector3 pos, float yaw)
        {
            // the NPC prefab is a default capsule (1 wide, 2 tall), so this lands on
            // Nav.HumanHeight with shoulders about half a metre across
            return Place(assetPath, parent, pos, yaw, 0f, 0f, new Vector3(0.5f, Nav.PlayerScale, 0.5f));
        }

        /// <summary>
        /// Drops the player in standing the same height as the NPCs around them.
        ///
        /// player.prefab carries a 1.5 stretch on its Y axis, which scales its CharacterController
        /// to 3 m tall. Every other chapter shares that prefab, so the fix belongs on the instance:
        /// the whole rig is rescaled uniformly, which keeps the camera, ground check and flashlight
        /// at the right height relative to the body instead of shearing them.
        /// </summary>
        public static GameObject PlacePlayer(string assetPath, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (src == null)
            {
                Debug.LogWarning("[LevelKit] missing player prefab: " + assetPath);
                return null;
            }

            // Place multiplies the prefab scale, so divide it out first: whatever the prefab was
            // saved with, the instance ends up at a uniform Nav.PlayerScale on all three axes.
            Vector3 a = src.transform.localScale;
            Vector3 fix = new Vector3(
                Nav.PlayerScale / Mathf.Max(0.0001f, a.x),
                Nav.PlayerScale / Mathf.Max(0.0001f, a.y),
                Nav.PlayerScale / Mathf.Max(0.0001f, a.z));

            var go = Place(assetPath, parent, pos, yaw, 0f, 0f, fix);
            if (go == null) return null;
            go.name = "player";

            // The player has no MeshRenderer (nobody renders their own body in first person), so
            // the renderer-bounds grounding in Place cannot see it and leaves the origin - which
            // is the capsule centre - sitting on the floor, burying the player to the waist.
            // Stand them on the floor using the controller instead.
            var cc = go.GetComponent<CharacterController>();
            if (cc != null)
            {
                float sy = Mathf.Abs(go.transform.lossyScale.y);
                var lp = go.transform.localPosition;
                lp.y = pos.y + (cc.height * 0.5f - cc.center.y) * sy + 0.02f;
                go.transform.localPosition = lp;
            }
            return go;
        }

        /// <summary>Every prefab dropped in by <see cref="Place"/> during the current build.</summary>
        static readonly List<GameObject> _placed = new List<GameObject>();

        /// <summary>Call at the top of a build so the placed-prop list only covers this level.</summary>
        public static void ResetPlaced() { _placed.Clear(); }

        /// <summary>Read-only view of the prefabs this build has placed.</summary>
        public static List<GameObject> Placed { get { return _placed; } }

        /// <summary>Instantiates a project prefab/fbx, drops it on the ground and optionally rescales it.</summary>
        public static GameObject Place(string assetPath, Transform parent, Vector3 pos, float yaw = 0f,
                                       float targetHeight = 0f, float pitch = 0f, Vector3 scale = default(Vector3))
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (src == null)
            {
                Debug.LogWarning("[LevelKit] missing asset: " + assetPath);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (go == null) return null;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            // Keep the rotation the prefab was authored with and layer yaw/pitch on top of it.
            // Several street props carry a -90 X correction for their Z-up source model, and
            // overwriting it outright used to lay oil drums and gas cans on their side.
            go.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f) * src.transform.rotation;
            if (scale != default(Vector3))
                go.transform.localScale = Vector3.Scale(go.transform.localScale, scale);
            _placed.Add(go);

            Bounds b;
            if (!TryBounds(go, out b)) { go.transform.localPosition = pos; return go; }

            if (targetHeight > 0f && b.size.y > 0.0001f)
            {
                go.transform.localScale = go.transform.localScale * (targetHeight / b.size.y);
                TryBounds(go, out b);
            }

            Vector3 cW = b.center;
            Vector3 baseW = new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 cL = parent != null ? parent.InverseTransformPoint(cW) : cW;
            Vector3 baseL = parent != null ? parent.InverseTransformPoint(baseW) : baseW;
            go.transform.localPosition += new Vector3(pos.x - cL.x, pos.y - baseL.y, pos.z - cL.z);
            return go;
        }

        public static bool TryBounds(GameObject go, out Bounds b)
        {
            b = new Bounds(go.transform.position, Vector3.zero);
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return false;
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return true;
        }

        // ───────────────────────── lighting helpers ─────────────────────────
        public static Light AddLight(Transform parent, string name, Vector3 localPos, Vector3 localEuler,
                                     LightType type, Color colour, float intensity, float range,
                                     float spotAngle = 60f, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = localEuler;
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = colour;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        /// <summary>Emissive neon tube: a thin glowing box plus an optional real light.</summary>
        public static Transform NeonStrip(string name, Transform parent, Vector3 centre, Vector3 size, string mat,
                                          Color lightColour, float lightIntensity = 0f, float lightRange = 8f)
        {
            var g = Group(name, parent);
            g.localPosition = centre;
            Box("Tube", g, Vector3.zero, size, mat, default(Vector3), false);
            if (lightIntensity > 0f)
                AddLight(g, "Light", Vector3.zero, Vector3.zero, LightType.Point, lightColour, lightIntensity, lightRange);
            return g;
        }
    }
}
