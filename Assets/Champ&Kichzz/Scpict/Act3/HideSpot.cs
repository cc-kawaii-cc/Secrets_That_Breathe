using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// ที่ซ่อน (ตู้เสื้อผ้าในห้องนอนชั้นสอง) — เล็งแล้วกด E เพื่อมุดเข้าไปแอบฟัง
    ///
    /// ตอนซ่อน: ผู้เล่นถูกย้ายเข้าจุดซ่อน ล็อกการเดิน และยามมองไม่เห็น
    /// จากนั้นฉากแชมป์ขับรถกลับบ้านจะเริ่มทำงาน
    ///
    /// ปลดล็อกให้ซ่อนใหม่ได้ด้วย <see cref="Rearm"/> เผื่อผู้เล่นโดนจับตอนแอบฟัง
    /// แล้วต้องย้อนกลับมาลองอีกรอบ
    /// </summary>
    public class HideSpot : Act2Interactable
    {
        [Header("ที่ซ่อน")]
        [Tooltip("จุดที่ผู้เล่นจะไปยืนตอนซ่อน (เว้นว่าง = ยืนที่เดิม)")]
        public Transform hideViewpoint;
        [Tooltip("ออกจากที่ซ่อนแล้วกลับมายืนตรงไหน (เว้นว่าง = จุดเดิมก่อนซ่อน)")]
        public Transform exitViewpoint;

        [Header("ฉากที่จะเริ่มเมื่อซ่อนสำเร็จ")]
        public ChampArrivalSequence sequence;

        /// <summary>ที่ซ่อนที่ผู้เล่นกำลังมุดอยู่ (null = ไม่ได้ซ่อน)</summary>
        public static HideSpot Current { get; private set; }
        public static bool PlayerHidden { get { return Current != null; } }

        Vector3 _returnPos;
        Quaternion _returnRot;

        public override void DoInteract()
        {
            if (hasInteracted) return;
            base.DoInteract();
            if (!hasInteracted) return;   // ยังถูกล็อกด้วยเงื่อนไข objective
            Enter();
        }

        public void Enter()
        {
            if (Current == this) return;
            var pm = PlayerManager.Instance;
            if (pm == null || pm.playerRoot == null) return;

            _returnPos = pm.playerRoot.transform.position;
            _returnRot = pm.playerRoot.transform.rotation;

            // เช็คพอยต์ไว้ตรงนี้ — โดนจับตอนแอบฟังจะได้กลับมาโผล่ใกล้ ๆ ไม่ต้องเดินใหม่ทั้งด่าน
            var director = Act2Director.Instance;
            if (director != null) director.SetCheckpoint(_returnPos, _returnRot);

            Current = this;
            if (StealthTarget.Instance != null) StealthTarget.Instance.Hidden = true;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Cutscene);

            if (hideViewpoint != null)
                pm.TeleportTo(hideViewpoint.position, hideViewpoint.rotation);

            if (sequence != null) sequence.Play();
        }

        /// <summary>ออกจากที่ซ่อน คืนการควบคุมให้ผู้เล่น</summary>
        public void Exit()
        {
            if (Current != this) return;
            Current = null;
            if (StealthTarget.Instance != null) StealthTarget.Instance.Hidden = false;

            var pm = PlayerManager.Instance;
            if (pm != null && pm.playerRoot != null)
            {
                Vector3 pos = exitViewpoint != null ? exitViewpoint.position : _returnPos;
                Quaternion rot = exitViewpoint != null ? exitViewpoint.rotation : _returnRot;
                pm.TeleportTo(pos, rot);
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Exploration);
        }

        /// <summary>ให้กดซ่อนได้อีกครั้ง (ใช้หลังผู้เล่นโดนจับแล้วต้องเริ่มฉากใหม่)</summary>
        public void Rearm() { hasInteracted = false; }
    }
}
