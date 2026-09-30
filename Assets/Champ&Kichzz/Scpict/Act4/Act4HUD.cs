using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// UI ของ ACT 4 — สร้าง Canvas เองตอนรันไทม์ทั้งหมดแบบ Act2HUD (ซีนถูก generate ใหม่ได้ทุกเมื่อ)
    ///
    /// แบ่งเป็นสอง Canvas:
    ///   ชั้นหลัง (sorting -1): จอดำ + แถบซีเนมาบน/ล่าง — อยู่ใต้ UI ของผู้เล่น ซับไตเติลจึงยังอ่านได้ตอนจอดำ
    ///   ชั้นหน้า (sorting 150): กล่องบทพูด, หลอดความน่าเชื่อถือ, หน้าซักค้าน, QTE, ชื่อตอน
    ///
    /// รับเมาส์/คีย์บอร์ดเองใน Update ไม่พึ่ง EventSystem — ซีนที่ generate มาไม่มี EventSystem ให้เสมอไป
    /// </summary>
    public partial class Act4HUD : MonoBehaviour
    {
        public static Act4HUD Instance { get; private set; }

        [Tooltip("ฟอนต์ (เว้นว่าง = หยิบฟอนต์ภาษาไทยตัวเดียวกับซับไตเติลในเกม)")]
        public TMP_FontAsset font;

        static readonly Color Ink = new Color(0.96f, 0.96f, 0.98f, 1f);
        static readonly Color Dim = new Color(0.72f, 0.74f, 0.80f, 1f);
        static readonly Color Gold = new Color(0.93f, 0.78f, 0.42f, 1f);
        static readonly Color Good = new Color(0.40f, 0.95f, 0.60f, 1f);
        static readonly Color Bad = new Color(1f, 0.30f, 0.26f, 1f);
        static readonly Color Panel = new Color(0.03f, 0.03f, 0.04f, 0.80f);
        const float LetterboxHeight = 120f;

        TMP_FontAsset _font;
        RectTransform _backRoot, _frontRoot;

        // จอดำ + แถบซีเนมา
        Image _fade;
        RectTransform _barTop, _barBottom;
        float _letterbox, _letterboxTarget;

        // กล่องบทพูด
        CanvasGroup _lineGroup;
        TextMeshProUGUI _lineSpeaker, _lineText, _lineHint;

        // ความน่าเชื่อถือ
        CanvasGroup _credGroup;
        Image _credFill;
        TextMeshProUGUI _credValue;
        float _credShown, _credTarget, _credFlash;
        Color _credFlashColour = Ink;

        // ชื่อตอน / การ์ดบท
        CanvasGroup _titleGroup;
        TextMeshProUGUI _titleSmall, _titleBig, _titleSub;

        // QTE
        CanvasGroup _qteGroup;
        Image _qteFill, _redTint;
        TextMeshProUGUI _qtePrompt;

        // ซักค้าน
        CanvasGroup _xGroup;
        TextMeshProUGUI _xSpeaker, _xStatement, _xDescName, _xDesc;
        RectTransform _xList, _xPresent;
        Image _xPresentBg;
        readonly List<Row> _rows = new List<Row>();
        List<Act4Evidence> _items;
        Action<string> _onPresent;
        int _selected;
        bool _xOpen, _xCancel;
        TextMeshProUGUI _xHint;

        class Row
        {
            public RectTransform rt;
            public Image bg;
            public TextMeshProUGUI label;
        }

        public bool CrossExamOpen { get { return _xOpen; } }
        /// <summary>ข้อความคำเบิกความ/คำถามที่หน้าแสดงหลักฐานกำลังโชว์อยู่</summary>
        public string CrossExamPrompt { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _font = ResolveFont();
            Build();
            // เริ่มซีนที่จอดำ แล้วให้ sequence ของซีนเป็นคนสั่งเฟดเข้า
            SetFade(1f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        TMP_FontAsset ResolveFont()
        {
            if (font != null) return font;
            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TMP_FontAsset fallback = null;
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null || t.font == null) continue;
                string n = t.gameObject.name;
                if (n == "SubtitleText" || n == "DialogText" || n == "NameText") return t.font;
                if (fallback == null && !t.font.name.StartsWith("LiberationSans")) fallback = t.font;
            }
            if (fallback != null) return fallback;
            Debug.LogWarning("[Act4HUD] ไม่พบฟอนต์ที่รองรับภาษาไทย — ข้อความอาจขึ้นเป็นสี่เหลี่ยม");
            return TMP_Settings.defaultFontAsset;
        }

        // ───────────────────────── จอดำ / แถบซีเนมา ─────────────────────────

        public void SetFade(float alpha)
        {
            if (_fade == null) return;
            var c = _fade.color;
            c.a = Mathf.Clamp01(alpha);
            _fade.color = c;
            _fade.enabled = c.a > 0.001f;
        }

        public float FadeAlpha { get { return _fade != null ? _fade.color.a : 0f; } }

        public IEnumerator Fade(float to, float seconds)
        {
            float from = FadeAlpha;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                SetFade(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.0001f, seconds))));
                yield return null;
            }
            SetFade(to);
        }

        public void SetLetterbox(bool on) { _letterboxTarget = on ? 1f : 0f; }

        // ───────────────────────── บทพูด ─────────────────────────

        public void ShowLine(string speaker, string text, bool showHint)
        {
            _lineSpeaker.text = speaker ?? "";
            _lineSpeaker.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            // ผู้พูดว่าง = คำบรรยาย ขึ้นเป็นตัวเอียงสีจาง
            _lineText.text = string.IsNullOrEmpty(speaker) ? "<i>" + text + "</i>" : text;
            _lineText.color = string.IsNullOrEmpty(speaker) ? Dim : Ink;
            _lineHint.gameObject.SetActive(showHint);
            _lineGroup.alpha = 1f;
        }

        public void HideLine() { _lineGroup.alpha = 0f; }

        // ───────────────────────── ความน่าเชื่อถือ ─────────────────────────

        public void ShowCredibility(bool on) { _credGroup.alpha = on ? 1f : 0f; }

        /// <summary>ตั้งค่าหลอด 0-100 แถบจะไหลไปหาค่าใหม่ พร้อมกะพริบเขียว/แดงตามทิศ</summary>
        public void SetCredibility(float value, bool instant = false)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            if (!instant && Mathf.Abs(value - _credTarget) > 0.5f)
            {
                _credFlash = 1f;
                _credFlashColour = value > _credTarget ? Good : Bad;
            }
            _credTarget = value;
            if (instant) _credShown = value;
        }

        // ───────────────────────── ชื่อตอน ─────────────────────────

        public void ShowTitle(string small, string big, string sub)
        {
            _titleSmall.text = small ?? "";
            _titleBig.text = big ?? "";
            _titleSub.text = sub ?? "";
        }

        public IEnumerator TitleAlpha(float to, float seconds)
        {
            float from = _titleGroup.alpha, t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _titleGroup.alpha = Mathf.Lerp(from, to, t / Mathf.Max(0.0001f, seconds));
                yield return null;
            }
            _titleGroup.alpha = to;
        }

        /// <summary>การ์ดบทสั้น ๆ: เฟดเข้า ค้าง แล้วเฟดออก</summary>
        public IEnumerator Card(string small, string big, string sub, float hold)
        {
            ShowTitle(small, big, sub);
            yield return TitleAlpha(1f, 0.8f);
            yield return new WaitForSeconds(hold);
            yield return TitleAlpha(0f, 0.8f);
        }

        // ───────────────────────── QTE ─────────────────────────

        public void ShowQTE(string prompt)
        {
            _qtePrompt.text = prompt;
            _qteGroup.alpha = 1f;
            SetQTE(0f);
        }

        public void SetQTE(float progress) { SetBar(_qteFill, progress); }
        public void HideQTE() { _qteGroup.alpha = 0f; }

        public void SetRedTint(float alpha)
        {
            var c = _redTint.color;
            c.a = Mathf.Clamp01(alpha);
            _redTint.color = c;
            _redTint.enabled = c.a > 0.001f;
        }

        // ───────────────────────── ซักค้าน ─────────────────────────

        /// <summary>เปิดหน้าซักค้าน: คำเบิกความด้านบน แฟ้มคดีด้านซ้าย รายละเอียดด้านขวา</summary>
        /// <param name="allowCancel">กด Esc/คลิกขวาเพื่อถอยกลับ (callback ได้ null)</param>
        /// <param name="label">ชื่อที่โชว์ในแฟ้ม (เช่น เติมหมายเลขพยานหลักฐาน) เว้นว่าง = ใช้ชื่อหลักฐาน</param>
        public void ShowCrossExam(string speaker, string statement, List<Act4Evidence> items, Action<string> onPresent,
                                  bool allowCancel = false, Func<Act4Evidence, string> label = null)
        {
            _items = items;
            _onPresent = onPresent;
            _xCancel = allowCancel;
            _xHint.text = "[W/S] หรือคลิก เลือกหลักฐาน   ·   [Enter] แสดงหลักฐาน" +
                          (allowCancel ? "   ·   [Esc] ถอยกลับ" : "") + "   ·   [Tab] แฟ้มคดี   ·   เลือกผิด ความน่าเชื่อถือลด";
            _xSpeaker.text = speaker;
            CrossExamPrompt = statement;
            _xStatement.text = "“" + statement + "”";

            for (int i = 0; i < _rows.Count; i++) Destroy(_rows[i].rt.gameObject);
            _rows.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var bg = Solid(_xList, "Row_" + i, new Vector2(0.5f, 1f), new Vector2(0f, -70f - i * 58f),
                               new Vector2(540f, 52f), new Color(1f, 1f, 1f, 0.06f));
                var rowLabel = Text((RectTransform)bg.transform, "Label", new Vector2(0f, 0.5f), new Vector2(20f, 0f),
                                 new Vector2(500f, 50f), 26f, TextAlignmentOptions.MidlineLeft, Ink);
                rowLabel.text = label != null ? label(items[i]) : items[i].name;
                _rows.Add(new Row { rt = (RectTransform)bg.transform, bg = bg, label = rowLabel });
            }
            _selected = 0;
            RefreshSelection(-1);

            _xGroup.alpha = 1f;
            _xOpen = true;
        }

        public void HideCrossExam()
        {
            _xGroup.alpha = 0f;
            _xOpen = false;
            _onPresent = null;
        }

        void RefreshSelection(int hover)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                bool sel = i == _selected;
                _rows[i].bg.color = sel ? new Color(Gold.r, Gold.g, Gold.b, 0.32f)
                                  : (i == hover ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
                _rows[i].label.color = sel ? Gold : Ink;
            }
            if (_items != null && _selected >= 0 && _selected < _items.Count)
            {
                _xDescName.text = _items[_selected].name;
                _xDesc.text = _items[_selected].desc;
            }
        }

        void UpdateCrossExamInput()
        {
            if (!_xOpen || _fileOpen || _items == null || _items.Count == 0) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            int hover = -1;

            if (_xCancel && ((kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame))
                             || (mouse != null && mouse.rightButton.wasPressedThisFrame)))
            {
                var cb = _onPresent;
                HideCrossExam();
                if (cb != null) cb(null);
                return;
            }

            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(_rows[i].rt, mp, null)) continue;
                    hover = i;
                    if (mouse.leftButton.wasPressedThisFrame) _selected = i;
                }
                bool overPresent = RectTransformUtility.RectangleContainsScreenPoint(_xPresent, mp, null);
                _xPresentBg.color = overPresent ? new Color(0.62f, 0.10f, 0.10f, 1f) : new Color(0.45f, 0.06f, 0.07f, 0.95f);
                if (overPresent && mouse.leftButton.wasPressedThisFrame) { Present(); return; }
            }
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                    _selected = (_selected - 1 + _items.Count) % _items.Count;
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                    _selected = (_selected + 1) % _items.Count;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Present(); return; }
            }
            RefreshSelection(hover);
        }

        void Present()
        {
            if (_items == null || _selected < 0 || _selected >= _items.Count) return;
            var cb = _onPresent;
            string id = _items[_selected].id;
            HideCrossExam();
            if (cb != null) cb(id);
        }

        // ───────────────────────── update ─────────────────────────

        void Update()
        {
            _letterbox = Mathf.MoveTowards(_letterbox, _letterboxTarget, Time.deltaTime * 2.2f);
            float h = LetterboxHeight * Mathf.SmoothStep(0f, 1f, _letterbox);
            _barTop.sizeDelta = new Vector2(0f, h);
            _barBottom.sizeDelta = new Vector2(0f, h);

            _credShown = Mathf.MoveTowards(_credShown, _credTarget, Time.deltaTime * 45f);
            SetBar(_credFill, _credShown / 100f);
            _credValue.text = Mathf.RoundToInt(_credShown) + "%";
            _credFlash = Mathf.MoveTowards(_credFlash, 0f, Time.deltaTime * 1.5f);
            Color baseCol = _credShown < 25f ? Bad : Gold;
            _credFill.color = Color.Lerp(baseCol, _credFlashColour, _credFlash);

            UpdateCrossExamInput();
            UpdateCourtPanels();
        }

        // ───────────────────────── building ─────────────────────────

        void Build()
        {
            _backRoot = MakeCanvas("Act4_Back", -1);
            _frontRoot = MakeCanvas("Act4_Front", 150);

            // ── ชั้นหลัง ──
            _fade = Stretch(_backRoot, "Fade", Color.black);
            _barTop = Band(_backRoot, "Letterbox_Top", true);
            _barBottom = Band(_backRoot, "Letterbox_Bottom", false);

            // ── ชั้นหน้า: ผ้าแดงตอน QTE อยู่ล่างสุด ──
            _redTint = Stretch(_frontRoot, "RedTint", new Color(0.55f, 0f, 0f, 0f));
            _redTint.enabled = false;

            // กล่องบทพูด (ล่างกลาง ทับแถบซีเนมา)
            _lineGroup = Group(_frontRoot, "LineBox", new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1500f, 170f));
            var lineRt = (RectTransform)_lineGroup.transform;
            var lineBg = Solid(lineRt, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 170f), new Color(0f, 0f, 0f, 0.55f));
            lineBg.raycastTarget = false;
            _lineSpeaker = Text(lineRt, "Speaker", new Vector2(0f, 1f), new Vector2(34f, -14f), new Vector2(900f, 40f), 30f,
                                TextAlignmentOptions.TopLeft, Gold);
            _lineText = Text(lineRt, "Text", new Vector2(0f, 1f), new Vector2(34f, -56f), new Vector2(1430f, 104f), 34f,
                             TextAlignmentOptions.TopLeft, Ink);
            _lineHint = Text(lineRt, "Hint", new Vector2(1f, 0f), new Vector2(-24f, 12f), new Vector2(300f, 30f), 20f,
                             TextAlignmentOptions.BottomRight, Dim);
            _lineHint.text = "[Space] ถัดไป";
            _lineGroup.alpha = 0f;

            // ความน่าเชื่อถือ (กลางบน ใต้แถบซีเนมา — เหนือกล่องคำเบิกความพอดี)
            _credGroup = Group(_frontRoot, "Credibility", new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(760f, 50f));
            var credRt = (RectTransform)_credGroup.transform;
            var credLabel = Text(credRt, "Label", new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(560f, 30f), 22f,
                                 TextAlignmentOptions.TopLeft, Dim);
            credLabel.text = "ความน่าเชื่อถือของทนายโจทก์";
            _credValue = Text(credRt, "Value", new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(140f, 30f), 24f,
                              TextAlignmentOptions.TopRight, Ink);
            var credBg = Bar(credRt, "Bar", new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(760f, 14f),
                             new Color(0f, 0f, 0f, 0.6f), out _credFill, Gold);
            credBg.raycastTarget = false;
            _credGroup.alpha = 0f;

            // หน้าซักค้าน
            BuildCrossExam();

            // QTE (กลางล่าง)
            _qteGroup = Group(_frontRoot, "QTE", new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(760f, 110f));
            var qteRt = (RectTransform)_qteGroup.transform;
            _qtePrompt = Text(qteRt, "Prompt", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, 50f), 36f,
                              TextAlignmentOptions.Top, Ink);
            Bar(qteRt, "Meter", new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(700f, 26f),
                new Color(0f, 0f, 0f, 0.65f), out _qteFill, new Color(0.75f, 0.05f, 0.05f, 1f));
            _qteGroup.alpha = 0f;

            // ชื่อตอน (กลางจอ)
            _titleGroup = Group(_frontRoot, "Title", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 400f));
            var titleRt = (RectTransform)_titleGroup.transform;
            _titleSmall = Text(titleRt, "Small", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 60f), 34f,
                               TextAlignmentOptions.Top, Dim);
            _titleBig = Text(titleRt, "Big", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1600f, 130f), 86f,
                             TextAlignmentOptions.Center, Ink);
            _titleBig.fontStyle = FontStyles.Bold;
            _titleSub = Text(titleRt, "Sub", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1600f, 60f), 36f,
                             TextAlignmentOptions.Bottom, Gold);
            _titleGroup.alpha = 0f;

            BuildCourtPanels();
        }

        void BuildCrossExam()
        {
            _xGroup = Group(_frontRoot, "CrossExam", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var root = (RectTransform)_xGroup.transform;

            // คำเบิกความ (บน)
            var stmt = Solid(root, "Statement", new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(1500f, 230f), Panel);
            var stmtRt = (RectTransform)stmt.transform;
            Solid(stmtRt, "Rule", new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1500f, 4f), Gold);
            var header = Text(stmtRt, "Header", new Vector2(0f, 1f), new Vector2(30f, -16f), new Vector2(600f, 36f), 24f,
                              TextAlignmentOptions.TopLeft, Dim);
            header.text = "คำเบิกความ";
            _xSpeaker = Text(stmtRt, "Speaker", new Vector2(1f, 1f), new Vector2(-30f, -16f), new Vector2(800f, 36f), 26f,
                             TextAlignmentOptions.TopRight, Gold);
            _xStatement = Text(stmtRt, "Text", new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(1430f, 160f), 32f,
                               TextAlignmentOptions.Center, Ink);

            // แฟ้มคดี (ซ้าย)
            var list = Solid(root, "CaseFile", new Vector2(0f, 0.5f), new Vector2(390f, -150f), new Vector2(600f, 520f), Panel);
            _xList = (RectTransform)list.transform;
            var listHeader = Text(_xList, "Header", new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(540f, 40f), 26f,
                                  TextAlignmentOptions.Top, Gold);
            listHeader.text = "แฟ้มคดี — เลือกหลักฐานที่ขัดกับคำเบิกความ";

            // รายละเอียด + ปุ่มแสดงหลักฐาน (ขวา)
            var desc = Solid(root, "Detail", new Vector2(1f, 0.5f), new Vector2(-400f, -80f), new Vector2(640f, 330f), Panel);
            var descRt = (RectTransform)desc.transform;
            _xDescName = Text(descRt, "Name", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(580f, 44f), 32f,
                              TextAlignmentOptions.Top, Gold);
            _xDesc = Text(descRt, "Desc", new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(580f, 220f), 27f,
                          TextAlignmentOptions.TopLeft, Ink);

            _xPresentBg = Solid(root, "Present", new Vector2(1f, 0.5f), new Vector2(-400f, -300f), new Vector2(420f, 74f),
                                new Color(0.45f, 0.06f, 0.07f, 0.95f));
            _xPresent = (RectTransform)_xPresentBg.transform;
            var pl = Text(_xPresent, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 64f), 30f,
                          TextAlignmentOptions.Center, Ink);
            pl.text = "แสดงหลักฐาน  [Enter]";

            _xHint = Text(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1400f, 40f), 22f,
                          TextAlignmentOptions.Center, Dim);
            _xGroup.alpha = 0f;
        }

        RectTransform MakeCanvas(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.GetComponent<RectTransform>();
        }

        static Image Stretch(RectTransform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = colour;
            img.raycastTarget = false;
            return img;
        }

        static RectTransform Band(RectTransform parent, string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return rt;
        }

        static CanvasGroup Group(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var g = go.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        TextMeshProUGUI Text(RectTransform parent, string name, Vector2 anchor, Vector2 offset,
                             Vector2 size, float fontSize, TextAlignmentOptions align, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = colour;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        static Image Solid(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = go.AddComponent<Image>();
            img.color = colour;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>แถบพื้นหลัง + แถบเติม (ยืดด้วย anchor เพราะ Image.fillAmount ใช้ไม่ได้ถ้าไม่มี sprite)</summary>
        static Image Bar(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size,
                         Color background, out Image fill, Color fillColour)
        {
            var bg = Solid(parent, name, anchor, offset, size, background);
            var go = new GameObject(name + "_Fill", typeof(RectTransform));
            go.transform.SetParent(bg.transform, false);
            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            fill = go.AddComponent<Image>();
            fill.color = fillColour;
            fill.raycastTarget = false;
            return bg;
        }

        static void SetBar(Image fill, float value)
        {
            if (fill == null) return;
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), rt.anchorMax.y);
        }
    }
}
