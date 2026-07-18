using System.Collections;
using UnityEngine;
public class CinematicCamera : MonoBehaviour
{
    public static CinematicCamera Instance { get; private set; }

    [Header("Optional: Cinemachine virtual camera (ถ้าใช้)")]
    public GameObject gameplayVCam;

    private Camera _cam;
    private bool _overriding;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private Camera Cam
    {
        get
        {
            if (_cam == null)
                _cam = PlayerManager.Instance ? PlayerManager.Instance.playerCamera : Camera.main;
            return _cam;
        }
    }
    
    private void BeginOverride()
    {
        if (_overriding) return;
        _overriding = true;
        if (PlayerManager.Instance && PlayerManager.Instance.mouseLook)
            PlayerManager.Instance.mouseLook.enabled = false;
        if (gameplayVCam) gameplayVCam.SetActive(false);
    }
    
    public void EndOverride()
    {
        if (!_overriding) return;
        _overriding = false;
        if (gameplayVCam) gameplayVCam.SetActive(true);
    }
    
    public IEnumerator LookAt(Transform target, float duration, Vector3 worldOffset = default)
    {
        if (Cam == null || target == null) yield break;
        BeginOverride();

        Quaternion startRot = Cam.transform.rotation;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            Vector3 dir = (target.position + worldOffset) - Cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                Cam.transform.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(dir), k);
            yield return null;
        }
    }
    
    public IEnumerator TurnBodyAndLookAt(Transform body, Transform target,
                                         float duration, Vector3 camWorldOffset = default)
    {
        if (target == null) yield break;
        BeginOverride();

        Quaternion startBody = body ? body.rotation : Quaternion.identity;
        Quaternion startCam  = Cam ? Cam.transform.rotation : Quaternion.identity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            if (body)
            {
                Vector3 bodyDir = target.position - body.position;
                bodyDir.y = 0f;
                if (bodyDir.sqrMagnitude > 0.0001f)
                    body.rotation = Quaternion.Slerp(startBody, Quaternion.LookRotation(bodyDir), k);
            }
            if (Cam)
            {
                Vector3 camDir = (target.position + camWorldOffset) - Cam.transform.position;
                if (camDir.sqrMagnitude > 0.0001f)
                    Cam.transform.rotation = Quaternion.Slerp(startCam, Quaternion.LookRotation(camDir), k);
            }
            yield return null;
        }
    }
}