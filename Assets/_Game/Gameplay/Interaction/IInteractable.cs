using Parallax.Core;

namespace Parallax.Gameplay.Interaction
{
    public interface IInteractable
    {
        void Interact(ObserverId observer, IAnchorRequester requester);
    }
}
