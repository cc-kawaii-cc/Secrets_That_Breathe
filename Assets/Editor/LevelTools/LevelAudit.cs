using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// Walkability checker for the procedural levels.
    ///
    /// It sweeps the player capsule over a grid of the level, flood fills from the player
    /// spawn, and reports anything the player cannot actually reach. That turns "the door
    /// looks open" into a number, which is the only way to know a level is navigable without
    /// entering play mode and walking it by hand.
    ///
    /// It also checks every DOOR_ marker for clearance and every placed prefab for floating,
    /// sinking or intersecting the structure.
    ///
    /// Menu: Tools > Secrets That Breathe > Audit Walkability (open scene)
    /// </summary>
    public static class LevelAudit
    {
        public class Result
        {
            public int walkable;             // cells the capsule fits in
            public int reachable;            // cells reachable from a spawn
            public float cell;
            public List<string> problems = new List<string>();
            public List<string> notes = new List<string>();

            public float WalkableArea { get { return walkable * cell * cell; } }
            public float ReachableArea { get { return reachable * cell * cell; } }
            public int Stranded { get { return walkable - reachable; } }
            public bool Ok { get { return problems.Count == 0; } }
        }

        struct Node
        {
            public int ix, iz;
            public float y;
            public Node(int x, int z, float yy) { ix = x; iz = z; y = yy; }
        }

        const float SurfaceNormalMin = 0.70f;   // cos(45 deg), matches the controller slope limit
        const float SurfaceMerge = 0.25f;       // surfaces closer than this count as one floor
        const float ConnectRise = 0.45f;        // max step between neighbouring cells (stairs + ramps)

        [MenuItem("Tools/Secrets That Breathe/Audit Walkability (open scene)", false, 40)]
        public static void AuditOpenScene()
        {
            var root = FindLevelRoot();
            if (root == null)
            {
                Debug.LogError("[LevelAudit] no level root found. Expected a root object named \"=== ... ===\".");
                return;
            }
            var r = Run(root, 0.4f);
            Debug.Log(Format(root.name, r));
        }

        /// <summary>The builders call this straight after a build so a broken level never ships silently.</summary>
        /// <param name="ceiling">
        /// Highest surface the player could ever stand on. Anything above it is a roof slab, a
        /// parapet or the top of a street backdrop, none of which is a walkable route.
        /// </param>
        public static Result Run(Transform root, float cell, float ceiling = float.PositiveInfinity)
        {
            var res = new Result();
            res.cell = cell;

            Bounds area;
            if (!WorldBounds(root, out area))
            {
                res.problems.Add("level has no renderers, nothing to audit");
                return res;
            }
            // trim the sky/roof off the top so the sweep only covers places a person could stand
            float yTop = area.max.y;
            float yBot = area.min.y - 1f;

            Physics.SyncTransforms();

            int nx = Mathf.CeilToInt(area.size.x / cell);
            int nz = Mathf.CeilToInt(area.size.z / cell);
            if ((long)nx * nz > 400000)
            {
                res.notes.Add("level is large, sampling at " + (cell * 2f).ToString("0.00") + " m instead");
                cell *= 2f;
                res.cell = cell;
                nx = Mathf.CeilToInt(area.size.x / cell);
                nz = Mathf.CeilToInt(area.size.z / cell);
            }

            // Measure the player that is really in the scene rather than trusting the constants:
            // if someone rescales the prefab again, the audit follows instead of going quietly wrong.
            float r = LevelKit.Nav.ProbeRadius;
            float h = LevelKit.Nav.Height;
            var pc = FindPlayerController(root);
            if (pc != null)
            {
                Vector3 ls = pc.transform.lossyScale;
                h = pc.height * Mathf.Abs(ls.y);
                r = pc.radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z)) + pc.skinWidth + 0.02f;
                res.notes.Add(string.Format("player capsule measured in scene: {0:0.00} m tall, {1:0.00} m wide", h, r * 2f));
                if (Mathf.Abs(h - LevelKit.Nav.HumanHeight) > 0.05f)
                    res.problems.Add(string.Format("player stands {0:0.00} m tall but the NPCs are {1:0.00} m - they will not read as the same species",
                                                   h, LevelKit.Nav.HumanHeight));
            }
            var grid = new Dictionary<long, List<float>>(nx * nz);
            var surfaces = new List<float>(8);

            for (int ix = 0; ix < nx; ix++)
            {
                float x = area.min.x + (ix + 0.5f) * cell;
                for (int iz = 0; iz < nz; iz++)
                {
                    float z = area.min.z + (iz + 0.5f) * cell;
                    surfaces.Clear();

                    var hits = Physics.RaycastAll(new Vector3(x, yTop + 1f, z), Vector3.down,
                                                  (yTop - yBot) + 2f, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].normal.y < SurfaceNormalMin) continue;
                        float sy = hits[i].point.y;
                        if (sy > ceiling) continue;
                        bool merged = false;
                        for (int k = 0; k < surfaces.Count; k++)
                        {
                            if (Mathf.Abs(surfaces[k] - sy) >= SurfaceMerge) continue;
                            // Levels stack slabs: ground, yard, apron, interior floor. They are all
                            // within a few centimetres, and the one you actually stand on is the
                            // TOP one. Keeping the lowest would bury the test capsule in the rest.
                            if (sy > surfaces[k]) surfaces[k] = sy;
                            merged = true;
                            break;
                        }
                        if (!merged) surfaces.Add(sy);
                    }
                    if (surfaces.Count == 0) continue;

                    List<float> standable = null;
                    for (int i = 0; i < surfaces.Count; i++)
                    {
                        float sy = surfaces[i];
                        // The capsule is tested from one step offset up, because the controller
                        // climbs anything that low. Without this, kerbs, wheel stops and door
                        // thresholds all read as walls even though the player walks over them.
                        Vector3 p0 = new Vector3(x, sy + LevelKit.Nav.StepOffset + r, z);
                        Vector3 p1 = new Vector3(x, sy + h - r + 0.03f, z);
                        if (Physics.CheckCapsule(p0, p1, r, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (standable == null) standable = new List<float>(2);
                        standable.Add(sy);
                    }
                    if (standable == null) continue;
                    grid[Key(ix, iz)] = standable;
                    res.walkable += standable.Count;
                }
            }

            // ── flood fill from every player spawn we can find ──
            var seeds = new List<Vector3>();
            CollectSeeds(root, seeds);
            if (seeds.Count == 0)
            {
                res.problems.Add("no PlayerSpawn / player object found, cannot test reachability");
                return res;
            }

            var visited = new HashSet<long>();
            var queue = new Queue<Node>();
            int seeded = 0;
            for (int i = 0; i < seeds.Count; i++)
            {
                int ix = Mathf.FloorToInt((seeds[i].x - area.min.x) / cell);
                int iz = Mathf.FloorToInt((seeds[i].z - area.min.z) / cell);
                Node n;
                if (!NearestNode(grid, ix, iz, seeds[i].y, out n)) continue;
                seeded++;
                long k = NodeKey(n, cell, area.min.y);
                if (visited.Add(k)) queue.Enqueue(n);
            }
            if (queue.Count == 0)
            {
                res.problems.Add("player spawn is inside geometry, the capsule does not fit there");
                return res;
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            var reachCols = new HashSet<long>();
            var reachTop = new Dictionary<long, float>();
            Vector3 reachMin = new Vector3(float.MaxValue, 0f, float.MaxValue);
            Vector3 reachMax = new Vector3(float.MinValue, 0f, float.MinValue);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                long ck = Key(cur.ix, cur.iz);
                reachCols.Add(ck);
                float top;
                if (!reachTop.TryGetValue(ck, out top) || cur.y > top) reachTop[ck] = cur.y;
                float wx = area.min.x + (cur.ix + 0.5f) * cell, wz = area.min.z + (cur.iz + 0.5f) * cell;
                if (wx < reachMin.x) reachMin.x = wx;
                if (wx > reachMax.x) reachMax.x = wx;
                if (wz < reachMin.z) reachMin.z = wz;
                if (wz > reachMax.z) reachMax.z = wz;

                for (int d = 0; d < 4; d++)
                {
                    int nx2 = cur.ix + dx[d], nz2 = cur.iz + dz[d];
                    List<float> ys;
                    if (!grid.TryGetValue(Key(nx2, nz2), out ys)) continue;
                    for (int i = 0; i < ys.Count; i++)
                    {
                        if (Mathf.Abs(ys[i] - cur.y) > ConnectRise) continue;
                        var n = new Node(nx2, nz2, ys[i]);
                        long k = NodeKey(n, cell, area.min.y);
                        if (!visited.Add(k)) continue;
                        queue.Enqueue(n);
                    }
                }
            }
            res.reachable = visited.Count;
            _reachTop = reachTop;

            // ── report the stranded regions, largest first ──
            if (res.Stranded > 0)
            {
                var islands = Islands(grid, visited, area, cell);
                islands.Sort((a, b) => b.count.CompareTo(a.count));
                int shown = 0, roofs = 0;
                for (int i = 0; i < islands.Count; i++)
                {
                    var isl = islands[i];
                    float m2 = isl.count * cell * cell;
                    if (m2 < 2f) continue;                       // slivers behind props are not rooms
                    // A patch you cannot stand on, directly above a patch you can, is a table top,
                    // a car roof or the roof of the building. None of those are a level design bug.
                    if (isl.overSomethingReachable > isl.count * 0.6f) { roofs++; continue; }
                    if (++shown > 8) break;
                    res.problems.Add(string.Format(
                        "unreachable area {0:0.0} m2 around ({1:0.0}, {2:0.0}, {3:0.0}) - nothing connects it to the spawn",
                        m2, isl.centre.x, isl.centre.y, isl.centre.z));
                }
                if (roofs > 0) res.notes.Add(roofs + " raised surface(s) ignored (prop tops, vehicle roofs, the roof slab)");
                if (islands.Count - roofs - shown > 0)
                    res.notes.Add((islands.Count - roofs - shown) + " pocket(s) under 2 m2 ignored (gaps behind props)");
            }
            res.notes.Add(string.Format("reachable region spans x {0:0.0}..{1:0.0}  z {2:0.0}..{3:0.0}",
                                        reachMin.x, reachMax.x, reachMin.z, reachMax.z));
            res.notes.Add(seeded + " of " + seeds.Count + " spawn/nav markers connected to it");

            AuditDoors(root, res);
            AuditPlacedProps(root, res);
            return res;
        }

        // ───────────────────────── doors ─────────────────────────

        static void AuditDoors(Transform root, Result res)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            float r = LevelKit.Nav.ProbeRadius;
            float h = LevelKit.Nav.Height;
            int checkedDoors = 0;

            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                // SHUT_ markers are doors that are locked by design; they are not failures
                if (!n.StartsWith("DOOR_") && n != "CLEAR" && n != "OPENING") continue;
                checkedDoors++;

                // Markers are authored at y 0 while the slab they stand on is a few centimetres
                // up, so drop the probe onto the actual floor before testing it.
                Vector3 p = all[i].position;
                var down = Physics.RaycastAll(new Vector3(p.x, p.y + 1.5f, p.z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < down.Length; k++)
                {
                    if (down[k].normal.y < SurfaceNormalMin) continue;
                    if (down[k].point.y > p.y + 0.6f) continue;
                    if (down[k].point.y > p.y) p.y = down[k].point.y;
                }
                Vector3 p0 = new Vector3(p.x, p.y + LevelKit.Nav.StepOffset + r, p.z);
                Vector3 p1 = new Vector3(p.x, p.y + h - r + 0.05f, p.z);
                var blockers = Physics.OverlapCapsule(p0, p1, r, ~0, QueryTriggerInteraction.Ignore);
                if (blockers.Length == 0) continue;

                // naming the offending collider is the difference between a report and a fix
                var names = new StringBuilder();
                for (int k = 0; k < blockers.Length && k < 4; k++)
                {
                    if (names.Length > 0) names.Append(", ");
                    names.Append(Path(blockers[k].transform, root));
                }
                if (blockers.Length > 4) names.Append(", +" + (blockers.Length - 4) + " more");
                res.problems.Add(string.Format("{0} at ({1:0.0}, {2:0.0}, {3:0.0}) is blocked by: {4}",
                                               Path(all[i], root), p.x, p.y, p.z, names));
            }
            res.notes.Add(checkedDoors + " door/opening markers checked");
        }

        // ───────────────────────── placed prefabs ─────────────────────────

        static void AuditPlacedProps(Transform root, Result res)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            var seen = new HashSet<GameObject>();
            int floated = 0, sunk = 0, buried = 0;

            Physics.SyncTransforms();
            for (int i = 0; i < all.Length; i++)
            {
                var go = PrefabUtility.GetOutermostPrefabInstanceRoot(all[i].gameObject);
                if (go == null || !seen.Add(go)) continue;
                if (go.transform == root) continue;

                Bounds b;
                if (!LevelKit.TryBounds(go, out b)) continue;

                // find the surface under it, ignoring itself
                var hits = Physics.RaycastAll(new Vector3(b.center.x, b.min.y + 1.5f, b.center.z),
                                              Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore);
                float floor = float.NegativeInfinity;
                for (int k = 0; k < hits.Length; k++)
                {
                    if (hits[k].collider.transform.IsChildOf(go.transform)) continue;
                    if (hits[k].normal.y < SurfaceNormalMin) continue;
                    if (hits[k].point.y > b.min.y + 0.10f) continue;
                    if (hits[k].point.y > floor) floor = hits[k].point.y;
                }
                if (float.IsNegativeInfinity(floor)) continue;

                float gap = b.min.y - floor;
                // anything more than half a metre up was put there on purpose (a car on a lift,
                // a crate on a shelf), so only the small errors count as placement mistakes
                if (gap > 0.03f && gap < 0.5f) { floated++; res.notes.Add(string.Format("floating {0:0.00} m: {1}", gap, Path(go.transform, root))); }
                else if (gap < -0.02f) { sunk++; res.notes.Add(string.Format("sunk {0:0.00} m: {1}", -gap, Path(go.transform, root))); }

                Collider hit;
                if (LevelKit.OverlapsGeometry(go, 0.12f, out hit) && hit != null)
                {
                    // only care when it is buried in the structure, not touching a neighbouring prop
                    var t = hit.transform;
                    while (t != null && t != root)
                    {
                        if (t.name == LevelKit.Cat.Structure)
                        {
                            buried++;
                            res.notes.Add("intersects " + Path(hit.transform, root) + ": " + Path(go.transform, root));
                            break;
                        }
                        t = t.parent;
                    }
                }
            }
            if (floated > 0) res.problems.Add(floated + " placed prefab(s) hover above the floor");
            if (sunk > 0) res.problems.Add(sunk + " placed prefab(s) sink into the floor");
            if (buried > 0) res.problems.Add(buried + " placed prefab(s) intersect the building structure");
        }

        // ───────────────────────── plumbing ─────────────────────────

        class Island { public int count; public Vector3 centre; public int overSomethingReachable; }

        /// <summary>Highest reachable surface per grid column, used to spot prop tops.</summary>
        static Dictionary<long, float> _reachTop;

        static List<Island> Islands(Dictionary<long, List<float>> grid, HashSet<long> visited, Bounds area, float cell)
        {
            var list = new List<Island>();
            var done = new HashSet<long>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            foreach (var kv in grid)
            {
                int ix = (int)(kv.Key >> 32);
                int iz = (int)(kv.Key & 0xFFFFFFFF);
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var start = new Node(ix, iz, kv.Value[i]);
                    long sk = NodeKey(start, cell, area.min.y);
                    if (visited.Contains(sk) || !done.Add(sk)) continue;

                    var isl = new Island();
                    Vector3 sum = Vector3.zero;
                    var q = new Queue<Node>();
                    q.Enqueue(start);
                    while (q.Count > 0)
                    {
                        var cur = q.Dequeue();
                        isl.count++;
                        float below;
                        if (_reachTop != null && _reachTop.TryGetValue(Key(cur.ix, cur.iz), out below) && below < cur.y - 0.25f)
                            isl.overSomethingReachable++;
                        sum += new Vector3(area.min.x + (cur.ix + 0.5f) * cell, cur.y, area.min.z + (cur.iz + 0.5f) * cell);
                        for (int d = 0; d < 4; d++)
                        {
                            List<float> ys;
                            if (!grid.TryGetValue(Key(cur.ix + dx[d], cur.iz + dz[d]), out ys)) continue;
                            for (int k = 0; k < ys.Count; k++)
                            {
                                if (Mathf.Abs(ys[k] - cur.y) > ConnectRise) continue;
                                var n = new Node(cur.ix + dx[d], cur.iz + dz[d], ys[k]);
                                long nk = NodeKey(n, cell, area.min.y);
                                if (visited.Contains(nk) || !done.Add(nk)) continue;
                                q.Enqueue(n);
                            }
                        }
                    }
                    isl.centre = sum / Mathf.Max(1, isl.count);
                    list.Add(isl);
                }
            }
            return list;
        }

        static void CollectSeeds(Transform root, List<Vector3> seeds)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.StartsWith("PlayerSpawn") || n == "player" || n.StartsWith("ENTRY_") || n.StartsWith("NAV_"))
                    seeds.Add(all[i].position);
            }
        }

        static bool NearestNode(Dictionary<long, List<float>> grid, int ix, int iz, float y, out Node node)
        {
            node = default(Node);
            float best = float.MaxValue;
            bool found = false;
            for (int ox = -3; ox <= 3; ox++)
                for (int oz = -3; oz <= 3; oz++)
                {
                    List<float> ys;
                    if (!grid.TryGetValue(Key(ix + ox, iz + oz), out ys)) continue;
                    for (int i = 0; i < ys.Count; i++)
                    {
                        float d = Mathf.Abs(ys[i] - y) * 4f + Mathf.Abs(ox) + Mathf.Abs(oz);
                        if (d >= best) continue;
                        best = d;
                        node = new Node(ix + ox, iz + oz, ys[i]);
                        found = true;
                    }
                }
            return found;
        }

        static long Key(int ix, int iz) { return ((long)ix << 32) | (uint)iz; }

        static long NodeKey(Node n, float cell, float baseY)
        {
            int level = Mathf.RoundToInt((n.y - baseY) / 0.5f);
            return ((long)(n.ix + 8192) << 42) | ((long)(n.iz + 8192) << 20) | (uint)(level + 1024);
        }

        static bool WorldBounds(Transform root, out Bounds b)
        {
            b = new Bounds(root.position, Vector3.zero);
            var rs = root.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return false;
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return true;
        }

        static CharacterController FindPlayerController(Transform root)
        {
            var all = root.GetComponentsInChildren<CharacterController>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gameObject.name == "player") return all[i];
            return all.Length > 0 ? all[0] : null;
        }

        static Transform FindLevelRoot()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].name.StartsWith("===")) return roots[i].transform;
            return roots.Length > 0 ? roots[0].transform : null;
        }

        static string Path(Transform t, Transform root)
        {
            var sb = new StringBuilder(t.name);
            var p = t.parent;
            while (p != null && p != root) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }

        public static string Format(string level, Result r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[LevelAudit] " + level);
            sb.AppendLine(string.Format("  grid {0:0.00} m   walkable {1:0} m2   reachable {2:0} m2   stranded {3} cells",
                                        r.cell, r.WalkableArea, r.ReachableArea, r.Stranded));
            if (r.problems.Count == 0) sb.AppendLine("  PASS - every walkable cell is reachable from the spawn");
            else
            {
                sb.AppendLine("  " + r.problems.Count + " PROBLEM(S):");
                for (int i = 0; i < r.problems.Count; i++) sb.AppendLine("   x " + r.problems[i]);
            }
            for (int i = 0; i < r.notes.Count && i < 30; i++) sb.AppendLine("   . " + r.notes[i]);
            if (r.notes.Count > 30) sb.AppendLine("   . (" + (r.notes.Count - 30) + " more notes)");
            return sb.ToString();
        }
    }
}
