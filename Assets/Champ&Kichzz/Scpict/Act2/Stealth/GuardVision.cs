using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    public enum GuardAwareness { Unaware, Suspicious, Alerted }

    /// <summary>
    /// สายตาและหูของยามหนึ่งคน
    ///
    /// ความสงสัยเป็นค่าต่อเนื่อง 0..1 ไม่ใช่สวิตช์ เห็นแวบเดียวจึงยังไม่โดนจับ
    /// แต่ยืนโด่อยู่กลางลานจะไต่ขึ้นเร็ว — ผู้เล่นมีจังหวะถอยเสมอ
    ///
    /// อ่านค่า "เห็นง่ายแค่ไหน" จาก <see cref="StealthTarget"/> ที่เดียว
    /// ปรับสมดุลทั้งเกมจึงทำได้จากตัวนั้นตัวเดียว
    /// </summary>
    public class GuardVision : MonoBehaviour
    {
        [Header("สายตา")]
        public float viewDistance = 12f;
        [Range(20f, 180f)] public float viewAngle = 95f;
        [Tooltip("ระยะที่เห็นแน่นอนไม่ว่าจะหันไปทางไหน (เฉียดตัว)")]
        public float proximityRadius = 1.4f;
        public Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);
        public LayerMask sightBlockers = ~0;

        [Header("ความสงสัย")]
        [Tooltip("วินาทีที่ใช้จาก 0 ถึงจับได้ เมื่อเห็นเต็มๆ ระยะประชิด")]
        public float secondsToCatch = 3.5f;
        [Tooltip("ความสงสัยลดลงต่อวินาทีเมื่อไม่เห็น")]
        public float decayPerSecond = 0.6f;
        [Tooltip("เกินค่านี้ = เริ่มสงสัย หันมามอง")]
        [Range(0.1f, 0.9f)] public float suspiciousThreshold = 0.3f;

        [Header("การได้ยิน")]
        public bool canHear = true;
        [Tooltip("ได้ยินเสียงแล้วเดินไปดูนานกี่วินาที")]
        public float investigateSeconds = 6f;

        public float Suspicion { get; private set; }
        public GuardAwareness Awareness { get; private set; }
        /// <summary>จุดที่ยามกำลังสนใจ (เสียงหรือตัวผู้เล่นครั้งสุดท้าย)</summary>
        public Vector3 PointOfInterest { get; private set; }
        public bool HasPointOfInterest { get { return _investigateTimer > 0f; } }

        GuardPatrol _patrol;
        Transform _player;
        float _investigateTimer;

        void Awake() { _patrol = GetComponent<GuardPatrol>(); }

        /// <summary>ล้างความสงสัยทั้งหมด — ใช้หลังผู้เล่นโดนจับ กันโดนจับซ้ำทันทีที่โผล่</summary>
        public void ResetSuspicion()
        {
            Suspicion = 0f;
            Awareness = GuardAwareness.Unaware;
            _investigateTimer = 0f;
            if (_patrol != null) _patrol.SetAlerted(false);
        }

        void OnEnable() { StealthEvents.OnNoise += OnNoise; }
        void OnDisable() { StealthEvents.OnNoise -= OnNoise; }

        Vector3 Eye { get { return transform.position + transform.TransformVector(eyeOffset); } }

        void OnNoise(Vector3 pos, float radius, float strength)
        {
            if (!canHear || Awareness == GuardAwareness.Alerted) return;
            float d = Vector3.Distance(Eye, pos);
            if (d > radius) return;

            // เสียงไกลก็แค่เอะใจ เสียงใกล้ถึงจะเดินไปดู
            float weight = Mathf.Clamp01(1f - d / Mathf.Max(0.01f, radius)) * strength;
            Suspicion = Mathf.Min(Suspicion + weight * 0.25f, 0.95f);
            if (weight < 0.15f) return;
            PointOfInterest = pos;
            _investigateTimer = investigateSeconds;
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration)) return;
            var alert = AlertDirector.Instance;
            if (alert != null && alert.IsResolving) return;
            if (alert != null && alert.InGracePeriod)
            {
                // เพิ่งโดนจับแล้วโผล่กลับมา ให้ตั้งหลักก่อน
                Suspicion = 0f;
                return;
            }

            float exposure = SeePlayer();
            if (exposure > 0f)
            {
                Suspicion += exposure / Mathf.Max(0.1f, secondsToCatch) * Time.deltaTime;
                PointOfInterest = _player.position;
                _investigateTimer = investigateSeconds;
            }
            else
            {
                Suspicion -= decayPerSecond * Time.deltaTime;
            }
            Suspicion = Mathf.Clamp01(Suspicion);

            if (_investigateTimer > 0f) _investigateTimer -= Time.deltaTime;

            GuardAwareness next = Suspicion >= 1f ? GuardAwareness.Alerted
                                : Suspicion >= suspiciousThreshold ? GuardAwareness.Suspicious
                                : GuardAwareness.Unaware;
            if (next != Awareness)
            {
                Awareness = next;
                if (_patrol != null) _patrol.SetAlerted(next != GuardAwareness.Unaware);
                if (next == GuardAwareness.Alerted && AlertDirector.Instance != null)
                    AlertDirector.Instance.Caught(this);
            }

            // สงสัยแล้วหันไปทางจุดที่สนใจ
            if (Awareness != GuardAwareness.Unaware && HasPointOfInterest)
            {
                Vector3 flat = PointOfInterest - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.04f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(flat), 4f * Time.deltaTime);
            }
        }

        /// <summary>0 = มองไม่เห็น, มากกว่านั้น = เห็นชัดแค่ไหน (คิดระยะ มุม และค่าซ่อนตัว)</summary>
        float SeePlayer()
        {
            if (_player == null)
            {
                var st = StealthTarget.Instance;
                if (st == null) return 0f;
                _player = st.transform;
            }
            var target = StealthTarget.Instance;
            if (target == null) return 0f;

            Vector3 eye = Eye;
            Vector3 toPlayer = _player.position - eye;
            float dist = toPlayer.magnitude;
            if (dist > viewDistance) return 0f;

            bool inCone = Vector3.Angle(transform.forward, toPlayer) <= viewAngle * 0.5f;
            bool pointBlank = dist <= proximityRadius;
            if (!inCone && !pointBlank) return 0f;

            // มีอะไรบังอยู่ไหม — เล็งไปที่ลำตัว ไม่ใช่จุด origin ที่อาจจมพื้น
            // ต้องใช้ RaycastAll แล้วกรองเอง เพราะจุดตาอยู่ในตัวยามเอง
            // Linecast ธรรมดาจะชนแคปซูลของยามทุกครั้งจนมองไม่เห็นอะไรเลย
            Vector3 chest = _player.position + Vector3.up * 0.2f;
            Vector3 ray = chest - eye;
            var blockers = Physics.RaycastAll(eye, ray.normalized, ray.magnitude, sightBlockers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < blockers.Length; i++)
            {
                Transform hit = blockers[i].collider.transform;
                if (hit.IsChildOf(transform)) continue;          // ตัวยามเอง
                if (hit.IsChildOf(_player)) continue;            // ตัวผู้เล่นเอง
                return 0f;
            }

            // เดิมที่ระยะไกลสุดยังได้ค่า 0.35 ยามจึงจับได้ข้ามลานภายในไม่กี่วินาที
            // ตอนนี้ไต่ลงเป็นเส้นโค้ง ขอบระยะ = 0 จริง ต้องเข้ามาใกล้ถึงจะเริ่มนับ
            float t = Mathf.Clamp01(dist / viewDistance);
            float distFactor = (1f - t) * (1f - t * 0.5f);
            return target.Visibility * distFactor;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 eye = Application.isPlaying ? Eye : transform.position + transform.TransformVector(eyeOffset);
            Gizmos.color = Awareness == GuardAwareness.Alerted ? Color.red
                         : Awareness == GuardAwareness.Suspicious ? new Color(1f, 0.6f, 0f) : Color.yellow;
            Gizmos.DrawWireSphere(eye, proximityRadius);
            Vector3 l = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewDistance;
            Vector3 r = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * viewDistance;
            Gizmos.DrawLine(eye, eye + l);
            Gizmos.DrawLine(eye, eye + r);
            Gizmos.DrawLine(eye + l, eye + r);
        }
    }
}
