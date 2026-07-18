using System.Collections;
using UnityEngine;
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