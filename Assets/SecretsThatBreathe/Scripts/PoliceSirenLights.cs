using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class PoliceSirenLights : MonoBehaviour
    {
        public enum FlashPattern { RotatingBeacon, AlternatingDoubleFlash }

        [Header("Siren")]
        [SerializeField] private bool sirenOn = true;
        [SerializeField] private FlashPattern pattern = FlashPattern.RotatingBeacon;
        [Min(0.1f)] [SerializeField] private float cyclesPerSecond = 1.6f;
        [ColorUsage(false, true)] [SerializeField] private Color emissionColor = new Color(7f, 0.025f, 0.008f);
        [Range(0f, 0.3f)] [SerializeField] private float dimEmission = 0.025f;

        [Header("Separate lens material slots")]
        [SerializeField] private Renderer lensRenderer;
        [SerializeField] private int leftMaterialIndex;
        [SerializeField] private int rightMaterialIndex = 1;

        [Header("Real red lights")]
        [SerializeField] private Light leftLight;
        [SerializeField] private Light rightLight;
        [Min(0f)] [SerializeField] private float lightIntensity = 32f;
        [Min(0.1f)] [SerializeField] private float lightRange = 11f;
        [Range(1f, 179f)] [SerializeField] private float spotAngle = 100f;
        [Range(0f, 60f)] [SerializeField] private float downwardTilt = 18f;
        [SerializeField] private Color lightColor = new Color(1f, 0.015f, 0.005f);

        private static int emissionId;
        private MaterialPropertyBlock leftBlock;
        private MaterialPropertyBlock rightBlock;
        private float startedAt;
        private bool appliedOff;
        private int materialCount;

        public bool SirenOn
        {
            get => sirenOn;
            set { sirenOn = value; appliedOff = false; }
        }

        public void SetSirenEnabled(bool enabled) => SirenOn = enabled;

        private void OnEnable()
        {
            // Native Unity resources cannot be created during prefab deserialization
            // or MonoBehaviour field initialization. Recreate them after reload too.
            if (leftBlock == null) leftBlock = new MaterialPropertyBlock();
            if (rightBlock == null) rightBlock = new MaterialPropertyBlock();
            emissionId = Shader.PropertyToID("_EmissionColor");
            startedAt = Time.time;
            materialCount = lensRenderer != null ? lensRenderer.sharedMaterials.Length : 0;
            appliedOff = false;
            ConfigureLight(leftLight);
            ConfigureLight(rightLight);
            ApplyFrame(0f);
        }

        private void Update()
        {
            if (!sirenOn && appliedOff) return;
            ApplyFrame((Time.time - startedAt) * Mathf.Max(0.1f, cyclesPerSecond));
        }

        // Public pure timing calculation also permits deterministic verification of both banks.
        public static float EvaluatePulse(FlashPattern flashPattern, float cycle, bool rightBank)
        {
            float phase = Mathf.Repeat(cycle + (rightBank ? 0.5f : 0f), 1f);
            if (flashPattern == FlashPattern.AlternatingDoubleFlash)
                return phase < 0.105f || (phase >= 0.19f && phase < 0.295f) ? 1f : 0f;
            float facing = Mathf.Max(0f, Mathf.Cos(phase * Mathf.PI * 2f));
            return facing * facing * facing * facing;
        }

        private void ApplyFrame(float cycle)
        {
            float left = sirenOn ? EvaluatePulse(pattern, cycle, false) : 0f;
            float right = sirenOn ? EvaluatePulse(pattern, cycle, true) : 0f;
            SetEmission(leftMaterialIndex, leftBlock, sirenOn ? Mathf.Lerp(dimEmission, 1f, left) : 0f);
            SetEmission(rightMaterialIndex, rightBlock, sirenOn ? Mathf.Lerp(dimEmission, 1f, right) : 0f);
            ApplyLight(leftLight, cycle, left, false);
            ApplyLight(rightLight, cycle, right, true);
            appliedOff = !sirenOn;
        }

        private void SetEmission(int materialIndex, MaterialPropertyBlock block, float brightness)
        {
            if (block == null || lensRenderer == null || materialIndex < 0 || materialIndex >= materialCount) return;
            lensRenderer.GetPropertyBlock(block, materialIndex);
            block.SetColor(emissionId, emissionColor * brightness);
            lensRenderer.SetPropertyBlock(block, materialIndex);
        }

        private void ConfigureLight(Light lamp)
        {
            if (lamp == null) return;
            lamp.type = LightType.Spot;
            lamp.color = lightColor;
            lamp.range = lightRange;
            lamp.spotAngle = spotAngle;
            lamp.innerSpotAngle = spotAngle * 0.55f;
            lamp.shadows = LightShadows.Soft;
            lamp.shadowBias = 0.025f;
            lamp.shadowNormalBias = 0.08f;
            lamp.bounceIntensity = 0f;
        }

        private void ApplyLight(Light lamp, float cycle, float pulse, bool rightBank)
        {
            if (lamp == null) return;
            bool rotating = pattern == FlashPattern.RotatingBeacon;
            float yaw = rotating ? Mathf.Repeat(cycle, 1f) * 360f + (rightBank ? 180f : 0f)
                                 : (rightBank ? 180f : 0f);
            lamp.transform.localRotation = Quaternion.Euler(downwardTilt, yaw, 0f);
            // A rotating real reflector keeps casting its beam as it sweeps away from the viewer.
            lamp.intensity = sirenOn ? lightIntensity * (rotating ? 1f : pulse) : 0f;
            lamp.enabled = sirenOn && (rotating || pulse > 0f);
        }

        private void OnDisable()
        {
            SetEmission(leftMaterialIndex, leftBlock, 0f);
            SetEmission(rightMaterialIndex, rightBlock, 0f);
            if (leftLight != null) leftLight.enabled = false;
            if (rightLight != null) rightLight.enabled = false;
        }
    }
}
