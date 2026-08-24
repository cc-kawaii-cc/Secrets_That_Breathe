using SecretsThatBreathe.Act3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// HUD ของ ACT 2 — สร้าง Canvas เองทั้งหมดตอนรันไทม์
    ///
    /// จงใจไม่ทำเป็น prefab เพราะซีนทั้งสามถูก generate จากสคริปต์
    /// การผูก UI ไว้ใน prefab จะพังทุกครั้งที่ rebuild ด่าน
    /// สร้างจากโค้ดจึงเป็นทางเดียวที่ทนต่อการ rebuild
    /// </summary>
    public class Act2HUD : MonoBehaviour
    {
        [Header("รูปแบบ")]
        public bool showDebugStealthValues = false;
        [Tooltip("ฟอนต์ของ HUD (เว้นว่างจะไปหยิบฟอนต์เดียวกับบทสนทนาในเกม)")]
        public TMP_FontAsset font;

        static readonly Color Ink = new Color(0.96f, 0.96f, 0.98f, 0.95f);
        static readonly Color Dim = new Color(0.75f, 0.76f, 0.82f, 0.75f);
        static readonly Color Warn = new Color(1f, 0.55f, 0.15f);
        static readonly Color Danger = new Color(1f, 0.25f, 0.25f);
        static readonly Color Good = new Color(0.35f, 1f, 0.6f);

        Canvas _canvas;
        TMP_FontAsset _font;
        TextMeshProUGUI _objective, _clock, _notice, _framingHint, _abilities, _debug;
        Image _suspicionFill, _suspicionBg;
        Image _framingFill, _framingBg;
        Image _listenFill, _listenBg, _listenClarity, _listenClarityBg;
        Image _noiseFill, _noiseBg, _noiseDanger, _noiseDangerBg;
        TextMeshProUGUI _noiseLabel;
        RectTransform _viewfinder;
        Image _crosshair;
        Image _waypoint;
        TextMeshProUGUI _waypointLabel;
        Transform _waypointTarget;

        /// <summary>
        /// ตั้งความยาวหลอด
        ///
        /// Image.fillAmount ใช้ไม่ได้เลยถ้าไม่ได้ใส่ sprite — Unity จะเมินค่านั้น
        /// แล้วเรนเดอร์เป็นแท่งเต็มตลอด หลอดทุกอันจึงต้องยืด/หดด้วย anchor แทน
        /// </summary>
        static void SetBar(Image fill, float value)
        {
            if (fill == null) return;
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), rt.anchorMax.y);
        }

        float _noticeTimer;
        float _shownSuspicion;

        void Awake()
        {
            _font = ResolveFont();
            Build();
        }

        /// <summary>
        /// ฟอนต์ default ของ TMP (LiberationSans) ไม่มีสระและวรรณยุกต์ไทย
        /// ตัวอักษรจะกลายเป็นสี่เหลี่ยมทั้งหมด จึงไปหยิบฟอนต์ตัวเดียวกับที่
        /// บทสนทนาในเกมใช้อยู่แล้ว แทนการอ้างอิง asset ตรงๆ ซึ่งจะพังตอน rebuild ด่าน
        /// </summary>
        TMP_FontAsset ResolveFont()
        {
            if (font != null) return font;

            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TMP_FontAsset firstNonDefault = null;
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null || t.font == null) continue;
                string n = t.gameObject.name;
                if (n == "SubtitleText" || n == "DialogText" || n == "NameText") return t.font;
                if (firstNonDefault == null && !t.font.name.StartsWith("LiberationSans")) firstNonDefault = t.font;
            }
            if (firstNonDefault != null) return firstNonDefault;

            Debug.LogWarning("[Act2HUD] ไม่พบฟอนต์ที่รองรับภาษาไทย — ข้อความบน HUD อาจขึ้นเป็นสี่เหลี่ยม");
            return TMP_Settings.defaultFontAsset;
        }

        void OnEnable()
        {
            Act2Director.OnObjectiveChanged += OnObjective;
            Act2Director.OnNotice += OnNotice;
            AlertDirector.OnAlertLevel += OnAlert;
            DeadlineClock.OnTick += OnClockTick;
            EvidenceCamera.OnModeChanged += OnPhotoMode;
            EvidenceCamera.OnFramingChanged += OnFraming;
            EavesdropZone.OnListening += OnListening;
            EavesdropZone.OnEnded += OnListenEnded;
            SilentEavesdropZone.OnNoiseLevel += OnNoise;
            SilentEavesdropZone.OnNoiseMeterHide += OnNoiseHide;
        }

        void OnDisable()
        {
            Act2Director.OnObjectiveChanged -= OnObjective;
            Act2Director.OnNotice -= OnNotice;
            AlertDirector.OnAlertLevel -= OnAlert;
            DeadlineClock.OnTick -= OnClockTick;
            EvidenceCamera.OnModeChanged -= OnPhotoMode;
            EvidenceCamera.OnFramingChanged -= OnFraming;
            EavesdropZone.OnListening -= OnListening;
            EavesdropZone.OnEnded -= OnListenEnded;
            SilentEavesdropZone.OnNoiseLevel -= OnNoise;
            SilentEavesdropZone.OnNoiseMeterHide -= OnNoiseHide;
        }

        void Start()
        {
            var director = Act2Director.Instance;
            if (director != null && director.Current != null) OnObjective(director.Current);
            SetPhotoVisible(false);
            SetListenVisible(false);
            SetNoiseVisible(false);
        }

        // ───────────────────────── event handlers ─────────────────────────

        void OnObjective(Act2Objective objective)
        {
            if (_objective == null || objective == null) return;
            _objective.text = "<size=70%><color=#8FA0B8>เป้าหมาย</color></size>\n" + objective.text;
            _waypointTarget = FindWaypoint(objective.waypoint);
        }

        /// <summary>หาหมุดนำทางของเป้าหมายปัจจุบันในซีน</summary>
        static Transform FindWaypoint(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        /// <summary>
        /// หมุดบนจอชี้ว่าเป้าหมายอยู่ทางไหนและไกลแค่ไหน
        /// ถ้าเป้าอยู่นอกจอ หมุดจะไปเกาะขอบจอด้านนั้นแทนที่จะหายไป
        /// ด่านลานจอดกว้าง 44 เมตร ไม่มีตัวนำทางแล้วหารถคันเดียวไม่เจอ
        /// </summary>
        void UpdateWaypoint()
        {
            if (_waypoint == null) return;
            var director = Act2Director.Instance;
            if (_waypointTarget == null && director != null && director.Current != null)
                _waypointTarget = FindWaypoint(director.Current.waypoint);

            var cam = PlayerManager.Instance != null ? PlayerManager.Instance.playerCamera : Camera.main;
            if (_waypointTarget == null || cam == null)
            {
                _waypoint.enabled = false;
                if (_waypointLabel != null) _waypointLabel.enabled = false;
                return;
            }

            Vector3 world = _waypointTarget.position + Vector3.up * 1.2f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z <= 0f;
            if (behind) { sp.x = Screen.width - sp.x; sp.y = 0f; }

            float m = 60f;
            sp.x = Mathf.Clamp(sp.x, m, Screen.width - m);
            sp.y = Mathf.Clamp(sp.y, m, Screen.height - m);

            var canvasRect = (RectTransform)_canvas.transform;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local);

            _waypoint.enabled = true;
            ((RectTransform)_waypoint.transform).anchoredPosition = local;

            float dist = Vector3.Distance(cam.transform.position, _waypointTarget.position);
            if (_waypointLabel != null)
            {
                _waypointLabel.enabled = true;
                _waypointLabel.text = dist.ToString("0") + " m";
                ((RectTransform)_waypointLabel.transform).anchoredPosition = local + new Vector2(0f, -26f);
            }
        }

        void OnNotice(string text)
        {
            if (_notice == null) return;
            _notice.text = text;
            _notice.color = Ink;
            _noticeTimer = 4f;
        }

        void OnAlert(float level)
        {
            _shownSuspicion = Mathf.MoveTowards(_shownSuspicion, level, 3f * Time.deltaTime);
            if (_suspicionFill == null) return;

            bool visible = _shownSuspicion > 0.02f;
            _suspicionBg.enabled = visible;
            _suspicionFill.enabled = visible;
            if (!visible) return;

            SetBar(_suspicionFill, _shownSuspicion);
            _suspicionFill.color = _shownSuspicion > 0.75f ? Danger
                                 : _shownSuspicion > 0.35f ? Warn
                                 : Dim;
        }

        void OnClockTick(float secondsLeft, float normalised)
        {
            if (_clock == null) return;
            var clock = DeadlineClock.Instance;
            if (clock == null) return;
            string colour = normalised < 0.15f ? "#FF4040" : normalised < 0.35f ? "#FF8C26" : "#C9D2E0";
            _clock.text = "<color=" + colour + ">" + clock.StoryTimeText() + "</color>"
                        + "\n<size=60%><color=#8FA0B8>เหลือ " + clock.CountdownText() + "</color></size>";
        }

        void OnPhotoMode(bool aiming)
        {
            SetPhotoVisible(aiming);
        }

        void OnFraming(float quality, string hint)
        {
            if (_framingFill == null) return;
            var cam = EvidenceCamera.Instance;
            bool ready = cam != null && quality >= cam.requiredQuality;
            SetBar(_framingFill, quality);
            _framingFill.color = ready ? Good : (quality > 0.35f ? Warn : Dim);
            if (_framingHint != null)
            {
                _framingHint.text = hint;
                _framingHint.color = ready ? Good : Dim;
            }
            if (_viewfinder != null)
            {
                // กรอบหดเข้าเมื่อจัดเฟรมได้ดี — ฟีดแบ็กที่อ่านได้โดยไม่ต้องอ่านตัวหนังสือ
                float s = Mathf.Lerp(1.08f, 0.94f, quality);
                _viewfinder.localScale = new Vector3(s, s, 1f);
            }
        }

        void OnListening(float clarity, float progress)
        {
            SetListenVisible(true);
            SetBar(_listenFill, progress);
            SetBar(_listenClarity, clarity);
            if (_listenClarity != null)
                _listenClarity.color = clarity > 0.75f ? Danger : clarity > 0.4f ? Warn : Dim;
        }

        void OnListenEnded() { SetListenVisible(false); }

        /// <summary>ตอนแอบฟังแบบ "ห้ามส่งเสียง" (ACT 3) — โชว์หลอดความดังจากไมค์จริง</summary>
        void OnNoise(float loudness, float danger)
        {
            SetNoiseVisible(true);
            SetBar(_noiseFill, loudness);
            _noiseFill.color = danger > 0.6f ? Danger : danger > 0.25f ? Warn : Good;

            SetBar(_noiseDanger, danger);
            if (_noiseDanger != null) _noiseDanger.color = danger > 0.6f ? Danger : Warn;
        }

        void OnNoiseHide() { SetNoiseVisible(false); }

        void Update()
        {
            if (_noticeTimer > 0f)
            {
                _noticeTimer -= Time.deltaTime;
                if (_notice != null)
                {
                    var c = _notice.color;
                    c.a = Mathf.Clamp01(_noticeTimer);
                    _notice.color = c;
                }
            }

            UpdateAbilityLine();
            UpdateDebugLine();
            UpdateWaypoint();
        }

        void UpdateAbilityLine()
        {
            if (_abilities == null) return;
            var st = StealthTarget.Instance;
            if (st == null) { _abilities.text = ""; return; }

            var crouch = st.GetComponent<CrouchAbility>();
            var throwAbility = st.GetComponent<ThrowAbility>();

            string crouchTag = crouch != null && crouch.IsCrouching
                ? "<color=#5BE38C>[Ctrl] หมอบอยู่</color>"
                : "<color=#8FA0B8>[Ctrl] หมอบ</color>";
            string shadowTag = st.InShadow ? "  <color=#5BE38C>อยู่ในที่กำบัง</color>" : "";
            string throwTag = throwAbility != null
                ? "\n<color=#8FA0B8>[G] ขว้างล่อยาม ×" + throwAbility.Remaining + "</color>"
                : "";
            string photoTag = EvidenceCamera.Instance != null
                ? "\n<color=#8FA0B8>[Q] กล้อง</color>"
                : "";
            string torchTag = st.FlashlightOn
                ? "\n<color=#FF6B4A>[F] ไฟฉายเปิดอยู่ — ยามเห็นแต่ไกล</color>"
                : "\n<color=#8FA0B8>[F] ไฟฉาย</color>";
            string skipTag = ChampArrivalSequence.SkipAvailable
                ? "\n<color=#FF8C26>" + ChampArrivalSequence.SkipHintText + "</color>"
                : "";

            _abilities.text = crouchTag + shadowTag + throwTag + photoTag + torchTag + skipTag;
        }

        void UpdateDebugLine()
        {
            if (_debug == null) return;
            if (!showDebugStealthValues) { _debug.text = ""; return; }
            var st = StealthTarget.Instance;
            if (st == null) return;
            _debug.text = string.Format("vis {0:0.00}  noise {1:0.00}  shadow {2}  exposure x{3:0.00}",
                st.Visibility, st.Noise, st.InShadow ? "yes" : "no", st.ExternalVisibilityMultiplier);
        }

        void SetPhotoVisible(bool visible)
        {
            if (_viewfinder != null) _viewfinder.gameObject.SetActive(visible);
            if (_framingBg != null) _framingBg.enabled = visible;
            if (_framingFill != null) _framingFill.enabled = visible;
            if (_framingHint != null) _framingHint.enabled = visible;
            if (_crosshair != null) _crosshair.enabled = !visible;
        }

        void SetListenVisible(bool visible)
        {
            if (_listenBg != null) _listenBg.enabled = visible;
            if (_listenFill != null) _listenFill.enabled = visible;
            if (_listenClarityBg != null) _listenClarityBg.enabled = visible;
            if (_listenClarity != null) _listenClarity.enabled = visible;
        }

        void SetNoiseVisible(bool visible)
        {
            if (_noiseBg != null) _noiseBg.enabled = visible;
            if (_noiseFill != null) _noiseFill.enabled = visible;
            if (_noiseDangerBg != null) _noiseDangerBg.enabled = visible;
            if (_noiseDanger != null) _noiseDanger.enabled = visible;
            if (_noiseLabel != null) _noiseLabel.enabled = visible;
        }

        // ───────────────────────── building the canvas ─────────────────────────

        void Build()
        {
            var go = new GameObject("Act2_HUD_Canvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var root = go.GetComponent<RectTransform>();

            // เป้าหมายปัจจุบัน (ซ้ายบน)
            _objective = Text(root, "Objective", new Vector2(0f, 1f), new Vector2(40f, -40f),
                              new Vector2(620f, 120f), 30f, TextAlignmentOptions.TopLeft, Ink);

            // ความสงสัยของยาม (กลางบน)
            _suspicionBg = Bar(root, "SuspicionBg", new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                               new Vector2(420f, 12f), new Color(0f, 0f, 0f, 0.45f), out _suspicionFill,
                               new Color(1f, 1f, 1f, 0.9f));
            _suspicionBg.enabled = false;
            _suspicionFill.enabled = false;

            // นาฬิกาเส้นตาย (ขวาบน)
            _clock = Text(root, "Clock", new Vector2(1f, 1f), new Vector2(-40f, -40f),
                          new Vector2(320f, 110f), 40f, TextAlignmentOptions.TopRight, Ink);
            _clock.text = "";

            // เป้าเล็ง
            _crosshair = Solid(root, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero,
                               new Vector2(5f, 5f), new Color(1f, 1f, 1f, 0.6f));

            // หมุดนำทางไปยังเป้าหมายปัจจุบัน
            _waypoint = Solid(root, "Waypoint", new Vector2(0.5f, 0.5f), Vector2.zero,
                              new Vector2(16f, 16f), Warn);
            ((RectTransform)_waypoint.transform).localRotation = Quaternion.Euler(0f, 0f, 45f);
            _waypointLabel = Text(root, "WaypointDist", new Vector2(0.5f, 0.5f), Vector2.zero,
                                  new Vector2(140f, 30f), 20f, TextAlignmentOptions.Center, Warn);

            // กรอบกล้อง + หลอดคุณภาพการจัดเฟรม
            _viewfinder = Frame(root, "Viewfinder", new Vector2(760f, 470f), new Color(1f, 1f, 1f, 0.55f));
            _framingBg = Bar(root, "FramingBg", new Vector2(0.5f, 0f), new Vector2(0f, 210f),
                             new Vector2(420f, 14f), new Color(0f, 0f, 0f, 0.55f), out _framingFill, Good);
            _framingHint = Text(root, "FramingHint", new Vector2(0.5f, 0f), new Vector2(0f, 232f),
                                new Vector2(700f, 44f), 26f, TextAlignmentOptions.Center, Dim);

            // แอบฟัง: หลอดความคืบหน้า + แถบความชัด/ความเสี่ยง
            _listenBg = Bar(root, "ListenBg", new Vector2(0.5f, 0f), new Vector2(0f, 150f),
                            new Vector2(480f, 16f), new Color(0f, 0f, 0f, 0.55f), out _listenFill, Good);
            _listenClarityBg = Solid(root, "ListenClarityBg", new Vector2(0.5f, 0f), new Vector2(0f, 132f),
                                     new Vector2(480f, 6f), new Color(0f, 0f, 0f, 0.4f));
            _listenClarity = StretchFill(_listenClarityBg, "ListenClarity", Dim);

            // แอบฟังแบบห้ามส่งเสียง (ACT 3): หลอดความดังจากไมค์ + แถบความใกล้โดนจับ
            _noiseLabel = Text(root, "NoiseLabel", new Vector2(0.5f, 0f), new Vector2(0f, 320f),
                               new Vector2(480f, 36f), 22f, TextAlignmentOptions.Center, Dim);
            _noiseLabel.text = "อย่าส่งเสียง";
            _noiseBg = Bar(root, "NoiseBg", new Vector2(0.5f, 0f), new Vector2(0f, 300f),
                           new Vector2(480f, 16f), new Color(0f, 0f, 0f, 0.55f), out _noiseFill, Good);
            _noiseDangerBg = Solid(root, "NoiseDangerBg", new Vector2(0.5f, 0f), new Vector2(0f, 282f),
                                   new Vector2(480f, 6f), new Color(0f, 0f, 0f, 0.4f));
            _noiseDanger = StretchFill(_noiseDangerBg, "NoiseDanger", Dim);

            // ข้อความแจ้งเตือนสั้นๆ
            _notice = Text(root, "Notice", new Vector2(0.5f, 0f), new Vector2(0f, 96f),
                           new Vector2(1100f, 60f), 30f, TextAlignmentOptions.Center, Ink);
            _notice.text = "";

            // ปุ่มความสามารถ (ซ้ายล่าง)
            _abilities = Text(root, "Abilities", new Vector2(0f, 0f), new Vector2(40f, 40f),
                              new Vector2(520f, 130f), 24f, TextAlignmentOptions.BottomLeft, Dim);

            // ตัวเลข stealth สำหรับจูนค่า
            _debug = Text(root, "StealthDebug", new Vector2(1f, 0f), new Vector2(-40f, 40f),
                          new Vector2(560f, 40f), 20f, TextAlignmentOptions.BottomRight, Dim);
            _debug.text = "";
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

        /// <summary>แถบเติมที่ยืดจากขอบซ้ายของพื้นหลังที่ให้มา</summary>
        static Image StretchFill(Image background, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(background.transform, false);
            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = colour;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>แถบพื้นหลัง + แถบเติมด้านใน คืนพื้นหลัง ส่งแถบเติมออกทาง out</summary>
        static Image Bar(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size,
                         Color background, out Image fill, Color fillColour)
        {
            var bg = Solid(parent, name, anchor, offset, size, background);

            var go = new GameObject(name + "_Fill", typeof(RectTransform));
            go.transform.SetParent(bg.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);

            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);          // เริ่มที่ความยาว 0 แล้วให้ SetBar ยืดออก

            fill = go.AddComponent<Image>();
            fill.color = fillColour;
            fill.raycastTarget = false;
            return bg;
        }

        /// <summary>กรอบสี่มุมแบบช่องมองภาพ</summary>
        static RectTransform Frame(RectTransform parent, string name, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            const float thick = 3f, len = 70f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? -1f : 1f;
                float sy = (i & 2) == 0 ? -1f : 1f;
                Vector2 corner = new Vector2(size.x * 0.5f * sx, size.y * 0.5f * sy);
                Solid(rt, "H" + i, new Vector2(0.5f, 0.5f),
                      corner - new Vector2(sx * len * 0.5f, 0f), new Vector2(len, thick), colour);
                Solid(rt, "V" + i, new Vector2(0.5f, 0.5f),
                      corner - new Vector2(0f, sy * len * 0.5f), new Vector2(thick, len), colour);
            }
            return rt;
        }
    }
}
