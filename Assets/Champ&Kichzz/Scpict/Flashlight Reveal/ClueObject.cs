using UnityEngine;
public class ClueObject : MonoBehaviour
{
    public GameObject glintEffect; 
    void Start()
    {
        if(glintEffect != null) glintEffect.SetActive(false);
    }
    public void OnLightEnter()
    {
        if(glintEffect != null) glintEffect.SetActive(true);
    }
    public void OnLightExit()
    {
        if(glintEffect != null) glintEffect.SetActive(false);
    }
}
