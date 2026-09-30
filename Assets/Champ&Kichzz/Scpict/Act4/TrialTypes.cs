using System;
using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    // ชนิดข้อมูลของการพิจารณาคดี — ทุกตัว [Serializable] เพื่อให้แก้ใน Inspector ผ่าน Act4CourtroomData ได้

    /// <summary>เหตุคัดค้านตามแบบที่ใช้ในศาลไทย</summary>
    public enum Ground { None, Leading, Irrelevant, Opinion, Badgering, Repetitive, Hearsay }

    /// <summary>ชื่อ/คำอธิบาย/คำวินิจฉัยของเหตุคัดค้านหนึ่งเหตุ</summary>
    [Serializable]
    public class GroundText
    {
        public Ground ground;
        [Tooltip("ชื่อที่ขึ้นให้ผู้เล่นเลือก")]
        public string name;
        [Tooltip("คำอธิบายในแฟ้มคดี หน้าเหตุคัดค้าน")]
        [TextArea(2, 4)] public string help;
        [Tooltip("ศาลพูดเมื่อรับคำคัดค้านด้วยเหตุนี้")]
        public string sustained;
    }

    /// <summary>ตัวเลือกหนึ่งข้อ (แถลงเปิดคดี / คำถามซักถาม / ถามติง)</summary>
    [Serializable]
    public class Pick
    {
        [Tooltip("ข้อความบนปุ่มตัวเลือก")]
        public string label;
        [Tooltip("บทที่เล่นหลังเลือกข้อนี้")]
        public Line[] lines;
        [Tooltip("ความน่าเชื่อถือที่ได้ (+) หรือเสีย (-) เมื่อเลือกข้อนี้")]
        public int cred;
        [Tooltip("ฝ่ายจำเลยคัดค้านข้อนี้ด้วยเหตุอะไร (None = ไม่คัดค้าน) — ถ้าโดนคัดค้าน ข้อนี้หายไปแล้วผู้เล่นเลือกใหม่")]
        public Ground objection = Ground.None;
        [Tooltip("ศาลบันทึกคำเบิกความหลังคำตอบนี้ (เว้นว่าง = ไม่บันทึก)")]
        public string record;
    }

    /// <summary>หนึ่งรอบที่ผู้เล่นต้องเลือก</summary>
    [Serializable]
    public class Turn
    {
        [Tooltip("หัวข้อบนแผงตัวเลือก")]
        public string title;
        public Pick[] picks;
    }

    /// <summary>คำถามของทนายจำเลย ผู้เล่นมีเวลาให้กด [Q] คัดค้าน</summary>
    [Serializable]
    public class DefenseQ
    {
        [TextArea(1, 3)] public string question;
        [Tooltip("เหตุที่คัดค้านได้ (ว่าง = คำถามชอบด้วยกฎหมาย คัดค้านไปจะโดนศาลยก)")]
        public Ground[] grounds = new Ground[0];
        [Tooltip("คำตอบของพยาน (เล่นเมื่อไม่ได้คัดค้าน หรือคัดค้านไม่ขึ้น)")]
        public Line[] answer;
        [Tooltip("ถ้าคัดค้านขึ้น: ทนายจำเลยถามใหม่แบบถูกวิธี แล้วพยานตอบ (ว่าง = ข้ามคำถามนี้ไปเลย)")]
        public string rephrase;
        [Tooltip("ความน่าเชื่อถือที่เสียถ้าปล่อยให้คำถามที่ควรคัดค้านผ่านไป")]
        public int penalty = 5;
        [Tooltip("ศาลบันทึกหลังพยานตอบ (เว้นว่าง = ไม่บันทึก)")]
        public string record;
        [Tooltip("ศาลพูดตอนรับคำคัดค้าน (ว่าง = ใช้ประโยคมาตรฐานของเหตุนั้น)")]
        public string ruling;
    }

    /// <summary>หนึ่งประโยคในคำเบิกความที่ผู้เล่นถามค้าน</summary>
    [Serializable]
    public class Statement
    {
        [TextArea(1, 3)] public string text;
        [Tooltip("บทเมื่อผู้เล่นกด [Q] ซักไซ้ข้อนี้")]
        public Line[] press;
        [Tooltip("ความน่าเชื่อถือที่ได้ครั้งแรกที่ซักไซ้ข้อนี้")]
        public int pressCred;
        [Tooltip("ซักไซ้แล้วมีข้อใหม่โผล่ขึ้นมา (ลำดับข้อในรายการนี้ นับจาก 0, -1 = ไม่มี)")]
        public int reveals = -1;
        [Tooltip("ซ่อนไว้ก่อน จนกว่าจะถูกเปิดด้วย reveals ของข้ออื่น")]
        public bool hidden;
        [Tooltip("id หลักฐานที่หักล้างข้อนี้ได้ (เว้นว่าง = ข้อนี้หักล้างไม่ได้)")]
        public string contradiction;
        [Tooltip("บทเมื่อหักล้างสำเร็จ")]
        public Line[] broken;
        [Tooltip("หมายเลขพยานหลักฐานที่ศาลหมายตอนหักล้างสำเร็จ เช่น จ.2")]
        public string exhibit;
        public string record;
    }

    /// <summary>คำเบิกความที่ผู้เล่นถามค้านทีละข้อ — จบเมื่อหักล้างข้อใดข้อหนึ่งสำเร็จ</summary>
    [Serializable]
    public class Testimony
    {
        public string title;
        public Statement[] statements;
    }

    /// <summary>พยานหนึ่งปาก: ใครในซีน สาบานตัว ตอบชื่อ-อายุ-อาชีพ</summary>
    [Serializable]
    public class WitnessInfo
    {
        [Tooltip("ชื่อ GameObject ของนักแสดงใน 50_ACTORS")]
        public string actor;
        [Tooltip("ชื่อที่ขึ้นในกล่องบทพูดตอนอยู่ในคอกพยาน")]
        public string speaker;
        [Tooltip("\"พยานโจทก์\" หรือ \"พยานจำเลย\"")]
        public string side;
        [Tooltip("ข้อความในตัวเลือกตอนผู้เล่นเลือกลำดับพยาน")]
        public string orderLabel;
        [Tooltip("ตอบศาลเรื่องชื่อ อายุ อาชีพ")]
        [TextArea(1, 3)] public string identity;
        public string record;
        [Tooltip("หมายเหตุในแฟ้มคดี หน้าพยาน")]
        public string note;
    }

    /// <summary>ขั้นให้พยานดูวัตถุพยาน — ผู้เล่นต้องเลือกหลักฐานให้ถูกชิ้น</summary>
    [Serializable]
    public class ExhibitStep
    {
        public string prompt;
        public string evidenceId;
        [Tooltip("หมายเลขที่ศาลหมาย เช่น วจ.1")]
        public string code;
        public Line[] onCorrect;
        public Line onWrong;
        public int wrongPenalty = 5;
        public string record;
    }

    /// <summary>พยานโจทก์หนึ่งปาก: ซักถาม (ผู้เล่นเลือกคำถาม) -> ทนายจำเลยถามค้าน (ผู้เล่นคัดค้าน) -> ถามติง</summary>
    [Serializable]
    public class PlaintiffWitness
    {
        public WitnessInfo info;
        [Tooltip("ซักถาม — แต่ละ Turn คือหนึ่งคำถามที่ผู้เล่นเลือก")]
        public Turn[] direct;
        [Tooltip("ให้พยานดูวัตถุพยานหลังคำถามลำดับนี้ (นับจาก 0, -1 = ไม่มีขั้นนี้)")]
        public int exhibitAfterTurn = -1;
        public ExhibitStep exhibit;
        [Tooltip("เลือกคำถามดีในข้อสุดท้ายแล้ว ศาลหมายหลักฐานนี้เพิ่ม (เว้นว่าง = ไม่มี)")]
        public string markAfterDirectEvidence;
        public string markAfterDirectCode;
        [Tooltip("ทนายจำเลยถามค้าน — ผู้เล่นกด [Q] คัดค้านได้")]
        public DefenseQ[] cross;
        [Tooltip("ถามติง (ผู้เล่นเลือก)")]
        public Turn redirect;
    }

    /// <summary>พยานจำเลยหนึ่งปาก: ทนายจำเลยซักถาม (ผู้เล่นคัดค้าน) -> ผู้เล่นถามค้านทีละข้อ</summary>
    [Serializable]
    public class DefenseWitness
    {
        public WitnessInfo info;
        [Tooltip("บทก่อนเรียกพยาน (เช่น ทนายจำเลยขอนำพยานเข้าสืบ)")]
        public Line[] beforeCall;
        [Tooltip("ทนายจำเลยซักถาม — คำถามนำคัดค้านได้")]
        public DefenseQ[] direct;
        [Tooltip("คำเบิกความที่ผู้เล่นถามค้าน")]
        public Testimony testimony;
        [Tooltip("บทหลังถามค้านเสร็จ")]
        public Line[] afterCross;
    }

    /// <summary>ข้อต่อสู้สุดท้ายของฝ่ายจำเลย — ผู้เล่นต้องแสดงหลักฐานชิ้นสุดท้าย</summary>
    [Serializable]
    public class FinalClaim
    {
        public Line[] defense;
        public string prompt;
        public string evidenceId;
        public string code;
        public Line[] onCorrect;
    }

    /// <summary>คำตอบเมื่อแสดงหลักฐานผิดชิ้น</summary>
    [Serializable]
    public class WrongEvidenceRule
    {
        public string evidenceId;
        [Tooltip("ใช้เฉพาะตอนถามค้านคำเบิกความ")]
        public bool onlyTestimony;
        [Tooltip("ใช้เฉพาะตอนข้อต่อสู้สุดท้าย")]
        public bool onlyFinal;
        public Line line;
        [Tooltip("หักหนักกว่าปกติ (ใช้ค่า heavyLoss)")]
        public bool heavy;
        [Tooltip("ไม่หักคะแนน (เช่น ไพ่ตายที่ยังไม่ถึงเวลา)")]
        public bool noPenalty;
    }
}
