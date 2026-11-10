using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight rain. It starts as a light drizzle when the car begins its first turn on the drive, slowly gets heavier,
    // and becomes the heaviest once the skid mark photo is taken (then Khem thinks "ฝนตกแล้ว ต้องรีบแล้วสิ").
    // Drops only land on the Terrain and on the car: everything else (trees, props, invisible walls) is passed through.
    // Where a drop lands on the Terrain a small ripple and splash appear; on the car only a splash. The ground and props near the
    // crash site slowly turn wet, the blood stains fade, the rain is heard. Right-click the component > Start Rain Now to test it.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(15100)]
    public sealed class MidnightRain : MonoBehaviour
    {
        [Header("When it starts and how heavy it gets")]
        [Tooltip("The drive: the rain starts when this car begins its first turn.")]
        public PorschePassengerRide ride;
        [Tooltip("Degrees the car must have turned from its starting heading.")]
        [Range(.5f, 45f)] public float turnDegrees = 4f;
        [Tooltip("After this photo (the skid mark) the rain becomes the heaviest.")]
        public MidnightSkidPhoto afterPhoto;
        [Tooltip("Drops per second at the heaviest rain, over the whole area around Khem.")]
        [Min(0f)] public float heaviestDropsPerSecond = 3600f;
        [Tooltip("How heavy it is when it starts (fraction of the heaviest).")]
        [Range(0f, 1f)] public float startAmount = .08f;
        [Tooltip("How heavy it has become just before the skid mark photo.")]
        [Range(0f, 1f)] public float beforePhotoAmount = .45f;
        [Tooltip("Seconds from the first drops to the 'before photo' amount.")]
        [Min(1f)] public float buildUpSeconds = 120f;
        [Tooltip("Seconds to reach the heaviest rain after the photo.")]
        [Min(.1f)] public float heavyRampSeconds = 12f;

        [Header("Khem's line")]
        public TMP_Text thought;
        [Tooltip("Seconds after the skid mark photo.")]
        [Min(0f)] public float thoughtDelay = .7f;
        [Min(0f)] public float thoughtSeconds = 3f;

        [Header("Drops")]
        public Material dropMaterial;
        [Tooltip("Size of the area the drops fall in, centred a little ahead of Khem (metres).")]
        public Vector2 area = new Vector2(30f, 30f);
        [Min(2f)] public float startHeight = 14f;
        public Vector2 fallSpeed = new Vector2(9.5f, 12f);
        [Tooltip("Sideways drift (wind), metres per second.")]
        public Vector2 wind = new Vector2(.6f, .25f);
        [Min(.001f)] public float dropWidth = .014f;
        [Tooltip("Streak length per metre per second of speed.")]
        [Min(0f)] public float streakLength = .035f;
        public Color dropColour = new Color(.62f, .68f, .76f, .38f);

        [Header("Ripples and splashes where drops land")]
        public Material rippleMaterial;
        public Material splashMaterial;
        [Min(.01f)] public float rippleSize = .32f;
        public Color rippleColour = new Color(.7f, .76f, .84f, .5f);
        [Range(0, 6)] public int splashDroplets = 2;
        [Tooltip("Ripples are only drawn this close to Khem (far away they are too small to see).")]
        [Min(1f)] public float rippleRadius = 14f;

        [Header("What the drops land on")]
        [Tooltip("The car (drops stop on it, so it never rains inside). Empty = the drive car.")]
        public Transform car;

        [Header("Wet ground and props")]
        [Tooltip("Centre of the area that gets wet (empty = the skid mark).")]
        public Transform wetCentre;
        [Min(0f)] public float wetRadius = 40f;
        [Tooltip("Seconds from dry to fully wet, from the moment the rain starts.")]
        [Min(.1f)] public float wetSeconds = 90f;
        [Range(0f, 1f)] public float wetSmoothness = .82f;
        [Tooltip("Base colour multiplier when wet (wet surfaces look darker).")]
        [Range(.2f, 1f)] public float wetDarken = .7f;

        [Header("Blood washed away by the rain")]
        [Tooltip("Stains that slowly fade while it rains (their Stain Strength / _Opacity goes down).")]
        public Renderer[] fadingMarks;
        [Range(0f, 1f)] public float fadedOpacity = .35f;
        [Tooltip("Seconds of rain until the stains reach the faded strength.")]
        [Min(1f)] public float fadeSeconds = 160f;

        [Header("Sound")]
        public AudioClip rainLoop;
        [Range(0f, 1f)] public float volume = .45f;

        public bool Started { get; private set; }
        /// <summary>0 = dry, 1 = the heaviest rain.</summary>
        public float Intensity { get; private set; }

        private ParticleSystem drops, ripples, splashes;
        private AudioSource sound;
        private float soundFadeStart = -1f, soundFadeSeconds = 1f;
        private float startTime;
        private float wetness;
        private float heavyStart = -1f, thoughtAt = -1f;
        private bool headingKnown;
        private Vector3 startHeading;
        private ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[0];
        private Terrain[] terrains = new Terrain[0];
        private Collider[] carColliders = new Collider[0];
        private Bounds carLocalBounds;
        private bool carHasBounds;
        private readonly List<Material> fadingMaterials = new List<Material>();
        private readonly List<float> fadingStart = new List<float>();
        private float lastFade = -1f;
        private bool thoughtShown;

        private struct WetMaterial
        {
            public Material material;
            public float drySmoothness;
            public bool hasColour;
            public Color dryColour;
        }
        private readonly List<WetMaterial> wetMaterials = new List<WetMaterial>();

        private void Awake() => SetThought(false);

        private void Update()
        {
            bool photoDone = afterPhoto != null && afterPhoto.Completed && !afterPhoto.IsRunning;
            if (!Started)
            {
                if (CarStartsTurning() || photoDone || (ride != null && ride.HasArrived)) StartRain();   // (testing shortcuts skip the drive)
                else return;
            }
            if (photoDone && heavyStart < 0f) { heavyStart = Time.time; thoughtAt = Time.time + thoughtDelay; }

            float elapsed = Time.time - startTime;
            float build = Mathf.Lerp(startAmount, beforePhotoAmount, Smooth(elapsed / buildUpSeconds)) * Smooth(elapsed / 4f);
            float heavy = heavyStart < 0f ? 0f : Smooth((Time.time - heavyStart) / heavyRampSeconds);
            Intensity = Mathf.Lerp(build, 1f, heavy);
            if (drops != null) { var emission = drops.emission; emission.rateOverTime = heaviestDropsPerSecond * Intensity; }
            if (sound != null)
            {
                float fadeOut = soundFadeStart < 0f ? 1f : 1f - Smooth((Time.time - soundFadeStart) / soundFadeSeconds);
                sound.volume = volume * Mathf.Lerp(.25f, 1f, Intensity) * Smooth(elapsed / 3f) * fadeOut;
                if (fadeOut <= 0f && sound.isPlaying) sound.Stop();
            }
            float fade = Smooth(elapsed / fadeSeconds);
            if (fade > lastFade + .002f) { lastFade = fade; ApplyFade(fade); }
            float newWetness = Smooth(elapsed / wetSeconds);
            if (newWetness > wetness + .002f || (newWetness >= 1f && wetness < 1f)) { wetness = newWetness; ApplyWetness(); }
            if (!thoughtShown && thoughtAt > 0f && Time.time >= thoughtAt) { thoughtShown = true; StartCoroutine(Think()); }
        }

        // The car has started its first turn: its heading has moved away from the heading it started the drive with.
        private bool CarStartsTurning()
        {
            if (ride == null || !ride.IsMoving) return false;
            Vector3 heading = ride.transform.forward;
            heading.y = 0f;
            if (heading.sqrMagnitude < .0001f) return false;
            if (!headingKnown) { headingKnown = true; startHeading = heading.normalized; return false; }
            return Vector3.Angle(startHeading, heading.normalized) >= turnDegrees;
        }

        private void LateUpdate()
        {
            if (drops == null) return;
            var view = Camera.main;
            if (view == null) return;
            LandDrops(view.transform.position);
            // the rain area follows Khem (not his view rotation), a little ahead of where he looks
            Vector3 ahead = view.transform.forward;
            ahead.y = 0f;
            if (ahead.sqrMagnitude > .0001f) ahead = ahead.normalized * (area.y * .2f);
            drops.transform.position = view.transform.position + Vector3.up * startHeight + ahead;
            drops.transform.rotation = Quaternion.identity;
        }

        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>The rain sound fades out over this many seconds and stops (the ending, once Khem gets into the car).</summary>
        public void FadeOutSound(float seconds)
        {
            if (soundFadeStart >= 0f) return;
            soundFadeStart = Time.time;
            soundFadeSeconds = Mathf.Max(.01f, seconds);
        }

        /// <summary>The rain goes to the heaviest from now on (normally after the skid mark photo).</summary>
        [ContextMenu("Make Rain Heavy")]
        public void MakeHeavy()
        {
            if (!Started) StartRain();
            if (heavyStart < 0f) heavyStart = Time.time;
        }

        [ContextMenu("Start Rain Now")]
        public void StartRain()
        {
            if (Started) return;
            Started = true;
            startTime = Time.time;
            BuildRain();
            CollectLandingSurfaces();
            CollectWetMaterials();
            CollectFadingMarks();
            if (rainLoop != null)
            {
                sound = gameObject.AddComponent<AudioSource>();
                sound.clip = rainLoop;
                sound.loop = true;
                sound.spatialBlend = 0f;
                sound.playOnAwake = false;
                sound.volume = 0f;
                sound.Play();
            }
        }

        private IEnumerator Think()
        {
            if (thought == null) yield break;
            SetThought(true);
            yield return new WaitForSeconds(thoughtSeconds);
            SetThought(false);
        }

        private void SetThought(bool show)
        {
            if (thought == null) return;
            var box = thought.transform.parent != null ? thought.transform.parent.gameObject : thought.gameObject;
            if (!thought.gameObject.activeSelf) thought.gameObject.SetActive(true);
            if (box.activeSelf != show) box.SetActive(show);
        }

        // ---- particles ------------------------------------------------------------------------------------------------------------------
        private void BuildRain()
        {
            drops = NewSystem("RainDrops", transform);
            var main = drops.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = (startHeight + 8f) / Mathf.Max(1f, fallSpeed.x);   // long enough to reach the ground in a dip
            main.startSpeed = 0f;
            main.startSize = dropWidth;
            main.startColor = dropColour;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(heaviestDropsPerSecond * main.startLifetime.constant * 1.2f) + 100;
            var emission = drops.emission;
            emission.rateOverTime = 0f;
            var shape = drops.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, .1f, area.y);
            var velocity = drops.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(wind.x * .6f, wind.x * 1.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed.y, -fallSpeed.x);
            velocity.z = new ParticleSystem.MinMaxCurve(wind.y * .6f, wind.y * 1.2f);
            // no physics collision: LandDrops() stops the drops on the Terrain and the car only
            var renderer = drops.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1f;
            renderer.velocityScale = streakLength;
            renderer.cameraVelocityScale = 0f;
            Prepare(renderer, dropMaterial);

            // a flat ring that grows and fades where a drop lands (emitted by LandDrops)
            ripples = NewSystem("RainRipples", transform);
            main = ripples.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .55f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(rippleSize * .7f, rippleSize * 1.2f);
            main.startColor = rippleColour;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 3000;
            emission = ripples.emission;
            emission.rateOverTime = 0f;
            shape = ripples.shape;
            shape.enabled = false;
            var size = ripples.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, .15f, 1f, 1f));
            var colour = ripples.colorOverLifetime;
            colour.enabled = true;
            colour.color = new ParticleSystem.MinMaxGradient(Fade());
            renderer = ripples.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            Prepare(renderer, rippleMaterial);

            // a few tiny droplets that bounce up
            splashes = NewSystem("RainSplashes", transform);
            main = splashes.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.7f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.008f, .016f);
            main.startColor = rippleColour;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            emission = splashes.emission;
            emission.rateOverTime = 0f;
            shape = splashes.shape;
            shape.enabled = false;
            colour = splashes.colorOverLifetime;
            colour.enabled = true;
            colour.color = new ParticleSystem.MinMaxGradient(Fade());
            renderer = splashes.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.5f;
            renderer.velocityScale = .02f;
            Prepare(renderer, splashMaterial);

            LateUpdate();
            drops.Play(false);
            ripples.Play(false);    // nothing is emitted on its own; LandDrops() emits where drops land
            splashes.Play(false);
        }

        // ---- where drops land: only the Terrain and the car ---------------------------------------------------------------------------
        private void CollectLandingSurfaces()
        {
            terrains = Terrain.activeTerrains;
            if (car == null && ride != null) car = ride.transform;
            carHasBounds = false;
            if (car == null) return;
            var list = new List<Collider>();
            foreach (var collider in car.GetComponentsInChildren<Collider>(true))
                if (collider != null && !collider.isTrigger) list.Add(collider);
            carColliders = list.ToArray();
            // the car's own box (in its local space), used when it has no colliders and to skip far drops quickly
            foreach (var renderer in car.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                Bounds world = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? world.min.x : world.max.x, (i & 2) == 0 ? world.min.y : world.max.y, (i & 4) == 0 ? world.min.z : world.max.z);
                    var local = car.InverseTransformPoint(corner);
                    if (!carHasBounds) { carLocalBounds = new Bounds(local, Vector3.zero); carHasBounds = true; }
                    else carLocalBounds.Encapsulate(local);
                }
            }
        }

        private bool TerrainHeight(Vector3 position, out float height)
        {
            height = 0f;
            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                Vector3 origin = terrain.GetPosition();
                Vector3 size = terrain.terrainData.size;
                if (position.x < origin.x || position.z < origin.z || position.x > origin.x + size.x || position.z > origin.z + size.z) continue;
                height = origin.y + terrain.SampleHeight(position);
                return true;
            }
            return false;
        }

        // Did a drop that moved down by 'fall' this frame hit the car? Returns the height it landed at.
        private bool CarHit(Vector3 position, float fall, out float height)
        {
            height = 0f;
            if (!carHasBounds || car == null) return false;
            Vector3 local = car.InverseTransformPoint(position);
            if (local.x < carLocalBounds.min.x || local.x > carLocalBounds.max.x || local.z < carLocalBounds.min.z || local.z > carLocalBounds.max.z) return false;
            if (local.y > carLocalBounds.max.y + .05f) return false;
            if (carColliders.Length > 0)
            {
                var ray = new Ray(position + Vector3.up * (fall + .05f), Vector3.down);
                float best = float.MaxValue;
                foreach (var collider in carColliders)
                    if (collider != null && collider.enabled && collider.Raycast(ray, out RaycastHit hit, fall + .1f) && hit.distance < best)
                    { best = hit.distance; height = hit.point.y; }
                if (best < float.MaxValue) return true;
                // inside the car's box but no surface between the last frame and now: inside the cabin? stop it there
                return local.y < carLocalBounds.max.y - .05f && local.y > carLocalBounds.min.y;
            }
            height = car.TransformPoint(new Vector3(local.x, carLocalBounds.max.y, local.z)).y;
            return true;
        }

        private void LandDrops(Vector3 eye)
        {
            int count = drops.particleCount;
            if (count == 0) return;
            if (buffer.Length < count) buffer = new ParticleSystem.Particle[Mathf.Max(count, drops.main.maxParticles)];
            count = drops.GetParticles(buffer, count);
            float checkAbove = eye.y + 2.5f;            // only drops near the ground need checking
            float rippleSqr = rippleRadius * rippleRadius;
            float dt = Mathf.Max(Time.deltaTime, .001f);
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                var particle = buffer[i];
                Vector3 position = particle.position;
                if (position.y > checkAbove) continue;
                float fall = Mathf.Max(.05f, -particle.velocity.y * dt * 1.5f);
                bool landed = false, onGround = false;
                float height;
                if (CarHit(position, fall, out height)) landed = true;
                else if (TerrainHeight(position, out height) && position.y <= height) { landed = true; onGround = true; }
                if (!landed) continue;
                particle.remainingLifetime = 0f;
                buffer[i] = particle;
                changed = true;
                Vector3 at = new Vector3(position.x, height + .015f, position.z);
                float dx = at.x - eye.x, dz = at.z - eye.z;
                if (dx * dx + dz * dz > rippleSqr) continue;
                if (onGround) ripples.Emit(new ParticleSystem.EmitParams { position = at, applyShapeToPosition = false }, 1);
                for (int s = 0; s < splashDroplets; s++)
                {
                    var up = new Vector3(Random.Range(-.45f, .45f), 1f, Random.Range(-.45f, .45f)).normalized * Random.Range(.7f, 1.4f);
                    splashes.Emit(new ParticleSystem.EmitParams { position = at, velocity = up, applyShapeToPosition = false }, 1);
                }
            }
            if (changed) drops.SetParticles(buffer, count);
        }

        private static Gradient Fade()
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(.6f, .4f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static ParticleSystem NewSystem(string name, Transform parent)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var system = holder.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            return system;
        }

        private static void Prepare(ParticleSystemRenderer renderer, Material material)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = 0f;
            if (material != null) renderer.sharedMaterial = material;
        }

        // ---- wet ground and props --------------------------------------------------------------------------------------------------------
        private void CollectWetMaterials()
        {
            wetMaterials.Clear();
            Vector3 centre = wetCentre != null ? wetCentre.position
                           : afterPhoto != null && afterPhoto.skidMark != null ? afterPhoto.skidMark.position : transform.position;
            Transform playerRoot = afterPhoto != null && afterPhoto.player != null ? afterPhoto.player.transform.root : null;
            Scene scene = gameObject.scene;
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled || renderer.gameObject.scene != scene) continue;
                if (playerRoot != null && renderer.transform.IsChildOf(playerRoot)) continue;
                if (renderer.gameObject.layer == 5 || renderer.name.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                Vector3 offset = renderer.bounds.ClosestPoint(centre) - centre;
                offset.y = 0f;
                if (offset.sqrMagnitude > wetRadius * wetRadius) continue;
                bool any = false;
                foreach (var shared in renderer.sharedMaterials)
                    if (Wettable(shared)) { any = true; break; }
                if (!any) continue;
                foreach (var material in renderer.materials)   // copies for this renderer only; the material assets are never changed
                {
                    if (!Wettable(material)) continue;
                    var entry = new WetMaterial { material = material, drySmoothness = material.GetFloat("_Smoothness") };
                    if (material.HasProperty("_BaseColor")) { entry.hasColour = true; entry.dryColour = material.GetColor("_BaseColor"); }
                    wetMaterials.Add(entry);
                }
            }
        }

        private void CollectFadingMarks()
        {
            fadingMaterials.Clear();
            fadingStart.Clear();
            if (fadingMarks == null) return;
            foreach (var renderer in fadingMarks)
            {
                if (renderer == null) continue;
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_Opacity")) { fadingMaterials.Add(material); fadingStart.Add(material.GetFloat("_Opacity")); }
                    else if (material.HasProperty("_BaseColor")) { fadingMaterials.Add(material); fadingStart.Add(material.GetColor("_BaseColor").a); }
                }
            }
        }

        private void ApplyFade(float fade)
        {
            for (int i = 0; i < fadingMaterials.Count; i++)
            {
                var material = fadingMaterials[i];
                if (material == null) continue;
                float value = Mathf.Lerp(fadingStart[i], fadingStart[i] * fadedOpacity, fade);
                if (material.HasProperty("_Opacity")) material.SetFloat("_Opacity", value);
                else { var colour = material.GetColor("_BaseColor"); colour.a = value; material.SetColor("_BaseColor", colour); }
            }
        }

        private static bool Wettable(Material material)
            => material != null && material.HasProperty("_Smoothness") && material.renderQueue < 2900;   // opaque / cut-out only

        private void ApplyWetness()
        {
            foreach (var entry in wetMaterials)
            {
                if (entry.material == null) continue;
                entry.material.SetFloat("_Smoothness", Mathf.Lerp(entry.drySmoothness, Mathf.Max(entry.drySmoothness, wetSmoothness), wetness));
                if (entry.hasColour)
                {
                    Color wet = entry.dryColour * wetDarken;
                    wet.a = entry.dryColour.a;
                    entry.material.SetColor("_BaseColor", Color.Lerp(entry.dryColour, wet, wetness));
                }
            }
        }

        private void OnDisable() => SetThought(false);

        private void OnDestroy()
        {
            foreach (var entry in wetMaterials) if (entry.material != null) Destroy(entry.material);
            wetMaterials.Clear();
            foreach (var material in fadingMaterials) if (material != null) Destroy(material);
            fadingMaterials.Clear();
        }
    }
}
