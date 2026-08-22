using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// Navigation-aware half of the level kit: player clearance figures, doorways that are
    /// actually walkable, and the snapping helpers that keep props sitting on the floor
    /// instead of hovering over it or sinking into it.
    ///
    /// Every builder in this project sizes its circulation from <see cref="Nav"/> rather than
    /// from hand-picked numbers, so a change to the player capsule propagates to every level.
    /// </summary>
    public static partial class LevelKit
    {
        /// <summary>Clearance figures taken from the CharacterController on player.prefab.</summary>
        public static class Nav
        {
            /// <summary>
            /// Standing height shared by the player and every NPC stand-in.
            ///
            /// player.prefab is authored with a 1.5 Y stretch on its root, which makes its
            /// CharacterController 3 m tall - two thirds taller than the NPC capsules it stands
            /// next to. The prefab is shared with the other chapters, so the levels rescale the
            /// instance instead of editing it. See <see cref="PlayerScale"/>.
            /// </summary>
            public const float HumanHeight = 1.80f;

            // player.prefab -> CharacterController: height 2, radius 0.5, skinWidth 0.08, stepOffset 0.3
            const float PrefabHeight = 2.00f;
            const float PrefabRadius = 0.50f;
            const float PrefabSkin = 0.08f;

            /// <summary>Uniform scale that brings the player instance down to <see cref="HumanHeight"/>.</summary>
            public const float PlayerScale = HumanHeight / PrefabHeight;      // 0.9

            public const float Radius = PrefabRadius * PlayerScale;           // 0.45
            public const float Skin = PrefabSkin * PlayerScale;               // 0.072
            public const float Height = HumanHeight;                          // 1.80
            public const float StepOffset = 0.30f;                            // not scaled by the transform

            /// <summary>Absolute width the capsule sweeps. Anything narrower is a hard block.</summary>
            public const float BodyWidth = (Radius + Skin) * 2f;      // 1.16 m

            /// <summary>Clear width of a doorway: body plus room to not scrape both jambs at once.</summary>
            public const float DoorClear = 1.80f;
            /// <summary>Clear width of a walking route between props.</summary>
            public const float PathClear = 1.60f;
            /// <summary>Clear width of a gap the player only squeezes through (cover, gaps in car rows).</summary>
            public const float SqueezeClear = 1.30f;
            /// <summary>Clear height under any header, beam or duct on a walking route.</summary>
            public const float HeadClear = 2.40f;
            /// <summary>Clear height of a doorway.</summary>
            public const float DoorHeight = 2.30f;

            /// <summary>Capsule radius the audit sweeps with: the body plus a small tolerance.</summary>
            public const float ProbeRadius = Radius + Skin + 0.02f;   // 0.60 m
        }

        /// <summary>Standard top level folders. Numbered so the Hierarchy always sorts in build order.</summary>
        public static class Cat
        {
            public const string Env = "00_ENV";
            public const string Structure = "10_STRUCTURE";
            public const string Circulation = "20_CIRCULATION";
            public const string Dressing = "30_DRESSING";
            public const string Lighting = "40_LIGHTING";
            public const string Actors = "50_ACTORS";
            public const string Gameplay = "60_GAMEPLAY";

            public static readonly string[] All =
            {
                Env, Structure, Circulation, Dressing, Lighting, Actors, Gameplay
            };
        }

        /// <summary>Find-or-create one of the standard category folders under a level root.</summary>
        public static Transform Category(Transform root, string category)
        {
            var t = root.Find(category);
            if (t != null) return t;
            return Group(category, root);
        }

        /// <summary>Creates all seven category folders in order, so every level reads the same.</summary>
        public static void BuildCategories(Transform root)
        {
            for (int i = 0; i < Cat.All.Length; i++) Category(root, Cat.All[i]);
        }

        // ───────────────────────── doorways ─────────────────────────

        /// <summary>
        /// Frame around a clear opening: two jambs and a head, all of them sitting OUTSIDE the
        /// clear box so the opening stays walkable. Replaces the solid slabs that used to be
        /// named "DoorFrame" and silently blocked the doorway they were framing.
        /// </summary>
        /// <param name="centre">Floor level centre of the opening (y = floor).</param>
        /// <param name="clearW">Clear width that must stay free of colliders.</param>
        /// <param name="clearH">Clear height that must stay free of colliders.</param>
        /// <param name="yaw">0 = the opening is punched through a wall whose normal is Z.</param>
        public static Transform DoorFrame(string name, Transform parent, Vector3 centre, float clearW, float clearH,
                                          float jamb, float depth, string mat, float yaw = 0f, bool collider = true)
        {
            var g = Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);

            float x = (clearW + jamb) * 0.5f;
            Box("Jamb_L", g, new Vector3(-x, clearH * 0.5f, 0f), new Vector3(jamb, clearH, depth), mat, default(Vector3), collider);
            Box("Jamb_R", g, new Vector3(x, clearH * 0.5f, 0f), new Vector3(jamb, clearH, depth), mat, default(Vector3), collider);
            Box("Head", g, new Vector3(0f, clearH + jamb * 0.5f, 0f), new Vector3(clearW + jamb * 2f, jamb, depth), mat, default(Vector3), collider);
            Marker("CLEAR", g, Vector3.zero);
            return g;
        }

        /// <summary>
        /// A door leaf, hung on a hinge and swung open. Geometry only: the collider is always
        /// stripped, because a leaf parked across the opening is indistinguishable from a wall.
        /// </summary>
        /// <param name="hinge">Hinge position at floor level.</param>
        /// <param name="openAngle">Degrees the leaf is swung away from the closed position.</param>
        public static Transform DoorLeaf(string name, Transform parent, Vector3 hinge, float width, float height,
                                         float thickness, string mat, float yaw = 0f, float openAngle = 95f,
                                         float sillY = 0f)
        {
            var g = Group(name, parent);
            g.localPosition = hinge;
            g.localEulerAngles = new Vector3(0f, yaw + openAngle, 0f);

            // the leaf hangs off +X of the hinge so the swing direction reads correctly
            Box("Leaf", g, new Vector3(width * 0.5f, sillY + height * 0.5f, 0f),
                new Vector3(width, height, thickness), mat, default(Vector3), false);
            Cyl("Handle", g, new Vector3(width - 0.09f, sillY + height * 0.47f, -thickness * 0.9f),
                0.04f, 0.13f, "Alu", new Vector3(90f, 0f, 0f));
            return g;
        }

        /// <summary>
        /// Straight wall run along local X with a doorway punched through it. The two wall panels
        /// carry the colliders; the opening itself is left completely empty.
        /// </summary>
        public static Transform WallWithOpening(string name, Transform parent, Vector3 centre, float length,
                                                float height, float thickness, string mat,
                                                float openingCentreX, float clearW, float clearH, float yaw = 0f)
        {
            var g = Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);

            float half = length * 0.5f;
            float oL = openingCentreX - clearW * 0.5f;
            float oR = openingCentreX + clearW * 0.5f;

            float leftW = oL + half;
            if (leftW > 0.01f)
                Box("Panel_L", g, new Vector3(-half + leftW * 0.5f, height * 0.5f, 0f), new Vector3(leftW, height, thickness), mat);
            float rightW = half - oR;
            if (rightW > 0.01f)
                Box("Panel_R", g, new Vector3(oR + rightW * 0.5f, height * 0.5f, 0f), new Vector3(rightW, height, thickness), mat);
            if (height > clearH + 0.01f)
                Box("Header", g, new Vector3(openingCentreX, (clearH + height) * 0.5f, 0f),
                    new Vector3(clearW, height - clearH, thickness), mat);

            Marker("OPENING", g, new Vector3(openingCentreX, 0f, 0f));
            return g;
        }

        // ───────────────────────── snapping ─────────────────────────

        /// <summary>
        /// Sits an object exactly on a surface: its lowest renderer touches surfaceY, no sinking,
        /// no floating. Returns the correction applied in metres (positive = it had been sunk).
        /// </summary>
        public static float SnapToSurface(GameObject go, float surfaceY)
        {
            Bounds b;
            if (go == null || !TryBounds(go, out b)) return 0f;
            float delta = surfaceY - b.min.y;
            if (Mathf.Abs(delta) < 0.0005f) return 0f;
            go.transform.position += new Vector3(0f, delta, 0f);
            return delta;
        }

        /// <summary>
        /// Drops an object onto whatever collider is underneath it, so props land on the slab or
        /// platform they were authored over rather than on the world origin.
        ///
        /// Only corrections up to <paramref name="maxCorrection"/> are applied. Anything further
        /// off the ground was put there on purpose (a car on a raised lift, a sign on a wall) and
        /// must not be dragged down to the floor.
        /// </summary>
        public static float SnapDown(GameObject go, float maxCorrection = 0.35f, float maxDrop = 3f, int layerMask = ~0)
        {
            Bounds b;
            if (go == null || !TryBounds(go, out b)) return 0f;

            Physics.SyncTransforms();
            Vector3 from = new Vector3(b.center.x, b.min.y + maxDrop * 0.5f, b.center.z);
            var hits = Physics.RaycastAll(from, Vector3.down, maxDrop * 1.5f, layerMask, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(go.transform)) continue;
                if (hit.point.y > b.min.y + 0.05f) continue;      // only surfaces at or below the object
                if (hit.point.y > best) best = hit.point.y;
            }
            if (float.IsNegativeInfinity(best)) return 0f;
            if (Mathf.Abs(best - b.min.y) > maxCorrection) return 0f;
            return SnapToSurface(go, best);
        }

        /// <summary>True when the object bounds poke into a collider that is not its own.</summary>
        public static bool OverlapsGeometry(GameObject go, float shrink, out Collider hit)
        {
            hit = null;
            Bounds b;
            if (go == null || !TryBounds(go, out b)) return false;

            Physics.SyncTransforms();
            Vector3 half = b.extents - Vector3.one * shrink;
            if (half.x <= 0f || half.y <= 0f || half.z <= 0f) return false;

            var cols = Physics.OverlapBox(b.center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].transform.IsChildOf(go.transform)) continue;
                if (go.transform.IsChildOf(cols[i].transform)) continue;
                hit = cols[i];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Replaces whatever colliders an imported model brought with it (usually one mesh collider
        /// per wheel and panel) with a single clean box, so the player cannot snag on a wheel arch.
        /// </summary>
        public static BoxCollider FitBoxCollider(GameObject go, float inset = 0.04f)
        {
            if (go == null) return null;
            var old = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < old.Length; i++) Object.DestroyImmediate(old[i]);

            // The box has to be measured in the object's OWN space. Taking a world-space AABB
            // and dividing it by lossyScale only works for an unrotated object: a car parked at
            // yaw 90 would come back with its length and width swapped, giving every side-on car
            // a collider turned across the bay.
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var inverse = go.transform.worldToLocalMatrix;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled) continue;
                Bounds wb = renderers[i].bounds;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3(
                        (c & 1) == 0 ? wb.min.x : wb.max.x,
                        (c & 2) == 0 ? wb.min.y : wb.max.y,
                        (c & 4) == 0 ? wb.min.z : wb.max.z);
                    Vector3 local = inverse.MultiplyPoint3x4(corner);
                    if (!any) { min = max = local; any = true; continue; }
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }
            if (!any) return null;

            var bc = go.AddComponent<BoxCollider>();
            bc.center = (min + max) * 0.5f;
            Vector3 size = max - min;
            bc.size = new Vector3(
                Mathf.Max(0.05f, size.x - inset * 2f),
                Mathf.Max(0.05f, size.y - inset * 2f),
                Mathf.Max(0.05f, size.z - inset * 2f));
            return bc;
        }
    }
}
