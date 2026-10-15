using UnityEngine;

namespace SecretsThatBreathe
{
    public static class InteractionLook
    {
        public static bool IsLookingAtBox(Camera view, BoxCollider target)
        {
            if (view == null || target == null || !target.enabled || !target.gameObject.activeInHierarchy) return false;
            Ray ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            var localRay = new Ray(target.transform.InverseTransformPoint(ray.origin), target.transform.InverseTransformVector(ray.direction));
            var box = new Bounds(target.center, target.size);
            return box.Contains(localRay.origin) || box.IntersectRay(localRay, out _);
        }

        public static bool IsPointInBox(Vector3 position, BoxCollider zone)
        {
            return zone != null && zone.enabled && zone.gameObject.activeInHierarchy
                && new Bounds(zone.center, zone.size).Contains(zone.transform.InverseTransformPoint(position));
        }

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
