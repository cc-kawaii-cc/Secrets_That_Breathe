using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed class PostMealThoughtUI : MonoBehaviour
    {
        public TMP_Text firstThought, secondThought, thirdThought;
        [SerializeField, HideInInspector] private int objectiveRevision;

        public static PostMealThoughtUI FindOrCreate(SeatPromptUI canvas, TMP_Text objective)
        {
            Transform group = SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Gameplay);
            var ui = group.GetComponent<PostMealThoughtUI>();
            if (ui == null) ui = group.gameObject.AddComponent<PostMealThoughtUI>();
            if (ui.firstThought == null) ui.firstThought = canvas.EnsureNamedText("AfterEatingThought1");
            if (ui.secondThought == null) ui.secondThought = canvas.EnsureNamedText("AfterEatingThought2");
            if (ui.thirdThought == null) ui.thirdThought = canvas.EnsureNamedText("AfterEatingThought3");
            if (ui.objectiveRevision < 1 && objective != null)
            {
                objective.text = "เอกสารที่ต้องเซ็นต์อยู่ลิ้นชักฝั่งซ้าย";
                ui.objectiveRevision = 1;
            }
            return ui;
        }

        public bool ShowAtTime(float time, float initialDelay, float duration, float gap, float objectiveDelay)
        {
            initialDelay = Mathf.Max(0f, initialDelay);
            duration = Mathf.Max(0.1f, duration);
            gap = Mathf.Max(0f, gap);
            int visible = -1;
            for (int i = 0; i < 3; i++)
            {
                float start = initialDelay + i * (duration + gap);
                if (time >= start && time < start + duration) { visible = i; break; }
            }
            Set(firstThought, visible == 0);
            Set(secondThought, visible == 1);
            Set(thirdThought, visible == 2);
            return time >= initialDelay + 3f * duration + 2f * gap + Mathf.Max(0f, objectiveDelay);
        }

        private static void Set(TMP_Text label, bool visible)
        {
            if (label != null && label.transform.parent.gameObject.activeSelf != visible)
                label.transform.parent.gameObject.SetActive(visible);
        }
        public void HideAll() { Set(firstThought, false); Set(secondThought, false); Set(thirdThought, false); }
    }
}
