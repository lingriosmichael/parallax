using UnityEngine;

namespace Parallax.Gameplay.Anchors
{
    public abstract class RealityManifestation : MonoBehaviour
    {
        public abstract void SetTarget(float value, bool snap);
    }
}
