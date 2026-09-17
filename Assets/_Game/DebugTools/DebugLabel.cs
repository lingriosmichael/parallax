using UnityEngine;

namespace Parallax.DebugTools
{
    public sealed class DebugLabel : MonoBehaviour
    {
        [SerializeField] string text;
        [SerializeField] Vector2 offset;

        public string Text => text;
        public Vector2 Offset => offset;
    }
}
