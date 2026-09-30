using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ส่วนเสริมของ Act4HUD สำหรับการพิจารณาคดีเต็มรูปแบบ
    ///
    ///   ตัวเลือก      — แถลงเปิดคดี / เลือกคำถามซักถาม / เลือกเหตุคัดค้าน
    ///   คัดค้าน       — หน้าต่างเวลาสั้น ๆ ระหว่างทนายจำเลยถาม กด [Q] ให้ทัน
    ///   คำเบิกความ    — ไล่ดูทีละข้อ [A/D] ซักไซ้ [Q] แสดงหลักฐาน [R]
    ///   แฟ้มคดี [Tab] — สรุปคดี / หลักฐาน / พยาน / บันทึกคำเบิกความ / เหตุคัดค้าน
    ///   ป้ายใหญ่ "คัดค้าน!" + ข้อความสั้นมุมบน (ศาลหมายพยานหลักฐาน ฯลฯ)
    /// </summary>
    public partial class Act4HUD
    {
        public enum TestimonyInput { None, Prev, Next, Press, Present }

        // ── ตัวเลือก ──
        CanvasGroup _choiceGroup;
        TextMeshProUGUI _choiceTitle, _choiceHint;
        RectTransform _choiceBox;
        readonly List<Row> _choiceRows = new List<Row>();
        Action<int> _onChoice;
        int _choiceSel;
        bool _choiceOpen;

        // ── คัดค้าน ──
        CanvasGroup _objGroup;
        Image _objFill, _objBg;
        RectTransform _objRt;

        // ── คำเบิกความ ──
        CanvasGroup _tGroup;
        TextMeshProUGUI _tTitle, _tIndex, _tText, _tSpeaker;
        RectTransform _tPrev, _tNext, _tPress, _tPresent;
        Image _tPressBg, _tPresentBg;
        bool _tOpen;

        // ── ป้าย ──
        TextMeshProUGUI _splash, _toast;
        float _splashT, _toastT, _toastLen;

        // ── แฟ้มคดี ──
        CanvasGroup _fileGroup;
        RectTransform _fileBox;
        readonly List<Canvas> _hudHiddenByFile = new List<Canvas>();
        TextMeshProUGUI _fileBody;
        readonly List<Row> _fileTabs = new List<Row>();
        string[] _fileTabNames;
        Func<int, string> _fileProvider;
        int _fileTab;
        bool _fileOpen, _fileTookControl;
        bool _cursorShown;

        /// <summary>เปิดแฟ้มคดีด้วย Tab ได้ตอนนี้หรือไม่ (sequence เป็นคนตั้ง)</summary>
        public bool CaseFileAllowed { get; set; }
        public bool CaseFileOpen { get { return _fileOpen; } }
        public bool ChoiceOpen { get { return _choiceOpen; } }
        public int ChoiceCount { get { return _choiceOpen ? _choiceRows.Count : 0; } }
        public bool TestimonyOpen { get { return _tOpen; } }
        /// <summary>ข้อคำเบิกความที่กำลังโชว์อยู่</summary>
        public string TestimonyText { get; private set; }
        public bool ObjectionPromptVisible { get { return _objGroup != null && _objGroup.alpha > 0.5f; } }

        static readonly Color RowIdle = new Color(1f, 1f, 1f, 0.06f);
        static readonly Color RowHover = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color RowSelected = new Color(0.93f, 0.78f, 0.42f, 0.32f);
        static readonly Color Crimson = new Color(0.45f, 0.06f, 0.07f, 0.95f);
        static readonly Color CrimsonHot = new Color(0.66f, 0.10f, 0.10f, 1f);

        // ───────────────────────── ตัวเลือก ─────────────────────────

        public void ShowChoice(string title, string[] options, Action<int> onPick)
        {
            _onChoice = onPick;
            _choiceTitle.text = title;
            for (int i = 0; i < _choiceRows.Count; i++) Destroy(_choiceRows[i].rt.gameObject);
            _choiceRows.Clear();

            float h = 58f;
            _choiceBox.sizeDelta = new Vector2(1180f, 90f + options.Length * h + 40f);
            for (int i = 0; i < options.Length; i++)
            {
                var bg = Solid(_choiceBox, "Option_" + i, new Vector2(0.5f, 1f), new Vector2(0f, -86f - i * h),
                               new Vector2(1120f, h - 6f), RowIdle);
                var label = Text((RectTransform)bg.transform, "Label", new Vector2(0f, 0.5f), new Vector2(22f, 0f),
                                 new Vector2(1080f, h - 8f), 27f, TextAlignmentOptions.MidlineLeft, Ink);
                label.text = (i + 1) + ".  " + options[i];
                _choiceRows.Add(new Row { rt = (RectTransform)bg.transform, bg = bg, label = label });
            }
            _choiceSel = 0;
            RefreshRows(_choiceRows, _choiceSel, -1);
            _choiceGroup.alpha = 1f;
            _choiceOpen = true;
        }

        public void HideChoice()
        {
            _choiceGroup.alpha = 0f;
            _choiceOpen = false;
            _onChoice = null;
        }

        void UpdateChoiceInput()
        {
            if (!_choiceOpen || _fileOpen || _choiceRows.Count == 0) return;
            int hover = -1;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                for (int i = 0; i < _choiceRows.Count; i++)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(_choiceRows[i].rt, mp, null)) continue;
                    hover = i;
                    if (mouse.leftButton.wasPressedThisFrame) { Choose(i); return; }
                }
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                    _choiceSel = (_choiceSel - 1 + _choiceRows.Count) % _choiceRows.Count;
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                    _choiceSel = (_choiceSel + 1) % _choiceRows.Count;
                for (int i = 0; i < _choiceRows.Count && i < 9; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame) { Choose(i); return; }
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                { Choose(_choiceSel); return; }
            }
            RefreshRows(_choiceRows, _choiceSel, hover);
        }

        void Choose(int i)
        {
            var cb = _onChoice;
            HideChoice();
            if (cb != null) cb(i);
        }

        static void RefreshRows(List<Row> rows, int selected, int hover)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].bg.color = i == selected ? RowSelected : (i == hover ? RowHover : RowIdle);
                rows[i].label.color = i == selected ? Gold : Ink;
            }
        }

        // ───────────────────────── คัดค้าน ─────────────────────────

        public void ShowObjectionPrompt(bool on)
        {
            _objGroup.alpha = on ? 1f : 0f;
            if (on) SetObjectionTimer(1f);
        }

        public void SetObjectionTimer(float remaining01)
        {
            var rt = (RectTransform)_objFill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(remaining01), rt.anchorMax.y);
            // กะพริบถี่ขึ้นเมื่อใกล้หมดเวลา
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(18f, 6f, remaining01));
            _objBg.color = Color.Lerp(Crimson, CrimsonHot, pulse);
        }

        /// <summary>กด [Q] หรือคลิกป้ายคัดค้านในเฟรมนี้</summary>
        public bool ObjectionPressed()
        {
            if (_objGroup.alpha < 0.5f || _fileOpen) return false;
            if (TakeDebugObjection()) return true;
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame &&
                   RectTransformUtility.RectangleContainsScreenPoint(_objRt, mouse.position.ReadValue(), null);
        }

        // ───────────────────────── คำเบิกความ ─────────────────────────

        public void ShowTestimony(string title, string speaker, string text, int index, int count, bool pressed, bool isNew)
        {
            _tTitle.text = "คำเบิกความ: " + title;
            _tSpeaker.text = speaker;
            _tIndex.text = "ข้อ " + (index + 1) + " / " + count + (pressed ? "   <color=#8FA0B8>(ซักไซ้แล้ว)</color>" : "")
                         + (isNew ? "   <color=#EEC76B>ใหม่</color>" : "");
            TestimonyText = text;
            _tText.text = "“" + text + "”";
            _tGroup.alpha = 1f;
            _tOpen = true;
        }

        public void HideTestimony()
        {
            _tGroup.alpha = 0f;
            _tOpen = false;
        }

        public TestimonyInput PollTestimony()
        {
            if (!_tOpen || _fileOpen) return TestimonyInput.None;
            var dbg = TakeDebugTestimony();
            if (dbg != TestimonyInput.None) return dbg;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) return TestimonyInput.Prev;
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) return TestimonyInput.Next;
                if (kb.qKey.wasPressedThisFrame) return TestimonyInput.Press;
                if (kb.rKey.wasPressedThisFrame) return TestimonyInput.Present;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                bool overPress = RectTransformUtility.RectangleContainsScreenPoint(_tPress, mp, null);
                bool overPresent = RectTransformUtility.RectangleContainsScreenPoint(_tPresent, mp, null);
                _tPressBg.color = overPress ? RowHover : new Color(1f, 1f, 1f, 0.08f);
                _tPresentBg.color = overPresent ? CrimsonHot : Crimson;
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_tPrev, mp, null)) return TestimonyInput.Prev;
                    if (RectTransformUtility.RectangleContainsScreenPoint(_tNext, mp, null)) return TestimonyInput.Next;
                    if (overPress) return TestimonyInput.Press;
                    if (overPresent) return TestimonyInput.Present;
                }
            }
            return TestimonyInput.None;
        }

        // ───────────────────────── ป้าย ─────────────────────────

        /// <summary>ตัวหนังสือใหญ่กลางจอ (คัดค้าน! / ขอค้าน!) พุ่งเข้ามาแล้วจางหาย</summary>
        public void Splash(string text, Color colour)
        {
            _splash.text = text;
            _splash.color = colour;
            _splashT = 1.3f;
        }

        /// <summary>ข้อความสั้นใต้หลอดความน่าเชื่อถือ (ศาลหมายพยานหลักฐาน / บันทึกคำเบิกความ)</summary>
        public void Toast(string text, float seconds = 3.5f)
        {
            _toast.text = text;
            _toastT = _toastLen = seconds;
        }

        // ───────────────────────── แฟ้มคดี ─────────────────────────

        public void ConfigureCaseFile(string[] tabs, Func<int, string> provider)
        {
            _fileTabNames = tabs;
            _fileProvider = provider;
            for (int i = 0; i < _fileTabs.Count; i++) Destroy(_fileTabs[i].rt.gameObject);
            _fileTabs.Clear();
            float w = 1480f / tabs.Length;
            var box = _fileBox;
            for (int i = 0; i < tabs.Length; i++)
            {
                var bg = Solid(box, "Tab_" + i, new Vector2(0f, 1f), new Vector2(40f + w * (i + 0.5f), -86f), new Vector2(w - 8f, 50f), RowIdle);
                var label = Text((RectTransform)bg.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w - 12f, 46f), 24f,
                                 TextAlignmentOptions.Center, Ink);
                label.text = (i + 1) + "  " + tabs[i];
                _fileTabs.Add(new Row { rt = (RectTransform)bg.transform, bg = bg, label = label });
            }
        }

        public void SetCaseFileOpen(bool open)
        {
            if (_fileProvider == null) open = false;
            if (open == _fileOpen) return;
            _fileOpen = open;
            _fileGroup.alpha = open ? 1f : 0f;

            // เป้าหมาย/หมุดนำทางของ ACT 2 ลอยทับหน้าแฟ้ม — ซ่อนไว้ระหว่างเปิด แล้วคืนเฉพาะตัวที่เราซ่อนเอง
            // (ตอนคัตซีน HUD ถูกซ่อนอยู่แล้ว ปิดแฟ้มก็ต้องไม่ไปเปิดมันขึ้นมา)
            if (open)
            {
                _hudHiddenByFile.Clear();
                var act2 = FindAnyObjectByType<SecretsThatBreathe.Act2.Act2HUD>();
                if (act2 != null)
                    foreach (var c in act2.GetComponentsInChildren<Canvas>(true))
                        if (c.enabled) { c.enabled = false; _hudHiddenByFile.Add(c); }
            }
            else
            {
                foreach (var c in _hudHiddenByFile) if (c != null) c.enabled = true;
                _hudHiddenByFile.Clear();
            }
            // เปิดตอนเดินเล่น: หยุดเดิน/หันกล้องไว้ก่อน ไม่งั้นเลื่อนเมาส์อ่านแฟ้มแล้วกล้องหมุนตาม
            var gm = GameManager.Instance;
            var pm = PlayerManager.Instance;
            if (open && gm != null && gm.IsState(GameState.Exploration) && pm != null)
            {
                pm.SetControl(false);
                _fileTookControl = true;
            }
            else if (!open && _fileTookControl)
            {
                _fileTookControl = false;
                if (pm != null && gm != null && gm.IsState(GameState.Exploration)) pm.SetControl(true);
            }
            if (open) RefreshCaseFile();
        }

        void RefreshCaseFile()
        {
            if (_fileProvider == null) return;
            _fileBody.text = _fileProvider(_fileTab);
            RefreshRows(_fileTabs, _fileTab, -1);
        }

        void UpdateCaseFileInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!_fileOpen)
            {
                if (CaseFileAllowed && kb.tabKey.wasPressedThisFrame) SetCaseFileOpen(true);
                return;
            }
            if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || !CaseFileAllowed)
            {
                SetCaseFileOpen(false);
                return;
            }
            int n = _fileTabNames != null ? _fileTabNames.Length : 0;
            int before = _fileTab;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) _fileTab = (_fileTab - 1 + n) % n;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) _fileTab = (_fileTab + 1) % n;
            for (int i = 0; i < n && i < 9; i++) if (kb[Key.Digit1 + i].wasPressedThisFrame) _fileTab = i;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                for (int i = 0; i < _fileTabs.Count; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(_fileTabs[i].rt, mouse.position.ReadValue(), null)) _fileTab = i;
            if (before != _fileTab) RefreshCaseFile();
        }

        // ───────────────────────── ทางลัดสำหรับเทสอัตโนมัติ ─────────────────────────
        // Input System ไม่รับปุ่มตอนหน้าต่าง Unity ไม่ได้โฟกัส ใช้ชุดนี้สั่งแทนผู้เล่นจากสคริปต์เทส

        bool _dbgObjection;
        TestimonyInput _dbgTestimony;

        public void DebugChoose(int index) { if (_choiceOpen && index >= 0 && index < _choiceRows.Count) Choose(index); }
        public void DebugObjection() { _dbgObjection = true; }
        public void DebugTestimony(TestimonyInput input) { _dbgTestimony = input; }

        /// <summary>เลือกหลักฐานตาม id ในหน้าแสดงหลักฐาน (null = ถอยกลับ ถ้ากดถอยได้)</summary>
        public void DebugPresent(string evidenceId)
        {
            if (!_xOpen || _items == null) return;
            if (evidenceId == null)
            {
                if (!_xCancel) return;
                var cb = _onPresent;
                HideCrossExam();
                if (cb != null) cb(null);
                return;
            }
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].id == evidenceId) { _selected = i; Present(); return; }
        }

        bool TakeDebugObjection()
        {
            if (!_dbgObjection) return false;
            _dbgObjection = false;
            return true;
        }

        TestimonyInput TakeDebugTestimony()
        {
            var t = _dbgTestimony;
            _dbgTestimony = TestimonyInput.None;
            return t;
        }

        // ───────────────────────── update / cursor ─────────────────────────

        void UpdateCourtPanels()
        {
            UpdateCaseFileInput();
            UpdateChoiceInput();

            if (_splashT > 0f)
            {
                _splashT -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(_splashT / 1.3f);             // 0 -> 1
                float scale = k < 0.12f ? Mathf.Lerp(2.2f, 1f, k / 0.12f) : 1f;
                _splash.rectTransform.localScale = Vector3.one * scale;
                var c = _splash.color;
                c.a = _splashT > 0.4f ? 1f : Mathf.Clamp01(_splashT / 0.4f);
                _splash.color = c;
            }
            else if (_splash.color.a > 0f) { var c = _splash.color; c.a = 0f; _splash.color = c; }

            if (_toastT > 0f)
            {
                _toastT -= Time.deltaTime;
                var c = _toast.color;
                c.a = Mathf.Clamp01(Mathf.Min(_toastT, _toastLen - _toastT) * 3f);
                _toast.color = c;
            }

            bool want = _xOpen || _choiceOpen || _tOpen || _fileOpen;
            if (want != _cursorShown)
            {
                _cursorShown = want;
                Cursor.lockState = want ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = want;
            }
        }

        // ───────────────────────── building ─────────────────────────

        void BuildCourtPanels()
        {
            // ── ตัวเลือก (กลางล่าง เหนือกล่องบทพูด) ──
            // ชิดล่าง (ตอนเลือกไม่มีกล่องบทพูด) พยานในคอกจะไม่โดนแผงบัง
            _choiceGroup = Group(_frontRoot, "Choice", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1180f, 400f));
            _choiceBox = (RectTransform)_choiceGroup.transform;
            var cbg = Stretch(_choiceBox, "Bg", Panel);
            cbg.raycastTarget = false;
            Solid(_choiceBox, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1180f, 4f), Gold);
            _choiceTitle = Text(_choiceBox, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1120f, 44f), 28f,
                                TextAlignmentOptions.Top, Gold);
            _choiceHint = Text(_choiceBox, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(1120f, 30f), 20f,
                               TextAlignmentOptions.Bottom, Dim);
            _choiceHint.text = "[W/S] หรือกดตัวเลข เลือก   ·   [Enter] ตกลง   ·   [Tab] เปิดแฟ้มคดี";
            _choiceGroup.alpha = 0f;

            // ── ป้ายคัดค้าน (ขวา กลางจอ) ──
            _objGroup = Group(_frontRoot, "Objection", new Vector2(1f, 0.5f), new Vector2(-80f, -40f), new Vector2(360f, 110f));
            _objRt = (RectTransform)_objGroup.transform;
            _objBg = Stretch(_objRt, "Bg", Crimson);
            var ol = Text(_objRt, "Label", new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(340f, 60f), 40f,
                          TextAlignmentOptions.Top, Ink);
            ol.text = "[Q] คัดค้าน!";
            ol.fontStyle = FontStyles.Bold;
            Bar(_objRt, "Timer", new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(320f, 12f),
                new Color(0f, 0f, 0f, 0.6f), out _objFill, Gold);
            _objGroup.alpha = 0f;

            // ── คำเบิกความ (บน) + ปุ่ม (ล่าง) ──
            _tGroup = Group(_frontRoot, "Testimony", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var troot = (RectTransform)_tGroup.transform;
            var stmt = Solid(troot, "Statement", new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(1500f, 250f), Panel);
            var srt = (RectTransform)stmt.transform;
            Solid(srt, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1500f, 4f), Gold);
            _tTitle = Text(srt, "Title", new Vector2(0f, 1f), new Vector2(30f, -16f), new Vector2(900f, 36f), 24f,
                           TextAlignmentOptions.TopLeft, Dim);
            _tSpeaker = Text(srt, "Speaker", new Vector2(1f, 1f), new Vector2(-30f, -16f), new Vector2(600f, 36f), 24f,
                             TextAlignmentOptions.TopRight, Gold);
            _tText = Text(srt, "Text", new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(1300f, 130f), 34f,
                          TextAlignmentOptions.Center, Ink);
            _tIndex = Text(srt, "Index", new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(900f, 32f), 22f,
                           TextAlignmentOptions.Bottom, Dim);

            var prev = Solid(srt, "Prev", new Vector2(0f, 0.5f), new Vector2(40f, -8f), new Vector2(60f, 90f), new Color(1f, 1f, 1f, 0.08f));
            _tPrev = (RectTransform)prev.transform;
            // ฟอนต์ไทยไม่มีลูกศร ◀ ▶ ใช้ < > ธรรมดา (ปิด rich text ไม่งั้น "<" ถูกอ่านเป็นแท็ก)
            var lt = Text(_tPrev, "L", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 80f), 44f, TextAlignmentOptions.Center, Ink);
            lt.richText = false;
            lt.fontStyle = FontStyles.Bold;
            lt.text = "<";
            var next = Solid(srt, "Next", new Vector2(1f, 0.5f), new Vector2(-40f, -8f), new Vector2(60f, 90f), new Color(1f, 1f, 1f, 0.08f));
            _tNext = (RectTransform)next.transform;
            var rtx = Text(_tNext, "R", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 80f), 44f, TextAlignmentOptions.Center, Ink);
            rtx.richText = false;
            rtx.fontStyle = FontStyles.Bold;
            rtx.text = ">";

            _tPressBg = Solid(troot, "Press", new Vector2(0.5f, 0f), new Vector2(-230f, 250f), new Vector2(420f, 74f), new Color(1f, 1f, 1f, 0.08f));
            _tPress = (RectTransform)_tPressBg.transform;
            Text(_tPress, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 64f), 28f, TextAlignmentOptions.Center, Ink)
                .text = "ซักไซ้ข้อนี้  [Q]";
            _tPresentBg = Solid(troot, "Present", new Vector2(0.5f, 0f), new Vector2(230f, 250f), new Vector2(420f, 74f), Crimson);
            _tPresent = (RectTransform)_tPresentBg.transform;
            Text(_tPresent, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 64f), 28f, TextAlignmentOptions.Center, Ink)
                .text = "แสดงหลักฐานค้าน  [R]";
            var th = Text(troot, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1400f, 36f), 22f,
                          TextAlignmentOptions.Center, Dim);
            th.text = "[A/D] เลื่อนข้อ   ·   [Q] ซักไซ้ให้พยานอธิบาย   ·   [R] แสดงหลักฐานที่ขัดกับข้อนี้   ·   [Tab] แฟ้มคดี";
            _tGroup.alpha = 0f;

            // ── ป้ายใหญ่ + ข้อความสั้น ──
            _splash = Text(_frontRoot, "Splash", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 200f), 140f,
                           TextAlignmentOptions.Center, Bad);
            _splash.fontStyle = FontStyles.Bold;
            _splash.textWrappingMode = TextWrappingModes.NoWrap;
            var sc = _splash.color; sc.a = 0f; _splash.color = sc;

            _toast = Text(_frontRoot, "Toast", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1300f, 44f), 26f,
                          TextAlignmentOptions.Top, Gold);
            var tc = _toast.color; tc.a = 0f; _toast.color = tc;

            // ── แฟ้มคดี ──
            // เต็มจอ: หรี่ฉากหลังทั้งจอ แล้ววางแผ่นแฟ้มทึบไว้ตรงกลาง อ่านตัวหนังสือได้ชัดไม่ว่าข้างหลังสว่างแค่ไหน
            _fileGroup = Group(_frontRoot, "CaseFile", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var fdim = (RectTransform)_fileGroup.transform;
            fdim.anchorMin = Vector2.zero;
            fdim.anchorMax = Vector2.one;
            fdim.offsetMin = fdim.offsetMax = Vector2.zero;
            Stretch(fdim, "Dim", new Color(0f, 0f, 0f, 0.72f));
            var panel = Solid(fdim, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 820f),
                              new Color(0.035f, 0.035f, 0.045f, 1f));
            _fileBox = (RectTransform)panel.transform;
            var froot = _fileBox;
            Solid(froot, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1560f, 4f), Gold);
            var ft = Text(froot, "Title", new Vector2(0f, 1f), new Vector2(40f, -16f), new Vector2(900f, 44f), 30f,
                          TextAlignmentOptions.TopLeft, Gold);
            ft.text = "แฟ้มคดี — ทนายโจทก์";
            var fh = Text(froot, "Hint", new Vector2(1f, 1f), new Vector2(-40f, -22f), new Vector2(600f, 34f), 20f,
                          TextAlignmentOptions.TopRight, Dim);
            fh.text = "[A/D] หรือเลข เปลี่ยนหน้า   ·   [Tab] ปิด";
            _fileBody = Text(froot, "Body", new Vector2(0f, 1f), new Vector2(40f, -130f), new Vector2(1480f, 660f), 26f,
                             TextAlignmentOptions.TopLeft, Ink);
            _fileGroup.alpha = 0f;
        }
    }
}
