using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // Put this on a banana tree (BananaTree_B, BananaTree_C_Cloth and their _Ripe versions). When the player
    // walks close, "[E] กินกล้วย" appears; pressing E plays the eating animation (BananaEatDriver).
    // Trees named BananaTree... get this component automatically when a scene loads, so nothing has to be set
    // up by hand. Adding it yourself lets you change the values per tree.
    [DisallowMultipleComponent]
    public sealed class BananaEatInteraction : MonoBehaviour
    {
        public enum FruitColour { Auto, Ripe, Green }

        [Header("When the prompt appears")]
        [Tooltip("Optional trigger box. Midnight only accepts interactions with a zone assigned.")]
        public BoxCollider interactionZone;
        [Tooltip("How close the player must be to the trunk, in metres on the ground.")]
        [Min(.5f)] public float eatDistance = 2.4f;
        [Tooltip("Vertical tolerance, so the prompt does not show from a roof or a hole.")]
        [Min(.5f)] public float heightTolerance = 2.5f;
        [Tooltip("Optional: the point the distance is measured from (for example where the bunch hangs). Empty uses the tree's own position.")]
        public Transform anchor;

        [Header("Bananas")]
        [Tooltip("Bananas this tree can give. The whole game also has a total limit (BananaEatInteraction.MaxBananasInTotal, 2).")]
        [Min(1)] public int bananasOnTree = 5;
        [Tooltip("Auto: trees whose name contains Ripe give yellow bananas, the others green.")]
        public FruitColour fruit = FruitColour.Auto;
        [Tooltip("Edit the text shown on the SeatingCanvas prompt (Thai fonts come from the canvas).")]
        public string promptText = "[E] กินกล้วย";
        public UnityEvent onEaten = new UnityEvent();
        [SerializeField] private int eaten;

        // The limit is for the whole game, not per tree. After the last banana Phi Tani tricks the player and
        // the scene restarts; from then on no tree can be eaten from (the counters survive scene reloads).
        public static int MaxBananasInTotal = 2;
        public static int TotalEaten { get; private set; }
        public static bool Tricked { get; private set; }
        public static bool CanEatAny => !Tricked && TotalEaten < MaxBananasInTotal;
        public static bool LimitReached => TotalEaten >= MaxBananasInTotal;
        public static void MarkTricked() => Tricked = true;
        internal static void ResetProgressForTesting() { TotalEaten = 0; Tricked = false; }

        public int BananasLeft => CanEatAny ? Mathf.Max(0, bananasOnTree - eaten) : 0;
        public bool IsRipe => fruit == FruitColour.Ripe
            || (fruit == FruitColour.Auto && (name.IndexOf("Ripe", StringComparison.OrdinalIgnoreCase) >= 0
                || (interactionZone != null && transform.parent != null
                    && transform.parent.name.IndexOf("Ripe", StringComparison.OrdinalIgnoreCase) >= 0)));
        public Vector3 Center => anchor != null ? anchor.position : transform.position;
        public static bool RequiresZone(Scene scene) => scene.name == "MainScene_2_MidNight";

        internal bool ContainsPlayer(Vector3 feet, Vector3 bodyCenter)
        {
            if (interactionZone == null)
            {
                if (RequiresZone(gameObject.scene)) return false;
                Vector3 offset = Center - feet;
                return Mathf.Abs(offset.y) <= heightTolerance
                    && offset.x * offset.x + offset.z * offset.z <= eatDistance * eatDistance;
            }

            if (!interactionZone.enabled || !interactionZone.gameObject.activeInHierarchy) return false;
            Vector3 local = interactionZone.transform.InverseTransformPoint(bodyCenter) - interactionZone.center;
            Vector3 half = interactionZone.size * .5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }
        public void TakeBanana() { eaten++; TotalEaten++; onEaten.Invoke(); }
        public void ResetBananas() => eaten = 0;

        private static readonly List<BananaEatInteraction> all = new List<BananaEatInteraction>();
        public static IReadOnlyList<BananaEatInteraction> All => all;

        // BananaTree, BananaTree_B, BananaTree_B_Ripe, BananaTree_C_Cloth_Ripe, "BananaTree_B (1)" ...
        // but not a folder such as "BananaTrees".
        private static readonly Regex TreeName = new Regex(@"^BananaTree(_[A-Za-z0-9]+)*( \(\d+\))?$");
        public static bool IsBananaTreeName(string objectName) => TreeName.IsMatch(objectName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { all.Clear(); ResetProgressForTesting(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) AttachToScene(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToScene(scene);

        // Adds the component to every banana tree in the scene that does not have it yet. Returns how many were added.
        public static int AttachToScene(Scene scene)
        {
            if (!scene.isLoaded || RequiresZone(scene) || scene.path == "Assets/MainScenes/From_PM/MainScene_2_Evening.unity") return 0;
            int added = 0;
            foreach (GameObject root in scene.GetRootGameObjects()) added += Attach(root.transform);
            return added;
        }

        private static int Attach(Transform candidate)
        {
            if (IsBananaTreeName(candidate.name))
            {
                // A real tree has a mesh; do not look inside it for more trees.
                if (candidate.GetComponentInChildren<Renderer>(true) == null || candidate.GetComponent<BananaEatInteraction>() != null) return 0;
                candidate.gameObject.AddComponent<BananaEatInteraction>();
                return 1;
            }
            int added = 0;
            for (int i = 0; i < candidate.childCount; i++) added += Attach(candidate.GetChild(i));
            return added;
        }

        private void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
            BananaEatDriver.Ensure();
        }

        private void OnDisable() => all.Remove(this);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .9f, .2f, .6f);
            if (interactionZone != null)
            {
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = interactionZone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(interactionZone.center, interactionZone.size);
                Gizmos.matrix = previous;
            }
            else if (!RequiresZone(gameObject.scene))
                Gizmos.DrawWireSphere(Center, eatDistance);
        }
    }
}
