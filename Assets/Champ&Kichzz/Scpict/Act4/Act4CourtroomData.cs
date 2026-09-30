using System;
using SecretsThatBreathe.Act2;
using UnityEngine;
using T = SecretsThatBreathe.Act4.TrialScript;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ทุกอย่างของซีนห้องพิจารณาคดีที่แก้ได้ใน Inspector: เวลาคัดค้าน ตัวเลขความน่าเชื่อถือ
    /// บทพูดทุกบรรทัด คำถาม พยาน คำเบิกความ หลักฐาน เหตุคัดค้าน
    ///
    /// asset อยู่ที่ Assets/MainScenes/Main4_Court/Data/Act4_CourtroomData.asset
    /// (Ch4CourtroomBuilder สร้างให้ครั้งแรก และจะไม่เขียนทับถ้ามีอยู่แล้ว)
    /// คลิกขวาที่หัว component > "คืนค่าเริ่มต้นทั้งหมด" = ย้อนเนื้อหากลับเป็นค่าใน TrialScript.cs
    /// </summary>
    [CreateAssetMenu(menuName = "Secrets That Breathe/Act 4/Courtroom Data", fileName = "Act4_CourtroomData")]
    public class Act4CourtroomData : ScriptableObject
    {
        [Header("เวลาคัดค้าน — ผู้เล่นมีเวลากด [Q] หลังทนายจำเลยถาม")]
        [Tooltip("วินาทีพื้นฐานที่เปิดให้กดคัดค้าน")]
        [Range(2f, 20f)] public float objectionSeconds = 7f;
        [Tooltip("วินาทีเพิ่มต่อตัวอักษรของคำถาม — คำถามยาวได้เวลาอ่านนานขึ้น (70 ตัวอักษร x 0.03 = +2.1 วินาที)")]
        [Range(0f, 0.1f)] public float objectionSecondsPerChar = 0.03f;

        [Header("ความน่าเชื่อถือของทนายโจทก์ (0-100)")]
        public CredibilityRules credibility = new CredibilityRules();

        [Header("ชื่อผู้พูดบนจอ")]
        public Act4Cast cast = new Act4Cast();

        [Header("การ์ดเปิดบท + ช็อตเปิดห้อง")]
        public CourtIntro intro = new CourtIntro();

        [Header("แฟ้มคดี [Tab]")]
        public CaseFileText caseFile = new CaseFileText();

        [Header("ประโยคประจำของศาล (ใช้ซ้ำทุกพยาน)")]
        public CourtPhrases phrases = new CourtPhrases();

        [Header("เหตุคัดค้าน")]
        public GroundText[] grounds;

        [Header("1) อ่านฟ้อง / ถามคำให้การ")]
        public Line[] arraignment;
        public Line[] plea;
        [Tooltip("ศาลบันทึกหลังจำเลยให้การ")]
        public string pleaRecord = "จำเลยให้การปฏิเสธ";

        [Header("2) แถลงเปิดคดี")]
        public Turn[] opening;
        public Line[] afterOpening;

        [Header("3) สืบพยานโจทก์ (ผู้เล่นเลือกว่าจะเรียกปากไหนก่อน)")]
        public string plaintiffOrderTitle = "เรียกพยานโจทก์ปากแรก";
        public PlaintiffWitness[] plaintiffWitnesses;
        public Line[] plaintiffRests;

        [Header("4) สืบพยานจำเลย (เรียงตามลำดับ)")]
        public DefenseWitness[] defenseWitnesses;

        [Header("5) ข้อต่อสู้สุดท้าย")]
        public FinalClaim finalClaim;

        [Header("แสดงหลักฐานผิดชิ้น")]
        public WrongEvidenceRule[] wrongEvidence;
        [Tooltip("หลักฐานที่ไม่มีกฎเฉพาะ (ข้อต่อสู้สุดท้าย / ขั้นให้พยานดูวัตถุ)")]
        public Line wrongFallback;
        [Tooltip("หลักฐานที่ไม่มีกฎเฉพาะ ตอนถามค้านคำเบิกความ")]
        public Line wrongInTestimony;
        [Tooltip("ศาลเตือนต่อหลังแสดงหลักฐานผิดตอนถามค้าน")]
        public Line wrongInTestimonyJudge;

        [Header("ความน่าเชื่อถือหมดหลอด")]
        public Line[] credibilityLost;

        [Header("เปิดคลิป -> หักมุม -> พิพากษา -> ห้องว่าง")]
        public VerdictText verdict = new VerdictText();

        // ───────────────────────── ใช้ตอนเล่น ─────────────────────────

        public Act4Evidence FindEvidence(string id)
        {
            if (caseFile != null && caseFile.evidence != null)
                for (int i = 0; i < caseFile.evidence.Length; i++)
                    if (caseFile.evidence[i].id == id) return caseFile.evidence[i];
            return null;
        }

        GroundText G(Ground g)
        {
            if (grounds != null) for (int i = 0; i < grounds.Length; i++) if (grounds[i].ground == g) return grounds[i];
            return null;
        }

        public string GroundName(Ground g) { var t = G(g); return t != null ? t.name : g.ToString(); }
        public string GroundHelp(Ground g) { var t = G(g); return t != null ? t.help : ""; }
        public string Sustained(Ground g) { var t = G(g); return t != null ? t.sustained : ""; }

        /// <summary>บทตอบเมื่อแสดงหลักฐานผิดชิ้น + ความน่าเชื่อถือที่เสีย</summary>
        public Line ResolveWrong(string evidenceId, bool final, out int penalty)
        {
            if (wrongEvidence != null)
                foreach (var r in wrongEvidence)
                {
                    if (r.evidenceId != evidenceId) continue;
                    if (r.onlyFinal && !final) continue;
                    if (r.onlyTestimony && final) continue;
                    penalty = r.noPenalty ? 0 : (r.heavy ? credibility.heavyLoss : credibility.wrongLoss);
                    return r.line;
                }
            penalty = credibility.wrongLoss;
            return final ? wrongFallback : wrongInTestimony;
        }

        // ───────────────────────── ค่าเริ่มต้น ─────────────────────────

        void Reset() { ResetToDefaults(); }

        /// <summary>เติมเนื้อหาทั้งหมดจากค่าเริ่มต้นใน TrialScript.cs (ลบสิ่งที่แก้ไว้ใน Inspector)</summary>
        [ContextMenu("คืนค่าเริ่มต้นทั้งหมด (ลบสิ่งที่แก้ไว้)")]
        public void ResetToDefaults()
        {
            objectionSeconds = 7f;
            objectionSecondsPerChar = 0.03f;
            credibility = new CredibilityRules();
            cast = new Act4Cast();
            intro = new CourtIntro();
            phrases = new CourtPhrases();
            verdict = new VerdictText();
            caseFile = new CaseFileText
            {
                evidence = Act4Script.DefaultCaseFile(),
                summary = T.CaseSummary,
                stages = (string[])T.Stages.Clone(),
            };
            grounds = T.DefaultGrounds();

            arraignment = Copy(T.Arraignment);
            plea = Copy(T.Plea);
            pleaRecord = "จำเลยให้การปฏิเสธ";
            opening = Copy(T.Opening);
            afterOpening = Copy(T.AfterOpening);

            plaintiffOrderTitle = "เรียกพยานโจทก์ปากแรก";
            plaintiffWitnesses = new[]
            {
                new PlaintiffWitness
                {
                    info = Copy(T.AuntWitness), direct = Copy(T.AuntDirect), exhibitAfterTurn = -1,
                    exhibit = new ExhibitStep(), cross = Copy(T.AuntCross), redirect = Copy(T.AuntRedirect),
                },
                new PlaintiffWitness
                {
                    info = Copy(T.GarageWitness), direct = new[] { Copy(T.GarageDirect1), Copy(T.GarageDirect3) },
                    exhibitAfterTurn = 0,
                    exhibit = new ExhibitStep
                    {
                        prompt = T.GarageExhibitPrompt, evidenceId = Act2Script.EV_PaintChip, code = "วจ.1",
                        onCorrect = Copy(T.GarageExhibitOk),
                        onWrong = new Line(Who.Judge, "ทนายโจทก์ ให้พยานดูวัตถุที่พยานตรวจด้วยตัวเอง"),
                        wrongPenalty = 5,
                        record = "พยานยืนยันว่าเป็นเศษกันชนที่ตนตรวจ ศาลหมายเป็นวัตถุพยาน วจ.1",
                    },
                    markAfterDirectEvidence = Act2Script.EV_PaintCode, markAfterDirectCode = "จ.1",
                    cross = Copy(T.GarageCross), redirect = Copy(T.GarageRedirect),
                },
            };
            plaintiffRests = Copy(T.PlaintiffRests);

            defenseWitnesses = new[]
            {
                new DefenseWitness
                {
                    info = Copy(T.ResortWitness), beforeCall = new Line[0], direct = Copy(T.ResortDirect),
                    testimony = Copy(T.ResortTestimony),
                    afterCross = new[] { new Line(Who.Defense, "(กัดฟัน) ...ฝ่ายจำเลยไม่ถามติงครับ") },
                },
                new DefenseWitness
                {
                    info = Copy(T.MechanicWitness),
                    beforeCall = new[] { new Line(Who.Defense, "ฝ่ายจำเลยขอนำพยานปากต่อไปเข้าสืบครับ") },
                    direct = Copy(T.MechanicDirect), testimony = Copy(T.MechanicTestimony), afterCross = new Line[0],
                },
            };

            finalClaim = new FinalClaim
            {
                defense = Copy(T.DefenseLastStand), prompt = T.FinalPrompt,
                evidenceId = Act2Script.EV_SDCard, code = "วจ.2", onCorrect = Copy(T.FinalCorrect),
            };

            wrongEvidence = new[]
            {
                new WrongEvidenceRule { evidenceId = Act2Script.EV_ConfessionCall, heavy = true,
                    line = new Line(Who.Defense, "คัดค้านครับ! เสียงที่ว่านี้ ทนายโจทก์ไปได้มาจากไหน? ศาลโปรดบันทึกไว้ด้วยครับ") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_BumperPhoto, onlyFinal = true,
                    line = new Line(Who.Defense, "รูปนั้นศาลเห็นไปแล้วครับ มันบอกแค่ว่ารถพัง ไม่ได้บอกว่าใครขับ") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_SDCard, onlyTestimony = true, noPenalty = true,
                    line = new Line(Who.KhemThink, "ยังก่อน... ไพ่ใบนี้ต้องเก็บไว้ ต้องหักคำพยานข้อนี้ด้วยหลักฐานอื่นให้ได้ก่อน") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_PaintChip,
                    line = new Line(Who.Judge, "ศาลรับเศษกันชนเป็นวัตถุพยานไปแล้ว ทนายโจทก์ มันหักล้างคำเบิกความตรงไหน") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_PaintCode,
                    line = new Line(Who.Defense, "ผลเทียบสีจากอู่ข้างถนน ไม่ใช่ผลตรวจของกองพิสูจน์หลักฐานครับท่าน") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_OwnerName,
                    line = new Line(Who.Defense, "ฝ่ายจำเลยไม่เคยปฏิเสธว่ารถเป็นของจำเลยครับ ทนายโจทก์ถามวนไปวนมา") },
                new WrongEvidenceRule { evidenceId = Act2Script.EV_DashcamMissing,
                    line = new Line(Who.Defense, "กล้องหน้ารถถอดไปซ่อม มันผิดกฎหมายข้อไหนครับ") },
            };
            wrongFallback = new Line(Who.Judge, "ทนายโจทก์ หลักฐานชิ้นนี้ไม่เกี่ยวกับประเด็น");
            wrongInTestimony = new Line(Who.Defense, "ท่านครับ หลักฐานชิ้นนั้นไม่ได้ขัดกับคำเบิกความข้อนี้เลย ทนายโจทก์กำลังเดาสุ่ม");
            wrongInTestimonyJudge = new Line(Who.Judge, "ทนายโจทก์ ถามให้ตรงประเด็น");

            credibilityLost = new[]
            {
                new Line(Who.Judge, "ทนายโจทก์ ถ้ายังไม่มีหลักฐานที่เกี่ยวข้อง ศาลจะงดการซักค้าน"),
                new Line(Who.KhemThink, "ใจเย็น... อ่านคำเบิกความอีกรอบ มันต้องมีจุดที่ขัดกับหลักฐาน"),
            };
        }

        // สำเนาแยกตัวจากค่าเริ่มต้น (ผ่าน JSON) แก้ใน asset แล้วไม่ไปกระทบค่าใน TrialScript
        static TObj Copy<TObj>(TObj o) where TObj : class
        {
            return o == null ? null : JsonUtility.FromJson<TObj>(JsonUtility.ToJson(o));
        }

        static TObj[] Copy<TObj>(TObj[] a) where TObj : class
        {
            if (a == null) return null;
            var r = new TObj[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = Copy(a[i]);
            return r;
        }
    }

    // ───────────────────────── กลุ่มข้อมูลย่อย (พับได้ใน Inspector) ─────────────────────────

    [Serializable]
    public class CredibilityRules
    {
        [Range(0f, 100f)] public float start = 50f;
        [Tooltip("ตอบถูก / หักล้างคำเบิกความสำเร็จ")]
        public int correctGain = 15;
        [Tooltip("แสดงหลักฐานผิดชิ้น")]
        public int wrongLoss = 15;
        [Tooltip("แสดงหลักฐานที่ได้มาไม่ชอบ (เสียงแอบฟัง)")]
        public int heavyLoss = 25;
        [Tooltip("ศาลรับคำคัดค้านของเรา")]
        public int sustainedGain = 8;
        [Tooltip("คัดค้านด้วยเหตุที่ผิด (คำถามควรค้านแต่เลือกเหตุผิด)")]
        public int wrongGroundPenalty = 5;
        [Tooltip("คัดค้านคำถามที่ชอบอยู่แล้ว ศาลยก")]
        public int overruledPenalty = 10;
        [Tooltip("ความน่าเชื่อถือหมดหลอดแล้ว ศาลเตือน รีเซ็ตกลับมาเท่านี้")]
        [Range(0f, 100f)] public float afterReset = 30f;
        [Tooltip("หลังเปิดคลิป — ผู้เล่นรู้สึกชนะ")]
        [Range(0f, 100f)] public float afterClip = 100f;
        [Tooltip("หลังทนายจำเลยเปิดเผยว่าได้หลักฐานมาโดยมิชอบ")]
        [Range(0f, 100f)] public float afterTwist = 15f;
        [Tooltip("หลังศาลพิพากษายกฟ้อง")]
        [Range(0f, 100f)] public float afterVerdict = 0f;
    }

    [Serializable]
    public class CourtIntro
    {
        public string cardSmall = "ACT 4";
        public string cardBig = "ความยุติธรรมที่ถูกซื้อ";
        public string cardSub = "ห้องพิจารณาคดี  ·  เช้าวันพิพากษา";
        [Min(0.5f)] public float cardHold = 2.2f;
        [Tooltip("ช็อตกว้างทั้งห้อง")]
        public Line[] roomShot =
        {
            new Line(Who.Narrator, "ห้องพิจารณาคดีแน่นขนัด ฝั่งหนึ่งคือชาวบ้านที่มาให้กำลังใจป้าสมร อีกฝั่งคือคนของแชมป์"),
        };
        [Tooltip("ช็อตแชมป์ที่ที่นั่งจำเลย")]
        public Line[] champShot = { new Line(Who.Narrator, "แชมป์นั่งไขว่ห้าง ยิ้มเหมือนมาดูละคร") };
        [Tooltip("ขึ้นตอนผู้เล่นเดินได้แล้ว (ก่อนไปนั่งโต๊ะ)")]
        public Line[] exploring =
        {
            new Line(Who.KhemThink, "วันนี้แหละป้า... ผมจะลากคอมันเข้าคุกให้ได้ อย่างที่สัญญาไว้"),
            new Line(Who.KhemThink, "ก่อนศาลขึ้นบัลลังก์ ทวนแฟ้มคดีอีกรอบดีกว่า  (กด [Tab] เปิดแฟ้มคดี)"),
        };
    }

    [Serializable]
    public class CaseFileText
    {
        public string[] tabs = { "คดี", "หลักฐาน", "พยาน", "บันทึกศาล", "เหตุคัดค้าน" };
        [Tooltip("หน้าแรกของแฟ้ม (ใช้แท็ก <b> ได้)")]
        [TextArea(6, 14)] public string summary;
        [Tooltip("ขั้นตอนการพิจารณา — ต้องมี 5 ขั้นตามลำดับ (อ่านฟ้อง, แถลงเปิดคดี, พยานโจทก์, พยานจำเลย, พิพากษา)")]
        public string[] stages;
        [Tooltip("หลักฐานในแฟ้มคดี (ชื่อ/คำอธิบายที่โชว์ในเกม)")]
        public Act4Evidence[] evidence;
    }

    [Serializable]
    public class CourtPhrases
    {
        public Line sessionOpen = new Line(Who.Clerk, "ศาลขึ้นบัลลังก์ — ทุกคนในห้องโปรดลุกขึ้นยืน และทำความเคารพ");
        [Tooltip("{0} = พยานโจทก์ / พยานจำเลย")]
        public string callWitness = "เชิญ{0}เข้าคอกพยาน";
        public Line oathPrompt = new Line(Who.Clerk, "พยานยกมือขวาขึ้น แล้วกล่าวคำสาบานตามนะครับ");
        [TextArea(1, 3)]
        public string oath = "ข้าพเจ้าขอสาบานต่อหน้าศาลว่า จะเบิกความตามสัตย์จริงทุกประการ หากเบิกความเท็จ ขอให้มีอันเป็นไป";
        public Line askIdentity = new Line(Who.Judge, "พยานชื่ออะไร อายุเท่าไร ประกอบอาชีพอะไร");
        public Line dismiss = new Line(Who.Judge, "พยานกลับไปนั่งได้");
        public Line askRedirect = new Line(Who.Judge, "ทนายโจทก์ จะถามติงหรือไม่");
        public Line askCross = new Line(Who.Judge, "ทนายโจทก์ ถามค้าน");

        [Header("คัดค้าน")]
        [Tooltip("{0} = ชื่อเหตุคัดค้าน")]
        public string khemObjects = "คัดค้านครับท่าน! {0}";
        [Tooltip("{0} = ชื่อเหตุคัดค้าน")]
        public string defenseObjects = "คัดค้านครับท่าน! {0}";
        [Tooltip("ศาลให้เข้มถามใหม่หลังฝ่ายจำเลยคัดค้าน {0} = ประโยครับคำคัดค้านของเหตุนั้น")]
        public string askAgain = "{0} ทนายโจทก์ ถามใหม่";
        public string wrongGround = "คัดค้านด้วยเหตุนั้นไม่ได้ ทนายโจทก์ พยานตอบ";
        public string overruledLeadingOnCross = "ยกคำคัดค้าน — การถามค้านใช้คำถามนำได้ ทนายโจทก์";
        public string overruled = "ยกคำคัดค้าน คำถามชอบแล้ว พยานตอบ";
        [Tooltip("{0} = คำถามที่กำลังคัดค้าน")]
        public string groundChoiceTitle = "คัดค้านด้วยเหตุ... ({0})";
        public string withdraw = "ถอนคำคัดค้าน — ให้พยานตอบ";
        public string splashObjection = "คัดค้าน!";
        public string splashContradiction = "ขอค้าน!";
        public string missedObjectionToast = "คำถามนั้นน่าจะคัดค้านได้ — เปิด [Tab] ดูเหตุคัดค้าน";

        [Header("สอนผู้เล่นครั้งแรก")]
        public Line[] objectionTutorial =
        {
            new Line(Who.KhemThink, "ทีนี้ถึงตาทนายจำเลยถาม — คำถามไหนไม่ชอบด้วยกฎหมาย ต้องกด [Q] คัดค้านให้ทัน"),
            new Line(Who.KhemThink, "แต่ถ้าคัดค้านมั่ว ศาลจะยกคำคัดค้าน แล้วความน่าเชื่อถือเราจะหาย (ดูเหตุคัดค้านได้ในแฟ้มคดี [Tab])"),
        };
        public Line[] testimonyTutorial =
        {
            new Line(Who.KhemThink, "ฟังคำเบิกความทีละข้อ ข้อไหนคลุมเครือ กด [Q] ซักไซ้ให้มันพูดออกมาอีก"),
            new Line(Who.KhemThink, "ข้อไหนขัดกับหลักฐานในแฟ้ม กด [R] แสดงหลักฐานค้าน — แต่ถ้าค้านผิดข้อ ศาลจะเริ่มไม่เชื่อเรา"),
        };

        [Header("ถามค้าน / บันทึก / หมายพยาน")]
        public Line pressLead = new Line(Who.Khem, "ขอถามพยานเรื่องนี้ครับ");
        public string revealToast = "คำเบิกความเพิ่มขึ้น 1 ข้อ";
        public Line firstRecordNarration = new Line(Who.Narrator, "ผู้พิพากษาโน้มตัวเข้าหาไมโครโฟน สรุปคำเบิกความให้เครื่องบันทึก");
        public string recordSpokenPrefix = "บันทึก... ";
        public string recordToastPrefix = "ศาลบันทึก — ";
        [Tooltip("{0} = หมายเลข เช่น จ.1, {1} = ชื่อหลักฐาน")]
        public string exhibitToast = "ศาลหมายเป็นพยานหลักฐาน {0} — {1}";

        [Header("จบการสืบพยาน")]
        public Line afterTrial = new Line(Who.KhemThink, "ถึงเวลาแล้ว... เดินไปที่จอ เปิดคลิปให้ทั้งศาลดู");
    }

    [Serializable]
    public class VerdictText
    {
        [Header("The Climax — เปิดคลิป")]
        public Line clipIntro = new Line(Who.Khem, "นี่คือภาพจากกล้องหน้ารถของจำเลย คืนวันที่ 14 เวลาตีหนึ่งสี่สิบเจ็ดนาที");
        [Tooltip("เสียงแชมป์ในคลิป ขึ้นหลังรถชน")]
        public Line clipVoice = new Line("เสียงในคลิป — แชมป์", "เชี่ย! ...ช่างแม่ง ไม่มีใครเห็นหรอก ไป ๆ ๆ", 2.6f);
        public Line[] auntJoy = { new Line(Who.Aunt, "นั่นไง... นั่นไงลูกแม่..."), new Line(Who.Aunt, "ฟ้ามีตาจริง ๆ") };
        public Line[] champPale = { new Line(Who.Narrator, "รอยยิ้มของแชมป์หายไป หน้าซีดเผือด") };
        public Line[] khemVictory = { new Line(Who.KhemThink, "จบแล้ว... คราวนี้มันหนีไม่รอดแน่") };

        [Header("The Twist")]
        public Line[] defenseRises = { new Line(Who.Narrator, "ทนายจำเลยค่อย ๆ ลุกขึ้น... แล้วยิ้ม") };
        public Line[] defenseQuestion =
        {
            new Line(Who.Defense, "ท่านครับ ฝ่ายจำเลยขอถามคำถามเดียว — SD Card ใบนี้ ทนายโจทก์ไปได้มาจากไหน?"),
        };
        public Line[] khemShock = { new Line(Who.KhemThink, "...!", 1.4f) };
        public Line[] defenseReveal =
        {
            new Line(Who.Defense, "ฝ่ายจำเลยมีภาพกล้องวงจรปิดของคอนโดจำเลย เห็นทนายโจทก์ลอบเข้าไปในห้องตอนตีสอง"),
            new Line(Who.Defense, "หลักฐานนี้ได้มาจากการบุกรุกเคหสถานยามวิกาลครับศาลที่เคารพ"),
        };

        [Header("The Ruling")]
        public Line inadmissible = new Line(Who.Judge, "พยานหลักฐานนี้... ได้มาโดยมิชอบ");
        [Tooltip("ตอนนี้จอทีวีดับเป็น NO SIGNAL")]
        public Line excluded = new Line(Who.Judge, "ศาลห้ามรับฟัง");
        [Tooltip("เคาะค้อนก่อนและหลังประโยคนี้")]
        public Line judgment = new Line(Who.Judge, "พิพากษา... ยกฟ้อง!", 1.6f);
        public Line[] auntCollapse = { new Line(Who.Narrator, "ป้าสมรทรุดลงนั่ง... นิ่งไปทั้งตัว") };

        [Header("The Aftermath")]
        public Line champSneer1 = new Line(Who.Champ, "กฎหมายมีไว้ขังหมากับคนจน...");
        public Line champSneer2 = new Line(Who.Champ, "จำไว้ ไอ้ทนาย");
        public Line[] emptyRoom = { new Line(Who.Narrator, "ไม่กี่นาทีต่อมา ห้องพิจารณาคดีก็ว่างเปล่า") };
        public Line[] khemAfter = { new Line(Who.KhemThink, "ป้าสมร... ป้าออกไปไหนแล้ว") };
    }
}
