using System.Collections.Generic;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Distance-sampled rounded corners keep vehicle speed independent of curve parameter t.
    public sealed class PassengerRidePath
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> tangents = new List<Vector3>();
        private readonly List<float> distances = new List<float>();
        public float Length => distances.Count == 0 ? 0f : distances[distances.Count - 1];
        public int SampleCount => positions.Count;
        public Vector3 GetSample(int index) => positions[index];

        public PassengerRidePath(Vector3 start, Vector3[] corners, Vector3 end, float radius)
        {
            var points = new List<Vector3> { start };
            if (corners != null)
                foreach (var point in corners)
                    if ((point - points[points.Count - 1]).sqrMagnitude > .000001f) points.Add(point);
            if ((end - points[points.Count - 1]).sqrMagnitude > .000001f) points.Add(end);
            if (points.Count == 1) { Add(start, Vector3.forward); return; }
            Add(start, (points[1] - start).normalized);
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 incoming = points[i] - points[i - 1];
                Vector3 outgoing = points[i + 1] - points[i];
                float trim = Mathf.Min(Mathf.Max(0f, radius), incoming.magnitude * .45f, outgoing.magnitude * .45f);
                Vector3 inDirection = incoming.normalized, outDirection = outgoing.normalized;
                if (trim < .001f || Vector3.Dot(inDirection, outDirection) > .9999f)
                {
                    Add(points[i], inDirection);
                    continue;
                }
                Vector3 entry = points[i] - inDirection * trim;
                Vector3 exit = points[i] + outDirection * trim;
                Add(entry, inDirection);
                int steps = Mathf.Clamp(Mathf.CeilToInt(trim * 2f / .15f), 12, 256);
                for (int j = 1; j <= steps; j++)
                {
                    float t = (float)j / steps, remaining = 1f - t;
                    Vector3 position = remaining * remaining * entry + 2f * remaining * t * points[i] + t * t * exit;
                    Vector3 direction = (remaining * (points[i] - entry) + t * (exit - points[i])).normalized;
                    Add(position, direction);
                }
            }
            Add(end, (points[points.Count - 1] - points[points.Count - 2]).normalized);
        }

        private void Add(Vector3 position, Vector3 tangent)
        {
            float step = positions.Count == 0 ? 0f : Vector3.Distance(positions[positions.Count - 1], position);
            if (positions.Count > 0 && step < .00001f) return;
            float total = Length + step;
            positions.Add(position);
            tangents.Add(tangent);
            distances.Add(total);
        }

        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            int index = distances.BinarySearch(distance);
            if (index >= 0) { position = positions[index]; forward = tangents[index]; return; }
            int next = ~index;
            if (next <= 0) { position = positions[0]; forward = tangents[0]; return; }
            if (next >= positions.Count) next = positions.Count - 1;
            int previous = next - 1;
            float t = (distance - distances[previous]) / (distances[next] - distances[previous]);
            position = Vector3.Lerp(positions[previous], positions[next], t);
            forward = Vector3.Lerp(tangents[previous], tangents[next], t).normalized;
        }
    }
}
