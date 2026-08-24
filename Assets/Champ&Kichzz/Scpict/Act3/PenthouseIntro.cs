using System.Collections;
using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// บอกเป้าหมายกว้างๆ ของ ACT 3 ตอนเข้าฉากนี้ครั้งแรก — แอบเข้าไป ฟังให้ได้ความ แล้วรีบออกมา
    /// เล่นครั้งเดียว เช็คจาก Act2Director ว่ายังไม่เคยผ่านประตูแรกมาก่อน (กันพูดซ้ำทุกครั้งที่โหลดซีน)
    /// </summary>
    public class PenthouseIntro : MonoBehaviour
    {
        [TextArea] public string line =
            "แอบเข้าไปในเพนต์เฮาส์ของแชมป์จากข้างนอก ฟังให้ได้ความ แล้วรีบออกมาก่อนใครจะเห็น";
        public string gateObjectiveId = "OBJ_A3_01_EnterHouse";
        public float delay = 1.5f;
        public float showSeconds = 5f;

        IEnumerator Start()
        {
            var director = Act2Director.Instance;
            if (director != null && director.IsDone(gateObjectiveId)) yield break;

            yield return new WaitForSeconds(delay);
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(line, showSeconds);
        }
    }
}
