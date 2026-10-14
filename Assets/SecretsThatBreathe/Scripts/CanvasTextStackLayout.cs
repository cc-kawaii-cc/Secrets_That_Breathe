using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(20000)]
    public sealed class CanvasTextStackLayout : MonoBehaviour
    {
        [Tooltip("Space between overlapping sentences, in Canvas units.")]
        [Min(0f)] public float spacing = 12f;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Entry> visible = new List<Entry>();
        private readonly List<Rect> placed = new List<Rect>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform canvasRect;
        private int sequence;
        private sealed class Entry
        {
            public TMP_Text label;
            public float offset;
            public bool active;
            public int order;
            public bool positioned;
        }

        private void Start()
        {
            canvasRect = transform as RectTransform;
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                // Choice buttons already have their own layout; don't reposition their labels.
                if (label.GetComponentInParent<Button>(true) != null) continue;
                if (label.transform.parent.name.StartsWith("CallRow")) continue;
                entries.Add(new Entry { label = label });
            }
        }

        private void LateUpdate()
        {
            if (canvasRect == null) return;
            visible.Clear();
            placed.Clear();
            foreach (var entry in entries)
            {
                if (entry.label == null) continue;
                bool active = entry.label.isActiveAndEnabled && !string.IsNullOrWhiteSpace(entry.label.text);
                if (active && !entry.active)
                {
                    entry.order = ++sequence;
                    entry.positioned = false;
                }
                entry.active = active;
                if (!active)
                {
                    SetOffset(entry, 0f);
                    entry.positioned = false;
                    continue;
                }
                visible.Add(entry);
            }
            visible.Sort((a, b) => a.order.CompareTo(b.order));
            // Reserve all existing positions first. Keep them until their text disappears.
            foreach (var entry in visible)
                if (entry.positioned)
                {
                    Rect current = Measure(entry);
                    current.y += entry.offset;
                    placed.Add(current);
                }
            foreach (var entry in visible)
            {
                if (entry.positioned) continue;
                Rect original = Measure(entry);
                Rect candidate = original;
                // Moving above one sentence can collide with another, so resolve until clear.
                for (int pass = 0; pass <= placed.Count; pass++)
                {
                    bool moved = false;
                    foreach (var other in placed)
                    {
                        if (candidate.xMax <= other.xMin || candidate.xMin >= other.xMax) continue;
                        if (candidate.yMax + spacing <= other.yMin || candidate.yMin >= other.yMax + spacing) continue;
                        candidate.y = other.yMax + spacing;
                        moved = true;
                    }
                    if (!moved) break;
                }
                SetOffset(entry, candidate.y - original.y);
                entry.positioned = true;
                placed.Add(candidate);
            }
        }

        private Rect Measure(Entry entry)
        {
            var rect = entry.label.rectTransform;
            rect.GetWorldCorners(corners);
            Vector3 min = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 max = canvasRect.InverseTransformPoint(corners[2]);
            Vector3 centre = canvasRect.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            centre.y -= entry.offset;
            float scaleY = Mathf.Abs(canvasRect.InverseTransformVector(rect.TransformVector(Vector3.up)).y);
            float scaleX = Mathf.Abs(canvasRect.InverseTransformVector(rect.TransformVector(Vector3.right)).x);
            float width = Mathf.Min(Mathf.Abs(max.x - min.x), entry.label.preferredWidth * scaleX);
            float height = Mathf.Max(entry.label.fontSize * scaleY, entry.label.preferredHeight * scaleY);
            return new Rect(centre.x - width * .5f, centre.y - height * .5f, width, height);
        }

        private void SetOffset(Entry entry, float offset)
        {
            if (Mathf.Abs(offset - entry.offset) < .01f) return;
            entry.label.rectTransform.position += canvasRect.TransformVector(Vector3.up * (offset - entry.offset));
            entry.offset = offset;
        }
        private void OnDisable()
        {
            if (canvasRect == null) return;
            foreach (var entry in entries)
                if (entry.label != null)
                {
                    SetOffset(entry, 0f);
                    entry.positioned = false;
                    entry.active = false;
                }
        }
    }
}
