using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Header("Components")]
    public Light flashlightSource;
    public AudioSource clickSoundSource;
    [Header("Settings")]
    public bool isFlashlightOn = false;
    private bool isFlickering = false; 

    void Start()
    {
        UpdateFlashlightState();
    }

    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if (context.performed && !isFlickering) 
        {
            isFlashlightOn = !isFlashlightOn;
            UpdateFlashlightState();
            if (clickSoundSource != null && clickSoundSource.clip != null)
            {
                clickSoundSource.PlayOneShot(clickSoundSource.clip);
            }
        }
    }

    void UpdateFlashlightState()
    {
        if (flashlightSource != null && !isFlickering)
        {
            flashlightSource.enabled = isFlashlightOn;
        }
    }
    
    public void StartGlitchFlicker(float duration)
    {
        if (!isFlickering && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlickerRoutine(duration));
        }
    }

    private IEnumerator FlickerRoutine(float duration)
    {
        isFlickering = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if(flashlightSource) flashlightSource.enabled = Random.value > 0.5f;
            float waitTime = Random.Range(0.05f, 0.2f);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }
        isFlickering = false;
        UpdateFlashlightState();
    }
}