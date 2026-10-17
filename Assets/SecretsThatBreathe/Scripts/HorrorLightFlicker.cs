using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class HorrorLightFlicker : MonoBehaviour
    {
        [Header("Lights")]
        public Light[] targetLights;
        [Header("Time between flicker bursts (seconds)")]
        public Vector2 quietInterval = new Vector2(7f, 14f);
        [Header("Brief interruptions per burst")]
        public Vector2Int interruptions = new Vector2Int(1, 3);
        public Vector2 darkDuration = new Vector2(0.06f, 0.16f);
        public Vector2 litDuration = new Vector2(0.12f, 0.3f);
        [Range(0f, 1f)] public float dimBrightness = 0.08f;
        [Tooltip("Make the glowing tubes dim together with the light.")]
        public bool flickerEmission = true;
        [Tooltip("Use this material on the glowing tubes during each interruption. Leave empty to only dim emission.")]
        public Material offMaterial;
        [Header("Flicker sound")]
        [Tooltip("Assign the clip and adjust volume, pitch and audible distance on the FlickerAudio child. Plays once per flicker burst.")]
        public AudioSource flickerAudioSource;
        [Min(0f)] public float startupSoundDelay = 2f;

        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private float[] intensities;
        private bool[] enabledStates;
        private Coroutine routine;
        private readonly List<EmissionSlot> slots = new List<EmissionSlot>();
        private sealed class EmissionSlot
        {
            public Renderer renderer;
            public int index;
            public Vector4 emission;
            public Material litMaterial;
            public MaterialPropertyBlock original, working;
        }

        private void OnEnable()
        {
            if (targetLights == null || targetLights.Length == 0)
                targetLights = GetComponentsInChildren<Light>(true);
            intensities = new float[targetLights.Length];
            enabledStates = new bool[targetLights.Length];
            for (int i = 0; i < targetLights.Length; i++)
                if (targetLights[i] != null)
                {
                    intensities[i] = targetLights[i].intensity;
                    enabledStates[i] = targetLights[i].enabled;
                }
            slots.Clear();
            if (flickerEmission)
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material == null || !material.HasProperty(Emission) || !material.IsKeywordEnabled("_EMISSION")) continue;
                        var original = new MaterialPropertyBlock();
                        var working = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(original, i);
                        renderer.GetPropertyBlock(working, i);
                        // HDR emission is already in shader space. SetColor would convert it again.
                        var emission = original.HasProperty(Emission) ? original.GetVector(Emission) : material.GetVector(Emission);
                        if (Mathf.Max(emission.x, Mathf.Max(emission.y, emission.z)) <= 0f) continue;
                        slots.Add(new EmissionSlot { renderer = renderer, index = i, emission = emission, litMaterial = material, original = original, working = working });
                    }
                }
            StartCoroutine(PlayStartupSound());
            routine = StartCoroutine(Flicker());
        }

        private IEnumerator PlayStartupSound()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, startupSoundDelay));
            PlayFlickerSound();
        }

        private IEnumerator Flicker()
        {
            while (true)
            {
                yield return new WaitForSeconds(Sample(quietInterval));
                int minimum = Mathf.Clamp(interruptions.x, 1, 10);
                int count = Random.Range(minimum, Mathf.Clamp(interruptions.y, minimum, 10) + 1);
                PlayFlickerSound();
                for (int i = 0; i < count; i++)
                {
                    SetBrightness(dimBrightness);
                    yield return new WaitForSeconds(Sample(darkDuration));
                    SetBrightness(1f);
                    if (i + 1 < count) yield return new WaitForSeconds(Sample(litDuration));
                }
            }
        }

        private void PlayFlickerSound()
        {
            if (flickerAudioSource != null && flickerAudioSource.clip != null)
                flickerAudioSource.PlayOneShot(flickerAudioSource.clip);
        }

        private static float Sample(Vector2 range)
        {
            float minimum = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
            return Random.Range(minimum, Mathf.Max(minimum, Mathf.Max(range.x, range.y)));
        }

        private void SetBrightness(float amount)
        {
            for (int i = 0; i < targetLights.Length; i++)
                if (targetLights[i] != null) targetLights[i].intensity = intensities[i] * Mathf.Clamp01(amount);
            foreach (var slot in slots)
                if (slot.renderer != null)
                {
                    // Restore the exact original overrides when lit, including a missing override.
                    if (amount >= 1f)
                    {
                        RestoreMaterial(slot);
                        slot.renderer.SetPropertyBlock(slot.original.isEmpty ? null : slot.original, slot.index);
                        continue;
                    }
                    if (offMaterial != null)
                    {
                        var materials = slot.renderer.sharedMaterials;
                        materials[slot.index] = offMaterial;
                        slot.renderer.sharedMaterials = materials;
                        // Clear tube overrides so the off material is rendered exactly as authored.
                        slot.renderer.SetPropertyBlock(null, slot.index);
                        continue;
                    }
                    var emission = slot.emission;
                    float brightness = Mathf.Clamp01(amount);
                    emission.x *= brightness;
                    emission.y *= brightness;
                    emission.z *= brightness;
                    slot.working.SetVector(Emission, emission);
                    slot.renderer.SetPropertyBlock(slot.working, slot.index);
                }
        }

        private static void RestoreMaterial(EmissionSlot slot)
        {
            var materials = slot.renderer.sharedMaterials;
            if (slot.index >= materials.Length || materials[slot.index] == slot.litMaterial) return;
            materials[slot.index] = slot.litMaterial;
            slot.renderer.sharedMaterials = materials;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (flickerAudioSource != null) flickerAudioSource.Stop();
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (intensities != null)
                for (int i = 0; i < intensities.Length; i++)
                    if (targetLights[i] != null)
                    {
                        targetLights[i].intensity = intensities[i];
                        targetLights[i].enabled = enabledStates[i];
                    }
            foreach (var slot in slots)
                if (slot.renderer != null)
                {
                    RestoreMaterial(slot);
                    slot.renderer.SetPropertyBlock(slot.original.isEmpty ? null : slot.original, slot.index);
                }
            slots.Clear();
        }
    }
}
