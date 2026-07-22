using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI subtitleText;
    public float defaultDuration = 3f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (subtitleText) subtitleText.gameObject.SetActive(false);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ตอนข้าม scene: subtitleText ของ scene เก่าถูกทำลาย — หาตัวใหม่ใต้ Canvas ของ player ใหม่
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;
        if (subtitleText != null) return;

        routine = null; // coroutine เก่าตายไปพร้อม text เดิมแล้ว
        var newPlayer = FindFirstObjectByType<PlayerMovement>();
        if (newPlayer == null) return;
        Transform t = newPlayer.transform.Find("Canvas/SubtitleText");
        if (t != null)
        {
            subtitleText = t.GetComponent<TextMeshProUGUI>();
            subtitleText.gameObject.SetActive(false); // ซ่อนเหมือนตอน Awake ปกติ
        }
    }

    public void Show(string line, float? duration = null)
        => Show(new[] { line }, duration);
    
    public void Show(string[] lines, float? perLine = null)
    {
        if (subtitleText == null || lines == null || lines.Length == 0) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(lines, perLine ?? defaultDuration));
    }

    public void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (subtitleText) subtitleText.gameObject.SetActive(false);
    }

    private IEnumerator ShowRoutine(string[] lines, float perLine)
    {
        subtitleText.gameObject.SetActive(true);
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            subtitleText.text = line;
            yield return new WaitForSeconds(perLine);
        }
        subtitleText.gameObject.SetActive(false);
        routine = null;
    }
}