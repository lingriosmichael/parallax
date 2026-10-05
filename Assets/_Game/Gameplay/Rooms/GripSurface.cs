using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-105 (D-110 amendment 2): marks a grip wall's collider, the only kind of face a cat can grab
    /// (CatWallCling reads it). D-111 (the developer, 2026-10-04: "remove the scratches on the wall"): no look of its own; a
    /// grip wall wears its wall's skin until the real texture exists (NEEDED_ASSETS § Grip wall).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GripSurface : MonoBehaviour
    {
    }
}
