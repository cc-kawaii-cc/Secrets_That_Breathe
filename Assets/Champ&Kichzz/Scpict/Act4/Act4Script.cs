using System;
using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ผู้พูด — เลือกเป็นบทบาท ไม่ใช่พิมพ์ชื่อ ระบบกล้องจะตัดไปหาคนพูดให้เองตามบทบาท
    /// ชื่อที่ขึ้นบนจอตั้งได้ในหัวข้อ "ชื่อผู้พูด" ของ data asset
    /// </summary>
    public enum Who
    {
        Narrator,   // คำบรรยาย (ตัวเอียง ไม่มีชื่อ)
        Khem,
        KhemThink,  // ความคิดในใจเข้ม
        Judge,
        Clerk,
        Defense,
        Witness,    // พยานที่ยืนอยู่ในคอกพยานตอนนั้น (ชื่อตามพยานปากนั้น)
        Aunt,       // ป้าสมรตอนนั่งอยู่ที่ที่นั่งโจทก์
        Champ,
        Arin,
        Custom,     // พิมพ์ชื่อเองในช่อง custom
    }

    /// <summary>หนึ่งบรรทัดบท</summary>
    [Serializable]
    public class Line
    {
        public Who who;
        [Tooltip("ชื่อผู้พูด ใช้เมื่อ who = Custom")]
        public string custom;
        [TextArea(1, 4)] public string text;
        [Tooltip("ค้างบรรทัดนี้อย่างน้อยกี่วินาที (0 = คำนวณจากความยาวเอง)")]
        [Min(0f)] public float hold;

        public Line() { }
        public Line(Who who, string text, float hold = 0f) { this.who = who; this.text = text; this.hold = hold; }
        public Line(string customSpeaker, string text, float hold = 0f) { who = Who.Custom; custom = customSpeaker; this.text = text; this.hold = hold; }
    }

    /// <summary>ชื่อที่ขึ้นบนจอของแต่ละบทบาท</summary>
    [Serializable]
    public class Act4Cast
    {
        public string khem = "เข้ม";
        public string khemThink = "เข้ม (ในใจ)";
        public string judge = "ผู้พิพากษา";
        public string clerk = "เจ้าหน้าที่หน้าบัลลังก์";
        public string defense = "ทนายจำเลย";
        public string aunt = "ป้าสมร";
        public string champ = "แชมป์";
        public string arin = "หมออรินทร์";

        public string NameOf(Line line, string witnessName)
        {
            switch (line.who)
            {
                case Who.Khem: return khem;
                case Who.KhemThink: return khemThink;
                case Who.Judge: return judge;
                case Who.Clerk: return clerk;
                case Who.Defense: return defense;
                case Who.Witness: return witnessName ?? "พยาน";
                case Who.Aunt: return aunt;
                case Who.Champ: return champ;
                case Who.Arin: return arin;
                case Who.Custom: return line.custom;
                default: return "";
            }
        }
    }

    /// <summary>หลักฐานหนึ่งชิ้นในแฟ้มคดี</summary>
    [Serializable]
    public class Act4Evidence
    {
        [Tooltip("id หลักฐานของ Act2Director (อย่าเปลี่ยน ถ้าไม่ได้เพิ่มหลักฐานใหม่ในเกม)")]
        public string id;
        public string name;
        [TextArea(2, 4)] public string desc;

        public Act4Evidence() { }
        public Act4Evidence(string id, string name, string desc) { this.id = id; this.name = name; this.desc = desc; }
    }

    /// <summary>
    /// id ของ ACT 4 (objective อยู่ใน <see cref="Act2Script"/> ใช้ Act2Director ตัวเดิม)
    /// เนื้อหาทั้งหมด (บท คำถาม หลักฐาน ตัวเลข) อยู่ใน data asset ที่แก้ได้ใน Inspector:
    ///   Act4CourtroomData / Act4CourtStepsData  — ค่าเริ่มต้นตอนสร้าง asset มาจาก <see cref="TrialScript"/>
    /// </summary>
    public static class Act4Script
    {
        // ── objectives (ต้องตรงกับ Act2Script.Objectives) ──
        public const string OBJ_TakeSeat = "OBJ_A4_01_TakeSeat";
        public const string OBJ_CrossExamine = "OBJ_A4_02_CrossExamine";
        public const string OBJ_PlayClip = "OBJ_A4_03_PlayClip";
        public const string OBJ_LeaveCourt = "OBJ_A4_04_LeaveCourt";
        public const string OBJ_ReachAunt = "OBJ_A4_05_ReachAunt";
        public const string OBJ_Ritual = "OBJ_A4_06_Ritual";

        /// <summary>
        /// หลักฐานที่เรื่องรับประกันว่าเข้มมีครบแล้วเมื่อถึงวันพิพากษา (ค่าเริ่มต้นของแฟ้มคดี)
        /// </summary>
        public static Act4Evidence[] DefaultCaseFile()
        {
            return new[]
            {
                new Act4Evidence(Act2Script.EV_PaintChip, "เศษกันชนสีแดง",
                    "เจอจมดินข้างเสาไฟฟ้าในที่เกิดเหตุตอนตีสาม สีแดงสั่งทำพิเศษ"),
                new Act4Evidence(Act2Script.EV_PaintCode, "ผลเทียบรหัสสีจากอู่",
                    "เพื่อนที่อู่ส่องใต้แว่นขยาย รหัสสีตรงกับสีสั่งทำพิเศษของรถสปอร์ตสีแดงคันเดียวในเมือง"),
                new Act4Evidence(Act2Script.EV_OwnerName, "บันทึกสั่งอะไหล่",
                    "ใบสั่งกันชนหน้าชิ้นใหม่ในชื่อ \"แชมป์\" ลูกชาย ส.ส. สั่งหลังวันเกิดเหตุสองวัน"),
                new Act4Evidence(Act2Script.EV_BumperPhoto, "รูปถ่ายรถที่ลานจอดคลับ VIP",
                    "ถ่ายเองที่ลานจอด VIP คลับของแชมป์ กันชนหน้าพังยับ เนื้อสีตรงรอยแหว่งหายไปเป็นชิ้น"),
                new Act4Evidence(Act2Script.EV_DashcamMissing, "กล้องหน้ารถถูกถอด",
                    "ขายึดกล้องหน้ารถยังติดอยู่ที่กระจก แต่ตัวกล้องหายไป"),
                new Act4Evidence(Act2Script.EV_ConfessionCall, "เสียงแชมป์คุยโทรศัพท์กับพ่อ",
                    "ได้ยินเองจากใต้โต๊ะทำงานในเพนต์เฮาส์ — พ่อเคลียร์ตำรวจกับอัยการให้แล้ว"),
                new Act4Evidence(Act2Script.EV_SDCard, "SD Card กล้องหน้ารถ",
                    "การ์ดความจำจากเซฟในเพนต์เฮาส์ บันทึกภาพคืนเกิดเหตุไว้ทั้งหมด"),
            };
        }
    }
}
