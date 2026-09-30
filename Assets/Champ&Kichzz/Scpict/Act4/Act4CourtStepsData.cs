using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ทุกอย่างของซีนหน้าศาล (ACT 4.3 - 4.5 + Post-Credit) ที่แก้ได้ใน Inspector: บทพูด การ์ด โลโก้ ความเร็ว ระยะ
    ///
    /// asset อยู่ที่ Assets/MainScenes/Main4_Court/Data/Act4_CourtStepsData.asset
    /// (Ch4CourtStepsBuilder สร้างให้ครั้งแรก และจะไม่เขียนทับถ้ามีอยู่แล้ว)
    /// </summary>
    [CreateAssetMenu(menuName = "Secrets That Breathe/Act 4/Court Steps Data", fileName = "Act4_CourtStepsData")]
    public class Act4CourtStepsData : ScriptableObject
    {
        [Header("ชื่อผู้พูดบนจอ")]
        public Act4Cast cast = new Act4Cast();

        [Header("การ์ดเปิดฉาก")]
        public string cardSmall = "";
        public string cardBig = "หน้าศาลสถิตยุติธรรม";
        public string cardSub = "ฝนตกหนัก";
        [Min(0.5f)] public float cardHold = 2f;

        [Header("4.4 ความเงียบที่ดังที่สุด")]
        [Tooltip("ขึ้นตอนผู้เล่นเดินได้ หลังการ์ดเปิดฉาก")]
        public Line[] khemSeesAunt = { new Line(Who.KhemThink, "ป้าสมร...") };
        [Tooltip("ขึ้นตอนป้าสมรทรุดลงที่ลานหน้าบันได")]
        public Line[] auntCollapsed = { new Line(Who.Narrator, "ป้าสมรทรุดลงกลางสายฝน... ร้องไห้โดยไม่มีเสียง") };
        public Line[] khemReaches = { new Line(Who.Khem, "ป้า... ผม—", 1.4f) };
        public Line[] auntLeaves = { new Line(Who.Narrator, "ป้าสมรไม่พูดอะไรสักคำ... แล้วเดินจากไปในสายฝน", 3.2f) };
        public Line[] khemGuilt =
        {
            new Line(Who.KhemThink, "ผมสัญญากับป้าไว้แท้ ๆ..."),
            new Line(Who.KhemThink, "ความผิดผม... ทั้งหมดเป็นความผิดผมเอง"),
        };

        [Header("ความเร็ว / ระยะ")]
        [Tooltip("ความเร็วป้าสมรเดินลงบันได (m/s)")]
        [Range(0.3f, 3f)] public float auntDescendSpeed = 1.0f;
        [Tooltip("ความเร็วป้าสมรเดินจากไป (m/s)")]
        [Range(0.3f, 3f)] public float auntLeaveSpeed = 0.9f;
        [Tooltip("ความสูงแคปซูลตอนป้าทรุด (เมตร)")]
        [Range(0.5f, 1.8f)] public float auntCollapsedHeight = 0.95f;
        [Tooltip("ผู้เล่นเดินเข้าใกล้ป้าเท่านี้ (เมตร) แล้วคัตซีนเริ่ม")]
        [Range(0.8f, 6f)] public float reachDistance = 2.4f;
        [Tooltip("ผู้เล่นเดินถึงริมถนนระยะเท่านี้ (เมตร) แล้วพิธีกรรมเริ่ม")]
        [Range(0.3f, 3f)] public float ritualDistance = 0.9f;
        [Tooltip("เสียงฝนหรี่ลงเหลือเท่านี้ตอนป้าเดินหนี (ถ้าใส่ rainAudio ไว้)")]
        [Range(0f, 1f)] public float rainDuckVolume = 0.25f;

        [Header("4.5 พิธีกรรม")]
        public Line[] ritualIntro =
        {
            new Line(Who.KhemThink, "เข็มกลัดทนายความ... วันที่ได้มันมา ผมเชื่อว่ากฎหมายคุ้มครองคนได้จริง"),
        };
        public Line[] monologue =
        {
            new Line(Who.Khem, "ถ้ากฎหมายมันไม่มีจริง...", 2.8f),
            new Line(Who.Khem, "กูจะเป็นคนสร้างมันขึ้นมาเอง", 3.2f),
        };

        [Header("Post-Credit (หมออรินทร์)")]
        [Tooltip("มุมหลังไหล่หมออรินทร์ มองเห็นเข้มผ่านกระจกรถ")]
        public Line[] carIntro = { new Line(Who.Narrator, "ฝั่งตรงข้ามศาล... รถหรูสีดำคันหนึ่งจอดติดเครื่องอยู่นานแล้ว", 3f) };
        [Tooltip("มุมรินชา")]
        public Line[] teaPour =
        {
            new Line(Who.Arin, "ตราชูที่ขึ้นสนิม...", 2.6f),
            new Line(Who.Arin, "ต่อให้คุณวางความจริงลงไปหนักแค่ไหน...", 2.8f),
        };
        [Tooltip("กลับมามุมหลังไหล่ ก่อนตัดจอดำ")]
        public Line[] finalWords =
        {
            new Line(Who.Arin, "มันก็ไม่มีวันเอียงไปหาความยุติธรรมหรอกครับ...", 3f),
            new Line(Who.Arin, "...คุณทนาย", 2.4f),
        };

        [Header("โลโก้ตอนจบ")]
        public string titleSmall = "EPISODE 1";
        public string titleBig = "THE SHATTERED SCALES";
        public string titleSub = "รอยร้าวของตราชู";
        [Tooltip("ค้างโลโก้กี่วินาทีก่อนประกาศจบ")]
        [Min(0f)] public float titleHold = 4f;
        [Tooltip("ซีนเมนูหลัก (ต้องอยู่ใน Build Settings ถึงจะขึ้นให้กดกลับ)")]
        public string menuScene = "Main Menu";
        public string menuPrompt = "กด [Space] เพื่อกลับเมนูหลัก";
    }
}
