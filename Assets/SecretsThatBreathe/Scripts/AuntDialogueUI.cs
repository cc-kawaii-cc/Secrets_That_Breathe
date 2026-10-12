using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Every sentence lives on its own Canvas TMP object; runtime only changes visibility.
    public sealed class AuntDialogueUI : MonoBehaviour
    {
        public TMP_Text firstLine, secondLine, moneyLine, refuseLine;
        public TMP_Text firstThought, secondThought, thirdThought;
        public TMP_Text thankYouLine;

        public static AuntDialogueUI FindOrCreate(SeatPromptUI canvas, TMP_Text template, string[] initialLines)
        {
            Transform group = SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Aunt);
            var ui = group.GetComponent<AuntDialogueUI>();
            if (ui == null) ui = group.gameObject.AddComponent<AuntDialogueUI>();
            if (ui.firstLine == null)
            {
                template.transform.parent.name = "01_AuntFirstLine";
                template.transform.parent.SetParent(group, false);
                template.text = initialLines[0];
                ui.firstLine = template;
            }
            ui.secondLine = ui.EnsureLine(ui.secondLine, "02_AuntSecondLine", initialLines[1]);
            ui.moneyLine = ui.EnsureLine(ui.moneyLine, "03_AuntMoneyLine", initialLines[2]);
            ui.refuseLine = ui.EnsureLine(ui.refuseLine, "04_KhemRefuseMoney", initialLines[3]);
            ui.firstThought = ui.EnsureLine(ui.firstThought, "05_ThoughtFirst", Quote(initialLines[4]));
            ui.secondThought = ui.EnsureLine(ui.secondThought, "06_ThoughtSecond", Quote(initialLines[5]));
            ui.thirdThought = ui.EnsureLine(ui.thirdThought, "07_ThoughtThird", Quote(initialLines[6]));
            ui.thankYouLine = ui.EnsureLine(ui.thankYouLine, "08_AuntThankYou", "ขอบคุณมากนะลูก");
            return ui;
        }

        private static string Quote(string text) { return "\"" + (text ?? "").Trim().Trim('"') + "\""; }

        private TMP_Text EnsureLine(TMP_Text reference, string name, string initialText)
        {
            if (reference != null) return reference;
            Transform existing = transform.Find(name);
            if (existing != null) return existing.GetComponentInChildren<TMP_Text>(true);
            var panel = Instantiate(firstLine.transform.parent.gameObject, transform);
            panel.name = name;
            var label = panel.GetComponentInChildren<TMP_Text>(true);
            label.text = initialText;
            panel.SetActive(false);
            return label;
        }

        public TMP_Text ShowLine(int lineIndex, int thoughtIndex)
        {
            HideAll();
            TMP_Text label = lineIndex == 0 ? firstLine : lineIndex == 1 ? secondLine : lineIndex == 2 ? moneyLine :
                lineIndex == 4 ? refuseLine : lineIndex == 5 ? thankYouLine : thoughtIndex == 0 ? firstThought : thoughtIndex == 1 ? secondThought : thirdThought;
            if (label != null) label.transform.parent.gameObject.SetActive(true);
            return label;
        }

        public void HideAll()
        {
            foreach (var label in new[] { firstLine, secondLine, moneyLine, refuseLine, firstThought, secondThought, thirdThought, thankYouLine })
                if (label != null) label.transform.parent.gameObject.SetActive(false);
        }
    }
}
