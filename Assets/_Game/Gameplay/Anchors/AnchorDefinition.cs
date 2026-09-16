using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Anchors
{
    [CreateAssetMenu(menuName = "PARALLAX/Anchor Definition")]
    public sealed class AnchorDefinition : ScriptableObject
    {
        [SerializeField, Range(1, 65534)] int id = 1;
        [SerializeField, Range(0f, 1f)] float initialValue;
        [SerializeField] string displayName;

        public AnchorId Id => new AnchorId((ushort)id);
        public float InitialValue => initialValue;
        public string DisplayName => displayName;
    }
}
