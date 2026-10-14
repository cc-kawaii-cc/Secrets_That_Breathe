using StarterAssets;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(15000)]
    public sealed class PhoneFlashlightView : MonoBehaviour, IPhoneFlashlightPresentation
    {
        public bool CanToggle => !OfficeIntroDialogue.IsFlashlightBlocking(gameObject.scene);
        public GameObject phonePrefab;
        public Camera viewCamera;
        public Material onMaterial;
        public Material offMaterial;
        [Tooltip("Viewport position: (0.5,0.5) is screen centre. Default places the phone at the lower right.")]
        public Vector2 screenPosition = new Vector2(0.78f, 0.27f);
        [Min(0.1f)] public float cameraDistance = 0.32f;
        public Vector3 localRotation = new Vector3(0f, 180f, -8f);
        [Min(0.01f)] public float phoneScale = 1f;
        private GameObject heldPhone;
        private Renderer screen;

        private void Start()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            EnsurePhone();
            HidePhone();
            var flashlight = GetComponent<FirstPersonFlashlight>();
            if (flashlight != null) flashlight.ConfigurePhone(this);
        }

        private bool EnsurePhone()
        {
            if (heldPhone != null) return true;
            if (phonePrefab == null || viewCamera == null) return false;
            heldPhone = Instantiate(phonePrefab, viewCamera.transform);
            heldPhone.name = "HeldPhone";
            foreach (var script in heldPhone.GetComponentsInChildren<SecretsThatBreathe.Phone.PhoneFlashlightScreen>(true)) script.enabled = false;
            foreach (var collider in heldPhone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var light in heldPhone.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var renderer in heldPhone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (renderer.name == "Screen") screen = renderer;
            }
            ApplyPose();
            heldPhone.SetActive(false);
            return true;
        }

        private void ApplyPose()
        {
            if (heldPhone == null || viewCamera == null) return;
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
        private void OnDestroy() { if (heldPhone != null) Destroy(heldPhone); }
    }
}
