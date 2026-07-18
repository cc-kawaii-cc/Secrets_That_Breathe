using System.Collections;
using UnityEngine;
public class PoliceConversationSequence : MonoBehaviour
{
    [Header("Police NPC")]
    public GameObject policeNPC;
    public Transform policeTargetPoint;
    public float policeSpeed = 4f;
    public Animator policeAnimator;

    [Header("Dialogue")]
    public string[] speakerNames;
    [TextArea(3, 5)] public string[] dialogLines;

    private bool _started;
    private Vector3 _originalPos;
    private Quaternion _originalRot;

    private void Start()
    {
        if (policeNPC != null)
        {
            _originalPos = policeNPC.transform.position;
            _originalRot = policeNPC.transform.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_started || !other.CompareTag("Player")) return;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _started = true;
        var pm = PlayerManager.Instance;
        GameManager.Instance.SetState(GameState.Cutscene);

        // ตำรวจวิ่งเข้ามาหา + หันมองผู้เล่น
        if (policeAnimator) policeAnimator.SetBool("isRunning", true);
        while (Vector3.Distance(policeNPC.transform.position, policeTargetPoint.position) > 0.1f)
        {
            policeNPC.transform.position = Vector3.MoveTowards(
                policeNPC.transform.position, policeTargetPoint.position, policeSpeed * Time.deltaTime);

            Vector3 look = pm.playerBody.position;
            look.y = policeNPC.transform.position.y;
            policeNPC.transform.LookAt(look);
            yield return null;
        }
        if (policeAnimator) policeAnimator.SetBool("isRunning", false);
        
        yield return CinematicCamera.Instance.LookAt(policeNPC.transform, 0.4f, Vector3.up * 1.5f);
        
        bool done = false;
        DialogueManager.Instance.StartDialogue(speakerNames, dialogLines, onComplete: () => done = true);
        yield return new WaitUntil(() => done);

        CinematicCamera.Instance.EndOverride();
        yield return ReturnPolice();
        enabled = false;
    }

    private IEnumerator ReturnPolice()
    {
        if (policeAnimator) policeAnimator.SetBool("isRunning", true);
        while (Vector3.Distance(policeNPC.transform.position, _originalPos) > 0.1f)
        {
            policeNPC.transform.position = Vector3.MoveTowards(
                policeNPC.transform.position, _originalPos, policeSpeed * Time.deltaTime);

            Vector3 dir = (_originalPos - policeNPC.transform.position).normalized;
            if (dir != Vector3.zero)
                policeNPC.transform.rotation = Quaternion.Slerp(
                    policeNPC.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            yield return null;
        }
        policeNPC.transform.rotation = _originalRot;
        if (policeAnimator) policeAnimator.SetBool("isRunning", false);
    }
}