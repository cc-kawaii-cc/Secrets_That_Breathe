using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class SlowWalkingCharacter : MonoBehaviour
    {
        [Tooltip("Disable to play the walking animation in place.")]
        public bool walkForward = true;
        [Min(0f)] public float walkSpeed = .28f;
        private CharacterController controller;

        private void Awake() => controller = GetComponent<CharacterController>();
        private void Update()
        {
            if (Time.timeScale <= 0f || controller == null || !controller.enabled) return;
            controller.SimpleMove(walkForward ? transform.forward * walkSpeed : Vector3.zero);
        }
    }
}
