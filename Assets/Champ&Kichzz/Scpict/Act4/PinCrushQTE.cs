using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// 4.5 พิธีกรรมทำลายศรัทธา — กดปุ่มรัว ๆ "บีบ" เข็มกลัดทนายความจนเลือดซึม แล้วกระชากขว้างลงท่อ
    ///
    /// มือเป็นชิ้นส่วน primitive ติดหน้ากล้องผู้เล่น (ยังไม่มีโมเดลมือ) หลอดจะค่อย ๆ ลดเองถ้าหยุดกด
    /// ยิ่งบีบแรง มือยิ่งสั่น เลือดยิ่งหยดถี่ จอยิ่งแดง หยดเลือดตกถึงพื้นแล้วค้างเป็นคราบ
    /// </summary>
    public class PinCrushQTE : MonoBehaviour
    {
        [Header("วัสดุ (builder ใส่ให้)")]
        public Material skin;
        public Material gold;
        public Material blood;

        [Header("ข้อความ")]
        public string prompt = "กด [Space] รัว ๆ — บีบให้แน่น";

        [Header("ความยาก")]
        [Tooltip("เพิ่มต่อการกดหนึ่งครั้ง")]
        public float pressGain = 0.075f;
        [Tooltip("ลดลงต่อวินาทีถ้าไม่กด")]
        public float decay = 0.22f;

        [Header("เสียง (เว้นว่างได้)")]
        public AudioClip squeezeSfx;
        public AudioClip crushSfx;

        public bool PinLanded { get; private set; }

        bool _forced;

        /// <summary>ข้าม QTE ให้เต็มหลอดทันที — ใช้เทสอัตโนมัติ (Input System ไม่รับปุ่มตอน Unity ไม่ได้โฟกัส)</summary>
        public void ForceComplete() { _forced = true; }

        Transform _rig, _fist, _pin, _cam;
        Vector3 _fistScale, _rigHome;
        readonly List<Drop> _drops = new List<Drop>();
        readonly List<GameObject> _stains = new List<GameObject>();
        float _groundY;

        class Drop { public Transform t; public float vy; }

        static bool Pressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        /// <summary>เล่น QTE จนหลอดเต็ม groundY = ระดับพื้นที่หยดเลือดจะตกไปค้าง</summary>
        public IEnumerator Run(Transform cam, float groundY)
        {
            _cam = cam;
            _groundY = groundY;
            BuildRig();
            var hud = Act4HUD.Instance;
            if (hud != null) hud.ShowQTE(prompt);

            _forced = false;
            float progress = 0f, pulse = 0f, dripTimer = 0f;
            while (progress < 1f)
            {
                float dt = Time.deltaTime;
                if (Pressed())
                {
                    progress += pressGain;
                    pulse = 1f;
                    if (squeezeSfx != null) AudioSource.PlayClipAtPoint(squeezeSfx, cam.position, 0.6f);
                }
                // ยิ่งใกล้เต็มยิ่งลดช้า ช่วงท้ายจะได้ไม่รู้สึกว่าลากไม่ขึ้น
                progress = Mathf.Max(0f, progress - decay * (1f - progress * 0.5f) * dt);
                if (_forced) progress = 1f;
                pulse = Mathf.MoveTowards(pulse, 0f, dt * 6f);

                if (hud != null)
                {
                    hud.SetQTE(progress);
                    hud.SetRedTint(progress * 0.32f + pulse * 0.08f);
                }

                // มือสั่น + บีบแน่นขึ้นตามแรง
                float amp = 0.002f + progress * 0.01f;
                _rig.localPosition = _rigHome + new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f);
                _fist.localScale = new Vector3(_fistScale.x * (1f - pulse * 0.05f - progress * 0.05f),
                                               _fistScale.y * (1f + pulse * 0.03f), _fistScale.z);

                // เลือดเริ่มซึมเมื่อบีบได้ราวหนึ่งในสี่
                if (progress > 0.25f)
                {
                    dripTimer -= dt;
                    if (dripTimer <= 0f)
                    {
                        SpawnDrop();
                        dripTimer = Mathf.Lerp(0.7f, 0.09f, progress);
                    }
                }
                UpdateDrops(dt);
                yield return null;
            }

            if (hud != null) hud.HideQTE();
            if (crushSfx != null) AudioSource.PlayClipAtPoint(crushSfx, cam.position);
            // CRUSH: กำแน่นสุดแล้วนิ่งไปครู่หนึ่ง
            _fist.localScale = new Vector3(_fistScale.x * 0.88f, _fistScale.y * 1.05f, _fistScale.z);
            for (int i = 0; i < 4; i++) SpawnDrop();
            float hold = 0f;
            while (hold < 0.6f)
            {
                hold += Time.deltaTime;
                if (hud != null) hud.SetRedTint(Mathf.Lerp(0.5f, 0.2f, hold / 0.6f));
                UpdateDrops(Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>กระชากเข็มกลัดขว้างลงท่อระบายน้ำ (โค้งตกตามแรงโน้มถ่วง)</summary>
        public IEnumerator Throw(Vector3 target, float flightSeconds)
        {
            PinLanded = false;
            if (_rig == null || _pin == null) { PinLanded = true; yield break; }
            var hud = Act4HUD.Instance;

            // เงื้อ
            Vector3 home = _rig.localPosition;
            yield return MoveRig(home, home + new Vector3(0.10f, 0.12f, -0.06f), 0.22f);
            // ฟาดลง แล้วปล่อยเข็มกลัด
            yield return MoveRig(_rig.localPosition, home + new Vector3(-0.08f, -0.10f, 0.14f), 0.08f);
            _pin.SetParent(null, true);
            Vector3 start = _pin.position;
            float e = 0f;
            while (e < flightSeconds)
            {
                e += Time.deltaTime;
                float k = e / flightSeconds;
                Vector3 p = Vector3.Lerp(start, target, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.6f;
                _pin.position = p;
                _pin.Rotate(new Vector3(900f, 500f, 0f) * Time.deltaTime, Space.Self);
                UpdateDrops(Time.deltaTime);
                if (hud != null) hud.SetRedTint(Mathf.Lerp(0.2f, 0f, k));
                yield return null;
            }
            // ลอดตะแกรงหายลงท่อ
            e = 0f;
            while (e < 0.45f)
            {
                e += Time.deltaTime;
                _pin.position = target + Vector3.down * (e / 0.45f) * 0.4f;
                yield return null;
            }
            _pin.gameObject.SetActive(false);
            PinLanded = true;
        }

        /// <summary>เก็บมือออกจากหน้ากล้อง (คราบเลือดบนพื้นยังอยู่)</summary>
        public void HideHand()
        {
            if (_rig != null) Destroy(_rig.gameObject);
            for (int i = 0; i < _drops.Count; i++) if (_drops[i].t != null) Destroy(_drops[i].t.gameObject);
            _drops.Clear();
            if (Act4HUD.Instance != null) Act4HUD.Instance.SetRedTint(0f);
        }

        IEnumerator MoveRig(Vector3 from, Vector3 to, float seconds)
        {
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                _rig.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / seconds));
                UpdateDrops(Time.deltaTime);
                yield return null;
            }
            _rig.localPosition = to;
        }

        // ───────────────────────── เลือด ─────────────────────────

        void SpawnDrop()
        {
            if (_fist == null) return;
            var g = Part(PrimitiveType.Sphere, "BloodDrop", null, Vector3.zero, Vector3.one * 0.014f, blood);
            g.transform.position = _fist.position + _cam.rotation * new Vector3(Random.Range(-0.03f, 0.03f), -0.045f, Random.Range(-0.01f, 0.03f));
            _drops.Add(new Drop { t = g.transform, vy = 0f });
        }

        void UpdateDrops(float dt)
        {
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                if (d.t == null) { _drops.RemoveAt(i); continue; }
                d.vy -= 9.8f * dt;
                d.t.position += Vector3.up * d.vy * dt;
                if (d.t.position.y > _groundY + 0.004f) continue;

                // ถึงพื้น: แผ่เป็นคราบ
                d.t.position = new Vector3(d.t.position.x, _groundY + 0.003f, d.t.position.z);
                float s = Random.Range(0.03f, 0.07f);
                d.t.localScale = new Vector3(s, 0.002f, s * Random.Range(0.8f, 1.3f));
                d.t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                _stains.Add(d.t.gameObject);
                _drops.RemoveAt(i);
            }
        }

        // ───────────────────────── มือ ─────────────────────────

        void BuildRig()
        {
            _rig = new GameObject("~KhemFist").transform;
            _rig.SetParent(_cam, false);
            // มุมขวาล่าง ไม่ทับข้อความ/หลอด QTE กลางจอ
            _rigHome = new Vector3(0.11f, -0.15f, 0.40f);
            _rig.localPosition = _rigHome;
            _rig.localRotation = Quaternion.Euler(-10f, -12f, 8f);

            Part(PrimitiveType.Capsule, "Forearm", _rig, new Vector3(0.10f, -0.13f, -0.14f), new Vector3(0.075f, 0.15f, 0.075f), skin)
                .transform.localRotation = Quaternion.Euler(55f, 0f, -32f);
            _fist = Part(PrimitiveType.Sphere, "Fist", _rig, Vector3.zero, new Vector3(0.105f, 0.09f, 0.115f), skin).transform;
            _fistScale = _fist.localScale;
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Sphere, "Knuckle_" + i, _fist, new Vector3(-0.3f + i * 0.2f, 0.36f, 0.38f), Vector3.one * 0.3f, skin);
            Part(PrimitiveType.Capsule, "Thumb", _fist, new Vector3(-0.42f, 0.12f, 0.22f), new Vector3(0.24f, 0.36f, 0.24f), skin)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 72f);

            // เข็มกลัดทนายความ: เหรียญทองโผล่ระหว่างนิ้ว + เข็มแหลม
            _pin = new GameObject("LawyerPin").transform;
            _pin.SetParent(_rig, false);
            _pin.localPosition = new Vector3(0.0f, 0.045f, 0.045f);
            _pin.localRotation = Quaternion.Euler(70f, 0f, 0f);
            Part(PrimitiveType.Cylinder, "Badge", _pin, Vector3.zero, new Vector3(0.034f, 0.003f, 0.034f), gold);
            Part(PrimitiveType.Cylinder, "Needle", _pin, new Vector3(0.012f, -0.004f, 0f), new Vector3(0.002f, 0.016f, 0.002f), gold)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            var c = g.GetComponent<Collider>();
            if (c != null) Destroy(c);
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            if (m != null) r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
    }
}
