using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI subtitleText;
    public float subtitleDuration = 3f;

    [Header("Dialogue Texts (กด + เพื่อเพิ่มคำพูด)")]
    [TextArea(2, 3)]
    public string[] dialogueLines;
    private bool hasTriggered = false;
    private Coroutine activeCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            activeCoroutine = StartCoroutine(PlaySubtitles());
        }
    }

    private IEnumerator PlaySubtitles()
    {
        if (subtitleText == null || dialogueLines == null || dialogueLines.Length == 0) yield break;
        subtitleText.gameObject.SetActive(true);
        foreach (string line in dialogueLines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                subtitleText.text = line;
                yield return new WaitForSeconds(subtitleDuration);
            }
        }
        subtitleText.gameObject.SetActive(false);
    }
}
