using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// เดินเข้าไปในเขตแล้วปิด objective ทันที — ใช้กับบีทที่แค่ "ไปให้ถึง"
    /// เช่น ข้ามฟลอร์เต้น หรือเข้าใกล้ขอบโซน VIP
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Act2ReachZone : MonoBehaviour
    {
        public string objectiveId;
        [Tooltip("ต้องเป็น objective ปัจจุบันเท่านั้นถึงจะนับ กันปิดข้ามลำดับ")]
        public bool onlyWhenCurrent = true;
        public bool setCheckpointHere = true;
        [Tooltip("ต้องหมอบอยู่ถึงจะนับ — ใช้สอนระบบหมอบ")]
        public bool requireCrouching = false;
        [TextArea] public string noticeText;
        [Tooltip("ใส่ชื่อ scene ถ้าเขตนี้คือทางไปด่านต่อไป")]
        public string loadSceneOnComplete;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other) { TryComplete(other); }
        void OnTriggerStay(Collider other) { if (requireCrouching) TryComplete(other); }

        void TryComplete(Collider other)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null) return;

            var director = Act2Director.Instance;
            if (director == null || string.IsNullOrEmpty(objectiveId)) return;
            if (director.IsDone(objectiveId)) return;
            if (onlyWhenCurrent && (director.Current == null || director.Current.id != objectiveId)) return;

            if (requireCrouching)
            {
                var crouch = player.GetComponent<CrouchAbility>();
                if (crouch == null || !crouch.IsCrouching) return;
            }

            if (setCheckpointHere)
                director.SetCheckpoint(player.transform.position, player.transform.rotation);
            if (!string.IsNullOrEmpty(noticeText)) director.Notice(noticeText);
            director.Complete(objectiveId);

            if (!string.IsNullOrEmpty(loadSceneOnComplete) && GameManager.Instance != null)
                GameManager.Instance.LoadScene(loadSceneOnComplete);
        }
    }
}
