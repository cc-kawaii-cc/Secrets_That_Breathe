using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class OfficeSequenceDirector : MonoBehaviour
    {
        public enum SequencePhase
        {
            Exploring,
            WaitingToEat,
            Eating,
            AuntApproaching,
            MoneyQTE,
            Completed
        }

        [Header("Player")]
        [SerializeField] private STBPlayerBridge playerBridge;
        [SerializeField] private CameraRayInteractor playerInteractor;
        [SerializeField] private Transform playerSitMarker;
        [SerializeField] private Transform playerStandMarker;

        [Header("Aunt")]
        [SerializeField] private GameObject auntRoot;
        [SerializeField] private Transform auntSpawnMarker;
        [SerializeField] private Transform auntStopMarker;
        [SerializeField] private Animator auntAnimator;
        [SerializeField, Min(0.1f)] private float auntMoveSpeed = 3.5f;

        [Header("Sequence")]
        [SerializeField] private GameObject moneyRoot;
        [SerializeField] private GameplayUI gameplayUI;
        [SerializeField] private HoldQTEController holdQTE;
        [SerializeField] private OfficeExitDoor exitDoor;
        [SerializeField, Min(0.1f)] private float lineDuration = 4f;

        [Header("Dialogue")]
        [SerializeField, TextArea(2, 4)] private string eatingLine =
            "วันนี้เพิ่งจะได้นั่งกินมาม่าดีๆ บ้าง ปกติแทบไม่มีเวลาได้นั่งกินเลย";
        [SerializeField, TextArea(2, 4)] private string[] auntPleaLines =
        {
            "หนูใช่ทนายเข้มมั้ย... ช่วยป้ารับทำคดีลูกป้าหน่อยนะ",
            "ไม่มีใครรับทำคดีของป้าเลย... ป้าแทบไม่มีเงินเลย ช่วยป้าหน่อยนะลูก"
        };
        [SerializeField, TextArea(2, 4)] private string refusalLine =
            "เก็บเงินไว้ทำศพน้องเถอะป้า คดีนี้ผมทำให้ฟรี... ผมสัญญาว่าจะลากคอมันมาเข้าคุกให้ได้";

        [SerializeField] private SequencePhase phase = SequencePhase.Exploring;

        public bool CanStart => phase == SequencePhase.Exploring;
        public SequencePhase Phase => phase;

        public void Configure(
            STBPlayerBridge bridge,
            CameraRayInteractor interactor,
            Transform sitMarker,
            Transform standMarker,
            GameObject aunt,
            Transform auntSpawn,
            Transform auntStop,
            GameObject money,
            GameplayUI ui,
            HoldQTEController qte,
            OfficeExitDoor door)
        {
            playerBridge = bridge;
            playerInteractor = interactor;
            playerSitMarker = sitMarker;
            playerStandMarker = standMarker;
            auntRoot = aunt;
            auntSpawnMarker = auntSpawn;
            auntStopMarker = auntStop;
            moneyRoot = money;
            gameplayUI = ui;
            holdQTE = qte;
            exitDoor = door;
            auntAnimator = auntRoot != null ? auntRoot.GetComponentInChildren<Animator>(true) : null;
        }

        private void Awake()
        {
            if (playerBridge == null)
                return;

            InitializeRuntime();
        }

        public void InitializeRuntime()
        {
            phase = SequencePhase.Exploring;
            if (auntRoot != null) auntRoot.SetActive(false);
            if (moneyRoot != null) moneyRoot.SetActive(false);
            gameplayUI?.HideAll();
            exitDoor?.SetUnlocked(false);
        }

        public void BeginSequence()
        {
            if (!CanStart) return;
            StartCoroutine(SequenceRoutine());
        }

        private IEnumerator SequenceRoutine()
        {
            phase = SequencePhase.WaitingToEat;
            playerInteractor?.SetInteractionEnabled(false);
            playerBridge?.SetControlEnabled(false);
            playerBridge?.TeleportTo(playerSitMarker);
            gameplayUI?.ShowEatPrompt(true);

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                yield return new WaitUntil(() => !keyboard.eKey.isPressed);
                yield return new WaitUntil(() => keyboard.eKey.wasPressedThisFrame);
            }
            else
            {
                Debug.LogError("[OfficeSequenceDirector] No keyboard was found. The office sequence cannot continue.", this);
                yield break;
            }

            gameplayUI?.ShowEatPrompt(false);
            phase = SequencePhase.Eating;
            if (gameplayUI != null)
                yield return gameplayUI.ShowSubtitle(eatingLine, lineDuration);
            else
                yield return new WaitForSeconds(lineDuration);

            yield return RunAuntSequence();

            phase = SequencePhase.Completed;
            if (auntRoot != null) auntRoot.SetActive(false);
            playerBridge?.TeleportTo(playerStandMarker);
            playerBridge?.SetControlEnabled(true);
            playerInteractor?.SetInteractionEnabled(true);
            exitDoor?.SetUnlocked(true);
        }

        private IEnumerator RunAuntSequence()
        {
            phase = SequencePhase.AuntApproaching;
            if (auntRoot != null)
            {
                auntRoot.transform.SetPositionAndRotation(auntSpawnMarker.position, auntSpawnMarker.rotation);
                auntRoot.SetActive(true);
            }

            SetWalkAnimation(true);
            if (auntRoot != null && auntStopMarker != null)
            {
                while (Vector3.Distance(auntRoot.transform.position, auntStopMarker.position) > 0.08f)
                {
                    Vector3 targetPosition = auntStopMarker.position;
                    Vector3 direction = targetPosition - auntRoot.transform.position;
                    Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
                    if (flatDirection.sqrMagnitude > 0.0001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
                        auntRoot.transform.rotation = Quaternion.Slerp(
                            auntRoot.transform.rotation,
                            targetRotation,
                            Time.deltaTime * 8f);
                    }

                    auntRoot.transform.position = Vector3.MoveTowards(
                        auntRoot.transform.position,
                        targetPosition,
                        auntMoveSpeed * Time.deltaTime);
                    yield return null;
                }
            }
            SetWalkAnimation(false);
            FaceAuntTowardsPlayer();

            if (gameplayUI != null)
                yield return gameplayUI.ShowSubtitles(auntPleaLines, lineDuration);

            phase = SequencePhase.MoneyQTE;
            if (moneyRoot != null) moneyRoot.SetActive(true);

            bool qteComplete = false;
            if (holdQTE != null)
                holdQTE.Begin(() => qteComplete = true);
            else
                qteComplete = true;
            yield return new WaitUntil(() => qteComplete);

            if (moneyRoot != null) moneyRoot.SetActive(false);
            if (gameplayUI != null)
                yield return gameplayUI.ShowSubtitle(refusalLine, lineDuration);
            else
                yield return new WaitForSeconds(lineDuration);
        }

        private void FaceAuntTowardsPlayer()
        {
            if (auntRoot == null || playerBridge == null) return;
            Vector3 direction = playerBridge.PlayerBody.position - auntRoot.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                auntRoot.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        private void SetWalkAnimation(bool walking)
        {
            if (auntAnimator == null) return;

            string[] supportedNames = { "isWalking", "isRunning" };
            for (int i = 0; i < auntAnimator.parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = auntAnimator.parameters[i];
                if (parameter.type != AnimatorControllerParameterType.Bool) continue;

                for (int nameIndex = 0; nameIndex < supportedNames.Length; nameIndex++)
                {
                    if (parameter.name == supportedNames[nameIndex])
                    {
                        auntAnimator.SetBool(parameter.name, walking);
                        return;
                    }
                }
            }
        }
    }
}
