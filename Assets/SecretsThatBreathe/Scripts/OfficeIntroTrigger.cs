using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class OfficeIntroTrigger : MonoBehaviour
    {
        public OfficeIntroDialogue dialogue;
        public PlayerSeatController player;

        private void OnTriggerEnter(Collider other) => TryStart(other);
        private void OnTriggerStay(Collider other) => TryStart(other);
        private void TryStart(Collider other)
        {
            if (dialogue == null || dialogue.HasStarted || Time.timeScale <= 0f) return;
            var enteringPlayer = other.GetComponentInParent<PlayerSeatController>();
            if (enteringPlayer == null || (player != null && enteringPlayer != player)) return;
            dialogue.BeginFromTrigger();
        }

        public static OfficeIntroTrigger Install(OfficeIntroDialogue dialogue)
        {
            var scene = dialogue.gameObject.scene;
            PlayerSeatController player = null;
            Transform room = null;
            Renderer floor = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<OfficeIntroTrigger>(true);
                if (existing != null) return existing;
                if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name == "Room") room = item;
                    if (item.name == "Main Floor") floor = item.GetComponentInChildren<Renderer>();
                }
            }
            Vector3 centre = player != null ? player.transform.position : Vector3.zero;
            if (room != null)
            {
                var meshes = room.GetComponentsInChildren<Renderer>();
                if (meshes.Length > 0)
                {
                    Bounds bounds = meshes[0].bounds;
                    foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    centre.x = bounds.center.x;
                    centre.z = bounds.center.z;
                }
            }
            if (floor != null) centre.y = floor.bounds.max.y;
            centre.y += 1f;
            var zone = new GameObject("OfficeIntroTrigger", typeof(BoxCollider), typeof(Rigidbody));
            SceneManager.MoveGameObjectToScene(zone, scene);
            zone.transform.position = centre;
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.8f, 2.2f, 2.8f);
            var body = zone.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var trigger = zone.AddComponent<OfficeIntroTrigger>();
            trigger.dialogue = dialogue;
            trigger.player = player;
            return trigger;
        }

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.15f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
