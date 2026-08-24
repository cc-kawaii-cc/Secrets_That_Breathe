using System.Collections.Generic;
using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// เดินตามเส้นทางที่ level builder วางไว้ให้ (PatrolRoute/Point_00, Point_01, ...)
    ///
    /// ไม่ใช้ NavMesh — ด่านพวกนี้เป็นพื้นเรียบล้วน และการเดินตรงระหว่างหมุด
    /// อ่านง่ายกว่าสำหรับผู้เล่นที่ต้องคาดเดาจังหวะยาม ซึ่งคือหัวใจของ stealth
    /// </summary>
    public class GuardPatrol : MonoBehaviour
    {
        [Header("การเดิน")]
        public float speed = 1.5f;
        public float turnSpeed = 4f;
        [Tooltip("หยุดที่หมุดกี่วินาที")]
        public float waitAtPoint = 2.5f;
        [Tooltip("เดินไปกลับ แทนที่จะวนเป็นวง")]
        public bool pingPong = true;

        [Header("ตอนสงสัย")]
        public float investigateSpeed = 2.4f;
        [Tooltip("เข้าใกล้จุดที่สนใจแค่ไหนถือว่าถึงแล้ว")]
        public float arriveDistance = 0.6f;

        [Header("พื้นและสิ่งกีดขวาง")]
        [Tooltip("ถ่วงให้เท้าติดพื้นเสมอ — กันยามลอยกลางอากาศเมื่อหมุดอยู่คนละระดับ")]
        public bool stickToGround = true;
        public LayerMask groundMask = ~0;
        [Tooltip("เริ่มยิงเรย์หาพื้นจากเหนือเท้าเท่าไร")]
        public float groundSnapUp = 0.6f;
        [Tooltip("มองหาพื้นลงไปไกลสุดเท่าไร — อย่าตั้งเกินความสูงระหว่างชั้น ไม่งั้นยามจะร่วงลงชั้นล่าง")]
        public float groundProbe = 2.0f;

        [Tooltip("หยุดเมื่อมีอะไรขวาง แล้วเปลี่ยนไปหมุดถัดไป — กันยามเดินทะลุกำแพง")]
        public bool avoidObstacles = true;
        public LayerMask obstacleMask = ~0;
        public float bodyRadius = 0.4f;
        [Tooltip("ตรวจล่วงหน้าไกลแค่ไหน")]
        public float probeAhead = 0.7f;
        [Tooltip("ความสูงที่ใช้ตรวจสิ่งกีดขวาง วัดจากเท้า (ต่ำกว่านี้ถือว่าก้าวข้ามได้)")]
        public float chestHeight = 0.9f;

        readonly List<Vector3> _points = new List<Vector3>();
        GuardVision _vision;
        int _index;
        int _dir = 1;
        float _waitTimer;
        bool _alerted;
        Vector3 _homePos;
        Quaternion _homeRot;
        float _feetOffset;

        /// <summary>ตำแหน่งเท้า — จุดหมุนของโมเดลมักอยู่กลางตัว ไม่ใช่ที่พื้น</summary>
        Vector3 Feet { get { return transform.position - Vector3.up * _feetOffset; } }

        void Awake()
        {
            _vision = GetComponent<GuardVision>();
            _homePos = transform.position;
            _homeRot = transform.rotation;

            var rend = GetComponentInChildren<Renderer>();
            _feetOffset = rend != null ? transform.position.y - rend.bounds.min.y : 0f;

            var route = transform.Find("PatrolRoute");
            if (route != null)
                foreach (Transform p in route)
                    if (p.name.StartsWith("Point_")) _points.Add(p.position);

            // หมุดจุดเดียว (หรือไม่มีเลย) = ยามยืนประจำที่
            if (_points.Count == 1) _points.Clear();
        }

        // วางให้ติดพื้นตั้งแต่เฟรมแรก เผื่อถูกวางค้างไว้ลอย ๆ หรือจมพื้นในหน้าต่าง Scene
        void Start() { StickToGround(); }

        public void SetAlerted(bool alerted) { _alerted = alerted; }

        /// <summary>เรียกหลังผู้เล่นโดนจับ — ให้ยามกลับไปยืนที่เดิม</summary>
        public void ResetToPost()
        {
            _alerted = false;
            _index = 0;
            _dir = 1;
            _waitTimer = 0f;
            transform.SetPositionAndRotation(_homePos, _homeRot);
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration)) return;
            if (AlertDirector.Instance != null && AlertDirector.Instance.IsResolving) return;

            if (_alerted && _vision != null && _vision.HasPointOfInterest)
            {
                MoveTowards(_vision.PointOfInterest, investigateSpeed);
                return;
            }
            if (_points.Count == 0) return;

            if (_waitTimer > 0f) { _waitTimer -= Time.deltaTime; return; }
            if (MoveTowards(_points[_index], speed)) Advance();
        }

        void Advance()
        {
            _waitTimer = waitAtPoint;
            if (pingPong)
            {
                if (_index + _dir < 0 || _index + _dir >= _points.Count) _dir = -_dir;
                _index = Mathf.Clamp(_index + _dir, 0, _points.Count - 1);
            }
            else _index = (_index + 1) % _points.Count;
        }

        /// <summary>คืน true เมื่อถึงที่หมายแล้ว (หรือไปต่อไม่ได้เพราะมีอะไรขวาง)</summary>
        bool MoveTowards(Vector3 target, float moveSpeed)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.magnitude <= arriveDistance) return true;

            Vector3 dir = flat.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

            // ชนกำแพงแล้วยังดันต่อ = เดินทะลุ ผังด่านก็หมดความหมาย
            // ถือว่า "ไปต่อไม่ได้" แล้วให้ไปหมุดถัดไปแทน ยามจึงไม่ค้างมุดกำแพงอยู่ตรงนั้น
            if (avoidObstacles && Blocked(dir)) return true;

            transform.position += dir * moveSpeed * Time.deltaTime;
            StickToGround();
            return false;
        }

        /// <summary>มีอะไรขวางในทิศที่กำลังจะเดินไหม (ไม่นับตัวเอง ผู้เล่น และ trigger)</summary>
        bool Blocked(Vector3 dir)
        {
            Vector3 origin = Feet + Vector3.up * chestHeight;
            var hits = Physics.SphereCastAll(origin, bodyRadius, dir, probeAhead,
                                             obstacleMask, QueryTriggerInteraction.Ignore);
            var player = StealthTarget.Instance;
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].collider.transform;
                if (hit.IsChildOf(transform)) continue;
                if (player != null && hit.IsChildOf(player.transform)) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// วางเท้าให้แตะพื้นที่อยู่ข้างใต้เสมอ
        /// groundProbe ตั้งสั้นกว่าความสูงระหว่างชั้นไว้ ยามชั้นบนจะได้ไม่ร่วงไปเกาะพื้นชั้นล่าง
        /// </summary>
        void StickToGround()
        {
            if (!stickToGround) return;

            Vector3 from = Feet + Vector3.up * groundSnapUp;
            var hits = Physics.RaycastAll(from, Vector3.down, groundSnapUp + groundProbe,
                                          groundMask, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].point.y > best) best = hits[i].point.y;
            }
            if (float.IsNegativeInfinity(best)) return;

            Vector3 p = transform.position;
            p.y = best + _feetOffset;
            transform.position = p;
        }

        void OnDrawGizmosSelected()
        {
            var route = transform.Find("PatrolRoute");
            if (route == null) return;
            Gizmos.color = Color.cyan;
            Vector3 prev = Vector3.zero;
            bool first = true;
            foreach (Transform p in route)
            {
                if (!p.name.StartsWith("Point_")) continue;
                Gizmos.DrawWireSphere(p.position, 0.3f);
                if (!first) Gizmos.DrawLine(prev, p.position);
                prev = p.position;
                first = false;
            }
        }
    }
}
