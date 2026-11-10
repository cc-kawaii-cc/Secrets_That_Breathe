using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecretsThatBreathe
{
    // A banana built entirely in code and held in front of the first-person camera. Three peel flaps open,
    // the fruit gets shorter with every bite, and the peel is thrown away at the end. No art assets are used,
    // so the banana matches any scene. The player's phone view model (lower right) is left alone.
    public sealed class BananaViewModel
    {
        // Shape. The banana grows along +Y from the stem (s = 0) to the tip (s = 1), curving in the XY plane.
        private const float Length = .21f;
        private const float BodyRadius = .0215f;
        private const float Arc = 62f * Mathf.Deg2Rad;
        private const float ArcStart = .35f;
        private const float StemEnd = .10f;
        private const float PeelEnd = .965f;
        private const int Rings = 30;
        private const int Sides = 20;
        private const int FlapCount = 3;
        private const float FlapOpenDegrees = 118f;
        private static readonly Vector3 Binormal = new Vector3(0f, 0f, 1f);
        private static readonly float[] SamplePoints = { 0f, .5f, 1f };

        private struct Pose
        {
            public Vector3 position, euler;
            public Pose(Vector3 position, Vector3 euler) { this.position = position; this.euler = euler; }
        }

        // Poses are in camera space (x right, y up, z forward, metres).
        private static readonly Pose Hidden = new Pose(new Vector3(.30f, -.55f, .42f), new Vector3(-30f, 10f, -32f));
        private static readonly Pose Held = new Pose(new Vector3(.10f, -.15f, .42f), new Vector3(-6f, 18f, -10f));
        private static readonly Pose Mouth = new Pose(new Vector3(.015f, -.12f, .36f), new Vector3(-55f, 6f, -4f));
        private static readonly Pose Bite = new Pose(new Vector3(.005f, -.095f, .30f), new Vector3(-66f, 4f, -3f));
        private static readonly Pose Tossed = new Pose(new Vector3(.38f, -.70f, .40f), new Vector3(-20f, 30f, -70f));

        private readonly Camera view;
        private readonly bool ripe;
        private readonly bool playEatingSounds;
        private readonly bool useSceneLighting;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Transform[] flaps = new Transform[FlapCount];
        private readonly Vector3[] hingeAxes = new Vector3[FlapCount];
        private Transform root;
        private GameObject fruitObject, tipObject;
        private MeshFilter fruitFilter;
        private Material fruitMaterial;
        private AudioSource audioSource;

        public Transform Root => root;
        // True once the whole sequence has played to the end (false if it was stopped early).
        public bool Completed { get; private set; }

        public static BananaViewModel Create(Camera view, bool ripe, bool playEatingSounds = true, bool useSceneLighting = false)
            => new BananaViewModel(view, ripe, playEatingSounds, useSceneLighting);

        private BananaViewModel(Camera view, bool ripe, bool playEatingSounds, bool useSceneLighting)
        {
            this.view = view;
            this.ripe = ripe;
            this.playEatingSounds = playEatingSounds;
            this.useSceneLighting = useSceneLighting;
            Build();
        }

        // ------------------------------------------------------------------ animation

        // Pick, peel, three bites, throw the peel away. keepGoing returning false stops it early.
        public IEnumerator Run(Func<bool> keepGoing)
        {
            SetPose(Hidden);
            yield return Tween(Hidden, Held, .55f, EaseOutCubic, keepGoing);
            yield return Hold(Held, .45f, true, keepGoing);
            for (int i = 0; i < FlapCount; i++)
            {
                if (!keepGoing()) yield break;
                yield return Peel(i, keepGoing);
            }
            yield return Hold(Held, .2f, false, keepGoing);
            yield return Tween(Held, Mouth, .45f, EaseInOut, keepGoing);
            float[] cuts = { .70f, .42f, 0f }; // where the fruit ends after each bite (0 = all eaten)
            for (int bite = 0; bite < cuts.Length; bite++)
            {
                if (!keepGoing()) yield break;
                yield return Tween(Mouth, Bite, .16f, EaseOutCubic, keepGoing);
                SetFruitEnd(cuts[bite]);
                if (playEatingSounds) Play(BiteSound(), BiteVolume());
                yield return Hold(Bite, .12f, false, keepGoing);
                yield return Tween(Bite, Mouth, .2f, EaseInOut, keepGoing);
                yield return Chew(Mouth, .5f, keepGoing);
            }
            yield return Tween(Mouth, Tossed, .55f, EaseInCubic, keepGoing);
            Completed = keepGoing();
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            foreach (var item in owned) if (item != null) UnityEngine.Object.Destroy(item);
            owned.Clear();
            root = null;
        }

        private IEnumerator Tween(Pose from, Pose to, float seconds, Func<float, float> ease, Func<bool> keepGoing)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (!keepGoing()) yield break;
                SetPose(Lerp(from, to, ease(Mathf.Clamp01(t / seconds))));
                yield return null;
            }
            SetPose(to);
        }

        private IEnumerator Hold(Pose pose, float seconds, bool sway, Func<bool> keepGoing)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (!keepGoing()) yield break;
                float turn = sway ? Mathf.Sin(t / seconds * Mathf.PI * 2f) * 22f : 0f;
                SetPose(Offset(pose, Vector3.zero, new Vector3(0f, turn, 0f)));
                yield return null;
            }
            SetPose(pose);
        }

        private IEnumerator Peel(int index, Func<bool> keepGoing)
        {
            const float seconds = .5f;
            if (playEatingSounds) Play(PeelSound(), .8f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (!keepGoing()) yield break;
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                flaps[index].localRotation = Quaternion.AngleAxis(FlapOpenDegrees * k, hingeAxes[index]);
                // The other hand pulls the peel, so the banana is tugged a little each time.
                float tug = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));
                float side = index % 2 == 0 ? 1f : -1f;
                SetPose(Offset(Held, new Vector3(-.004f * tug, -.006f * tug, -.012f * tug), new Vector3(4f * tug, 0f, 3f * tug * side)));
                yield return null;
            }
            flaps[index].localRotation = Quaternion.AngleAxis(FlapOpenDegrees, hingeAxes[index]);
            SetPose(Held);
        }

        private IEnumerator Chew(Pose pose, float seconds, Func<bool> keepGoing)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (!keepGoing()) yield break;
                float fade = 1f - t / seconds;
                float wobble = Mathf.Sin(t * Mathf.PI * 2f * 6f) * fade;
                SetPose(Offset(pose, new Vector3(0f, .004f * wobble, 0f), new Vector3(1.2f * wobble, 0f, .8f * wobble)));
                yield return null;
            }
            SetPose(pose);
        }

        private static Pose Lerp(Pose a, Pose b, float k)
            => new Pose(Vector3.Lerp(a.position, b.position, k), Quaternion.Slerp(Quaternion.Euler(a.euler), Quaternion.Euler(b.euler), k).eulerAngles);

        private static Pose Offset(Pose p, Vector3 position, Vector3 euler) => new Pose(p.position + position, p.euler + euler);
        private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
        private static float EaseInCubic(float x) => x * x * x;
        private static float EaseInOut(float x) => Mathf.SmoothStep(0f, 1f, x);

        private void SetPose(Pose pose)
        {
            if (root == null) return;
            root.localPosition = pose.position;
            root.localRotation = Quaternion.Euler(pose.euler);
            // Keep the whole banana beyond the camera's near plane so it is never cut off.
            float nearestZ = float.MaxValue;
            foreach (float s in SamplePoints)
                nearestZ = Mathf.Min(nearestZ, (root.localPosition + root.localRotation * Centre(s)).z);
            float limit = view.nearClipPlane + .06f;
            if (nearestZ < limit) root.localPosition += new Vector3(0f, 0f, limit - nearestZ);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            int layer = 0;
            if ((view.cullingMask & 1) == 0)
                for (int i = 0; i < 32; i++) if ((view.cullingMask & (1 << i)) != 0) { layer = i; break; }

            root = new GameObject("BananaViewModel").transform;
            root.SetParent(view.transform, false);
            root.gameObject.layer = layer;
            audioSource = root.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = .7f;

            Color peelColour = ripe ? new Color32(245, 206, 44, 255) : new Color32(142, 172, 62, 255);
            Material peelMaterial = Make(peelColour, .35f);
            Material insideMaterial = Make(new Color32(236, 226, 172, 255), .4f);
            fruitMaterial = Make(ripe ? new Color32(250, 238, 192, 255) : new Color32(232, 238, 196, 255), .45f);
            Material stemMaterial = Make(new Color32(96, 102, 46, 255), .3f);
            Material nubMaterial = Make(new Color32(52, 38, 26, 255), .25f);

            // The stem stays; the tip goes with the first bite.
            AddPart("Stem", BuildSimpleTube(0f, StemEnd + .012f, 8, s => Mathf.Lerp(.0045f, .006f, s / StemEnd), true, false), stemMaterial, Vector3.zero, layer);
            tipObject = AddPart("Tip", BuildSimpleTube(PeelEnd - .006f, 1f, 6, PeelRadius, false, true), nubMaterial, Vector3.zero, layer);

            // The fruit under the peel.
            fruitObject = AddPart("Fruit", BuildFruitMesh(PeelEnd), fruitMaterial, Vector3.zero, layer);
            fruitFilter = fruitObject.GetComponent<MeshFilter>();

            // Three peel flaps hinged at the stem end.
            Frame(StemEnd, out Vector3 tangent, out Vector3 normal);
            Vector3 pivot = Centre(StemEnd);
            for (int i = 0; i < FlapCount; i++)
            {
                float theta0 = i * Mathf.PI * 2f / FlapCount, theta1 = (i + 1) * Mathf.PI * 2f / FlapCount;
                var builder = new Builder(2);
                Tube(builder, 0, StemEnd, PeelEnd, Rings, theta0, theta1, 8, PeelRadius, .08f, false, pivot);
                Tube(builder, 1, StemEnd, PeelEnd, Rings, theta0, theta1, 8, s => PeelRadius(s) - .0022f, .08f, true, pivot);
                Mesh mesh = builder.ToMesh("PeelFlap" + i);
                owned.Add(mesh);
                var flap = new GameObject("PeelFlap" + i);
                flap.layer = layer;
                flap.transform.SetParent(root, false);
                flap.transform.localPosition = pivot;
                flap.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = flap.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { peelMaterial, insideMaterial };
                Configure(renderer);
                flaps[i] = flap.transform;
                float middle = (i + .5f) * Mathf.PI * 2f / FlapCount;
                Vector3 outward = normal * Mathf.Cos(middle) + Binormal * Mathf.Sin(middle);
                hingeAxes[i] = Vector3.Cross(tangent, outward).normalized;
            }
        }

        private GameObject AddPart(string partName, Mesh mesh, Material material, Vector3 position, int layer)
        {
            owned.Add(mesh);
            var part = new GameObject(partName);
            part.layer = layer;
            part.transform.SetParent(root, false);
            part.transform.localPosition = position;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Configure(renderer);
            return part;
        }

        private void Configure(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = useSceneLighting;
        }

        private Material Make(Color colour, float emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var material = new Material(shader) { name = "BananaViewModel" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .25f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .25f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_ReceiveShadows")) material.SetFloat("_ReceiveShadows", useSceneLighting ? 1f : 0f);
            if (!useSceneLighting && emission > 0f && material.HasProperty("_EmissionColor"))
            {
                // A little self-lighting keeps the banana readable in a dark scene.
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * emission);
            }
            owned.Add(material);
            return material;
        }

        // endS is where the fruit stops; 0 or anything near the stem hides it completely.
        private void SetFruitEnd(float endS)
        {
            if (fruitObject == null) return;
            // The dark tip is the end of the fruit, so the first bite takes it away with it.
            if (tipObject != null && endS < PeelEnd - .01f) tipObject.SetActive(false);
            if (endS <= StemEnd + .03f) { fruitObject.SetActive(false); return; }
            Mesh old = fruitFilter.sharedMesh;
            Mesh fresh = BuildFruitMesh(endS);
            owned.Add(fresh);
            fruitFilter.sharedMesh = fresh;
            if (old != null) { owned.Remove(old); UnityEngine.Object.Destroy(old); }
        }

        private static Mesh BuildFruitMesh(float endS)
        {
            var builder = new Builder(1);
            Func<float, float> radius = s => PeelRadius(s) * .86f;
            int rings = Mathf.Max(3, Mathf.RoundToInt(Rings * (endS - StemEnd) / (PeelEnd - StemEnd)));
            Tube(builder, 0, StemEnd, endS, rings, 0f, Mathf.PI * 2f, Sides, radius, .04f, false, Vector3.zero);
            Cap(builder, 0, StemEnd, Sides, radius, .04f, false, 0f, 0f);
            // The bitten end is jagged and slightly hollowed.
            Cap(builder, 0, endS, Sides, radius, .04f, true, .012f, endS < PeelEnd - .01f ? .22f : 0f);
            return builder.ToMesh("BananaFruit");
        }

        private static Mesh BuildSimpleTube(float s0, float s1, int rings, Func<float, float> radius, bool capStart, bool capEnd)
        {
            var builder = new Builder(1);
            Tube(builder, 0, s0, s1, rings, 0f, Mathf.PI * 2f, 12, radius, 0f, false, Vector3.zero);
            if (capStart) Cap(builder, 0, s0, 12, radius, 0f, false, 0f, 0f);
            if (capEnd) Cap(builder, 0, s1, 12, radius, 0f, true, 0f, 0f);
            return builder.ToMesh("BananaPart");
        }

        // ------------------------------------------------------------------ shape

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - a) / (b - a)));

        private static float PeelRadius(float s)
        {
            float tip = 1f - Smooth(.90f, 1f, s) * .82f;
            float neck = Smooth(.04f, .24f, s);
            float swell = 1f + .12f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((s - .12f) / .85f));
            return BodyRadius * Mathf.Lerp(.22f, 1f, neck) * swell * tip;
        }

        // Point on the banana's centre line. Closed form of integrating the tangent along the arc.
        private static Vector3 Centre(float s)
        {
            float k = Length / Arc;
            float a = Arc * (s - ArcStart), a0 = Arc * (0f - ArcStart);
            return new Vector3(k * (Mathf.Cos(a) - Mathf.Cos(a0)), k * (Mathf.Sin(a) - Mathf.Sin(a0)), 0f);
        }

        private static void Frame(float s, out Vector3 tangent, out Vector3 normal)
        {
            float a = Arc * (s - ArcStart);
            tangent = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
            normal = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        }

        private sealed class Builder
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<List<int>> triangles = new List<List<int>>();
            public Builder(int submeshes) { for (int i = 0; i < submeshes; i++) triangles.Add(new List<int>()); }

            public Mesh ToMesh(string meshName)
            {
                var mesh = new Mesh { name = meshName };
                mesh.SetVertices(vertices);
                mesh.subMeshCount = triangles.Count;
                for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // A tube around the centre line between s0 and s1, over the angle range theta0..theta1.
        // inward flips the faces so the same surface can be seen from inside (the back of a peel flap).
        private static void Tube(Builder builder, int submesh, float s0, float s1, int rings, float theta0, float theta1, int sides,
            Func<float, float> radius, float ridge, bool inward, Vector3 origin)
        {
            int start = builder.vertices.Count;
            for (int i = 0; i <= rings; i++)
            {
                float s = Mathf.Lerp(s0, s1, i / (float)rings);
                Frame(s, out Vector3 tangent, out Vector3 normal);
                Vector3 centre = Centre(s);
                float r = radius(s);
                for (int j = 0; j <= sides; j++)
                {
                    float theta = Mathf.Lerp(theta0, theta1, j / (float)sides);
                    float rr = r * (1f + ridge * Mathf.Cos(5f * theta));
                    builder.vertices.Add(centre + (normal * Mathf.Cos(theta) + Binormal * Mathf.Sin(theta)) * rr - origin);
                }
            }
            int row = sides + 1;
            List<int> list = builder.triangles[submesh];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = start + i * row + j, b = a + row, c = a + 1, d = b + 1;
                    if (!inward) { list.Add(a); list.Add(b); list.Add(c); list.Add(b); list.Add(d); list.Add(c); }
                    else { list.Add(a); list.Add(c); list.Add(b); list.Add(b); list.Add(c); list.Add(d); }
                }
        }

        // Closes a full tube at s. facingForward: the cap looks along +tangent (the tip end) or back toward the stem.
        // dimple pushes the middle of a forward cap inward; jagged shrinks random points on its rim (a bite).
        private static void Cap(Builder builder, int submesh, float s, int sides, Func<float, float> radius, float ridge,
            bool facingForward, float dimple, float jagged)
        {
            Frame(s, out Vector3 tangent, out Vector3 normal);
            Vector3 centre = Centre(s);
            float r = radius(s);
            var random = new System.Random(Mathf.RoundToInt(s * 1000f) + 11);
            var shrink = new float[sides];
            for (int j = 0; j < sides; j++) shrink[j] = 1f - jagged * (float)random.NextDouble();
            int start = builder.vertices.Count;
            for (int j = 0; j <= sides; j++)
            {
                float theta = j / (float)sides * Mathf.PI * 2f;
                float rr = r * (1f + ridge * Mathf.Cos(5f * theta)) * shrink[j % sides];
                builder.vertices.Add(centre + (normal * Mathf.Cos(theta) + Binormal * Mathf.Sin(theta)) * rr);
            }
            int middle = builder.vertices.Count;
            builder.vertices.Add(centre - tangent * (facingForward ? dimple : 0f));
            List<int> list = builder.triangles[submesh];
            for (int j = 0; j < sides; j++)
            {
                if (facingForward) { list.Add(start + j + 1); list.Add(start + j); list.Add(middle); }
                else { list.Add(start + j); list.Add(start + j + 1); list.Add(middle); }
            }
        }

        // ------------------------------------------------------------------ sound (generated, no audio files needed)

        private static AudioClip munch, peelSound;

        // The bite sound: the clip in BananaSoundSettings (Resources, only its biteStart..biteEnd part) when one is set,
        // otherwise the generated munch below.
        private static AudioClip BiteSound()
        {
            var settings = BananaSoundSettings.Current;
            return settings != null && settings.biteSound != null ? settings.BiteClip() : Munch();
        }

        private static float BiteVolume()
        {
            var settings = BananaSoundSettings.Current;
            return settings != null && settings.biteSound != null ? settings.biteVolume : .9f;
        }

        private static AudioClip Munch()
        {
            if (munch != null) return munch;
            const int rate = 44100;
            int count = (int)(rate * .42f);
            var data = new float[count];
            var random = new System.Random(7);
            float low = 0f, lower = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                low += .18f * (noise - low);
                lower += .5f * (low - lower);
                // Two quick crunches and a soft thump under them.
                float crunch = Mathf.Exp(-t * 28f) + (t > .11f ? .7f * Mathf.Exp(-(t - .11f) * 34f) : 0f);
                float thump = Mathf.Sin(2f * Mathf.PI * (95f - 40f * t) * t) * Mathf.Exp(-t * 18f) * .5f;
                data[i] = Mathf.Clamp((lower * 3.2f * crunch + thump) * .8f, -1f, 1f);
            }
            munch = AudioClip.Create("BananaMunch", count, 1, rate, false);
            munch.SetData(data, 0);
            return munch;
        }

        private static AudioClip PeelSound()
        {
            if (peelSound != null) return peelSound;
            const int rate = 44100;
            int count = (int)(rate * .38f);
            var data = new float[count];
            var random = new System.Random(21);
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                low += .35f * (noise - low);
                float highPass = noise - low;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / .38f));
                float texture = .55f + .45f * Mathf.Sin(2f * Mathf.PI * 38f * t);
                data[i] = Mathf.Clamp(highPass * envelope * texture * .6f, -1f, 1f);
            }
            peelSound = AudioClip.Create("BananaPeel", count, 1, rate, false);
            peelSound.SetData(data, 0);
            return peelSound;
        }
    }
}
