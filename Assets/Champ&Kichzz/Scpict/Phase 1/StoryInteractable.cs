using UnityEngine;
using UnityEngine.Events;

public class StoryInteractable : MonoBehaviour
{
    [Header("object information")]
    public string objectName;
    [TextArea(2, 4)]
    public string inspectText;

    [Header("Post-survey events")]
    public UnityEvent onInteract;
    public bool hasInteracted = false; 

    public void DoInteract()
    {
        if (!hasInteracted)
        {
            Debug.Log($"[explore {objectName}] Think intensely.: {inspectText}");
            onInteract.Invoke();
            hasInteracted = true;
        }
    }
}