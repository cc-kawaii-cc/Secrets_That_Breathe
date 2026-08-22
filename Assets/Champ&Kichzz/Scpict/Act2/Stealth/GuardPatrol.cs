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

        readonly List<Vector3> _points = new List<Vector3>();
        GuardVision _vision;
        int _index;
        int _dir = 1;
        float _waitTimer;
        bool _alerted;
        Vector3 _homePos;
        Quaternion _homeRot;

        void Awake()
        {
            _vision = GetComponent<GuardVision>();
            _homePos = transform.position;
            _homeRot = transform.rotation;

            var route = transform.Find("PatrolRoute");
            if (route != null)
                foreach (Transform p in route)
                    if (p.name.StartsWith("Point_")) _points.Add(p.position);

            // หมุดจุดเดียว (หรือไม่มีเลย) = ยามยืนประจำที่
            if (_points.Count == 1) _points.Clear();
        }

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

        /// <summary>คืน true เมื่อถึงที่หมายแล้ว</summary>
        bool MoveTowards(Vector3 target, float moveSpeed)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.magnitude <= arriveDistance) return true;

            Vector3 dir = flat.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            transform.position += dir * moveSpeed * Time.deltaTime;
            return false;
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
