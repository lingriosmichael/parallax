using UnityEngine;

namespace Parallax.Gameplay.Echo
{
    [CreateAssetMenu(menuName = "PARALLAX/Echo Config")]
    public sealed class EchoConfig : ScriptableObject
    {
        [SerializeField] float maxSeconds = 10f;
        public int MaxFrames(float fixedDeltaTime) => Mathf.Max(1, Mathf.CeilToInt(maxSeconds / fixedDeltaTime));
    }
}
