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

        // Defaults seed newly created Canvas objects only. Existing TMP text is never overwritten.
        public const string DefaultFirstLine = "หนู... หนูใช่ทนายเข้มใช่มั้ยลูก... ช่วยป้าด้วยนะ ลูกสาวป้า... โดนรถชนตาย";
        private static readonly string[] InitialLines =
        {
            DefaultFirstLine,
            "ป้าไปแจ้งความแล้ว... แต่ตำรวจเขาไม่รับแจ้ง บอกว่าคู่กรณีเป็นลูก ส.ส. ... สุดท้ายเขาสรุปว่าเป็นแค่อุบัติเหตุ",
            "ป้าเหลือเงินอยู่แค่นี้... ป้าขอจ้างหนูนะ ช่วยลูกป้าหน่อยนะลูก",
            "เก็บเงินไว้ทำศพน้องเถอะป้า คดีนี้ผมทำให้ฟรี... ผมสัญญาว่าจะลากคอมันมาเข้าคุกให้ได้",
            "จะดีหรอเข้ม ป้าเขาก็ดูไม่ค่อยมีเงินเท่าไหร่นะ",
            "เงินเก็บเราก็พอมีนะ ทำคดีช่วยป้าหน่อยไหม",
            "อย่าเลย ไม่ต้องรับเงินป้ามาหรอก"
        };

        public static AuntDialogueUI FindOrCreate(SeatPromptUI canvas, TMP_Text template)
        {
            Transform group = SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Aunt);
            var ui = group.GetComponent<AuntDialogueUI>();
            if (ui == null) ui = group.gameObject.AddComponent<AuntDialogueUI>();
            if (ui.firstLine == null)
            {
                template.transform.parent.name = "01_AuntFirstLine";
                template.transform.parent.SetParent(group, false);
                ui.firstLine = template;
            }
            ui.secondLine = ui.EnsureLine(ui.secondLine, "02_AuntSecondLine", InitialLines[1]);
            ui.moneyLine = ui.EnsureLine(ui.moneyLine, "03_AuntMoneyLine", InitialLines[2]);
            ui.refuseLine = ui.EnsureLine(ui.refuseLine, "04_KhemRefuseMoney", InitialLines[3]);
            ui.firstThought = ui.EnsureLine(ui.firstThought, "05_ThoughtFirst", Quote(InitialLines[4]));
            ui.secondThought = ui.EnsureLine(ui.secondThought, "06_ThoughtSecond", Quote(InitialLines[5]));
            ui.thirdThought = ui.EnsureLine(ui.thirdThought, "07_ThoughtThird", Quote(InitialLines[6]));
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
