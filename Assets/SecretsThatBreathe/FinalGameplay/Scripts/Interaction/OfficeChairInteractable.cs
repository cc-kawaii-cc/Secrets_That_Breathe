using UnityEngine;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class OfficeChairInteractable : InteractionTarget
    {
        [SerializeField] private OfficeSequenceDirector sequenceDirector;

        public override bool CanInteract => sequenceDirector != null && sequenceDirector.CanStart;

        public void Configure(OfficeSequenceDirector director)
        {
            sequenceDirector = director;
            SetPromptText("[E] นั่งที่โต๊ะ");
        }

        public override void Interact(CameraRayInteractor interactor)
        {
            sequenceDirector?.BeginSequence();
        }
    }
}
