using UnityEngine;

namespace SecretsThatBreathe.FinalGameplay
{
    public abstract class InteractionTarget : MonoBehaviour
    {
        [SerializeField] private string promptText = "[E] Interact";

        public string PromptText => promptText;
        public abstract bool CanInteract { get; }

        public void SetPromptText(string value)
        {
            promptText = value;
        }

        public abstract void Interact(CameraRayInteractor interactor);
    }
}
