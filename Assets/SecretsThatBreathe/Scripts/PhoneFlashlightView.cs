using StarterAssets;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(15000)]
    public sealed class PhoneFlashlightView : MonoBehaviour, IPhoneFlashlightPresentation
    {
        public bool CanToggle => !OfficeIntroDialogue.IsFlashlightBlocking(gameObject.scene)
            && !PhoneCallInteraction.IsBusy(gameObject.scene) && !ItemInspectionInteraction.IsBusy(gameObject.scene);
        public void OnFlashlightActivationRequested() => OfficeIntroDialogue.NotifyFlashlightActivationRequested(gameObject.scene);
        public GameObject phonePrefab;
        public Camera viewCamera;
        public Material onMaterial;
        public Material offMaterial;
        [Header("Editable phone instance in the scene")]
        public GameObject heldPhone;
        [Tooltip("Use HeldPhone's saved Transform. Disable to position it using the viewport settings below.")]
        public bool useScenePhoneTransform = true;
        [Tooltip("Viewport position: (0.5,0.5) is screen centre. Default places the phone at the lower right.")]
        public Vector2 screenPosition = new Vector2(0.78f, 0.27f);
        [Min(0.1f)] public float cameraDistance = 0.32f;
        public Vector3 localRotation = new Vector3(0f, 180f, -8f);
        [Min(0.01f)] public float phoneScale = 1f;
        private bool phoneInitialized;
        private bool ownsRuntimePhone;
        private Renderer screen;
        private Light phoneLight;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            EnsurePhone();
            HidePhone();
            var flashlight = GetComponent<FirstPersonFlashlight>();
            if (flashlight != null)
            {
                flashlight.ConfigurePhone(this);
                if (phoneLight != null) flashlight.ConfigureLightSource(phoneLight);
            }
        }

        private bool EnsurePhone()
        {
            if (phoneInitialized && heldPhone != null) return true;
            if (viewCamera == null) return false;
            if (heldPhone == null)
            {
                if (phonePrefab == null) return false;
                heldPhone = Instantiate(phonePrefab, viewCamera.transform);
                heldPhone.name = "HeldPhone";
                ownsRuntimePhone = true;
            }
            foreach (var script in heldPhone.GetComponentsInChildren<SecretsThatBreathe.Phone.PhoneFlashlightScreen>(true)) script.enabled = false;
            foreach (var collider in heldPhone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var light in heldPhone.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var anchor in heldPhone.GetComponentsInChildren<Transform>(true))
                if (anchor.name == "FlashlightAnchor")
                {
                    phoneLight = anchor.GetComponent<Light>();
                    if (phoneLight == null) phoneLight = anchor.GetComponentInChildren<Light>(true);
                    break;
                }
            if (phoneLight == null)
                Debug.LogWarning("[Phone flashlight] FlashlightAnchor needs a Light component in the phone prefab.", this);
            foreach (var renderer in heldPhone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (renderer.name == "Screen") screen = renderer;
            }
            ApplyPose();
            heldPhone.SetActive(false);
            phoneInitialized = true;
            return true;
        }

        private void ApplyPose()
        {
            if (heldPhone == null || viewCamera == null) return;
            if (!ownsRuntimePhone && useScenePhoneTransform) return;
            float distance = Mathf.Max(cameraDistance, viewCamera.nearClipPlane + 0.025f);
            heldPhone.transform.SetPositionAndRotation(viewCamera.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance)),
                viewCamera.transform.rotation * Quaternion.Euler(localRotation));
            heldPhone.transform.localScale = Vector3.one * phoneScale;
        }

        private void Show(Material material)
        {
            if (!EnsurePhone()) return;
            heldPhone.SetActive(true);
            ApplyPose();
            if (screen != null && material != null) screen.sharedMaterial = material;
        }
        public void ShowOn() { Show(onMaterial); }
        public void ShowOff() { Show(offMaterial); }
        public void HidePhone()
        {
            if (heldPhone != null) heldPhone.SetActive(false);
        }
        private void LateUpdate() { if (heldPhone != null && heldPhone.activeSelf) ApplyPose(); }
        private void OnDisable() { HidePhone(); }
        private void OnDestroy() { if (ownsRuntimePhone && heldPhone != null) Destroy(heldPhone); }
    }
}
