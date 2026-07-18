using UnityEngine;
public class SceneExitDoor : MonoBehaviour
{
    [Tooltip("ชื่อ scene ถัดไป (ต้องอยู่ใน Build Settings)")]
    public string nextSceneName;

    public void GoToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[SceneExitDoor] ยังไม่ได้ใส่ชื่อ scene ถัดไปใน Inspector");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("[SceneExitDoor] ไม่พบ GameManager — ตรวจว่ามี Bootstrap scene/Manager แกนหรือยัง");
            return;
        }
        GameManager.Instance.LoadScene(nextSceneName);
    }
}