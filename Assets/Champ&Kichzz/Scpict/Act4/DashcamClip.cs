using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// คลิปจาก SD Card กล้องหน้ารถของแชมป์ ขึ้นบนจอทีวีในห้องพิจารณาคดี (The Climax)
    ///
    /// ถ้าใส่ <see cref="videoClip"/> ไว้ จะเล่นวิดีโอจริงผ่าน VideoPlayer
    /// ถ้าไม่ใส่ (ตอนนี้โปรเจกต์ยังไม่มีไฟล์วิดีโอ) จะจำลองภาพเองตอนรันไทม์:
    /// ถนนกลางคืนมุมมองกล้องหน้ารถ ขับมาเจอคนข้ามถนน ชน แล้วเร่งหนี
    /// ภาพจำลองสร้างไว้ใต้พื้นโลก 400 m และถ่ายด้วยกล้องแยกลง RenderTexture
    /// วัสดุทั้งหมดเป็น Unlit — ไฟในห้องพิจารณาคดีจะได้ไม่ไปสว่างฉากถนนกลางคืน
    /// </summary>
    public class DashcamClip : MonoBehaviour
    {
        [Header("จอ")]
        [Tooltip("ผิวจอของทีวี (กล่องบาง ๆ ชื่อ Screen)")]
        public Renderer screen;
        [Tooltip("กลุ่มจอที่ไม่ถูกยืดสเกล ใช้เป็นที่แปะตัวหนังสือ REC/เวลา")]
        public Transform overlayRoot;
        [Tooltip("วัสดุ URP/Unlit ต้นแบบ — อ้างจาก asset เพื่อให้เชเดอร์ติดไปกับ build")]
        public Material unlitBase;

        [Header("ฟุตเทจจริง (ถ้ามี)")]
        public VideoClip videoClip;
        [Tooltip("วินาทีที่ชนในวิดีโอจริง")]
        public float videoImpactAt = 3.9f;

        [Header("ภาพจำลอง")]
        public float duration = 8.5f;
        public Vector3 dioramaOrigin = new Vector3(0f, -400f, 0f);
        public int width = 640, height = 360;
        [Tooltip("เวลาที่ขึ้นในคลิป")]
        public string stampDate = "2569/08/14";
        public int stampHour = 1, stampMinute = 47, stampSecond = 18;

        [Header("เสียงแชมป์ในคลิป (หลังชนกี่วินาที)")]
        public float voiceDelay = 0.9f;

        public bool Playing { get; private set; }
        public bool Finished { get; private set; }
        public bool ImpactHappened { get; private set; }
        public bool VoiceDue { get; private set; }

        RenderTexture _rt;
        Camera _dcam;
        Transform _diorama, _ped, _flash;
        Material _screenMat, _originalMat;
        readonly List<GameObject> _overlay = new List<GameObject>();
        readonly List<GameObject> _cracks = new List<GameObject>();
        TextMeshPro _rec, _stamp, _speed, _noSignal;
        Material _pedMat;
        float _clock;

        // ───────────────────────── public ─────────────────────────

        public IEnumerator Play()
        {
            if (Playing || screen == null) yield break;
            Playing = true;
            Finished = false;
            ImpactHappened = false;
            VoiceDue = false;

            _rt = new RenderTexture(width, height, 16) { name = "DashcamRT" };
            _rt.Create();
            _originalMat = screen.sharedMaterial;
            _screenMat = MakeMat(Color.white);
            _screenMat.SetTexture("_BaseMap", _rt);
            _screenMat.mainTexture = _rt;
            screen.sharedMaterial = _screenMat;

            if (videoClip != null) yield return PlayVideo();
            else yield return PlaySimulation();

            Finished = true;
            Playing = false;
        }

        /// <summary>ศาลสั่งไม่รับฟัง — จอดับเหลือ NO SIGNAL</summary>
        public void NoSignal()
        {
            if (_dcam != null) _dcam.enabled = false;
            if (_screenMat != null)
            {
                _screenMat.SetTexture("_BaseMap", null);
                _screenMat.mainTexture = null;
                _screenMat.SetColor("_BaseColor", new Color(0.02f, 0.03f, 0.08f));
            }
            foreach (var o in _overlay) if (o != null) o.SetActive(false);
            foreach (var c in _cracks) if (c != null) c.SetActive(false);
            if (overlayRoot != null)
            {
                if (_noSignal == null) _noSignal = Label("NoSignal", new Vector3(0f, 0f, 0f), new Vector2(0.9f, 0.16f), Color.white);
                _noSignal.text = "NO SIGNAL";
                _noSignal.gameObject.SetActive(true);
            }
            if (_diorama != null) Destroy(_diorama.gameObject);
        }

        void OnDestroy()
        {
            if (_rt != null) _rt.Release();
            if (_diorama != null) Destroy(_diorama.gameObject);
        }

        // ───────────────────────── วิดีโอจริง ─────────────────────────

        IEnumerator PlayVideo()
        {
            var vp = gameObject.GetComponent<VideoPlayer>();
            if (vp == null) vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.clip = videoClip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = _rt;
            vp.Prepare();
            while (!vp.isPrepared) yield return null;
            vp.Play();
            float len = (float)videoClip.length, e = 0f;
            while (e < len)
            {
                e += Time.deltaTime;
                if (!ImpactHappened && e >= videoImpactAt) ImpactHappened = true;
                if (!VoiceDue && e >= videoImpactAt + voiceDelay) VoiceDue = true;
                yield return null;
            }
        }

        // ───────────────────────── ภาพจำลอง ─────────────────────────

        IEnumerator PlaySimulation()
        {
            BuildDiorama();
            BuildOverlay();

            const float laneX = -1.9f;           // ขับชิดซ้ายแบบถนนไทย
            const float camY = 1.25f;
            float z = 0f, x = laneX, speed = 18f;
            float pedStart = 1.6f, pedSpeed = 1.35f;
            float impactAt = -1f;
            Vector3 pedVel = Vector3.zero;
            bool pedFlying = false, pedDown = false;
            float e = 0f;
            _clock = stampHour * 3600f + stampMinute * 60f + stampSecond;

            while (e < duration)
            {
                float dt = Time.deltaTime;
                e += dt;
                _clock += dt;

                // ── ความเร็วรถ: วิ่งปกติ -> ชน -> แตะเบรกแวบเดียว -> เร่งหนี ──
                if (impactAt < 0f) speed = 18f;
                else
                {
                    float since = e - impactAt;
                    speed = since < 0.8f ? Mathf.Lerp(18f, 6f, since / 0.8f) : Mathf.MoveTowards(speed, 24f, 9f * dt);
                    // หักหลบร่างที่ล้มอยู่ แล้วขับต่อไปเลย
                    x = Mathf.MoveTowards(x, since < 2.2f ? -0.6f : laneX, 1.2f * dt);
                }
                z += speed * dt;

                float bob = Mathf.Sin(e * 9f) * 0.012f;
                Vector3 shake = Vector3.zero;
                if (impactAt >= 0f && e - impactAt < 0.45f)
                    shake = Random.insideUnitSphere * 0.08f * (1f - (e - impactAt) / 0.45f);
                _dcam.transform.localPosition = new Vector3(x, camY + bob, z) + shake;

                // ── คนข้ามถนน (ลูกสาวป้าสมร) ──
                if (!pedFlying && !pedDown && e >= pedStart)
                    _ped.localPosition += Vector3.right * pedSpeed * dt;
                float dz = _ped.localPosition.z - z;
                // แสงไฟหน้ารถตกกระทบ: ยิ่งใกล้ยิ่งสว่าง
                _pedMat.SetColor("_BaseColor", Color.Lerp(new Color(0.10f, 0.10f, 0.12f), new Color(0.86f, 0.84f, 0.80f),
                                                          Mathf.InverseLerp(45f, 6f, Mathf.Abs(dz))));

                if (impactAt < 0f && dz < 1.8f && Mathf.Abs(_ped.localPosition.x - x) < 1.2f)
                {
                    impactAt = e;
                    ImpactHappened = true;
                    pedFlying = true;
                    pedVel = new Vector3(-1.2f, 4.2f, 13f);
                    StartCoroutine(Flash());
                    foreach (var c in _cracks) c.SetActive(true);
                }
                if (pedFlying)
                {
                    pedVel += Vector3.down * 9.8f * dt;
                    _ped.localPosition += pedVel * dt;
                    _ped.Rotate(new Vector3(420f, 0f, 160f) * dt, Space.Self);
                    if (_ped.localPosition.y <= 0.25f && pedVel.y < 0f)
                    {
                        pedFlying = false;
                        pedDown = true;
                        _ped.localPosition = new Vector3(_ped.localPosition.x, 0.25f, _ped.localPosition.z);
                        _ped.localRotation = Quaternion.Euler(0f, 30f, 90f);
                    }
                }
                if (impactAt >= 0f && !VoiceDue && e - impactAt >= voiceDelay) VoiceDue = true;

                // ── ตัวหนังสือบนจอ ──
                if (_rec != null) _rec.enabled = Mathf.Repeat(e, 1f) < 0.6f;
                if (_stamp != null)
                {
                    int s = Mathf.FloorToInt(_clock);
                    _stamp.text = string.Format("{0}  {1:00}:{2:00}:{3:00}", stampDate, (s / 3600) % 24, (s / 60) % 60, s % 60);
                }
                if (_speed != null) _speed.text = string.Format("FRONT  {0:0} km/h", speed * 3.6f);
                yield return null;
            }

            // ภาพค้างเฟรมสุดท้าย
            if (_dcam != null) _dcam.enabled = false;
        }

        IEnumerator Flash()
        {
            if (_flash == null) yield break;
            _flash.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.08f);
            _flash.gameObject.SetActive(false);
        }

        Material MakeMat(Color c)
        {
            Material m = unlitBase != null ? new Material(unlitBase) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c);
            return m;
        }

        GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            var col = g.GetComponent<Collider>();
            if (col != null) Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = MakeMat(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return g;
        }

        void BuildDiorama()
        {
            _diorama = new GameObject("~DashcamDiorama").transform;
            _diorama.position = dioramaOrigin;
            var d = _diorama;

            Color road = new Color(0.05f, 0.05f, 0.055f);
            Color verge = new Color(0.018f, 0.03f, 0.02f);
            Prim(PrimitiveType.Cube, "Road", d, new Vector3(0f, -0.05f, 130f), new Vector3(9f, 0.1f, 280f), road);
            Prim(PrimitiveType.Cube, "Verge_L", d, new Vector3(-9f, -0.06f, 130f), new Vector3(9f, 0.1f, 280f), verge);
            Prim(PrimitiveType.Cube, "Verge_R", d, new Vector3(9f, -0.06f, 130f), new Vector3(9f, 0.1f, 280f), verge);
            Prim(PrimitiveType.Cube, "Edge_L", d, new Vector3(-4.3f, 0.005f, 130f), new Vector3(0.12f, 0.02f, 280f), new Color(0.3f, 0.3f, 0.3f));
            Prim(PrimitiveType.Cube, "Edge_R", d, new Vector3(4.3f, 0.005f, 130f), new Vector3(0.12f, 0.02f, 280f), new Color(0.3f, 0.3f, 0.3f));
            for (float z = 4f; z < 270f; z += 8f)
                Prim(PrimitiveType.Cube, "Dash", d, new Vector3(0f, 0.006f, z), new Vector3(0.14f, 0.02f, 3f), new Color(0.45f, 0.38f, 0.16f));

            // เสาไฟถนนฝั่งซ้าย + วงแสงบนพื้น (ไฟปลอม เพราะทุกอย่างเป็น unlit)
            for (float z = 18f; z < 270f; z += 26f)
            {
                Prim(PrimitiveType.Cube, "Pole", d, new Vector3(-5.6f, 3.5f, z), new Vector3(0.14f, 7f, 0.14f), new Color(0.04f, 0.04f, 0.04f));
                Prim(PrimitiveType.Cube, "Lamp", d, new Vector3(-4.6f, 6.9f, z), new Vector3(0.5f, 0.12f, 0.3f), new Color(1f, 0.72f, 0.36f));
                Prim(PrimitiveType.Cylinder, "Pool", d, new Vector3(-3.2f, 0.012f, z), new Vector3(7f, 0.002f, 7f), new Color(0.15f, 0.11f, 0.06f));
            }
            // ต้นไม้เป็นเงาดำสองข้างทาง
            var rng = new System.Random(1447);
            for (int i = 0; i < 40; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float z = 10f + i * 6.5f;
                float s = 2.5f + (float)rng.NextDouble() * 3f;
                Prim(PrimitiveType.Sphere, "Tree", d, new Vector3(side * (7.5f + (float)rng.NextDouble() * 4f), s * 0.8f, z),
                     Vector3.one * s, new Color(0.01f, 0.018f, 0.012f));
            }

            // คนข้ามถนน: เดินจากขอบซ้ายเข้ามาในเลนของรถ
            _ped = new GameObject("Pedestrian").transform;
            _ped.SetParent(d, false);
            _ped.localPosition = new Vector3(-5.3f, 0.8f, 72f);
            var body = Prim(PrimitiveType.Capsule, "Body", _ped, Vector3.zero, new Vector3(0.45f, 0.8f, 0.45f), Color.gray);
            _pedMat = body.GetComponent<Renderer>().sharedMaterial;
            var head = Prim(PrimitiveType.Sphere, "Head", _ped, new Vector3(0f, 0.92f, 0f), Vector3.one * 0.24f, Color.gray);
            head.GetComponent<Renderer>().sharedMaterial = _pedMat;

            // กล้องหน้ารถ + ฝากระโปรงสีแดงขอบล่างของภาพ + วงไฟหน้ารถบนถนน
            var camGo = new GameObject("DashcamCamera");
            camGo.transform.SetParent(d, false);
            camGo.transform.localPosition = new Vector3(-1.9f, 1.25f, 0f);
            camGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            _dcam = camGo.AddComponent<Camera>();
            _dcam.targetTexture = _rt;
            _dcam.clearFlags = CameraClearFlags.SolidColor;
            _dcam.backgroundColor = new Color(0.012f, 0.016f, 0.03f);
            _dcam.fieldOfView = 72f;
            _dcam.nearClipPlane = 0.05f;
            _dcam.farClipPlane = 150f;
            _dcam.depth = -10f;

            Prim(PrimitiveType.Cube, "Hood", camGo.transform, new Vector3(0f, -0.62f, 1.5f), new Vector3(1.9f, 0.08f, 1.6f), new Color(0.30f, 0.02f, 0.03f));
            var beam = Prim(PrimitiveType.Cylinder, "HeadlightPool", camGo.transform, new Vector3(0f, -1.22f, 11f), new Vector3(7f, 0.002f, 16f), new Color(0.20f, 0.18f, 0.14f));
            beam.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            _flash = Prim(PrimitiveType.Cube, "Flash", camGo.transform, new Vector3(0f, 0f, 0.3f), new Vector3(2f, 2f, 0.01f), Color.white).transform;
            _flash.gameObject.SetActive(false);
        }

        // ───────────────────────── ตัวหนังสือบนจอ ─────────────────────────

        void BuildOverlay()
        {
            if (overlayRoot == null) return;
            // คนดูยืนฝั่ง +Z ของจอ มองกลับมาทาง -Z — แกน +X ของจอจึงอยู่ทาง "ซ้ายมือ" คนดู
            _rec = Label("Rec", new Vector3(0.44f, 0.28f, 0f), new Vector2(0.3f, 0.07f), new Color(1f, 0.15f, 0.12f));
            _rec.text = "● REC";
            _rec.alignment = TextAlignmentOptions.Left;
            _stamp = Label("Stamp", new Vector3(-0.3f, 0.28f, 0f), new Vector2(0.56f, 0.06f), Color.white);
            _stamp.alignment = TextAlignmentOptions.Right;
            _speed = Label("Speed", new Vector3(0.36f, -0.29f, 0f), new Vector2(0.46f, 0.055f), Color.white);
            _speed.alignment = TextAlignmentOptions.Left;
            _overlay.Add(_rec.gameObject);
            _overlay.Add(_stamp.gameObject);
            _overlay.Add(_speed.gameObject);

            // รอยร้าวกระจกหน้ารถตอนชน — เส้นดำแผ่ออกจากจุดเดียว
            for (int i = 0; i < 9; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.name = "Crack_" + i;
                c.transform.SetParent(overlayRoot, false);
                float ang = i * 40f + (i % 2) * 13f;
                float len = 0.12f + (i % 3) * 0.07f;
                Vector3 dir = Quaternion.Euler(0f, 0f, ang) * Vector3.right;
                c.transform.localPosition = new Vector3(-0.12f, 0.06f, 0.034f) + dir * len * 0.5f;
                c.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                c.transform.localScale = new Vector3(len, 0.004f, 0.002f);
                c.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.85f, 0.88f, 0.92f));
                c.SetActive(false);
                _cracks.Add(c);
            }
        }

        TextMeshPro Label(string name, Vector3 pos, Vector2 box, Color colour)
        {
            var g = new GameObject(name);
            g.transform.SetParent(overlayRoot, false);
            var t = g.AddComponent<TextMeshPro>();
            if (t.font == null) t.font = TMP_Settings.defaultFontAsset;
            var rt = g.GetComponent<RectTransform>();
            rt.sizeDelta = box;
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.05f;
            t.fontSizeMax = 40f;
            t.color = colour;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            // ข้อความ TMP 3D อ่านได้จากฝั่ง -Z จึงหมุนครึ่งรอบให้หันออกหน้าจอ (+Z)
            g.transform.localPosition = new Vector3(pos.x, pos.y, 0.035f);
            g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return t;
        }
    }
}
