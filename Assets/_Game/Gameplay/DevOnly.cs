using UnityEngine;

namespace Parallax.Gameplay
{
    /// <summary>Removes a development-only GameObject from non-debug player builds.</summary>
    public sealed class DevOnly : MonoBehaviour
    {
        public static bool ShouldRemainInBuild(bool isDebugBuild)
        {
            return isDebugBuild;
        }

        void Awake()
        {
            if (ShouldRemainInBuild(Debug.isDebugBuild)) return;
            Destroy(gameObject);
        }
    }
}
