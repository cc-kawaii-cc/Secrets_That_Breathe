using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// การพิจารณาคดีเต็มรูปแบบ (ACT 4.1) — เล่นตาม <see cref="Act4CourtroomData"/> ทีละขั้น
    ///
    ///   ศาลขึ้นบัลลังก์ -> อ่านฟ้อง/จำเลยให้การปฏิเสธ -> แถลงเปิดคดี (เลือกประเด็น)
    ///   -> สืบพยานโจทก์: เรียกพยาน สาบานตน ซักถาม (เลือกคำถาม ถ้าถามนำจะโดนคัดค้าน)
    ///      ทนายจำเลยถามค้าน (ผู้เล่นกด [Q] คัดค้าน + เลือกเหตุ) ถามติง
    ///   -> สืบพยานจำเลย: ทนายจำเลยซักถาม (ผู้เล่นคัดค้านคำถามนำ) แล้วผู้เล่นถามค้านทีละข้อ
    ///      [Q] ซักไซ้ / [R] แสดงหลักฐานค้าน
    ///   -> ข้อต่อสู้สุดท้าย -> แสดงหลักฐานชิ้นสุดท้าย
    ///
    /// ตลอดการพิจารณา ศาลหมายพยานหลักฐาน (จ.1, วจ.1 ...) และบันทึกคำเบิกความ ดูย้อนหลังได้ในแฟ้มคดี [Tab]
    /// </summary>
    public partial class CourtroomSequence
    {
        static readonly Color ObjectionRed = new Color(1f, 0.28f, 0.24f);

        readonly List<string> _record = new List<string>();
        readonly Dictionary<string, string> _exhibits = new Dictionary<string, string>();
        readonly Dictionary<string, int> _witnessState = new Dictionary<string, int>();   // 0 ยัง 1 กำลัง 2 เสร็จ
        readonly Dictionary<Transform, Pose> _seats = new Dictionary<Transform, Pose>();
        Transform[] _judges;
        Transform _onStand;
        string _standSpeaker;
        int _stage = -1;
        int _sustained, _overruled, _contradictions;
        bool _recordShown, _objectionTaught, _testimonyTaught, _missedTaught;

        // ───────────────────────── ลำดับการพิจารณา ─────────────────────────

        IEnumerator Trial(Transform seat)
        {
            var d = Data;
            BeginCutscene();
            Hud.CaseFileAllowed = false;
            PlacePlayerAt(seat);

            // ── ศาลขึ้นบัลลังก์: องค์คณะเดินออกจากประตูผู้พิพากษา ทุกคนลุกขึ้นยืน ──
            _stage = 0;
            Cut("CAM_Establishing", true);
            StartCoroutine(JudgesEnter());
            yield return Say(d.phrases.sessionOpen);
            yield return AllRise(true);
            while (!JudgesSeated()) yield return null;
            yield return new WaitForSeconds(0.4f);
            yield return AllRise(false);

            Cut("CAM_JudgesLowAngle", false);
            yield return PlayLines(d.arraignment);

            // จำเลยลุกขึ้นให้การ แล้วนั่งลงหลังพูดประโยคแรกของตัวเอง
            Cut("CAM_ChampDock", false);
            yield return Stance(_champ, standingHeight - 0.05f, 0.5f);
            bool sat = false;
            if (d.plea != null)
                foreach (var line in d.plea)
                {
                    ShotFor(line);
                    yield return Say(line);
                    if (!sat && line.who == Who.Champ) { sat = true; StartCoroutine(Stance(_champ, seatedHeight, 0.6f)); }
                }
            if (!sat) StartCoroutine(Stance(_champ, seatedHeight, 0.6f));
            if (!string.IsNullOrEmpty(d.pleaRecord)) yield return RecordLine(d.pleaRecord);

            _cred = d.credibility.start;
            Hud.ShowCredibility(true);
            Hud.SetCredibility(_cred, true);

            // ── แถลงเปิดคดี ──
            _stage = 1;
            if (d.opening != null)
                for (int i = 0; i < d.opening.Length; i++)
                {
                    KhemCloseUp();
                    yield return RunTurn(d.opening[i], false, null);
                }
            yield return PlayLines(d.afterOpening);

            // ── สืบพยานโจทก์: ผู้เล่นเลือกปากแรก ที่เหลือตามลำดับ ──
            _stage = 2;
            var plaintiffs = d.plaintiffWitnesses ?? new PlaintiffWitness[0];
            int first = 0;
            if (plaintiffs.Length > 1)
            {
                KhemCloseUp();
                var labels = new string[plaintiffs.Length];
                for (int i = 0; i < labels.Length; i++)
                    labels[i] = string.IsNullOrEmpty(plaintiffs[i].info.orderLabel) ? plaintiffs[i].info.speaker : plaintiffs[i].info.orderLabel;
                yield return Ask(d.plaintiffOrderTitle, labels, i => first = i);
            }
            if (plaintiffs.Length > 0) yield return PlaintiffOnStand(plaintiffs[first]);
            for (int i = 0; i < plaintiffs.Length; i++)
                if (i != first) yield return PlaintiffOnStand(plaintiffs[i]);
            yield return PlayLines(d.plaintiffRests);

            // ── สืบพยานจำเลย ──
            _stage = 3;
            if (d.defenseWitnesses != null)
                foreach (var dw in d.defenseWitnesses) yield return DefenseOnStand(dw);

            // ── ข้อต่อสู้สุดท้าย ──
            _stage = 4;
            yield return FinalClaim();

            Complete(Act4Script.OBJ_CrossExamine);
            EndCutscene();
            Hud.CaseFileAllowed = true;
            yield return Say(d.phrases.afterTrial);
        }

        // ───────────────────────── พยานแต่ละปาก ─────────────────────────

        IEnumerator PlaintiffOnStand(PlaintiffWitness pw)
        {
            var d = Data;
            yield return CallWitness(pw.info);

            Pick lastPick = null;
            var turns = pw.direct ?? new Turn[0];
            for (int i = 0; i < turns.Length; i++)
            {
                yield return RunTurn(turns[i], true, p => lastPick = p);
                if (i == pw.exhibitAfterTurn && pw.exhibit != null && !string.IsNullOrEmpty(pw.exhibit.evidenceId))
                    yield return ExhibitStep(pw.info, pw.exhibit);
            }
            if (!string.IsNullOrEmpty(pw.markAfterDirectEvidence) && lastPick != null && lastPick.cred > 0)
                Mark(pw.markAfterDirectEvidence, pw.markAfterDirectCode);

            if (pw.cross != null && pw.cross.Length > 0) yield return DefenseQuestions(pw.cross, true);

            if (pw.redirect != null && pw.redirect.picks != null && pw.redirect.picks.Length > 0)
            {
                ShotFor(d.phrases.askRedirect);
                yield return Say(d.phrases.askRedirect);
                yield return RunTurn(pw.redirect, true, null);
            }
            yield return Dismiss(pw.info);
        }

        /// <summary>ให้พยานดูวัตถุพยาน: ต้องเลือกหลักฐานให้ถูกชิ้น</summary>
        IEnumerator ExhibitStep(WitnessInfo w, ExhibitStep step)
        {
            while (true)
            {
                Cut("CAM_KhemOverShoulder", true);
                string chosen = "?";
                Hud.CaseFileAllowed = true;
                Hud.ShowCrossExam(w.speaker, step.prompt, CaseFileList(), id => chosen = id, false, EvidenceLabel);
                while (chosen == "?") yield return null;
                Hud.CaseFileAllowed = false;
                if (chosen == step.evidenceId)
                {
                    yield return PlayLines(step.onCorrect);
                    if (!string.IsNullOrEmpty(step.code)) Mark(step.evidenceId, step.code);
                    if (!string.IsNullOrEmpty(step.record)) yield return RecordLine(step.record);
                    yield break;
                }
                ShotFor(step.onWrong);
                yield return Say(step.onWrong);
                yield return ApplyCred(-step.wrongPenalty);
            }
        }

        IEnumerator DefenseOnStand(DefenseWitness dw)
        {
            var d = Data;
            yield return PlayLines(dw.beforeCall);
            yield return CallWitness(dw.info);
            if (dw.direct != null && dw.direct.Length > 0) yield return DefenseQuestions(dw.direct, false);
            if (dw.testimony != null && dw.testimony.statements != null && dw.testimony.statements.Length > 0)
            {
                ShotFor(d.phrases.askCross);
                yield return Say(d.phrases.askCross);
                yield return CrossTestimony(dw.testimony);
            }
            yield return PlayLines(dw.afterCross);
            yield return Dismiss(dw.info);
        }

        IEnumerator FinalClaim()
        {
            var f = Data.finalClaim;
            if (f == null || string.IsNullOrEmpty(f.evidenceId)) yield break;
            yield return DefenseStand();
            yield return PlayLines(f.defense);
            while (true)
            {
                KhemCloseUp();
                string chosen = "?";
                Hud.CaseFileAllowed = true;
                Hud.ShowCrossExam(Data.cast.defense, f.prompt, CaseFileList(), id => chosen = id, false, EvidenceLabel);
                while (chosen == "?") yield return null;
                Hud.CaseFileAllowed = false;

                if (chosen == f.evidenceId)
                {
                    Hud.Splash(Data.phrases.splashContradiction, ObjectionRed);
                    StartCoroutine(Shake(0.35f, 0.03f));
                    yield return PlayLines(f.onCorrect);
                    if (!string.IsNullOrEmpty(f.code)) Mark(f.evidenceId, f.code);
                    yield return ApplyCred(Data.credibility.correctGain);
                    break;
                }
                int penalty;
                var line = Data.ResolveWrong(chosen, true, out penalty);
                ShotFor(line);
                yield return Say(line);
                yield return ApplyCred(-penalty);
            }
            yield return DefenseSit();
        }

        // ───────────────────────── เรียกพยาน / สาบานตน ─────────────────────────

        IEnumerator CallWitness(WitnessInfo w)
        {
            var ph = Data.phrases;
            var actor = Find(w.actor);
            _witnessState[w.actor] = 1;
            Cut("CAM_Establishing", true);
            bool arrived = actor == null;
            if (actor != null)
            {
                _seats[actor] = new Pose(actor.position - Vector3.up * FeetOffset(actor), actor.rotation);
                StartCoroutine(WalkToStand(actor, () => arrived = true));
            }
            yield return Say(new Line(Who.Judge, string.Format(ph.callWitness, w.side)));
            while (!arrived) yield return null;

            _onStand = actor;
            _standSpeaker = w.speaker;
            Cut("CAM_Witness", false);
            yield return Say(ph.oathPrompt);
            yield return Say(new Line(Who.Witness, ph.oath));
            ShotFor(ph.askIdentity);
            yield return Say(ph.askIdentity);
            Cut("CAM_Witness", false);
            yield return Say(new Line(Who.Witness, w.identity));
            if (!string.IsNullOrEmpty(w.record)) yield return RecordLine(w.record);
        }

        IEnumerator Dismiss(WitnessInfo w)
        {
            _witnessState[w.actor] = 2;
            var actor = _onStand;
            ShotFor(Data.phrases.dismiss);
            yield return Say(Data.phrases.dismiss);
            _onStand = null;
            _standSpeaker = null;
            if (actor != null) StartCoroutine(WalkBackToSeat(actor));
        }

        IEnumerator WalkToStand(Transform actor, System.Action done)
        {
            yield return Stance(actor, standingHeight - 0.05f, 0.5f);
            Vector3 f = actor.position - Vector3.up * FeetOffset(actor);
            float laneZ = f.z + 0.35f;
            var pts = new[]
            {
                new Vector3(f.x, 0f, laneZ), new Vector3(0f, 0f, laneZ), new Vector3(0f, 0f, -1.5f),
                new Vector3(0f, 0f, 0.25f), new Vector3(0f, 0.15f, 1.15f),
            };
            for (int i = 0; i < pts.Length; i++) yield return WalkTo(actor, pts[i], witnessWalkSpeed);
            yield return Face(actor, actor.position + Vector3.forward, 0.4f);
            done();
        }

        IEnumerator WalkBackToSeat(Transform actor)
        {
            Pose seat;
            if (!_seats.TryGetValue(actor, out seat)) yield break;
            float laneZ = seat.position.z + 0.35f;
            var pts = new[]
            {
                new Vector3(0f, 0f, 0.25f), new Vector3(0f, 0f, -1.5f), new Vector3(0f, 0f, laneZ),
                new Vector3(seat.position.x, 0f, laneZ), seat.position,
            };
            for (int i = 0; i < pts.Length; i++) yield return WalkTo(actor, pts[i], witnessWalkSpeed + 0.1f);
            actor.rotation = seat.rotation;
            yield return Stance(actor, seatedHeight, 0.5f);
        }

        // ───────────────────────── เลือก (แถลง / ซักถาม / ถามติง) ─────────────────────────

        IEnumerator Ask(string title, string[] options, System.Action<int> result)
        {
            int idx = -1;
            Hud.CaseFileAllowed = true;
            Hud.ShowChoice(title, options, i => idx = i);
            while (idx < 0) yield return null;
            Hud.CaseFileAllowed = false;
            result(idx);
        }

        /// <summary>
        /// หนึ่งรอบเลือกคำถาม/คำแถลง ถ้าเลือกข้อที่ผิดวิธี ฝ่ายจำเลยคัดค้าน ศาลให้ถามใหม่ ข้อนั้นหายไปแล้วเลือกใหม่
        /// </summary>
        IEnumerator RunTurn(Turn turn, bool witnessShot, System.Action<Pick> result)
        {
            if (turn == null || turn.picks == null || turn.picks.Length == 0) yield break;
            var remaining = new List<Pick>(turn.picks);
            while (remaining.Count > 0)
            {
                if (witnessShot) Cut("CAM_KhemOverShoulder", true);
                var labels = new string[remaining.Count];
                for (int i = 0; i < labels.Length; i++) labels[i] = remaining[i].label;
                int idx = -1;
                yield return Ask(turn.title, labels, i => idx = i);

                var p = remaining[idx];
                yield return PlayLines(p.lines);
                if (p.objection != Ground.None)
                {
                    yield return DefenseObjects(p.objection);
                    yield return ApplyCred(p.cred);
                    remaining.RemoveAt(idx);
                    continue;
                }
                yield return ApplyCred(p.cred);
                if (!string.IsNullOrEmpty(p.record)) yield return RecordLine(p.record);
                if (result != null) result(p);
                yield break;
            }
        }

        /// <summary>ทนายจำเลยคัดค้านคำถามของเข้ม ศาลรับ</summary>
        IEnumerator DefenseObjects(Ground g)
        {
            var ph = Data.phrases;
            Cut("CAM_DefenseLawyer", false);
            Hud.Splash(ph.splashObjection, ObjectionRed);
            StartCoroutine(Shake(0.25f, 0.02f));
            yield return Say(new Line(Who.Defense, string.Format(ph.defenseObjects, Data.GroundName(g))));
            Cut("CAM_JudgesLowAngle", false);
            yield return Say(new Line(Who.Judge, string.Format(ph.askAgain, Data.Sustained(g))));
        }

        // ───────────────────────── ทนายจำเลยถาม — ผู้เล่นคัดค้าน ─────────────────────────

        Ground[] PlayerGrounds()
        {
            var list = new List<Ground>();
            if (Data.grounds != null) foreach (var g in Data.grounds) if (g.ground != Ground.None) list.Add(g.ground);
            return list.ToArray();
        }

        /// <param name="isCross">true = ทนายจำเลยถามค้านพยานโจทก์ (คำถามนำใช้ได้) / false = ซักถามพยานตัวเอง</param>
        IEnumerator DefenseQuestions(DefenseQ[] qs, bool isCross)
        {
            var d = Data;
            var ph = d.phrases;
            var c = d.credibility;
            yield return DefenseStand();
            if (!_objectionTaught)
            {
                _objectionTaught = true;
                KhemCloseUp();
                yield return SayAll(ph.objectionTutorial);
            }

            var grounds = PlayerGrounds();
            for (int qi = 0; qi < qs.Length; qi++)
            {
                var q = qs[qi];
                Cut("CAM_DefenseLawyer", false);
                Hud.ShowLine(d.cast.defense, q.question, false);
                Hud.ShowObjectionPrompt(true);
                float window = d.objectionSeconds + q.question.Length * d.objectionSecondsPerChar, left = window;
                bool objected = false;
                while (left > 0f)
                {
                    left -= Time.deltaTime;
                    Hud.SetObjectionTimer(left / window);
                    if (Hud.ObjectionPressed()) { objected = true; break; }
                    yield return null;
                }
                Hud.ShowObjectionPrompt(false);

                Ground g = Ground.None;
                if (objected)
                {
                    Hud.Splash(ph.splashObjection, ObjectionRed);
                    StartCoroutine(Shake(0.3f, 0.025f));
                    KhemCloseUp();
                    var names = new string[grounds.Length + 1];
                    for (int i = 0; i < grounds.Length; i++) names[i] = d.GroundName(grounds[i]);
                    names[names.Length - 1] = ph.withdraw;
                    int pick = -1;
                    Hud.HideLine();
                    yield return Ask(string.Format(ph.groundChoiceTitle, q.question), names, i => pick = i);
                    if (pick < grounds.Length) g = grounds[pick];
                }
                Hud.HideLine();

                bool answer = true;
                if (g != Ground.None)
                {
                    yield return Say(new Line(Who.Khem, string.Format(ph.khemObjects, d.GroundName(g))));
                    Cut("CAM_JudgesLowAngle", false);
                    if (q.grounds != null && System.Array.IndexOf(q.grounds, g) >= 0)
                    {
                        _sustained++;
                        yield return Say(new Line(Who.Judge, string.IsNullOrEmpty(q.ruling) ? d.Sustained(g) : q.ruling));
                        yield return ApplyCred(c.sustainedGain);
                        if (!string.IsNullOrEmpty(q.rephrase))
                        {
                            Cut("CAM_DefenseLawyer", false);
                            yield return Say(new Line(Who.Defense, q.rephrase));
                        }
                        else answer = false;
                    }
                    else if (q.grounds != null && q.grounds.Length > 0)
                    {
                        _overruled++;
                        yield return Say(new Line(Who.Judge, ph.wrongGround));
                        yield return ApplyCred(-c.wrongGroundPenalty);
                    }
                    else
                    {
                        _overruled++;
                        yield return Say(new Line(Who.Judge, isCross && g == Ground.Leading ? ph.overruledLeadingOnCross : ph.overruled));
                        yield return ApplyCred(-c.overruledPenalty);
                    }
                }
                else if (q.grounds != null && q.grounds.Length > 0)
                {
                    // ปล่อยให้คำถามที่ควรคัดค้านผ่านไป
                    yield return ApplyCred(-q.penalty);
                    if (!_missedTaught)
                    {
                        _missedTaught = true;
                        Hud.Toast(ph.missedObjectionToast, 4f);
                    }
                }

                if (answer)
                {
                    yield return PlayLines(q.answer);
                    if (!string.IsNullOrEmpty(q.record)) yield return RecordLine(q.record);
                }
            }
            yield return DefenseSit();
        }

        // ───────────────────────── ถามค้านคำเบิกความ ─────────────────────────

        IEnumerator CrossTestimony(Testimony t)
        {
            var d = Data;
            var ph = d.phrases;
            var visible = new List<int>();
            for (int i = 0; i < t.statements.Length; i++) if (!t.statements[i].hidden) visible.Add(i);
            if (visible.Count == 0) yield break;
            var pressed = new HashSet<int>();
            var fresh = new HashSet<int>();
            int cur = 0;

            if (!_testimonyTaught)
            {
                _testimonyTaught = true;
                KhemCloseUp();
                yield return SayAll(ph.testimonyTutorial);
            }

            while (true)
            {
                Cut("CAM_KhemOverShoulder", true);
                int idx = visible[cur];
                var st = t.statements[idx];
                Hud.CaseFileAllowed = true;
                Hud.ShowTestimony(t.title, _standSpeaker, st.text, cur, visible.Count, pressed.Contains(idx), fresh.Contains(idx));

                var input = Act4HUD.TestimonyInput.None;
                while (input == Act4HUD.TestimonyInput.None)
                {
                    yield return null;
                    input = Hud.PollTestimony();
                }

                if (input == Act4HUD.TestimonyInput.Prev || input == Act4HUD.TestimonyInput.Next)
                {
                    fresh.Remove(idx);
                    cur = (cur + (input == Act4HUD.TestimonyInput.Next ? 1 : -1) + visible.Count) % visible.Count;
                    continue;
                }

                Hud.HideTestimony();
                Hud.CaseFileAllowed = false;

                if (input == Act4HUD.TestimonyInput.Press)
                {
                    fresh.Remove(idx);
                    KhemCloseUp();
                    yield return Say(ph.pressLead);
                    yield return PlayLines(st.press);
                    if (pressed.Add(idx) && st.pressCred != 0) yield return ApplyCred(st.pressCred);
                    if (st.reveals >= 0 && st.reveals < t.statements.Length && !visible.Contains(st.reveals))
                    {
                        visible.Insert(cur + 1, st.reveals);
                        fresh.Add(st.reveals);
                        cur++;
                        Hud.Toast(ph.revealToast, 3f);
                    }
                    continue;
                }

                // แสดงหลักฐานค้าน
                string chosen = "?";
                Hud.CaseFileAllowed = true;
                Hud.ShowCrossExam(_standSpeaker, st.text, CaseFileList(), id => chosen = id, true, EvidenceLabel);
                while (chosen == "?") yield return null;
                Hud.CaseFileAllowed = false;
                if (chosen == null) continue;

                if (!string.IsNullOrEmpty(st.contradiction) && chosen == st.contradiction)
                {
                    _contradictions++;
                    Hud.Splash(ph.splashContradiction, ObjectionRed);
                    StartCoroutine(Shake(0.4f, 0.035f));
                    KhemCloseUp();
                    yield return PlayLines(st.broken);
                    if (!string.IsNullOrEmpty(st.exhibit)) Mark(chosen, st.exhibit);
                    yield return ApplyCred(d.credibility.correctGain);
                    if (!string.IsNullOrEmpty(st.record)) yield return RecordLine(st.record);
                    yield break;
                }

                int penalty;
                var line = d.ResolveWrong(chosen, false, out penalty);
                ShotFor(line);
                yield return Say(line);
                if (penalty > 0)
                {
                    ShotFor(d.wrongInTestimonyJudge);
                    yield return Say(d.wrongInTestimonyJudge);
                    yield return ApplyCred(-penalty);
                }
            }
        }

        // ───────────────────────── ทนายจำเลย ลุก/นั่ง ─────────────────────────

        Pose _defenseSeat;
        bool _defenseStanding, _defenseSeatKnown;

        IEnumerator DefenseStand()
        {
            if (_defense == null || _defenseStanding) yield break;
            _defenseStanding = true;
            if (!_defenseSeatKnown)
            {
                _defenseSeat = new Pose(_defense.position - Vector3.up * FeetOffset(_defense), _defense.rotation);
                _defenseSeatKnown = true;
            }
            var mark = Find("MARK_DefenseStand");
            yield return Stance(_defense, standingHeight, 0.4f);
            if (mark != null)
            {
                yield return WalkTo(_defense, mark.position, defenseWalkSpeed);
                _defense.rotation = mark.rotation;
            }
        }

        IEnumerator DefenseSit()
        {
            if (_defense == null || !_defenseStanding) yield break;
            _defenseStanding = false;
            yield return WalkTo(_defense, _defenseSeat.position, defenseWalkSpeed);
            _defense.rotation = _defenseSeat.rotation;
            yield return Stance(_defense, seatedHeight, 0.4f);
        }

        // ───────────────────────── องค์คณะ / ลุกยืน ─────────────────────────

        void HideJudgesUntilSession()
        {
            _judges = new[] { Find("Judge_Left"), Find("Judge_Presiding"), Find("Judge_Right") };
            foreach (var j in _judges) if (j != null) { _seats[j] = new Pose(j.position - Vector3.up * FeetOffset(j), j.rotation); j.gameObject.SetActive(false); }
        }

        int _judgesSeated;
        bool JudgesSeated() { return _judges == null || _judgesSeated >= _judges.Length; }

        IEnumerator JudgesEnter()
        {
            if (_judges == null) yield break;
            var door = Find("SHUT_JudgesDoor");
            Vector3 d = door != null ? door.position : new Vector3(6.4f, 0.9f, 9.4f);
            for (int i = 0; i < _judges.Length; i++)
            {
                var j = _judges[i];
                if (j == null) { _judgesSeated++; continue; }
                StartCoroutine(JudgeWalk(j, d, i * 0.9f));
            }
        }

        IEnumerator JudgeWalk(Transform j, Vector3 door, float delay)
        {
            yield return new WaitForSeconds(delay);
            Pose seat = _seats[j];
            j.gameObject.SetActive(true);
            PlaceActorFeet(j, new Vector3(door.x, seat.position.y, door.z));
            yield return WalkTo(j, new Vector3(door.x, seat.position.y, 7.7f), judgeWalkSpeed);
            yield return WalkTo(j, new Vector3(seat.position.x, seat.position.y, 7.7f), judgeWalkSpeed);
            yield return WalkTo(j, seat.position, judgeWalkSpeed);
            j.rotation = seat.rotation;
            _judgesSeated++;
        }

        /// <summary>ทุกคนในห้องลุกขึ้นยืนทำความเคารพ (และนั่งลงเมื่อองค์คณะนั่งแล้ว)</summary>
        IEnumerator AllRise(bool up)
        {
            var actors = Find("50_ACTORS");
            if (actors == null) yield break;
            foreach (Transform a in actors)
            {
                if (!a.gameObject.activeInHierarchy) continue;
                string n = a.name;
                bool seatedPerson = n.StartsWith("Gallery_") || n.StartsWith("Witness_") || n == "Aunt_Samorn"
                                    || n == "Champ" || n.StartsWith("Defense_") || n == "Clerk";
                if (!seatedPerson) continue;
                StartCoroutine(Stance(a, up ? standingHeight - 0.08f : seatedHeight, 0.6f));
            }
            yield return new WaitForSeconds(0.7f);
        }

        // ───────────────────────── ความน่าเชื่อถือ / บันทึก / หมายพยาน ─────────────────────────

        IEnumerator ApplyCred(int delta)
        {
            if (delta == 0) yield break;
            _cred = Mathf.Clamp(_cred + delta, 0f, 100f);
            Hud.SetCredibility(_cred);
            if (_cred > 0f) yield break;

            yield return PlayLines(Data.credibilityLost);
            _cred = Data.credibility.afterReset;
            Hud.SetCredibility(_cred);
        }

        /// <summary>
        /// ศาลบันทึกคำเบิกความ — ศาลไทยให้ผู้พิพากษาสรุปคำเบิกความใส่ไมโครโฟนเข้าเครื่องบันทึก
        /// ครั้งแรกเล่นให้เห็นเต็ม ๆ หลังจากนั้นขึ้นเป็นข้อความสั้นมุมบน (เก็บไว้ในแฟ้มคดีทั้งหมด)
        /// </summary>
        IEnumerator RecordLine(string text)
        {
            var ph = Data.phrases;
            _record.Add(text);
            if (!_recordShown)
            {
                _recordShown = true;
                Cut("CAM_JudgesLowAngle", false);
                yield return Say(ph.firstRecordNarration);
                yield return Say(new Line(Who.Judge, ph.recordSpokenPrefix + text));
                yield break;
            }
            Hud.Toast(ph.recordToastPrefix + text, 4f);
        }

        void Mark(string evidenceId, string code)
        {
            _exhibits[evidenceId] = code;
            var ev = Data.FindEvidence(evidenceId);
            Hud.Toast(string.Format(Data.phrases.exhibitToast, code, ev != null ? ev.name : evidenceId), 4f);
        }

        string EvidenceLabel(Act4Evidence ev)
        {
            string code;
            return _exhibits.TryGetValue(ev.id, out code) ? ev.name + "  <color=#EEC76B>[" + code + "]</color>" : ev.name;
        }

        // ───────────────────────── บทพูดทั้งชุด ─────────────────────────

        IEnumerator PlayLines(Line[] lines)
        {
            if (lines == null) yield break;
            for (int i = 0; i < lines.Length; i++)
            {
                ShotFor(lines[i]);
                yield return Say(lines[i]);
            }
        }

        // ───────────────────────── แฟ้มคดี ─────────────────────────

        void SetupCaseFile()
        {
            Hud.ConfigureCaseFile(Data.caseFile.tabs, CaseFilePage);
        }

        string CaseFilePage(int tab)
        {
            var d = Data;
            var sb = new StringBuilder();
            switch (tab)
            {
                case 0:
                    sb.Append(d.caseFile.summary).Append("\n\n<b>ขั้นตอนการพิจารณา</b>\n");
                    var stages = d.caseFile.stages ?? new string[0];
                    for (int i = 0; i < stages.Length; i++)
                    {
                        string c = i < _stage ? "#6FD39A" : (i == _stage ? "#EEC76B" : "#8FA0B8");
                        string tag = i < _stage ? "เสร็จแล้ว" : (i == _stage ? "กำลังดำเนินการ" : "");
                        sb.Append("<color=").Append(c).Append(">").Append(i + 1).Append(". ").Append(stages[i])
                          .Append(tag.Length > 0 ? "   (" + tag + ")" : "").Append("</color>\n");
                    }
                    if (_stage >= 1)
                        sb.Append("\n<size=85%><color=#8FA0B8>คัดค้านสำเร็จ ").Append(_sustained)
                          .Append(" ครั้ง  ·  ถูกยกคำคัดค้าน ").Append(_overruled)
                          .Append(" ครั้ง  ·  หักล้างคำเบิกความ ").Append(_contradictions).Append(" ครั้ง</color></size>");
                    break;

                case 1:
                    if (d.caseFile.evidence != null)
                        foreach (var ev in d.caseFile.evidence)
                        {
                            if (Director != null && !Director.HasEvidence(ev.id)) continue;
                            string code;
                            sb.Append("<b>").Append(ev.name).Append("</b>");
                            if (_exhibits.TryGetValue(ev.id, out code)) sb.Append("   <color=#EEC76B>[ศาลหมาย ").Append(code).Append("]</color>");
                            sb.Append("\n<size=85%><color=#C9D2E0>").Append(ev.desc).Append("</color></size>\n\n");
                        }
                    break;

                case 2:
                    sb.Append("<b>พยานโจทก์</b>\n");
                    if (d.plaintiffWitnesses != null) foreach (var p in d.plaintiffWitnesses) WitnessRow(sb, p.info);
                    sb.Append("\n<b>พยานจำเลย</b>\n");
                    if (d.defenseWitnesses != null) foreach (var w in d.defenseWitnesses) WitnessRow(sb, w.info);
                    break;

                case 3:
                    if (_record.Count == 0) sb.Append("<color=#8FA0B8>ยังไม่มีคำเบิกความที่ศาลบันทึก</color>");
                    int start = Mathf.Max(0, _record.Count - 13);
                    for (int i = start; i < _record.Count; i++)
                        sb.Append("<color=#8FA0B8>").Append(i + 1).Append(".</color>  ").Append(_record[i]).Append('\n');
                    break;

                case 4:
                    sb.Append("<size=90%>กด <b>[Q]</b> ตอนทนายจำเลยถาม แล้วเลือกเหตุให้ถูก — คัดค้านถูก ศาลรับ / คัดค้านมั่ว ศาลยก</size>\n\n");
                    foreach (var g in PlayerGrounds())
                        sb.Append("<b>").Append(d.GroundName(g)).Append("</b>\n<size=85%><color=#C9D2E0>")
                          .Append(d.GroundHelp(g)).Append("</color></size>\n\n");
                    break;
            }
            return sb.ToString();
        }

        void WitnessRow(StringBuilder sb, WitnessInfo w)
        {
            if (w == null) return;
            int st;
            _witnessState.TryGetValue(w.actor ?? "", out st);
            string status = st == 2 ? "<color=#6FD39A>เบิกความแล้ว</color>"
                          : st == 1 ? "<color=#EEC76B>กำลังเบิกความ</color>" : "<color=#8FA0B8>ยังไม่เบิกความ</color>";
            sb.Append("  ").Append(w.speaker).Append("   ").Append(status)
              .Append("\n  <size=85%><color=#C9D2E0>").Append(w.note).Append("</color></size>\n");
        }
    }
}
