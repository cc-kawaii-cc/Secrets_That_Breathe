using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    // One of these runs the whole feature: it picks the nearest banana tree in range, shows "[E] กินกล้วย" on the
    // scene's existing SeatingCanvas (no second canvas is created), and plays the eating animation when E is pressed.
    // It is created automatically by BananaEatInteraction, so it does not need to be placed in a scene.
    public sealed class BananaEatDriver : MonoBehaviour
    {
        private const string PromptName = "BananaEatPrompt";
        private static BananaEatDriver instance;

        public static bool IsEating => instance != null && instance.eating;

        public static BananaEatDriver Ensure()
        {
            if (instance != null) return instance;
            // After Unity recompiles during Play Mode the static is lost but the object survives.
            instance = FindFirstObjectByType<BananaEatDriver>();
            if (instance != null) return instance;
            var holder = new GameObject("BananaEatDriver");
            DontDestroyOnLoad(holder);
            instance = holder.AddComponent<BananaEatDriver>();
            return instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private FirstPersonController player;
        private CharacterController body;
        private Camera view;
        private Transform canvas;
        private Scene uiScene;
        private GameObject promptPanel;
        private TMP_Text promptLabel;
        private bool promptWarned, promptFailed, promptVisible, eating;
        private string shownText;
        private BananaViewModel viewModel;
        private readonly List<Behaviour> suspended = new List<Behaviour>();

        private void OnEnable() => instance = this;

        private void Update()
        {
            if (eating) return;
            BananaEatInteraction target = FindTarget();
            ShowPrompt(target);
            if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Eat(target));
        }

        // ------------------------------------------------------------------ choosing a tree

        private BananaEatInteraction FindTarget()
        {
            if (Time.timeScale <= 0f || !BananaEatInteraction.CanEatAny) return null;
            if (player == null) { player = FindFirstObjectByType<FirstPersonController>(); body = null; }
            if (player == null || !player.isActiveAndEnabled) return null;
            if (body == null) body = player.GetComponent<CharacterController>();
            // While the player is riding in the car or is locked into a scene, these are switched off.
            if (body == null || !body.enabled) return null;
            Scene scene = player.gameObject.scene;
            if (scene.path == "Assets/MainScenes/From_PM/MainScene_2_Evening.unity") return null;
            if (Busy(scene)) return null;

            Vector3 feet = player.transform.position;
            BananaEatInteraction best = null;
            float bestDistance = float.MaxValue;
            foreach (var tree in BananaEatInteraction.All)
            {
                if (tree == null || tree.gameObject.scene != scene || !tree.isActiveAndEnabled || tree.BananasLeft <= 0) continue;
                Vector3 offset = tree.Center - feet;
                if (Mathf.Abs(offset.y) > tree.heightTolerance) continue;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance <= tree.eatDistance && distance < bestDistance) { best = tree; bestDistance = distance; }
            }
            return best;
        }

        // Other scripted moments take priority: police encounter, document search, phone call, item inspection, skid mark photo.
        private static bool Busy(Scene scene)
            => EveningPoliceEncounter.BlocksFlashlightToggle(scene)
            || ItemInspectionInteraction.IsBusy(scene)
            || PhoneCallInteraction.IsBusy(scene)
            || DocumentSearchSequence.BlocksRoomInteractions(scene)
            || MidnightSkidPhoto.BlocksOtherPrompts(scene);   // midnight scene: the skid mark photo prompt / phone has priority

        // ------------------------------------------------------------------ prompt

        private void ShowPrompt(BananaEatInteraction target)
        {
            bool show = target != null;
            if (show && !FindPrompt(player.gameObject.scene)) show = false;
            if (show && promptLabel != null && shownText != target.promptText)
            {
                promptLabel.text = target.promptText;
                shownText = target.promptText;
                PrepareGlyphs(promptLabel);
            }
            if (promptPanel != null && promptVisible != show)
            {
                promptPanel.SetActive(show);
                promptVisible = show;
            }
        }

        // The prompt is a text label directly under the scene's SeatingCanvas, named BananaEatPrompt. If the scene does not
        // have it yet it is created once, in the style of a Thai prompt already on that canvas.
        // "Tools > Secrets That Breathe > Banana Eating" saves it in the scene so its text can be edited in the editor.
        private bool FindPrompt(Scene scene)
        {
            if (uiScene == scene)
            {
                if (promptPanel != null) return true;
                if (promptFailed) return false;
            }
            canvas = null; promptPanel = null; promptLabel = null; promptVisible = false; shownText = null;
            uiScene = scene;
            promptFailed = true;
            canvas = FindCanvas(scene, true);
            if (canvas == null) return false;
            promptLabel = BuildLabel(canvas, PromptName, "[E] กินกล้วย", .16f, 700f, 90f, 0f);
            if (promptLabel == null) return false;
            promptPanel = promptLabel.gameObject;
            promptPanel.SetActive(false);
            promptFailed = false;
            return true;
        }

        // The overlay canvas to put UI under: the scene's SeatingCanvas, else any screen-space canvas, else (only when
        // create is true) a small canvas of our own so the feature also works in a test scene.
        public static Transform FindCanvas(Scene scene, bool create)
        {
            Transform other = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "SeatingCanvas") return root.transform;
                var c = root.GetComponent<Canvas>();
                if (other == null && c != null && c.renderMode == RenderMode.ScreenSpaceOverlay) other = root.transform;
            }
            if (other != null || !create) return other;
            var holder = new GameObject("BananaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(holder, scene);
            holder.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            holder.GetComponent<Canvas>().sortingOrder = 20;
            var scaler = holder.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            return holder.transform;
        }

        public static Transform FindDeep(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }

        // Returns the label called labelName under the canvas, creating it (hidden) if it does not exist.
        // New labels copy font, size and colour from a Thai prompt already on the canvas, so Thai letters display.
        public static TMP_Text BuildLabel(Transform parent, string labelName, string text, float anchorY, float width, float height, float fontSize)
        {
            Transform existing = FindDeep(parent, labelName);
            if (existing != null) return existing.GetComponent<TMP_Text>() ?? existing.GetComponentInChildren<TMP_Text>(true);
            TMP_Text template = FindThaiTemplate(parent);
            var go = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, anchorY);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.text = text;
            if (template != null)
            {
                label.font = template.font;
                label.fontSharedMaterial = template.fontSharedMaterial;
                label.fontSize = fontSize > 0f ? fontSize : template.fontSize;
                label.color = template.color;
                label.fontStyle = template.fontStyle;
            }
            else
            {
                label.font = FindThaiFont();
                label.fontSize = fontSize > 0f ? fontSize : 34f;
                label.color = new Color(.86f, .86f, .86f);
            }
            go.AddComponent<ThaiTextDisplay>().Bind();
            PrepareGlyphs(label);
            go.SetActive(false);
            return label;
        }

        // A Thai label already on the canvas to copy the style from; short bracketed prompts like "[F] ..." come first.
        private static TMP_Text FindThaiTemplate(Transform parent)
        {
            TMP_Text any = null;
            foreach (var label in parent.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.font == null || !ContainsThai(label.text)) continue;
                if (label.text.TrimStart().StartsWith("[")) return label;
                if (any == null) any = label;
            }
            return any;
        }

        private static TMP_FontAsset FindThaiFont()
        {
            const char probe = '\u0E01';
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (font != null && font.HasCharacter(probe, true, true)) return font;
            return TMP_Settings.defaultFontAsset;
        }

        // Dynamic font atlases only contain letters that were asked for; ask for this label's before it is shown.
        private static void PrepareGlyphs(TMP_Text label)
        {
            if (label == null || label.font == null) return;
            if (label.font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                label.font.TryAddCharacters(label.text + ThaiTextPreparation.Prepare(label.text), out string missing, true);
            label.ForceMeshUpdate(true, true);
        }

        private static bool ContainsThai(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text) if (c >= '\u0E00' && c <= '\u0E7F') return true;
            return false;
        }

        // ------------------------------------------------------------------ eating

        private IEnumerator Eat(BananaEatInteraction tree)
        {
            view = Camera.main;
            if (view == null || player == null) yield break;
            eating = true;
            ShowPrompt(null);
            SuspendControls();
            viewModel = BananaViewModel.Create(view, tree.IsRipe);
            Transform treeTransform = tree.transform;
            yield return viewModel.Run(() => view != null && player != null && body != null && body.enabled && treeTransform != null);
            bool completed = viewModel != null && viewModel.Completed;
            if (completed && tree != null) tree.TakeBanana();
            if (viewModel != null) { viewModel.Dispose(); viewModel = null; }
            if (completed && BananaEatInteraction.LimitReached && !BananaEatInteraction.Tricked)
            {
                // The last banana: Phi Tani appears, then the scene restarts. Controls stay locked until then.
                yield return Trick();
                yield break;
            }
            ResumeControls();
            eating = false;
        }

        private void Finish()
        {
            if (viewModel != null) { viewModel.Dispose(); viewModel = null; }
            CleanupTrick();
            ResumeControls();
            eating = false;
        }

        // ------------------------------------------------------------------ Phi Tani's trick

        private GameObject ghost;
        private Vector3 ghostSpot;
        private readonly List<Behaviour> suspendedCamera = new List<Behaviour>();
        private static AudioClip sting;

        private IEnumerator Trick()
        {
            Scene scene = player != null ? player.gameObject.scene : SceneManager.GetActiveScene();
            yield return new WaitForSeconds(.8f);
            if (view == null || player == null) { Restart(scene); yield break; }

            // Free the camera so it can be turned toward her.
            foreach (var component in view.GetComponents<Behaviour>())
                if (component.enabled && component.GetType().Name == "CinemachineBrain") { component.enabled = false; suspendedCamera.Add(component); }

            // She appears in front of the player, on the ground, facing the player.
            Vector3 flat = view.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < .01f) flat = player.transform.forward;
            flat.Normalize();
            Vector3 spot = player.transform.position + flat * 2.4f;
            float groundY = player.transform.position.y;
            groundY = GroundBelow(spot, groundY);
            spot.y = groundY;
            ghostSpot = spot;

            Light glow = null;
            var prefab = Resources.Load<GameObject>("PhiTaniGhost");
            if (prefab != null)
            {
                ghost = Instantiate(prefab, spot, Quaternion.LookRotation(-flat, Vector3.up));
                ghost.name = "PhiTani";
                foreach (var collider in ghost.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                // A pale green glow in front of her so she can be seen in the dark.
                var glowObject = new GameObject("PhiTaniGlow");
                glowObject.transform.SetParent(ghost.transform, false);
                glowObject.transform.localPosition = new Vector3(0f, 1.4f, 1.1f);
                glow = glowObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(.6f, 1f, .65f);
                glow.range = 5f;
                glow.intensity = 8f;
                glow.shadows = LightShadows.None;
            }
            else Debug.LogWarning("[Banana] Resources/PhiTaniGhost.prefab was not found, so Phi Tani cannot appear.", this);

            PlaySting();

            // Turn the camera to her face.
            Vector3 face = spot + Vector3.up * 1.58f;
            Quaternion from = view.transform.rotation;
            Quaternion to = Quaternion.LookRotation(face - view.transform.position, Vector3.up);
            const float turnSeconds = .45f;
            for (float t = 0f; t < turnSeconds; t += Time.deltaTime)
            {
                if (view == null) break;
                view.transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / turnSeconds));
                Bob(Time.time);
                if (glow != null) glow.intensity = Mathf.Lerp(8f, 3f, t / turnSeconds);
                yield return null;
            }
            if (view != null) view.transform.rotation = to;

            // "That banana is mine!"
            TMP_Text line = null;
            if (canvas != null || FindPrompt(scene))
            {
                line = BuildLabel(canvas, "PhiTaniLine", "นั่นกล้วยกู!", .16f, 1400f, 120f, 46f);
                if (line != null)
                {
                    line.color = new Color(.92f, .98f, .9f);
                    line.gameObject.SetActive(true);
                }
            }
            for (float t = 0f; t < 2.6f; t += Time.deltaTime) { Bob(Time.time); yield return null; }

            // Black out, then restart this scene from the beginning.
            CanvasGroup fade = CreateFade();
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                if (fade != null) fade.alpha = Mathf.SmoothStep(0f, 1f, t / 1.1f);
                Bob(Time.time);
                yield return null;
            }
            if (fade != null) fade.alpha = 1f;
            yield return null;
            if (line != null) line.gameObject.SetActive(false);
            Restart(scene);
        }

        // A full-screen black cover under the SeatingCanvas (a nested canvas, so it can sit above the other UI).
        private CanvasGroup CreateFade()
        {
            if (canvas == null) return null;
            var go = new GameObject("BananaTrickFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var overlay = go.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 1000;
            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;
            return group;
        }

        private void Bob(float time)
        {
            if (ghost != null) ghost.transform.position = ghostSpot + Vector3.up * (.12f + .03f * Mathf.Sin(time * 2f));
        }

        private void Restart(Scene scene)
        {
            // From now on Phi Tani has fooled the player: no tree can be eaten from.
            BananaEatInteraction.MarkTricked();
            CleanupTrick();
            eating = false;
            ReloadScene(scene);
        }

        private void CleanupTrick()
        {
            if (ghost != null) { Destroy(ghost); ghost = null; }
            foreach (var component in suspendedCamera) if (component != null) component.enabled = true;
            suspendedCamera.Clear();
        }

        private static void ReloadScene(Scene scene)
        {
#if UNITY_EDITOR
            // Works in the editor even when the scene is not in the Build Settings. Loads the saved scene file.
            if (!string.IsNullOrEmpty(scene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        // The police encounter's jump-scare sound if this scene has one, otherwise a generated sting.
        private void PlaySting()
        {
            AudioSource source = ghost != null ? ghost.AddComponent<AudioSource>() : view != null ? view.gameObject.AddComponent<AudioSource>() : null;
            if (source == null) return;
            source.spatialBlend = 0f;
            var encounter = FindFirstObjectByType<EveningPoliceEncounter>();
            AudioClip clip = encounter != null && encounter.scareAudio != null ? encounter.scareAudio.clip : null;
            if (clip != null) source.PlayOneShot(clip, Mathf.Clamp(encounter.scareAudio.volume, .6f, 1f));
            else source.PlayOneShot(Sting(), .9f);
        }

        // The ground under a point: the nearest surface below it that is not part of a banana tree
        // (the trees have MeshColliders, so a ray from above could otherwise stop on a leaf).
        private static float GroundBelow(Vector3 spot, float fallback)
        {
            float best = float.MaxValue, height = fallback;
            foreach (var hit in Physics.RaycastAll(spot + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<BananaEatInteraction>() != null || BananaEatInteraction.IsBananaTreeName(hit.collider.transform.root.name)) continue;
                if (hit.distance >= best) continue;
                best = hit.distance;
                height = hit.point.y;
            }
            return height;
        }

        private static AudioClip Sting()
        {
            if (sting != null) return sting;
            const int rate = 44100;
            int count = (int)(rate * 1.4f);
            var data = new float[count];
            var random = new System.Random(3);
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 2.6f) * Mathf.Clamp01(t * 60f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                low += .25f * (noise - low);
                float scream = Mathf.Sin(2f * Mathf.PI * (1100f - 520f * t) * t) * .35f + Mathf.Sin(2f * Mathf.PI * (1630f - 700f * t) * t) * .2f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 48f * t) * .5f;
                data[i] = Mathf.Clamp((scream + low * .9f + rumble) * envelope, -1f, 1f);
            }
            sting = AudioClip.Create("PhiTaniSting", count, 1, rate, false);
            sting.SetData(data, 0);
            return sting;
        }

        private void SuspendControls()
        {
            suspended.Clear();
            Transform root = player.transform.root;
            foreach (var component in root.GetComponentsInChildren<Behaviour>(true))
                switch (component.GetType().Name)
                {
                    case "FirstPersonController": case "PlayerMovement": case "MouseLook": case "PlayerInteractor":
                        if (component.enabled) { component.enabled = false; suspended.Add(component); }
                        break;
                }
            ClearInput(root);
        }

        private void ResumeControls()
        {
            foreach (var component in suspended) if (component != null) component.enabled = true;
            suspended.Clear();
            if (player != null) ClearInput(player.transform.root);
        }

        private static void ClearInput(Transform root)
        {
            foreach (var input in root.GetComponentsInChildren<StarterAssetsInputs>(true))
            { input.move = Vector2.zero; input.look = Vector2.zero; input.jump = false; input.sprint = false; }
        }

        // If the driver goes away mid-bite (scene change, stopping Play Mode) the player must get control back.
        private void OnDisable()
        {
            if (instance == this) instance = null;
            if (eating) Finish();
            if (promptPanel != null) promptPanel.SetActive(false);
        }
    }
}
