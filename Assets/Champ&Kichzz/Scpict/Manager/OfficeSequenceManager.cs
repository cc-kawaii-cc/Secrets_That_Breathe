using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class OfficeSequenceManager : MonoBehaviour
{
    public enum Phase { Exploring, Seated, Eating, AuntCutscene, Completed }
    [SerializeField] private Phase phase = Phase.Exploring;
    public Phase CurrentPhase => phase;

    [Header("◾ Seated Setup")]
    [Tooltip("จุด+ทิศของ 'root ผู้เล่น' ตอนนั่ง (วางที่เก้าอี้ ระดับพื้น หันหน้าเข้าโต๊ะ)")]
    public Transform sitPosition;
    [Tooltip("จุดยืนหลังจบ cutscene (เว้นว่าง = อยู่ที่เดิม)")]
    public Transform standUpPosition;
    [Tooltip("ปุ่ม interact ตัวเดียวกับ PlayerInteractor (ใช้กดกินมาม่าตอนนั่ง)")]
    public InputActionReference interactAction;
    [Tooltip("ป้าย UI เช่น '[E] กินมาม่า' (optional)")]
    public GameObject eatPromptUI;

    [Header("◾ Door Gate")]
    [Tooltip("Collider ของประตู — ปิดไว้ตอนเริ่ม เปิดเมื่อจบ cutscene")]
    public Collider doorCollider;

    [Header("◾ Aunt NPC (ป้าสมร)")]
    public GameObject auntObject;
    public Transform auntSpawnPoint;
    public Transform auntStandPoint;
    public float auntRunSpeed = 3.5f;
    public Animator auntAnimator;
    public string auntRunBool = "isRunning";

    [Header("◾ Money QTE (ดันมือป้า)")]
    public GameObject moneyObject;
    public HoldQTE pushHandQTE;

    [Header("◾ Subtitles / Dialogue")]
    [TextArea] public string eatingLine =
        "วันนี้เพิ่งจะได้นั่งกินมาม่าดีๆ บ้าง ปกติแทบไม่มีเวลาได้นั่งกินเลย";
    [Tooltip("คำขอร้องของป้า (กด + เพิ่มบรรทัด เล่นเรียงทีละบรรทัด)")]
    [TextArea(2, 4)] public string[] auntPleaLines = new string[]
    {
        "หนูใช่ทนายเข้มมั้ย... ช่วยป้ารับทำคดีลูกป้าหน่อยนะ",
        "ไม่มีใครรับทำคดีของป้าเลย... ป้าแทบไม่มีเงินเลย ช่วยป้าหน่อยนะลูก"
    };
    [TextArea] public string khemRefuseLine =
        "เก็บเงินไว้ทำศพน้องเถอะป้า คดีนี้ผมทำให้ฟรี... ผมสัญญาว่าจะลากคอมันมาเข้าคุกให้ได้";
    public float lineDuration = 4f;

    private bool _awaitingRelease;

    private void Start()
    {
        phase = Phase.Exploring;
        if (eatPromptUI) eatPromptUI.SetActive(false);
        if (auntObject) auntObject.SetActive(false);
        if (moneyObject) moneyObject.SetActive(false);
        if (doorCollider) doorCollider.enabled = false;
    }

    private void OnEnable()  => interactAction?.action.Enable();
    private void OnDisable() => interactAction?.action.Disable();
    
    public void SitAtDesk()
    {
        if (phase != Phase.Exploring) return;
        phase = Phase.Seated;

        GameManager.Instance.SetState(GameState.Cutscene);
        if (sitPosition) PlayerManager.Instance.TeleportTo(sitPosition.position, sitPosition.rotation);

        _awaitingRelease = true;
        if (eatPromptUI) eatPromptUI.SetActive(true);
    }

    private void Update()
    {
        if (phase != Phase.Seated || interactAction == null) return;
        
        if (_awaitingRelease)
        {
            if (!interactAction.action.IsPressed()) _awaitingRelease = false;
            return;
        }
        
        if (interactAction.action.WasPressedThisFrame())
            EatNoodles();
    }

    private void EatNoodles()
    {
        phase = Phase.Eating;
        if (eatPromptUI) eatPromptUI.SetActive(false);
        StartCoroutine(NoodleThenAunt());
    }

    private IEnumerator NoodleThenAunt()
    {
        SubtitleManager.Instance.Show(eatingLine, lineDuration);
        yield return new WaitForSeconds(lineDuration);
        
        phase = Phase.AuntCutscene;
        yield return AuntSequence();
        
        phase = Phase.Completed;
        CinematicCamera.Instance.EndOverride();
        if (standUpPosition)
            PlayerManager.Instance.TeleportTo(standUpPosition.position, standUpPosition.rotation);
        GameManager.Instance.SetState(GameState.Exploration);
        if (doorCollider) doorCollider.enabled = true;
    }

    private IEnumerator AuntSequence()
    {
        if (auntObject)
        {
            if (auntSpawnPoint)
                auntObject.transform.SetPositionAndRotation(auntSpawnPoint.position, auntSpawnPoint.rotation);
            auntObject.SetActive(true);
        }
        if (auntAnimator) auntAnimator.SetBool(auntRunBool, true);
        
        if (auntStandPoint)
            StartCoroutine(CinematicCamera.Instance.LookAt(auntStandPoint, 0.6f, Vector3.up * 1.4f));
        
        if (auntObject && auntStandPoint)
        {
            while (Vector3.Distance(auntObject.transform.position, auntStandPoint.position) > 0.1f)
            {
                auntObject.transform.position = Vector3.MoveTowards(
                    auntObject.transform.position, auntStandPoint.position, auntRunSpeed * Time.deltaTime);

                Vector3 dir = auntStandPoint.position - auntObject.transform.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    auntObject.transform.rotation = Quaternion.Slerp(
                        auntObject.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                yield return null;
            }
        }
        if (auntAnimator) auntAnimator.SetBool(auntRunBool, false);
        
        if (auntObject && PlayerManager.Instance != null)
        {
            Vector3 faceKhem = PlayerManager.Instance.playerBody.position - auntObject.transform.position;
            faceKhem.y = 0f;
            if (faceKhem.sqrMagnitude > 0.0001f)
                auntObject.transform.rotation = Quaternion.LookRotation(faceKhem);
        }
        
        SubtitleManager.Instance.Show(auntPleaLines, lineDuration);
        yield return new WaitForSeconds(lineDuration * Mathf.Max(1, auntPleaLines.Length));
        
        if (moneyObject) moneyObject.SetActive(true);
        bool qteDone = false;
        if (pushHandQTE) pushHandQTE.Begin(() => qteDone = true);
        else qteDone = true;
        yield return new WaitUntil(() => qteDone);
        if (moneyObject) moneyObject.SetActive(false);
        
        SubtitleManager.Instance.Show(khemRefuseLine, lineDuration);
        yield return new WaitForSeconds(lineDuration);
        if (auntObject) auntObject.SetActive(false);
    }
}