using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    // Draw the reference's white outline directly, with a transparent centre and background.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrosshairDotGraphic : MaskableGraphic
    {
        [Min(.1f)] public float lineThickness = 1.5f;
        [HideInInspector] public int outlineVersion;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            Vector2 centre = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            float innerRadius = Mathf.Max(0f, radius - lineThickness);
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                helper.AddVert(centre + direction * radius, color, Vector2.zero);
                helper.AddVert(centre + direction * innerRadius, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int current = i * 2;
                int next = ((i + 1) % segments) * 2;
                helper.AddTriangle(current, next, current + 1);
                helper.AddTriangle(next, next + 1, current + 1);
            }
        }

        public static GameObject Install(SeatPromptUI canvas)
        {
            var existing = canvas.transform.Find("CrosshairDot");
            if (existing != null)
            {
                if (existing.GetComponent<CanvasRenderer>() == null) existing.gameObject.AddComponent<CanvasRenderer>();
                var graphic = existing.GetComponent<CrosshairDotGraphic>();
                if (graphic != null && graphic.outlineVersion < 1)
                {
                    var existingRect = existing.GetComponent<RectTransform>();
                    existingRect.sizeDelta = new Vector2(24f, 24f);
                    graphic.lineThickness = 1.5f;
                    graphic.color = Color.white;
                    graphic.outlineVersion = 1;
                    graphic.SetAllDirty();
                }
                return existing.gameObject;
            }
            var go = new GameObject("CrosshairDot", typeof(RectTransform), typeof(CrosshairDotGraphic));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(24f, 24f);
            var dot = go.GetComponent<CrosshairDotGraphic>();
            dot.color = Color.white;
            dot.raycastTarget = false;
            dot.outlineVersion = 1;
            return go;
        }
    }
}
