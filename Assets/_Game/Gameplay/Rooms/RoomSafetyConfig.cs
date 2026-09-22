using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [CreateAssetMenu(menuName = "PARALLAX/Room Safety Config")]
    public sealed class RoomSafetyConfig : ScriptableObject
    {
        [Tooltip("Ticks a death freezes the room for before the reset runs. 0 = today's synchronous reset.")]
        [SerializeField] int holdTicks = 30;

        [Tooltip("Margin added on every side of a room's computed element bounds, in world units.")]
        [SerializeField] float boundsMargin = 2f;

        public int HoldTicks => holdTicks;
        public float BoundsMargin => boundsMargin;
    }
}
