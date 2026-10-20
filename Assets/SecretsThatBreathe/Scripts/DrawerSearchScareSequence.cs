using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerSeatController), typeof(NoodleEatingController))]
    public sealed class DrawerSearchScareSequence : MonoBehaviour
    {
        public enum StoryStage { WaitingForMeal, WaitingAfterMeal, LookForDrawer, Searching, AuntApproaching, JumpScare, Completed, AuntDialogue, MoneyChoice, AuntLeaving }
        public StoryStage Stage { get; private set; }
        public int DialogueLineIndex { get; private set; } = -1;

        [Header("Scene references")]
        public Transform drawer;
        public Transform desk;
        [Tooltip("The separate Capsule in the scene, not PlayerCapsule or the player's visual mesh.")]
        public GameObject auntObject;
        public Transform auntStandPoint;
        public Transform auntFaceTarget;
        public Transform mainDoor;
        public Transform auntExitPoint;
        [Tooltip("Optional intermediate points, visited in order before AuntExitPoint, to avoid furniture.")]
        public Transform[] auntExitWaypoints = new Transform[0];
        [Tooltip("Optional standing position beside the chair. Empty uses the safe position where the player pressed E to sit.")]
        public Transform standUpPoint;
        public ScreenFadeUI screenFade;
        public SeatPromptUI promptCanvas;

        [Header("Aunt character animation")]
        public Animator auntAnimator;
        public RuntimeAnimatorController auntIdleController;
        public RuntimeAnimatorController auntWalkController;
        [Min(.01f)] public float auntIdleAnimationSpeed = 1f;
        [Min(.01f)] public float auntWalkAnimationSpeed = 1f;

        [Header("Aunt exit")]
        [Min(0.1f)] public float exitWalkSpeed = 1.2f;
        [Min(0.1f)] public float exitTurnSpeed = 5f;
        [Header("Automatic stand-up fade")]
        [Min(0f)] public float auntWalkBeforeStand = 2f;
        [Min(0.05f)] public float standFadeOutDuration = 0.7f;
        [Min(0f)] public float standBlackHold = 0.15f;
        [Min(0.05f)] public float standFadeInDuration = 0.7f;

        [Header("Canvas text (editable on these TMP components)")]
        public TMP_Text objectiveText;
        public TMP_Text searchPromptText;
        public TMP_Text searchingText;
        public TMP_Text auntDialogueText;
        public AuntMoneyChoiceUI moneyChoiceUI;
        public AuntDialogueUI auntDialogueUI;
        public PostMealThoughtUI postMealThoughtUI;

        [Header("Aunt dialogue after zoom")]
        [Min(0f)] public float dialogueStartDelay = 0.5f;

        [Header("Look at drawer")]
        [Range(1f, 80f)] public float lookAngle = 40f;
        [Min(0.1f)] public float lookDistance = 4f;

        [Header("Thoughts after eating")]
        [Min(0f)] public float afterMealThoughtDelay = 1f;
        [Min(0.1f)] public float afterMealThoughtDuration = 2f;
        [Tooltip("Pause between the first, second and third thoughts only.")]
        [Min(0f)] public float afterMealThoughtGap = 1f;
        [Tooltip("Seconds after the third thought disappears before showing the drawer objective.")]
        [Min(0f)] public float beforeDrawerObjectiveDelay = 1f;
        [Header("Search and approach")]
        [Min(0.1f)] public float searchDuration = 2.5f;
        [Min(0.1f)] public float approachDuration = 1.2f;
        public bool hideAuntUntilSearch = true;

        [Header("Searching camera movement")]
        [Tooltip("Extra left turn from the direction the player was looking when pressing E.")]
        [Range(0f, 60f)] public float searchExtraLeftAngle = 15f;
        [Range(-30f, 30f)] public float searchPitchAngle = 6f;
        [Tooltip("Camera-space offset while searching: X left/right, Y up/down, Z forward.")]
        public Vector3 searchLeanOffset = new Vector3(-0.08f, -0.035f, 0.16f);
        [Min(0.05f)] public float searchMoveDuration = 0.45f;

        [Header("Searching camera shake")]
        [Min(0f)] public float searchShakePosition = 0.008f;
        [Min(0f)] public float searchShakeRotation = 0.6f;
        [Min(0.1f)] public float searchShakeFrequency = 10f;

        [Header("Fast camera turn and zoom")]
        [Min(0.05f)] public float snapDuration = 0.18f;
        [Tooltip("Start zooming after this fraction of the camera turn. 0 = immediately, 1 = after fully facing the aunt.")]
        [Range(0f, 1f)] public float zoomStartTurnFraction = .65f;
        [Tooltip("Smooth zoom duration, independent of the camera turn duration.")]
        [Min(.05f)] public float scareZoomDuration = .6f;
        [Range(10f, 90f)] public float scareFieldOfView = 30f;
        [Tooltip("Time to return to normal FOV after the thank-you line, while still facing the aunt.")]
        [Min(0.05f)] public float returnDuration = 0.6f;

        [Header("Sound when turning to the aunt")]
        [InspectorName("Turn To Aunt Audio Source")]
        public AudioSource scareAudioSource;
        [HideInInspector]
        public AudioClip scareSound;
        [HideInInspector] public float scareVolume = 1f;
        [HideInInspector] public int auntTurnAudioRevision;

        [Header("Events for the next story step")]
        public UnityEvent onSearchStarted = new UnityEvent();
        public UnityEvent onScareStarted = new UnityEvent();
        public UnityEvent onSequenceFinished = new UnityEvent();
        public UnityEvent onMoneyRefused = new UnityEvent();
        private readonly AuntMoneyDecision moneyDecision = new AuntMoneyDecision();

        private PlayerSeatController player;
        private NoodleEatingController meal;
        private SeatCameraAnimation cameraAnimation;
        private InputAction interact;
        private InputAction advanceDialogue;
        private bool awaitingDialogueRelease;
        private Renderer[] drawerMeshes;
        private Coroutine sequence;
        private Coroutine auntExitRoutine;
        private bool auntDismissed;
        private bool awaitingRelease = true, cameraCaptured, oldLookLock, npcCaptured, oldNpcActive;
        private Camera capturedCamera;
        private Vector3 cameraPosition, npcPosition;
        private Quaternion cameraRotation, npcRotation;
        private float originalFov;
        private Collider[] npcColliders;
        private bool[] npcColliderStates;
        private float mealDelayElapsed, searchShakeTime, searchShakeBlend;
        private bool searchMotionActive;
        private Vector3 searchBasePosition;
        private Quaternion searchBaseRotation;

        private void Awake()
        {
            player = GetComponent<PlayerSeatController>();
            meal = GetComponent<NoodleEatingController>();
            cameraAnimation = GetComponent<SeatCameraAnimation>();
            interact = new InputAction("Search drawer", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonSouth");
            advanceDialogue = new InputAction("Advance aunt dialogue", InputActionType.Button, "<Mouse>/leftButton");
        }

        private void OnEnable() { interact?.Enable(); advanceDialogue?.Enable(); awaitingRelease = true; }

        private void Start()
        {
            ConfigureScene();
            SetAuntWalking(false);
            moneyChoiceUI.acceptButton.onClick.AddListener(ChooseAcceptMoney);
            moneyChoiceUI.refuseButton.onClick.AddListener(ChooseRefuseMoney);
            HideAllText();
            if (auntObject != null)
            {
                npcPosition = auntObject.transform.position;
                npcRotation = auntObject.transform.rotation;
                oldNpcActive = auntObject.activeSelf;
                npcCaptured = true;
                if (hideAuntUntilSearch) auntObject.SetActive(false);
            }
            if (drawer == null || auntObject == null)
                Debug.LogWarning("[Drawer search] Assign Drawer and Aunt Object on DrawerSearchScareSequence.", this);
        }

        // Fill only missing references; never overwrite user text or adjusted waypoints.
        public void ConfigureScene()
        {
            var seat = GetComponent<PlayerSeatController>();
            ChairInteractable chair = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (drawer == null && item.name == "3-tier cabinet (25)") drawer = item;
                    if (desk == null && item.name == "Table Kam") desk = item;
                    if (mainDoor == null && item.name == "Main Door") mainDoor = item;
                    if (item.name == "Chair Kam") chair = item.GetComponent<ChairInteractable>();
                    if (auntObject == null && item.name == "Capsule" && !item.IsChildOf(transform.root)) auntObject = item.gameObject;
                }
            if (promptCanvas == null) promptCanvas = SeatPromptUI.FindOrCreate(gameObject.scene);
            if (screenFade == null) screenFade = ScreenFadeUI.FindOrCreate(promptCanvas);
            if (objectiveText == null) objectiveText = promptCanvas.EnsureNamedText("DrawerObjective");
            if (searchPromptText == null) searchPromptText = promptCanvas.EnsureNamedText("DrawerSearchPrompt");
            if (searchingText == null) searchingText = promptCanvas.EnsureNamedText("DrawerSearching");
            postMealThoughtUI = PostMealThoughtUI.FindOrCreate(promptCanvas, objectiveText);
            ConfigureDialogueUI();
            if (drawer != null) drawerMeshes = drawer.GetComponentsInChildren<Renderer>(true);

            if (desk != null && auntObject != null && auntStandPoint == null)
            {
                GameObject points = null;
                foreach (GameObject root in gameObject.scene.GetRootGameObjects()) if (root.name == "LawyerRoomStoryPoints") points = root;
                if (points == null)
                {
                    points = new GameObject("LawyerRoomStoryPoints");
                    SceneManager.MoveGameObjectToScene(points, gameObject.scene);
                }
                auntStandPoint = points.transform.Find("AuntStandPoint");
                if (auntStandPoint == null)
                {
                    Vector3 right = chair != null && chair.seatedCameraPoint != null ? chair.seatedCameraPoint.right : desk.right;
                    right.y = 0f; right.Normalize();
                    var meshes = desk.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = meshes.Length > 0 ? meshes[0].bounds : new Bounds(desk.position, Vector3.one);
                    foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    float extent = Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.z) * bounds.extents.z;
                    Vector3 stand = bounds.center + right * (extent + 0.6f);
                    stand.y = auntObject.transform.position.y;
                    auntStandPoint = new GameObject("AuntStandPoint").transform;
                    auntStandPoint.SetParent(points.transform, false);
                    auntStandPoint.position = stand;
                    Vector3 facePlayer = (chair != null && chair.seatPoint != null ? chair.seatPoint.position : seat.transform.position) - stand;
                    facePlayer.y = 0f;
                    if (facePlayer.sqrMagnitude > 0.001f) auntStandPoint.rotation = Quaternion.LookRotation(facePlayer);
                }
            }
            if (auntObject != null && auntFaceTarget == null)
            {
                auntFaceTarget = auntObject.transform.Find("AuntFaceTarget");
                if (auntFaceTarget == null)
                {
                    var meshes = auntObject.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = meshes.Length > 0 ? meshes[0].bounds : new Bounds(auntObject.transform.position, Vector3.up * 2f);
                    foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    auntFaceTarget = new GameObject("AuntFaceTarget").transform;
                    auntFaceTarget.SetParent(auntObject.transform, false);
                    auntFaceTarget.position = bounds.center + Vector3.up * (bounds.extents.y * 0.7f);
                }
            }
            ConfigureExitPoint();
        }

        public void ConfigureExitPoint()
        {
            if (auntExitPoint != null || mainDoor == null || auntObject == null) return;
            Transform points = auntStandPoint != null ? auntStandPoint.parent : null;
            if (points == null)
            {
                var root = new GameObject("LawyerRoomStoryPoints");
                SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                points = root.transform;
            }
            auntExitPoint = points.Find("AuntExitPoint");
            if (auntExitPoint != null) return;
            var meshes = mainDoor.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = meshes.Length > 0 ? meshes[0].bounds : new Bounds(mainDoor.position, Vector3.one);
            foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
            Vector3 towardRoom = (auntStandPoint != null ? auntStandPoint.position : auntObject.transform.position) - bounds.center;
            towardRoom.y = 0f;
            Vector3 destination = bounds.center + towardRoom.normalized * 0.65f;
            destination.y = auntObject.transform.position.y;
            auntExitPoint = new GameObject("AuntExitPoint").transform;
            auntExitPoint.SetParent(points, false);
            auntExitPoint.position = destination;
        }

        public void ConfigureDialogueUI()
        {
            if (promptCanvas == null) promptCanvas = SeatPromptUI.FindOrCreate(gameObject.scene);
            SeatingCanvasGroups.Organize(promptCanvas);
            if (auntDialogueUI == null)
                auntDialogueUI = SeatingCanvasGroups.Group(promptCanvas, SeatingCanvasGroups.Aunt).GetComponent<AuntDialogueUI>();
            if (auntDialogueText == null && auntDialogueUI != null) auntDialogueText = auntDialogueUI.firstLine;
            if (auntDialogueText == null)
            {
                bool existed = SeatingCanvasGroups.FindPanel(promptCanvas, "AuntDialogue") != null;
                auntDialogueText = promptCanvas.EnsureNamedText("AuntDialogue");
                if (!existed && auntDialogueText != null)
                    auntDialogueText.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 180f);
            }
            auntDialogueUI = AuntDialogueUI.FindOrCreate(promptCanvas, auntDialogueText);
            if (moneyChoiceUI == null) moneyChoiceUI = AuntMoneyChoiceUI.FindOrCreate(promptCanvas, auntDialogueText);
        }

        private static void Show(TMP_Text text, bool visible)
        {
            if (text != null) text.transform.parent.gameObject.SetActive(visible);
        }
        private void HideAllText()
        {
            Show(objectiveText, false); Show(searchPromptText, false); Show(searchingText, false); Show(auntDialogueText, false);
            if (auntDialogueUI != null) auntDialogueUI.HideAll();
            if (moneyChoiceUI != null) moneyChoiceUI.HideChoices();
            if (postMealThoughtUI != null) postMealThoughtUI.HideAll();
        }

        private void Update()
        {
            bool seated = player.isActiveAndEnabled && player.State == PlayerSeatController.SeatState.Seated;
            if (Stage == StoryStage.WaitingForMeal && meal.State == NoodleEatingController.MealState.Finished)
            {
                mealDelayElapsed = 0f;
                Stage = StoryStage.WaitingAfterMeal;
                HideAllText();
            }
            else if (Stage == StoryStage.WaitingAfterMeal)
            {
                if (seated)
                {
                    mealDelayElapsed += Time.deltaTime;
                    if (postMealThoughtUI.ShowAtTime(mealDelayElapsed, afterMealThoughtDelay, afterMealThoughtDuration, afterMealThoughtGap, beforeDrawerObjectiveDelay))
                        Stage = StoryStage.LookForDrawer;
                }
                else postMealThoughtUI.HideAll();
            }
            if (searchMotionActive) searchShakeTime += Time.deltaTime;
            if (Stage == StoryStage.LookForDrawer)
            {
                bool available = seated && Time.timeScale > 0f;
                Show(objectiveText, available);
                bool looking = available && drawer != null && InteractionLook.IsLookingAt(player.playerCamera, drawerMeshes, lookAngle, lookDistance);
                Show(searchPromptText, looking);
                if (awaitingRelease && !interact.IsPressed()) awaitingRelease = false;
                if (looking && !awaitingRelease && interact.WasPressedThisFrame()) TryStartSearch();
            }
            else if ((sequence != null || Stage == StoryStage.AuntDialogue || Stage == StoryStage.MoneyChoice) &&
                ((Stage != StoryStage.AuntLeaving && !seated) || auntObject == null || auntStandPoint == null || auntFaceTarget == null || player.playerCamera == null)) CancelSequence();
            if (Stage == StoryStage.AuntDialogue && Time.timeScale > 0f)
            {
                if (awaitingDialogueRelease)
                {
                    if (!advanceDialogue.IsPressed()) awaitingDialogueRelease = false;
                }
                else if (advanceDialogue.WasPressedThisFrame()) AdvanceDialogue();
            }
        }

        public bool TryStartSearch()
        {
            if (!isActiveAndEnabled || Stage != StoryStage.LookForDrawer || Time.timeScale <= 0f ||
                !player.isActiveAndEnabled || player.State != PlayerSeatController.SeatState.Seated || auntObject == null ||
                auntStandPoint == null || auntFaceTarget == null || !InteractionLook.IsLookingAt(player.playerCamera, drawerMeshes, lookAngle, lookDistance)) return false;
            capturedCamera = player.playerCamera;
            cameraPosition = capturedCamera.transform.position;
            cameraRotation = capturedCamera.transform.rotation;
            originalFov = capturedCamera.fieldOfView;
            oldLookLock = player.SeatedLookLocked;
            player.SeatedLookLocked = true;
            cameraCaptured = true;
            Stage = StoryStage.Searching;
            HideAllText(); Show(searchingText, true);
            sequence = StartCoroutine(SearchThenScare());
            onSearchStarted.Invoke();
            return true;
        }

        public void UseThaiFontIfNeeded(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (var label in new[] { objectiveText, searchPromptText, searchingText, auntDialogueText })
                if (label != null && label.font == null) label.font = font;
            if (moneyChoiceUI != null)
                foreach (var label in new[] { moneyChoiceUI.acceptLabel, moneyChoiceUI.refuseLabel })
                    if (label != null && label.font == null) label.font = font;
            if (auntDialogueUI != null)
                foreach (var label in auntDialogueUI.GetComponentsInChildren<TMP_Text>(true))
                    if (label.font == null) label.font = font;
            if (postMealThoughtUI != null)
                foreach (var label in new[] { postMealThoughtUI.firstThought, postMealThoughtUI.secondThought, postMealThoughtUI.thirdThought })
                    if (label != null && label.font == null) label.font = font;
        }

        private IEnumerator SearchThenScare()
        {
            searchMotionActive = true;
            searchShakeTime = 0f;
            searchShakeBlend = 0f;
            searchBasePosition = cameraPosition;
            searchBaseRotation = cameraRotation;
            Vector3 leanPosition = cameraPosition + cameraRotation * searchLeanOffset;
            Quaternion leanRotation = cameraRotation * Quaternion.Euler(searchPitchAngle, -searchExtraLeftAngle, 0f);
            float moveDuration = Mathf.Max(0.05f, searchMoveDuration);
            for (float time = 0f; time < moveDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / moveDuration);
                searchBasePosition = Vector3.Lerp(cameraPosition, leanPosition, t);
                searchBaseRotation = Quaternion.Slerp(cameraRotation, leanRotation, t);
                searchShakeBlend = t;
                yield return null;
            }
            searchBasePosition = leanPosition;
            searchBaseRotation = leanRotation;
            searchShakeBlend = 1f;
            yield return new WaitForSeconds(Mathf.Max(0.1f, searchDuration));
            Stage = StoryStage.AuntApproaching;
            auntObject.SetActive(true);
            SetAuntWalking(true);
            npcColliders = auntObject.GetComponentsInChildren<Collider>(true);
            npcColliderStates = new bool[npcColliders.Length];
            for (int i = 0; i < npcColliders.Length; i++) { npcColliderStates[i] = npcColliders[i].enabled; npcColliders[i].enabled = false; }
            Vector3 start = auntObject.transform.position;
            Quaternion startRotation = auntObject.transform.rotation;
            float duration = Mathf.Max(0.1f, approachDuration);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / duration);
                auntObject.transform.SetPositionAndRotation(Vector3.Lerp(start, auntStandPoint.position, t), Quaternion.Slerp(startRotation, auntStandPoint.rotation, t));
                yield return null;
            }
            auntObject.transform.SetPositionAndRotation(auntStandPoint.position, auntStandPoint.rotation);
            SetAuntWalking(false);
            RestoreNpcColliders();
            HideAllText();
            Stage = StoryStage.JumpScare;
            searchMotionActive = false;
            Vector3 scareStartPosition = capturedCamera.transform.position;
            Quaternion scareStartRotation = capturedCamera.transform.rotation;
            if (scareAudioSource == null)
            {
                var soundObject = transform.Find("AuntTurnAudio");
                if (soundObject == null)
                {
                    soundObject = new GameObject("AuntTurnAudio").transform;
                    soundObject.SetParent(transform, false);
                }
                scareAudioSource = soundObject.GetComponent<AudioSource>();
                if (scareAudioSource == null)
                {
                    scareAudioSource = soundObject.gameObject.AddComponent<AudioSource>();
                    scareAudioSource.playOnAwake = false;
                    scareAudioSource.spatialBlend = 0f;
                    scareAudioSource.dopplerLevel = 0f;
                    scareAudioSource.clip = scareSound;
                    scareAudioSource.volume = Mathf.Clamp01(scareVolume);
                }
            }
            onScareStarted.Invoke();
            Quaternion faceRotation = Quaternion.LookRotation(auntFaceTarget.position - cameraPosition);
            yield return TurnThenZoomAtAunt(scareStartPosition, scareStartRotation, faceRotation);
            yield return new WaitForSeconds(Mathf.Max(0f, dialogueStartDelay));
            ShowDialogueLine(0);
            sequence = null;
            // Keep the camera pose, zoom and look lock until an explicit later story step ends dialogue.
        }

        public bool AdvanceDialogue()
        {
            if (!isActiveAndEnabled || Stage != StoryStage.AuntDialogue || Time.timeScale <= 0f ||
                auntDialogueText == null) return false;
            if (DialogueLineIndex == 0) ShowDialogueLine(1);
            else if (DialogueLineIndex == 1) ShowDialogueLine(2);
            else if (DialogueLineIndex == 2 || DialogueLineIndex == 3) ShowMoneyChoices();
            else if (DialogueLineIndex == 4) ShowDialogueLine(5);
            else if (DialogueLineIndex == 5) FinishDialogue();
            else return false;
            return true;
        }

        private void ShowDialogueLine(int index)
        {
            moneyChoiceUI.HideChoices();
            Stage = StoryStage.AuntDialogue;
            DialogueLineIndex = index;
            auntDialogueText = auntDialogueUI.ShowLine(index, moneyDecision.ThoughtIndex);
            awaitingDialogueRelease = true;
        }

        private void ShowMoneyChoices()
        {
            auntDialogueUI.HideAll();
            Stage = StoryStage.MoneyChoice;
            moneyChoiceUI.ShowChoices();
        }

        public void ChooseAcceptMoney()
        {
            if (!isActiveAndEnabled || Stage != StoryStage.MoneyChoice || Time.timeScale <= 0f) return;
            int index = moneyDecision.Accept();
            if (index < 0) return;
            ShowDialogueLine(3);
        }

        public void ChooseRefuseMoney()
        {
            if (!isActiveAndEnabled || Stage != StoryStage.MoneyChoice || Time.timeScale <= 0f || !moneyDecision.Refuse()) return;
            ShowDialogueLine(4);
            onMoneyRefused.Invoke();
        }

        // End the thank-you line, restore the camera, then let the aunt leave.
        public void FinishDialogue()
        {
            if (Stage != StoryStage.AuntDialogue || DialogueLineIndex != 5 || !moneyDecision.HasRefused || sequence != null) return;
            player.LockChairInteractions();
            HideAllText();
            Stage = StoryStage.JumpScare;
            sequence = StartCoroutine(ReturnAfterDialogue());
        }

        private IEnumerator ReturnAfterDialogue()
        {
            Vector3 facingPosition = capturedCamera.transform.position;
            Quaternion facingRotation = capturedCamera.transform.rotation;
            yield return CameraTransition(facingPosition, facingPosition, facingRotation,
                facingRotation, capturedCamera.fieldOfView, originalFov, returnDuration);
            // Keep looking at the aunt at normal FOV; keep mouse look locked until the stand-up fade finishes.
            Stage = StoryStage.AuntLeaving;
            if (auntObject != null && auntExitPoint != null)
            {
                SuspendNpcColliders();
                SetAuntWalking(true);
                auntExitRoutine = StartCoroutine(WalkAuntAway());
            }
            else Debug.LogWarning("[Aunt exit] Assign Main Door / Aunt Exit Point to let the aunt leave.", this);
            yield return new WaitForSeconds(Mathf.Max(0f, auntWalkBeforeStand));
            yield return screenFade.FadeTo(1f, standFadeOutDuration);
            if (auntExitRoutine != null) { StopCoroutine(auntExitRoutine); auntExitRoutine = null; }
            RestoreNpcColliders();
            if (auntObject != null) auntObject.SetActive(false);
            auntDismissed = true;
            capturedCamera.fieldOfView = originalFov;
            cameraCaptured = false;
            player.StandUp(standUpPoint, true);
            yield return new WaitForSeconds(Mathf.Max(0f, standBlackHold));
            yield return screenFade.FadeTo(0f, standFadeInDuration);
            player.ResumeAfterStand();
            Stage = StoryStage.Completed;
            sequence = null;
            onSequenceFinished.Invoke();
        }

        private IEnumerator WalkAuntAway()
        {
            foreach (Transform waypoint in auntExitWaypoints)
                if (waypoint != null) yield return WalkAuntTo(waypoint);
            yield return WalkAuntTo(auntExitPoint);
            SetAuntWalking(false);
            auntExitRoutine = null;
        }

        private void SetAuntWalking(bool walking)
        {
            if (auntAnimator == null && auntObject != null)
                auntAnimator = auntObject.GetComponentInChildren<Animator>(true);
            if (auntAnimator == null) return;
            var controller = walking ? auntWalkController : auntIdleController;
            if (controller == null) return;
            auntAnimator.applyRootMotion = false; // The story sequence owns position and path.
            auntAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (auntAnimator.runtimeAnimatorController != controller)
            {
                auntAnimator.runtimeAnimatorController = controller;
                auntAnimator.Rebind();
                if (auntAnimator.isActiveAndEnabled) auntAnimator.Update(0f);
            }
            auntAnimator.speed = Mathf.Max(.01f, walking ? auntWalkAnimationSpeed : auntIdleAnimationSpeed);
        }

        private void SuspendNpcColliders()
        {
            npcColliders = auntObject.GetComponentsInChildren<Collider>(true);
            npcColliderStates = new bool[npcColliders.Length];
            for (int i = 0; i < npcColliders.Length; i++) { npcColliderStates[i] = npcColliders[i].enabled; npcColliders[i].enabled = false; }
        }

        private IEnumerator WalkAuntTo(Transform target)
        {
            while (auntObject != null && target != null && Vector3.Distance(auntObject.transform.position, target.position) > 0.02f)
            {
                Vector3 direction = target.position - auntObject.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    auntObject.transform.rotation = Quaternion.Slerp(auntObject.transform.rotation, Quaternion.LookRotation(direction),
                        Mathf.Clamp01(Time.deltaTime * Mathf.Max(0.1f, exitTurnSpeed)));
                auntObject.transform.position = Vector3.MoveTowards(auntObject.transform.position, target.position,
                    Mathf.Max(0.1f, exitWalkSpeed) * Time.deltaTime);
                yield return null;
            }
            if (auntObject != null && target != null) auntObject.transform.position = target.position;
        }

        private void LateUpdate()
        {
            if (!searchMotionActive || !cameraCaptured || capturedCamera == null) return;
            float time = searchShakeTime * Mathf.Max(0.1f, searchShakeFrequency);
            float x = (Mathf.PerlinNoise(time, 0.13f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0.71f, time) - 0.5f) * 2f;
            Vector3 shake = new Vector3(x, y, 0f) * (Mathf.Max(0f, searchShakePosition) * searchShakeBlend);
            Vector3 angles = new Vector3(y, x, -x * 0.35f) * (Mathf.Max(0f, searchShakeRotation) * searchShakeBlend);
            cameraAnimation.SetPose(searchBasePosition + searchBaseRotation * shake, searchBaseRotation * Quaternion.Euler(angles));
        }

        private IEnumerator TurnThenZoomAtAunt(Vector3 fromPosition, Quaternion from, Quaternion to)
        {
            float turnDuration = Mathf.Max(.05f, snapDuration);
            float zoomDelay = turnDuration * Mathf.Clamp01(zoomStartTurnFraction);
            float zoomDuration = Mathf.Max(.05f, scareZoomDuration);
            float duration = Mathf.Max(turnDuration, zoomDelay + zoomDuration);
            bool soundStarted = false;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                if (!soundStarted && time >= zoomDelay)
                {
                    soundStarted = true;
                    PlayAuntZoomSound();
                }
                float turn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / turnDuration));
                float zoom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - zoomDelay) / zoomDuration));
                cameraAnimation.SetPose(Vector3.Lerp(fromPosition, cameraPosition, turn), Quaternion.Slerp(from, to, turn));
                capturedCamera.fieldOfView = Mathf.Lerp(originalFov, scareFieldOfView, zoom);
                yield return null;
            }
            cameraAnimation.SetPose(cameraPosition, to);
            capturedCamera.fieldOfView = scareFieldOfView;
            if (!soundStarted) PlayAuntZoomSound();
        }

        private void PlayAuntZoomSound()
        {
            if (scareAudioSource != null && scareAudioSource.clip != null)
                scareAudioSource.PlayOneShot(scareAudioSource.clip);
        }

        private IEnumerator CameraTransition(Vector3 fromPosition, Vector3 toPosition, Quaternion from, Quaternion to, float fromFov, float toFov, float duration)
        {
            duration = Mathf.Max(0.05f, duration);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / duration);
                cameraAnimation.SetPose(Vector3.Lerp(fromPosition, toPosition, t), Quaternion.Slerp(from, to, t));
                capturedCamera.fieldOfView = Mathf.Lerp(fromFov, toFov, t);
                yield return null;
            }
            cameraAnimation.SetPose(toPosition, to);
            capturedCamera.fieldOfView = toFov;
        }

        private void RestoreCamera()
        {
            searchMotionActive = false;
            if (!cameraCaptured) return;
            if (capturedCamera != null)
            {
                capturedCamera.fieldOfView = originalFov;
                if (player != null && player.State == PlayerSeatController.SeatState.Seated)
                    cameraAnimation.SetPose(cameraPosition, cameraRotation);
            }
            if (player != null && player.State == PlayerSeatController.SeatState.Seated) player.SeatedLookLocked = oldLookLock;
            cameraCaptured = false;
        }
        private void RestoreNpcColliders()
        {
            if (npcColliders == null) return;
            for (int i = 0; i < npcColliders.Length; i++) if (npcColliders[i] != null) npcColliders[i].enabled = npcColliderStates[i];
            npcColliders = null;
        }
        public void ResetForTesting()
        {
            CancelSequence();
            Stage = StoryStage.WaitingForMeal;
            auntDismissed = false;
            if (npcCaptured && auntObject != null)
            {
                auntObject.transform.SetPositionAndRotation(npcPosition, npcRotation);
                auntObject.SetActive(hideAuntUntilSearch ? false : oldNpcActive);
            }
            if (scareAudioSource != null) scareAudioSource.Stop();
        }
        public void CancelSequence()
        {
            if (sequence != null) { StopCoroutine(sequence); sequence = null; }
            if (auntExitRoutine != null) { StopCoroutine(auntExitRoutine); auntExitRoutine = null; }
            SetAuntWalking(false);
            RestoreCamera(); RestoreNpcColliders(); HideAllText();
            if (screenFade != null) screenFade.Clear();
            if (player != null && player.State == PlayerSeatController.SeatState.Exploring) player.ResumeAfterStand();
            if (npcCaptured && auntObject != null && !auntDismissed && Stage != StoryStage.Completed)
            {
                auntObject.transform.SetPositionAndRotation(npcPosition, npcRotation);
                auntObject.SetActive(oldNpcActive);
            }
            if (Stage != StoryStage.Completed) Stage = StoryStage.WaitingForMeal;
            mealDelayElapsed = 0f;
            DialogueLineIndex = -1;
            moneyDecision.Reset();
            awaitingDialogueRelease = true;
            awaitingRelease = true;
        }
        private void OnDisable() { interact?.Disable(); advanceDialogue?.Disable(); CancelSequence(); }
        private void OnDestroy()
        {
            if (moneyChoiceUI != null)
            {
                moneyChoiceUI.acceptButton.onClick.RemoveListener(ChooseAcceptMoney);
                moneyChoiceUI.refuseButton.onClick.RemoveListener(ChooseRefuseMoney);
            }
            interact?.Dispose(); advanceDialogue?.Dispose();
        }
    }
}
