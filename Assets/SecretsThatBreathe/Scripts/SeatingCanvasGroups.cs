using UnityEngine;

namespace SecretsThatBreathe
{
    public static class SeatingCanvasGroups
    {
        public const string Water = "WaterVendingUI";
        public const string Aunt = "AuntDialogueUI";
        public const string Gameplay = "MealAndSearchUI";

        public static Transform Group(SeatPromptUI canvas, string name)
        {
            Transform existing = canvas.transform.Find(name);
            if (existing != null) return existing;
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(canvas.transform, false);
            var rect = group.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return group.transform;
        }

        public static Transform FindPanel(SeatPromptUI canvas, string name)
        {
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        public static bool Organize(SeatPromptUI canvas)
        {
            bool changed = canvas.transform.Find(Water) == null || canvas.transform.Find(Aunt) == null || canvas.transform.Find(Gameplay) == null;
            Transform water = Group(canvas, Water), aunt = Group(canvas, Aunt), gameplay = Group(canvas, Gameplay);
            foreach (string name in new[] { "WaterMachinePrompt", "WaterMachineMessage" }) changed |= Move(FindPanel(canvas, name), water);
            foreach (string name in new[] { "AuntDialogue", "AuntMoneyChoices" }) changed |= Move(FindPanel(canvas, name), aunt);
            foreach (string name in new[] { "SitPrompt", "EatPrompt", "DrawerObjective", "DrawerSearchPrompt", "DrawerSearching" }) changed |= Move(FindPanel(canvas, name), gameplay);
            return changed;
        }

        private static bool Move(Transform panel, Transform group)
        {
            if (panel == null || panel.parent == group) return false;
            var rect = panel as RectTransform;
            if (rect == null) { panel.SetParent(group, false); return true; }
            Vector2 anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta;
            Vector3 position = rect.anchoredPosition3D, scale = rect.localScale;
            Quaternion rotation = rect.localRotation;
            rect.SetParent(group, false);
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition3D = position;
            rect.localScale = scale; rect.localRotation = rotation;
            return true;
        }
    }
}
