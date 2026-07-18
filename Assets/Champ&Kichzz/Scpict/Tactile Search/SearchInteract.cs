using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // เพิ่มไลบรารี Input System

public class SearchInteract : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference holdInteractAction; // รับค่าปุ่มกดค้าง

    [Header("UI References")]
    public Image handIcon;
    public Image progressBar;
    
    [Header("Settings")]
    public float searchTime = 2.0f;
    public float interactDistance = 3f;
    private float currentTimer = 0f;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        handIcon.gameObject.SetActive(false);
        progressBar.fillAmount = 0;
    }

    void Update()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.CompareTag("Searchable"))
            {
                handIcon.gameObject.SetActive(true);
                
                // ตรวจสอบว่ากำลังกดปุ่มค้างอยู่หรือไม่
                if (holdInteractAction.action.IsPressed())
                {
                    currentTimer += Time.deltaTime;
                    progressBar.fillAmount = currentTimer / searchTime;
                    if (currentTimer >= searchTime)
                    {
                        CompleteSearch(hit.collider.gameObject);
                    }
                }
                else
                {
                    ResetSearch();
                }
                return;
            }
        }
        handIcon.gameObject.SetActive(false);
        ResetSearch();
    }

    void ResetSearch()
    {
        currentTimer = 0;
        progressBar.fillAmount = 0;
    }

    void CompleteSearch(GameObject target)
    {
        Debug.Log("Find Item!: " + target.name);
        Destroy(target);
        ResetSearch();
        handIcon.gameObject.SetActive(false);
    }

    private void OnEnable() => holdInteractAction?.action.Enable();
    private void OnDisable() => holdInteractAction?.action.Disable();
}