using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class DocumentSearchSequence : MonoBehaviour
    {
        public enum SearchStage { BeforeCall, FirstCall, Searching, ReadyToCallBack, CallingBack, WaitingForDesk, Completed }
        public PhoneCallInteraction phone;
        public DocumentSearchItem[] documents;
        [Header("Edit text on these Canvas objects")]
        public TMP_Text checkPrompt;
        public TMP_Text[] checkMessages;
        public TMP_Text callBackPrompt;
        public TMP_Text callBackThought;
        public TMP_Text returnToDeskMessage;
        public TMP_Text unfinishedWorkThought;
        public GameObject testSkipHint;
        public GameObject testSkipAfterCallHint;
        public GameObject testSkipExitHint;
        [Header("Timing")]
        [Min(.1f)] public float messageDuration = 3f;
        [Min(0f)] public float beforeCallBackThoughtDelay = 2f;
        [HideInInspector] public float callBackThoughtDuration = 3f;
        [Min(0f)] public float returnToDeskDelay = 7f;
        [Min(.1f)] public float unfinishedWorkThoughtDuration = 3f;
        [Min(0f)] public float afterUnfinishedWorkThoughtDelay = 1f;
        [HideInInspector] public float returnToDeskDuration = 3f;
        [Min(0f)] public float afterChairMessageDelay = .5f;
        [HideInInspector] public int persistentReminderRevision;
        [Header("F1-F4 testing shortcuts")]
        [Tooltip("Enable or disable all four skip keys. Hint visibility is controlled separately on TestingShortcutHints.")]
        public bool enableTestingShortcuts = true;
        public SearchStage Stage { get; private set; }
        public int CheckedCount => checkedItems.Count;
        public bool CanCallBack => Stage == SearchStage.ReadyToCallBack;
        private readonly HashSet<DocumentSearchItem> checkedItems = new HashSet<DocumentSearchItem>();
        private bool showingMessage;
        private bool reachedOwnDesk;
        private int deskReminderRevision;
        private static readonly Dictionary<int, DocumentSearchSequence> Instances = new Dictionary<int, DocumentSearchSequence>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstances() => Instances.Clear();
        public static bool TestingShortcutsEnabled(Scene scene) => Instances.TryGetValue(scene.handle, out var sequence)
            && sequence != null && sequence.isActiveAndEnabled && sequence.enableTestingShortcuts;
        public static bool BlocksRoomInteractions(Scene scene) => Instances.TryGetValue(scene.handle, out var sequence)
            && sequence != null && sequence.isActiveAndEnabled && sequence.Stage != SearchStage.BeforeCall && sequence.Stage != SearchStage.Completed;
        public static bool BlocksChairInteractions(Scene scene) => Instances.TryGetValue(scene.handle, out var sequence)
            && sequence != null && sequence.Stage != SearchStage.Completed;
        public static bool TrySkipToIncomingCall(Scene scene)
        {
            if (!Instances.TryGetValue(scene.handle, out var sequence) || sequence == null || !sequence.isActiveAndEnabled || sequence.phone == null) return false;
            sequence.ResetCheckpointForTesting();
            sequence.Stage = SearchStage.BeforeCall;
            sequence.phone.RestartIncomingCallForTesting();
            return true;
        }

        public static void NotifyChairInspection(Scene scene, TMP_Text message)
        {
            if (!Instances.TryGetValue(scene.handle, out var sequence) || sequence == null || !sequence.isActiveAndEnabled
                || sequence.Stage != SearchStage.Completed || sequence.reachedOwnDesk) return;
            Panel(sequence.returnToDeskMessage, false);
            int revision = ++sequence.deskReminderRevision;
            sequence.StartCoroutine(sequence.ResumeDeskReminder(message, revision));
        }
        public static void NotifyOwnChairUsed(Scene scene)
        {
            if (!Instances.TryGetValue(scene.handle, out var sequence) || sequence == null || sequence.Stage != SearchStage.Completed) return;
            sequence.reachedOwnDesk = true;
            sequence.deskReminderRevision++;
            Panel(sequence.returnToDeskMessage, false);
        }
        private IEnumerator ResumeDeskReminder(TMP_Text message, int revision)
        {
            while (revision == deskReminderRevision && message != null && message.transform.parent.gameObject.activeInHierarchy)
                yield return null;
            if (revision != deskReminderRevision || reachedOwnDesk) yield break;
            yield return new WaitForSeconds(afterChairMessageDelay);
            if (revision == deskReminderRevision && !reachedOwnDesk && Stage == SearchStage.Completed)
                Panel(returnToDeskMessage, true);
        }

        private void Awake()
        {
            Instances[gameObject.scene.handle] = this;
            HideSearchUI();
            Panel(returnToDeskMessage, false);
            Panel(unfinishedWorkThought, false);
            Panel(callBackThought, false);
        }
        private static void Panel(TMP_Text label, bool show)
        {
            if (label != null) label.transform.parent.gameObject.SetActive(show);
        }
        private void HideSearchUI()
        {
            Panel(checkPrompt, false);
            if (checkMessages != null) foreach (var text in checkMessages) Panel(text, false);
        }
        public void BeginFirstCall() => Stage = SearchStage.FirstCall;
        public void BeginSearch()
        {
            Stage = SearchStage.Searching;
            HideSearchUI();
        }
        public void BeginCallBack()
        {
            Stage = SearchStage.CallingBack;
            Panel(callBackThought, false);
        }
        public void FinishCallBack()
        {
            Stage = SearchStage.WaitingForDesk;
            StartCoroutine(ReturnToDesk());
        }
        private IEnumerator ReturnToDesk(bool skipDelay = false)
        {
            if (!skipDelay)
            {
                yield return new WaitForSeconds(returnToDeskDelay);
                Panel(unfinishedWorkThought, true);
                yield return new WaitForSeconds(unfinishedWorkThoughtDuration);
                Panel(unfinishedWorkThought, false);
                yield return new WaitForSeconds(afterUnfinishedWorkThoughtDelay);
            }
            Stage = SearchStage.Completed;
            reachedOwnDesk = false;
            Panel(returnToDeskMessage, true);
        }
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (enableTestingShortcuts && Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
                SkipToExit();
            else if (enableTestingShortcuts && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                SkipToAfterCallBack();
            else if (enableTestingShortcuts && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                SkipToSearch();
            bool searching = Stage == SearchStage.Searching;
            if (documents != null) foreach (var item in documents) if (item != null) item.Pulse(searching);
            DocumentSearchItem selected = null;
            float nearest = float.PositiveInfinity;
            if (searching && !showingMessage && phone != null && phone.player != null && phone.player.CanExplore)
            {
                var camera = phone.playerCamera != null ? phone.playerCamera : Camera.main;
                var controller = phone.player.GetComponent<CharacterController>();
                Vector3 position = controller != null ? controller.transform.TransformPoint(controller.center) : phone.player.transform.position;
                foreach (var item in documents)
                {
                    if (item == null || !item.isActiveAndEnabled || item.HasChecked || item.interactionZone == null) continue;
                    if (!item.interactionZone.enabled || !item.interactionZone.gameObject.activeInHierarchy) continue;
                    if (!InteractionLook.IsPointInBox(position, item.interactionZone)
                        || !InteractionLook.IsLookingAtBox(camera, item.lookTarget)) continue;
                    float distance = (item.lookTarget.transform.TransformPoint(item.lookTarget.center) - position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; selected = item; }
                }
            }
            Panel(checkPrompt, selected != null);
            if (selected != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (!checkedItems.Add(selected)) return;
                selected.Check();
                StartCoroutine(ShowCheckMessage(CheckedCount - 1));
            }
        }
        private IEnumerator ShowCheckMessage(int index)
        {
            showingMessage = true;
            Panel(checkPrompt, false);
            var text = checkMessages != null && index < checkMessages.Length ? checkMessages[index] : null;
            Panel(text, true);
            yield return new WaitForSeconds(messageDuration);
            Panel(text, false);
            if (CheckedCount == 3)
            {
                yield return new WaitForSeconds(beforeCallBackThoughtDelay);
                Panel(callBackThought, true);
                Stage = SearchStage.ReadyToCallBack;
            }
            showingMessage = false;
        }
        public void SkipToSearch()
        {
            if (phone == null) return;
            ResetCheckpointForTesting();
            BeginSearch();
        }
        public void SkipToAfterCallBack()
        {
            if (phone == null) return;
            ResetCheckpointForTesting();
            if (documents != null)
                foreach (var item in documents)
                {
                    if (item == null) continue;
                    item.Check();
                    checkedItems.Add(item);
                }
            StartCoroutine(ReturnToDesk(true));
        }
        public void SkipToExit()
        {
            if (phone == null || phone.player == null) return;
            var story = phone.player.GetComponent<DrawerSearchScareSequence>();
            if (story == null) return;
            // Reuse the existing checkpoint reset to close phone/document/inspection
            // UI and release seated or dialogue camera control before jumping ahead.
            ResetCheckpointForTesting();
            Stage = SearchStage.Completed;
            reachedOwnDesk = true;
            if (documents != null)
                foreach (var item in documents)
                {
                    if (item == null) continue;
                    item.Check();
                    checkedItems.Add(item);
                    item.Pulse(false);
                }
            story.SkipToExitForTesting();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void ResetCheckpointForTesting()
        {
            StopAllCoroutines();
            showingMessage = false;
            reachedOwnDesk = false;
            deskReminderRevision++;
            HideSearchUI();
            Panel(callBackThought, false);
            Panel(callBackPrompt, false);
            Panel(unfinishedWorkThought, false);
            Panel(returnToDeskMessage, false);
            checkedItems.Clear();
            if (documents != null) foreach (var item in documents) if (item != null) item.ResetForTesting();
            phone.SkipToDocumentSearch();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var notebook in root.GetComponentsInChildren<ItemInspectionInteraction>(true)) notebook.CancelForTesting();
                foreach (var chair in root.GetComponentsInChildren<ChairInspectionInteractable>(true)) chair.ResetForTesting();
            }
            if (phone.player != null)
            {
                var story = phone.player.GetComponent<DrawerSearchScareSequence>();
                if (story != null) story.ResetForTesting();
                var meal = phone.player.GetComponent<NoodleEatingController>();
                if (meal != null) meal.ResetForTesting();
                phone.player.ResetForTesting();
            }
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            HideSearchUI();
            Panel(returnToDeskMessage, false);
            Panel(unfinishedWorkThought, false);
            Panel(callBackThought, false);
            if (documents != null) foreach (var item in documents) if (item != null) item.Pulse(false);
        }
        private void OnDestroy()
        {
            if (Instances.TryGetValue(gameObject.scene.handle, out var instance) && instance == this) Instances.Remove(gameObject.scene.handle);
        }
    }
}
