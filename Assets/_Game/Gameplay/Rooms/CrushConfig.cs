using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [CreateAssetMenu(menuName = "PARALLAX/Rooms/Crush Config", fileName = "CrushConfig_Default")]
    public sealed class CrushConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] float defaultCrushDepth = .15f;
        public float DefaultCrushDepth => defaultCrushDepth;
    }
}
