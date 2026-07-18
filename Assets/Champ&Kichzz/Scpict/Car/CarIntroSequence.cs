using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI; 

public class CarIntroSequence : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player; 
    public PlayerMovement playerMovementScript; 
    public CharacterController playerController; 

    [Header("Car Setup")]
    public Transform carSeat; 
    public Transform destination; 
    public float carSpeed = 5f;

    [Header("Door & Exit Setup")]
    public Transform door; 
    public Transform exitPoint; 
    public float doorOpenAngle = 70f; 
    public float doorOpenSpeed = 2f; 

    [Header("UI & Effects")]
    public GameObject interactUI; 
    public Image fadeImage; 
    public float fadeSpeed = 3f; 

    [Header("Input Actions")]
    public InputActionReference interactAction; 

    private bool isDriving = true;
    private bool canExit = false;
    private bool isExiting = false; 

    void Start()
    {
        
        playerMovementScript.enabled = false;
        playerController.enabled = false; 

        
        player.transform.position = carSeat.position;
        player.transform.SetParent(carSeat); 

     
        if (interactUI != null) interactUI.SetActive(false);
        
        
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        if (isDriving)
        {
            
            Vector3 targetPosition = new Vector3(destination.position.x, transform.position.y, destination.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, carSpeed * Time.deltaTime);

            
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isDriving = false;
                canExit = true;
                
               
                if (interactUI != null) interactUI.SetActive(true);
            }
        }

        
        if (canExit && !isExiting && interactAction.action.WasPressedThisFrame())
        {
            StartCoroutine(ExitCarSequence()); 
        }
    }

    IEnumerator ExitCarSequence()
    {
        isExiting = true;
        canExit = false;

       
        if (interactUI != null) interactUI.SetActive(false);

        
        Quaternion startRot = door.localRotation;
        Quaternion endRot = Quaternion.Euler(door.localEulerAngles.x, door.localEulerAngles.y + doorOpenAngle, door.localEulerAngles.z);
        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime * doorOpenSpeed;
            door.localRotation = Quaternion.Lerp(startRot, endRot, time);
            yield return null; 
        }

        
        if (fadeImage != null)
        {
            time = 0;
            Color c = fadeImage.color;
            while (time < 1)
            {
                time += Time.deltaTime * fadeSpeed;
                c.a = Mathf.Lerp(0, 1, time); 
                fadeImage.color = c;
                yield return null;
            }
        }

        
        player.transform.SetParent(null);
        player.transform.position = exitPoint.position;
        player.transform.rotation = exitPoint.rotation;

        
        if (fadeImage != null)
        {
            time = 0;
            Color c = fadeImage.color;
            while (time < 1)
            {
                time += Time.deltaTime * fadeSpeed;
                c.a = Mathf.Lerp(1, 0, time); 
                fadeImage.color = c;
                yield return null;
            }
        }

        
        playerController.enabled = true;
        playerMovementScript.enabled = true;
    }

    private void OnEnable()
    {
        if (interactAction != null) interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null) interactAction.action.Disable();
    }
}