using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [CreateAssetMenu(menuName = "PARALLAX/Room Safety Config")]
    public sealed class RoomSafetyConfig : ScriptableObject
    {
        /// <summary>D-298 (the developer: "Levels also reset wayyyy to quickly. Add an extra second"): 1.6 s at 50 Hz.</summary>
        public const int DefaultHoldTicks = 80;

        [Tooltip("Ticks a death freezes the room for before the reset runs. 0 = today's synchronous reset.")]
        [SerializeField] int holdTicks = DefaultHoldTicks;

        [Tooltip("Margin added on every side of a room's computed element bounds, in world units.")]
        [SerializeField] float boundsMargin = 2f;

        public int HoldTicks => holdTicks;
        public float BoundsMargin => boundsMargin;
    }
}
