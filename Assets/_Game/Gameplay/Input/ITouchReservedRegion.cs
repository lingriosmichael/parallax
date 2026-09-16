using UnityEngine;

namespace Parallax.Gameplay.Input
{
    public interface ITouchReservedRegion
    {
        bool ContainsScreenPoint(Vector2 screenPos);
    }
}
