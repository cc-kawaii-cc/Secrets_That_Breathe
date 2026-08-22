using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// จุดกดโต้ตอบที่ผูกกับบท ACT 2
    ///
    /// สืบทอดจาก <see cref="StoryInteractable"/> เพื่อให้ <see cref="PlayerInteractor"/>
    /// เดิมหาเจอโดยไม่ต้องแก้อะไร แต่เพิ่มการปิด objective / รับหลักฐาน / เล่นบทให้
    /// </summary>
    public class Act2Interactable : StoryInteractable
    {
        [Header("ACT 2")]
        [Tooltip("id ของ objective ที่จะปิดเมื่อกดสำเร็จ")]
        public string objectiveId;
        [Tooltip("id หลักฐานที่จะได้รับ (เว้นว่าง = ไม่ได้อะไร)")]
        public string evidenceId;

        [Header("เงื่อนไข")]
        [Tooltip("ต้องปิด objective นี้ก่อนถึงจะกดได้ (เว้นว่าง = กดได้เลย)")]
        public string requiresObjective;
        [Tooltip("ต้องมีหลักฐานชิ้นนี้ก่อน (เว้นว่าง = ไม่ต้อง)")]
        public string requiresEvidence;
        [TextArea] public string blockedLine = "ยังไม่ถึงเวลา";

        [Header("บทสนทนา")]
        public string speakerName;
        [TextArea(1, 4)] public string[] dialogueLines;

        [Header("ปลายทาง")]
        [Tooltip("ใส่ชื่อ scene ถ้าการกดนี้คือการไปด่านต่อไป")]
        public string loadSceneOnComplete;
        [Tooltip("ตั้งจุดเช็คพอยต์ตรงนี้เมื่อกดสำเร็จ")]
        public bool setCheckpointHere;

        public override void DoInteract()
        {
            if (hasInteracted) return;

            var director = Act2Director.Instance;
            if (director != null && !Unlocked(director))
            {
                if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(blockedLine))
                    SubtitleManager.Instance.Show(blockedLine);
                return;   // ยังไม่ mark hasInteracted — กลับมากดใหม่ได้
            }

            hasInteracted = true;
            onInteract.Invoke();
            GameEvents.Interacted(string.IsNullOrEmpty(objectName) ? name : objectName);

            if (dialogueLines != null && dialogueLines.Length > 0 && DialogueManager.Instance != null)
            {
                string[] names = new string[dialogueLines.Length];
                for (int i = 0; i < names.Length; i++) names[i] = speakerName;
                DialogueManager.Instance.StartDialogue(names, dialogueLines, Resolve);
                return;
            }
            Resolve();
        }

        bool Unlocked(Act2Director director)
        {
            if (!string.IsNullOrEmpty(requiresObjective) && !director.IsDone(requiresObjective)) return false;
            if (!string.IsNullOrEmpty(requiresEvidence) && !director.HasEvidence(requiresEvidence)) return false;
            return true;
        }

        void Resolve()
        {
            var director = Act2Director.Instance;
            if (director != null)
            {
                if (!string.IsNullOrEmpty(evidenceId)) director.GainEvidence(evidenceId);
                if (!string.IsNullOrEmpty(objectiveId)) director.Complete(objectiveId);
                if (setCheckpointHere)
                {
                    var pm = PlayerManager.Instance;
                    if (pm != null && pm.playerRoot != null)
                        director.SetCheckpoint(pm.playerRoot.transform.position, pm.playerRoot.transform.rotation);
                }
            }

            if (!string.IsNullOrEmpty(inspectText) && SubtitleManager.Instance != null)
                SubtitleManager.Instance.Show(inspectText);

            if (!string.IsNullOrEmpty(loadSceneOnComplete) && GameManager.Instance != null)
                GameManager.Instance.LoadScene(loadSceneOnComplete);
        }
    }
}
