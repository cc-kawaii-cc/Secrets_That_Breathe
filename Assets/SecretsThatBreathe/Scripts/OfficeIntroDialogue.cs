using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class OfficeIntroDialogue : MonoBehaviour
    {
        [Header("Edit sentences on each Canvas Text Input")]
        public TMP_Text[] lines;
        [Min(0.1f)] public float lineDuration = 3f;
        [Min(0f)] public float betweenLinesDelay = 2f;
        [Header("Flashlight introduction")]
        [Tooltip("Silence after the third sentence disappears.")]
        [Min(0f)] public float afterEveryoneLeftDelay = 10f;
        [Min(0.1f)] public float darkRoomLineDuration = 3f;
        [Min(0f)] public float beforeFlashlightHintDelay = 2f;
        [HideInInspector] public float flashlightHintDuration = 3f;
        [Header("Testing shortcut")]
        public GameObject testSkipHint;
        public bool IsFinished { get; private set; }
        public bool HasStarted { get; private set; }
        public bool SkippedForTesting { get; private set; }
        public bool FlashlightUnlocked { get; private set; }
        private bool waitingForFlashlightPress;
        private static readonly Dictionary<int, OfficeIntroDialogue> Instances = new Dictionary<int, OfficeIntroDialogue>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstances() => Instances.Clear();
        private void Awake()
        {
            Instances[gameObject.scene.handle] = this;
            HideLines();
        }

        public static bool IsBlocking(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            if (Instances.TryGetValue(scene.handle, out var cached)) return cached != null && !cached.IsFinished;
            foreach (var root in scene.GetRootGameObjects())
            {
                var intro = root.GetComponentInChildren<OfficeIntroDialogue>(true);
                if (intro != null)
                {
                    Instances[scene.handle] = intro;
                    return !intro.IsFinished;
                }
            }
            Instances[scene.handle] = null;
            return false;
        }

        public static bool IsFlashlightBlocking(Scene scene)
        {
            // Resolve the scene's intro through the same cache as object interactions.
            IsBlocking(scene);
            return Instances.TryGetValue(scene.handle, out var intro) && intro != null && !intro.FlashlightUnlocked;
        }
        public static void NotifyFlashlightActivationRequested(Scene scene)
        {
            if (!Instances.TryGetValue(scene.handle, out var intro) || intro == null || !intro.waitingForFlashlightPress) return;
            intro.waitingForFlashlightPress = false;
            if (intro.lines != null && intro.lines.Length > 4 && intro.lines[4] != null)
                intro.lines[4].transform.parent.gameObject.SetActive(false);
            intro.IsFinished = true;
        }

        public void BeginFromTrigger()
        {
            if (HasStarted || !isActiveAndEnabled || Time.timeScale <= 0f) return;
            HasStarted = true;
            StartCoroutine(PlayDialogue());
        }

        private void Update()
        {
            if (DocumentSearchSequence.TestingShortcutsEnabled(gameObject.scene) && Time.timeScale > 0f && Keyboard.current != null
                && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                if (!DocumentSearchSequence.TrySkipToIncomingCall(gameObject.scene)) SkipToPhoneCall();
            }
        }

        public void SkipToPhoneCall()
        {
            SkippedForTesting = true;
            waitingForFlashlightPress = false;
            StopAllCoroutines();
            HideLines();
            HasStarted = true;
            FlashlightUnlocked = true;
            IsFinished = true; // The phone's existing waiting routine starts its ringing sequence.
        }

        private IEnumerator PlayDialogue()
        {
            HideLines();
            if (lines != null)
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i == 4)
                    {
                        FlashlightUnlocked = true;
                        waitingForFlashlightPress = true;
                    }
                    if (lines[i] != null) lines[i].transform.parent.gameObject.SetActive(true);
                    if (i == 4)
                    {
                        while (waitingForFlashlightPress) yield return null;
                    }
                    else yield return new WaitForSeconds(i == 3 ? darkRoomLineDuration : lineDuration);
                    if (lines[i] != null) lines[i].transform.parent.gameObject.SetActive(false);
                    if (i + 1 < lines.Length)
                        yield return new WaitForSeconds(i == 2 ? afterEveryoneLeftDelay
                            : i == 3 ? beforeFlashlightHintDelay : betweenLinesDelay);
                }
            IsFinished = true;
            FlashlightUnlocked = true;
        }

        private void HideLines()
        {
            if (lines == null) return;
            foreach (var line in lines) if (line != null) line.transform.parent.gameObject.SetActive(false);
        }
        private void OnDisable() { StopAllCoroutines(); HideLines(); }
        private void OnDestroy()
        {
            if (Instances.TryGetValue(gameObject.scene.handle, out var instance) && instance == this)
                Instances.Remove(gameObject.scene.handle);
        }

        public static OfficeIntroDialogue Install(SeatPromptUI canvas)
        {
            var group = SeatingCanvasGroups.Group(canvas, "OfficeIntroUI");
            var intro = group.GetComponent<OfficeIntroDialogue>();
            if (intro == null) intro = group.gameObject.AddComponent<OfficeIntroDialogue>();
            if (intro.lines == null || intro.lines.Length != 5)
            {
                var previousLines = intro.lines;
                intro.lines = new TMP_Text[5];
                for (int i = 0; i < 5; i++)
                {
                    var line = previousLines != null && i < previousLines.Length && previousLines[i] != null
                        ? previousLines[i] : canvas.EnsureNamedText("OfficeIntroLine" + (i + 1));
                    line.transform.parent.SetParent(group, false);
                    intro.lines[i] = line;
                }
            }
            OfficeIntroTrigger.Install(intro);
            if (intro.testSkipHint == null)
            {
                var hint = canvas.EnsureNamedText("TestSkipPhoneHint");
                hint.transform.parent.SetParent(group, false);
                var rect = hint.transform.parent as RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(24f, -24f);
                rect.sizeDelta = new Vector2(900f, 60f);
                hint.alignment = TextAlignmentOptions.Left;
                intro.testSkipHint = rect.gameObject;
            }
            return intro;
        }
    }
}
