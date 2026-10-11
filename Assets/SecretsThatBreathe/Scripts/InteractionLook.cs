using UnityEngine;

namespace SecretsThatBreathe
{
    public static class InteractionLook
    {
        public static bool IsLookingAt(Camera view, Renderer[] renderers, float angle, float maxDistance)
        {
            if (view == null || renderers == null) return false;
            Bounds bounds = default;
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return false;
            Vector3 toTarget = bounds.center - view.transform.position;
            float distance = toTarget.magnitude;
            if (distance < 0.001f || distance > Mathf.Max(0.1f, maxDistance)) return false;
            float cosine = Mathf.Cos(Mathf.Clamp(angle, 1f, 80f) * Mathf.Deg2Rad);
            if (Vector3.Dot(view.transform.forward, toTarget / distance) < cosine) return false;
            Vector3 viewport = view.WorldToViewportPoint(bounds.center);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }
    }
}
