using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private static readonly Regex ControlKey = new Regex(@"\[(?:E|R|F|RMB|LMB)\]", RegexOptions.IgnoreCase);
        private static readonly Regex TestKey = new Regex(@"\[F[123]\]", RegexOptions.IgnoreCase);
        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<Transform, Entry> byTarget = new Dictionary<Transform, Entry>();
        private readonly List<TMP_Text> scanned = new List<TMP_Text>();
        private readonly HashSet<TMP_Text> known = new HashSet<TMP_Text>();
        private readonly List<Entry> visible = new List<Entry>();
        private readonly List<Rect> placed = new List<Rect>();
        private readonly List<Rect> commandRects = new List<Rect>();
        private readonly Dictionary<TMP_Text, float> hiddenAlphas = new Dictionary<TMP_Text, float>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform canvasRect;
        private int sequence;
        private sealed class Entry
        {
            public RectTransform target;
            public readonly List<TMP_Text> labels = new List<TMP_Text>();
            public float offset;
            public bool active, command, pinned;
            public int order, priority;
        }
        private void Discover()
        {
            scanned.Clear();
            GetComponentsInChildren(true, scanned);
            foreach (var label in scanned)
            {
                if (Excluded(label) || !known.Add(label)) continue;
                var target = label.rectTransform;
                if (target == null) continue;
                if (!byTarget.TryGetValue(target, out var entry))
                {
                    entry = new Entry { target = target };
                    byTarget.Add(target, entry);
                    entries.Add(entry);
                }
                entry.labels.Add(label);
            }
        }
        private bool Excluded(TMP_Text label)
        {
            if (label == null || TestKey.IsMatch(label.text ?? "")) return true;
            for (var item = label.transform; item != null && item != transform; item = item.parent)
                if (item.name == "FlowingDialogue" || item.name.StartsWith("TestSkip") || item.name.StartsWith("CallRow")
                    || item.name == "Conversation" || item.name == "CallBackLines"
                    || (item.name == "Lines" && item.parent != null && item.parent.name == "PhoneCallUI"))
                    return true;
            return false;
        }
        private static bool ReadingPage(TMP_Text label)
        {
            for (var item = label.transform; item != null; item = item.parent)
                if (item.name == "SamudNoteCanvas") return true;
            return false;
        }
        private void LateUpdate()
        {
            if (canvasRect == null) canvasRect = transform as RectTransform;
            if (canvasRect == null) return;
            Discover(); // Also include controls created after Start, without allocating a new scan array.
            visible.Clear(); placed.Clear(); commandRects.Clear();
            foreach (var entry in entries)
            {
                if (entry.target == null) continue;
                bool active = false, command = false, pinned = false;
                int priority = 0;
                foreach (var label in entry.labels)
                {
                    if (label == null) continue;
                    RestoreAlpha(label);
                    if (Excluded(label) || !label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text)) continue;
                    active = true;
                    bool button = label.GetComponentInParent<Button>(true) != null;
                    bool control = ControlKey.IsMatch(label.text);
                    command |= control;
                    string content = (label.text ?? "").Trim();
                    bool quoted = content.Length >= 2 && content[0] == '"' && content[content.Length - 1] == '"';
                    pinned |= control || button || quoted || ReadingPage(label);
                    priority = Mathf.Max(priority, button ? 2 : 1);
                    if (label.havePropertiesChanged) label.ForceMeshUpdate();
                }
                if (active && !entry.active) entry.order = ++sequence;
                if (!active || pinned) SetOffset(entry, 0f);
                entry.active = active; entry.command = command; entry.pinned = pinned; entry.priority = priority;
                if (active) visible.Add(entry);
            }
            // Controls always reserve their original bottom position before any dialogue.
            visible.Sort((a, b) => a.pinned != b.pinned ? (a.pinned ? -1 : 1)
                : a.pinned ? (a.priority != b.priority ? b.priority.CompareTo(a.priority) : b.order.CompareTo(a.order))
                : a.order.CompareTo(b.order));
            foreach (var entry in visible)
            {
                Rect original = Measure(entry);
                if (entry.pinned)
                {
                    // Two look prompts can become available together. Show one in the fixed slot.
                    bool duplicateSlot = false;
                    if (entry.command)
                        foreach (var slot in commandRects)
                            if (original.Overlaps(slot)) { duplicateSlot = true; break; }
                    if (duplicateSlot)
                    {
                        foreach (var label in entry.labels) HideAlpha(label);
                        continue;
                    }
                    if (entry.command) commandRects.Add(original);
                    placed.Add(original);
                    continue;
                }
                Rect candidate = original;
                // Reflow from the authored position every frame so dialogue drops back
                // when a control prompt disappears, then rises again if it returns.
                candidate = PlaceAbove(candidate, placed, spacing);
                SetOffset(entry, candidate.y - original.y);
                placed.Add(candidate);
            }
        }
        internal static Rect PlaceAbove(Rect candidate, List<Rect> obstacles, float gap)
        {
            for (int pass = 0; pass <= obstacles.Count; pass++)
            {
                bool moved = false;
                foreach (var other in obstacles)
                {
                    if (candidate.xMax <= other.xMin || candidate.xMin >= other.xMax) continue;
                    if (candidate.yMax + gap <= other.yMin || candidate.yMin >= other.yMax + gap) continue;
                    candidate.y = other.yMax + gap;
                    moved = true;
                }
                if (!moved) break;
            }
            return candidate;
        }
        private Rect Measure(Entry entry)
        {
            Rect total = default;
            bool found = false;
            foreach (var label in entry.labels)
            {
                if (label == null || Excluded(label) || !label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text)) continue;
                var rect = label.rectTransform;
                var bounds = label.textBounds;
                Vector3 min, max;
                if (bounds.size.x > .01f && bounds.size.y > .01f)
                {
                    min = canvasRect.InverseTransformPoint(rect.TransformPoint(bounds.min));
                    max = canvasRect.InverseTransformPoint(rect.TransformPoint(bounds.max));
                }
                else
                {
                    rect.GetWorldCorners(corners);
                    min = canvasRect.InverseTransformPoint(corners[0]);
                    max = canvasRect.InverseTransformPoint(corners[2]);
                }
                Rect measured = Rect.MinMaxRect(min.x, min.y - entry.offset, max.x, max.y - entry.offset);
                total = found ? Rect.MinMaxRect(Mathf.Min(total.xMin, measured.xMin), Mathf.Min(total.yMin, measured.yMin),
                    Mathf.Max(total.xMax, measured.xMax), Mathf.Max(total.yMax, measured.yMax)) : measured;
                found = true;
            }
            return total;
        }
        private void SetOffset(Entry entry, float offset)
        {
            if (entry.target == null || Mathf.Abs(offset - entry.offset) < .01f) return;
            entry.target.position += canvasRect.TransformVector(Vector3.up * (offset - entry.offset));
            entry.offset = offset;
        }
        private void HideAlpha(TMP_Text label)
        {
            if (label == null) return;
            hiddenAlphas[label] = label.canvasRenderer.GetAlpha();
            label.canvasRenderer.SetAlpha(0f);
        }
        private void RestoreAlpha(TMP_Text label)
        {
            if (label == null || !hiddenAlphas.TryGetValue(label, out float alpha)) return;
            label.canvasRenderer.SetAlpha(alpha);
            hiddenAlphas.Remove(label);
        }
        private void OnDisable()
        {
            foreach (var entry in entries)
            {
                if (canvasRect != null) SetOffset(entry, 0f);
                entry.active = false;
                foreach (var label in entry.labels) RestoreAlpha(label);
            }
        }
    }
}
