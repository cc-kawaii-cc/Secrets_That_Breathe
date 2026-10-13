using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class MiddleRoomThoughtTrigger : MonoBehaviour
    {
        public PlayerSeatController player;
        [Tooltip("Edit this TMP object's Text Input in SeatingCanvas, not on the trigger.")]
        public TMP_Text messageText;
        [Min(0.1f)] public float messageDuration = 5f;
        public bool triggerOnce = true;
        public bool HasTriggered { get; private set; }
        public bool HasFinishedShowing { get; private set; }
        private Coroutine showing;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            if (messageText != null) messageText.transform.parent.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other) { TryShow(other); }
        private void OnTriggerStay(Collider other)
        {
            // Catch entry during pause/input lock once the player is free to walk again.
            if (triggerOnce && !HasTriggered) TryShow(other);
        }

        private void TryShow(Collider other)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || showing != null || (triggerOnce && HasTriggered) ||
                player == null || !player.isActiveAndEnabled || !player.CanExplore || player.playerBody == null ||
                !other.transform.IsChildOf(player.playerBody) || messageText == null) return;
            HasTriggered = true;
            var canvas = messageText.GetComponentInParent<SeatPromptUI>();
            if (canvas != null) canvas.ShowRoomThought(messageText);
            else messageText.transform.parent.gameObject.SetActive(true);
            showing = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, messageDuration));
            if (messageText != null) messageText.transform.parent.gameObject.SetActive(false);
            HasFinishedShowing = true;
            showing = null;
        }

        private void OnDisable()
        {
            if (showing != null)
            {
                StopCoroutine(showing); showing = null;
                if (!HasFinishedShowing) HasTriggered = false; // An interrupted message must be allowed to finish on a later entry.
            }
            if (messageText != null) messageText.transform.parent.gameObject.SetActive(false);
        }

        public static MiddleRoomThoughtTrigger Install(Scene scene, PlayerSeatController player, SeatPromptUI canvas,
            string triggerName = "MiddleRoomThoughtTrigger", string panelName = "MiddleRoomThought", Vector3? initialPosition = null)
        {
            MiddleRoomThoughtTrigger existing = null;
            Transform room = null, floor = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<MiddleRoomThoughtTrigger>(true))
                    if (candidate.name == triggerName) existing = candidate;
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name == "Room") room = item;
                    if (item.name == "Main Floor") floor = item;
                }
            }
            if (existing == null)
            {
                Vector3 centre = player.playerBody != null ? player.playerBody.position : player.transform.position;
                var meshes = room != null ? room.GetComponentsInChildren<Renderer>(false) : new Renderer[0];
                if (meshes.Length > 0)
                {
                    Bounds bounds = meshes[0].bounds;
                    foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    centre.x = bounds.center.x; centre.z = bounds.center.z;
                }
                Renderer floorMesh = floor != null ? floor.GetComponentInChildren<Renderer>() : null;
                if (floorMesh != null) centre.y = floorMesh.bounds.max.y;
                centre.y += 1f;
                if (initialPosition.HasValue) centre = initialPosition.Value;
                var zoneObject = new GameObject(triggerName, typeof(BoxCollider), typeof(Rigidbody));
                SceneManager.MoveGameObjectToScene(zoneObject, scene);
                zoneObject.transform.position = centre;
                zoneObject.GetComponent<BoxCollider>().isTrigger = true;
                zoneObject.GetComponent<BoxCollider>().size = new Vector3(2.8f, 2.2f, 2.8f);
                zoneObject.GetComponent<Rigidbody>().isKinematic = true;
                zoneObject.GetComponent<Rigidbody>().useGravity = false;
                existing = zoneObject.AddComponent<MiddleRoomThoughtTrigger>();
            }
            if (existing.player == null) existing.player = player;
            if (existing.messageText == null) existing.messageText = canvas.EnsureNamedText(panelName);
            return existing;
        }

        public static MiddleRoomThoughtTrigger InstallDeskThought(Scene scene, PlayerSeatController player, SeatPromptUI canvas)
        {
            var first = Install(scene, player, canvas);
            // Keep the new box separate by default. Both positions remain freely editable.
            return Install(scene, player, canvas, "FindDeskThoughtTrigger", "FindDeskThought", first.transform.position + Vector3.forward * 4f);
        }

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.15f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
        }
    }
}
