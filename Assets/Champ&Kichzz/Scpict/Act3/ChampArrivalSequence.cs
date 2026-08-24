using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// ฉากแชมป์กลับบ้าน — เริ่มทำงานตอนผู้เล่นมุดเข้าที่ซ่อน
    ///
    /// รถขับจาก Champ Spawn เข้ามาจอดในลานจอด แชมป์ลงจากรถ เดินตามเส้นทางขึ้นบ้าน
    /// ไปยืนคุยโทรศัพท์ที่ห้องนอนชั้นสอง ระหว่างนี้กล้องของผู้เล่นถูกยึดไปเกาะตัวแชมป์
    /// พอแชมป์ถึงที่แล้วกล้องคืนกลับมาที่ตาผู้เล่นเหมือนเดิม แล้วฉากแอบฟังจึงเริ่ม
    ///
    /// กล้องถูกย้ายทั้งตำแหน่งและมุม ไม่ใช่แค่หันตาม เพราะผู้เล่นมุดอยู่ในตู้ชั้นสอง
    /// ถ้าหันอย่างเดียวจะได้ภาพทะลุกำแพงไปมองลานจอด ซึ่งดูพังกว่าไม่ทำ
    ///
    /// กด <see cref="skipKey"/> ระหว่างคัตซีนได้ตลอด — ข้ามตรงไปฉากแอบฟังทันที ไม่ต้องรอ
    /// ดูรถขับ/แชมป์เดินซ้ำทุกครั้งที่โดนจับแล้วต้องเล่นคัตซีนนี้ใหม่ (ไม่กดก็ดูต่อได้ตามปกติ)
    /// </summary>
    public class ChampArrivalSequence : MonoBehaviour
    {
        [Header("นักแสดง")]
        public GameObject car;
        [Tooltip("ตัวแชมป์ — เริ่มฉากโดยอยู่ในรถ แล้วค่อยลงมาเดิน")]
        public GameObject champ;

        [Header("เส้นทางรถ")]
        [Tooltip("จุดที่รถโผล่เข้ามา (หมุด Champ Spawn)")]
        public Transform carSpawn;
        [Tooltip("ช่องจอดที่รถจะเข้าไปจอด")]
        public Transform carParkSpot;
        public float driveSpeed = 9f;
        [Tooltip("หน่วงหลังจอดสนิท ก่อนแชมป์เปิดประตูลงมา")]
        public float pauseAfterPark = 1.2f;

        [Header("เส้นทางเดินของแชมป์")]
        [Tooltip("จุดที่แชมป์ยืนหลังลงจากรถ")]
        public Transform champExitSpot;
        [Tooltip("หมุดตามลำดับ จากลานจอดขึ้นไปห้องนอนชั้นสอง")]
        public Transform[] walkPath;
        [Tooltip("จุดยืนคุยโทรศัพท์ใน ZONE_SuiteBedroom")]
        public Transform phoneSpot;
        public float walkSpeed = 3.4f;
        public float turnSpeed = 8f;
        public float arriveDistance = 0.35f;

        [Header("กล้องผู้เล่น")]
        public bool lockCameraOnChamp = true;
        [Tooltip("ตำแหน่งกล้องเทียบตัวแชมป์ (หลัง/เหนือ)")]
        public Vector3 cameraOffset = new Vector3(2.2f, 3.0f, -5.0f);
        [Tooltip("จุดเล็งบนตัวแชมป์")]
        public Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
        [Tooltip("ยิ่งน้อยกล้องยิ่งตามหน่วง ๆ นุ่มนวล")]
        public float cameraDamp = 4f;

        [Header("ต่อด้วยฉากแอบฟัง")]
        public SilentEavesdropZone eavesdrop;

        [Header("ข้ามคัตซีน")]
        [Tooltip("ปิดถ้าไม่อยากให้ข้ามได้เลย")]
        public bool allowSkip = true;
        public Key skipKey = Key.Space;
        [Tooltip("ข้อความบอกปุ่มข้าม โชว์บน HUD ระหว่างคัตซีนนี้ทำงานอยู่")]
        public string skipHint = "ข้ามไปแอบฟังเลย";

        public bool Running { get; private set; }
        /// <summary>คัตซีนนี้กำลังเล่นอยู่และยังกดข้ามได้ — HUD ใช้โชว์ปุ่มลัด</summary>
        public static bool SkipAvailable { get; private set; }
        /// <summary>ข้อความ "[ปุ่ม] คำอธิบาย" ของอินสแตนซ์ที่กำลังเล่นอยู่ — ไว้ให้ HUD โชว์ตรงๆ</summary>
        public static string SkipHintText
        {
            get { return _active != null ? "[" + _active.skipKey + "] " + _active.skipHint : ""; }
        }
        static ChampArrivalSequence _active;

        Camera _cam;
        Transform _camParent;
        Vector3 _camLocalPos;
        Quaternion _camLocalRot;
        bool _camTaken;
        Coroutine _routine;
        bool _skipRequested;

        void Start() { Reset(); }

        public void Play()
        {
            if (Running) return;
            _routine = StartCoroutine(Run());
        }

        /// <summary>คืนทุกอย่างกลับจุดเริ่ม — ใช้ตอนผู้เล่นโดนจับแล้วต้องเล่นฉากใหม่</summary>
        public void Reset()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            Running = false;
            SkipAvailable = false;
            if (_active == this) _active = null;
            _skipRequested = false;
            ReleaseCamera();

            if (car != null)
            {
                if (carSpawn != null)
                    car.transform.SetPositionAndRotation(carSpawn.position, carSpawn.rotation);
                car.SetActive(false);
            }
            if (champ != null)
            {
                if (car != null) champ.transform.SetParent(car.transform, true);
                champ.SetActive(false);
            }
        }

        IEnumerator Run()
        {
            Running = true;
            _skipRequested = false;
            _active = this;
            SkipAvailable = allowSkip;

            if (car != null)
            {
                if (carSpawn != null)
                    car.transform.SetPositionAndRotation(carSpawn.position, carSpawn.rotation);
                car.SetActive(true);
            }
            if (champ != null)
            {
                if (car != null) champ.transform.SetParent(car.transform, false);
                champ.SetActive(true);
            }

            TakeCamera();

            // ── รถขับเข้ามาจอด ──
            if (car != null && carParkSpot != null)
                yield return MoveActor(car.transform, carParkSpot.position, driveSpeed, true);
            if (pauseAfterPark > 0f) yield return WaitSkippable(pauseAfterPark);

            // ── แชมป์ลงจากรถ ──
            if (champ != null)
            {
                champ.transform.SetParent(null, true);
                if (champExitSpot != null)
                    yield return MoveActor(champ.transform, champExitSpot.position, walkSpeed, true);
            }

            // ── เดินขึ้นบ้านตามหมุด ──
            if (champ != null && walkPath != null)
            {
                for (int i = 0; i < walkPath.Length; i++)
                {
                    if (walkPath[i] == null) continue;
                    yield return MoveActor(champ.transform, walkPath[i].position, walkSpeed, true);
                }
            }

            // ── ถึงจุดคุยโทรศัพท์ ──
            if (champ != null && phoneSpot != null)
            {
                yield return MoveActor(champ.transform, phoneSpot.position, walkSpeed, true);
                champ.transform.rotation = phoneSpot.rotation;
            }

            ReleaseCamera();
            Running = false;
            SkipAvailable = false;
            if (_active == this) _active = null;
            _routine = null;

            if (eavesdrop != null) eavesdrop.Begin();
        }

        /// <summary>
        /// เดิน/ขับตัวละครไปยังจุดหมาย พร้อมหันหน้าตามทิศที่ไป
        ///
        /// เช็คปุ่มข้ามทุกเฟรม — กดแล้ว "วาร์ป" ไปจุดหมายทันทีโดยไม่รอเฟรมถัดไป
        /// เพราะ Run() เรียก MoveActor/WaitSkippable ต่อกันหลายตัว แต่ละตัวที่เจอ
        /// _skipRequested อยู่แล้วจะจบทันทีในเฟรมเดียวกันหมด ทำให้ทั้งคัตซีนกระโดด
        /// ไปจบในเฟรมเดียวเมื่อกดข้าม แทนที่จะค่อยๆ วาร์ปทีละช่วง
        /// </summary>
        IEnumerator MoveActor(Transform actor, Vector3 target, float speed, bool face)
        {
            while (actor != null)
            {
                CheckSkip();
                if (_skipRequested) { actor.position = target; yield break; }

                Vector3 flat = target - actor.position;
                flat.y = 0f;
                if (flat.magnitude <= arriveDistance) yield break;

                if (face && flat.sqrMagnitude > 0.0001f)
                    actor.rotation = Quaternion.Slerp(actor.rotation,
                        Quaternion.LookRotation(flat.normalized), turnSpeed * Time.deltaTime);

                // ไต่ระดับความสูงตามหมุดด้วย เพื่อให้เดินขึ้นบันไดไปชั้นสองได้
                actor.position = Vector3.MoveTowards(actor.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        IEnumerator WaitSkippable(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                CheckSkip();
                if (_skipRequested) yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        void CheckSkip()
        {
            if (!allowSkip || _skipRequested) return;
            var kb = Keyboard.current;
            if (kb != null && kb[skipKey].wasPressedThisFrame)
            {
                _skipRequested = true;
                SkipAvailable = false;
            }
        }

        // ───────────────────────── กล้อง ─────────────────────────

        void TakeCamera()
        {
            if (!lockCameraOnChamp || _camTaken) return;
            var pm = PlayerManager.Instance;
            if (pm == null || pm.playerCamera == null) return;

            _cam = pm.playerCamera;
            _camParent = _cam.transform.parent;
            _camLocalPos = _cam.transform.localPosition;
            _camLocalRot = _cam.transform.localRotation;
            // ถอดออกจากตัวผู้เล่นก่อน ไม่งั้นการหมุนตัวผู้เล่นจะลากกล้องตามไปด้วย
            _cam.transform.SetParent(null, true);
            _camTaken = true;
        }

        void ReleaseCamera()
        {
            if (!_camTaken || _cam == null) { _camTaken = false; return; }
            _cam.transform.SetParent(_camParent, false);
            _cam.transform.localPosition = _camLocalPos;
            _cam.transform.localRotation = _camLocalRot;
            _camTaken = false;
        }

        void LateUpdate()
        {
            if (!_camTaken || _cam == null) return;

            // ตามตัวที่กำลังเคลื่อนที่อยู่: ตอนแรกคือรถ พอลงจากรถแล้วคือตัวแชมป์
            Transform focus = null;
            if (champ != null && champ.activeInHierarchy && champ.transform.parent == null) focus = champ.transform;
            else if (car != null && car.activeInHierarchy) focus = car.transform;
            if (focus == null) return;

            Vector3 wantPos = focus.TransformPoint(cameraOffset);
            Vector3 lookAt = focus.position + lookOffset;

            float k = 1f - Mathf.Exp(-cameraDamp * Time.deltaTime);   // เฟรมเรตไม่มีผลกับความหน่วง
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, wantPos, k);

            Vector3 dir = lookAt - _cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation,
                    Quaternion.LookRotation(dir), k);
        }
    }
}
