using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V07 §5 (skeleton; items 2+ fill it): the per-frame signals the cat's animation reads besides the motor:
    /// this cat's death hold, its respawn, level complete and a gravity flip. It will subscribe to RoomDeath, CatRespawn,
    /// RoomManager.LevelCompleted and GravityReceiver (found in the cat's own scene, §11 R1), expose plain read-only values,
    /// and reset on OnDisable. Item 1 (the ground states) needs none of them, so nothing reads it yet and the setup menu
    /// doesn't add it to the prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class CatPresentationSignals : MonoBehaviour
    {
        /// <summary>This cat's room is holding after its death (item 6).</summary>
        public bool Holding { get; private set; }
        /// <summary>This cat respawned on this frame (item 6).</summary>
        public bool RespawnedThisFrame { get; private set; }
        /// <summary>The level completed for this cat's observer (item 7).</summary>
        public bool LevelComplete { get; private set; }

        /// <summary>Item 2: the height against gravity of the face of `bounds` that faces against gravity (a floor's top).</summary>
        public static float FaceAgainstGravity(Bounds bounds, Vector2 down) =>
            -Vector2.Dot(bounds.center, down) + Mathf.Abs(Vector2.Dot(bounds.extents, down));

        /// <summary>Item 2: `paws` lie more than `tolerance` below the face against gravity of the solid whose bounds are
        /// `ground` (a grounded capsule rolling off that solid's corner draws its paws into it). Pure: reads only.</summary>
        public static bool SunkBelow(Bounds ground, Vector2 paws, Vector2 down, float tolerance) =>
            FaceAgainstGravity(ground, down) + Vector2.Dot(paws, down) > tolerance;

        /// <summary>Item 2: the room an air pose has around the paws, over the drawn cat's width (5 rays spread over
        /// `halfWidth` either side): `headroom`, how far above the paws (against gravity) the nearest solid is; `footroom`, how
        /// far below them (negative when a solid's face is above the paws, e.g. a step beside the cat), searched from the
        /// collider's centre. Up to `reach`; infinity when none. Reads physics only: the cat's own reality, never `self`.</summary>
        public static void Room(Vector2 paws, Vector2 down, float halfWidth, float reach, ContactFilter2D filter, Collider2D self, RaycastHit2D[] hits,
            out float headroom, out float footroom)
        {
            float above = self != null ? Mathf.Abs(Vector2.Dot(self.bounds.extents, down)) : 0f;
            Vector2 right = new(-down.y, down.x);
            headroom = footroom = float.PositiveInfinity;
            for (int k = -2; k <= 2; k++)
            {
                Vector2 at = paws + right * (halfWidth * k * 0.5f);
                headroom = Mathf.Min(headroom, Nearest(at, -down, reach, filter, self, hits));
                footroom = Mathf.Min(footroom, Nearest(at - down * above, down, reach + above, filter, self, hits) - above);
            }
        }

        static float Nearest(Vector2 origin, Vector2 direction, float reach, ContactFilter2D filter, Collider2D self, RaycastHit2D[] hits)
        {
            float best = float.PositiveInfinity;
            int count = Physics2D.Raycast(origin, direction, filter, hits, reach);
            for (int i = 0; i < count; i++)
                if (hits[i].collider != self && !hits[i].collider.isTrigger && hits[i].distance > 0f && hits[i].distance < best) best = hits[i].distance;
            return best;
        }

        void OnDisable()
        {
            Holding = false;
            RespawnedThisFrame = false;
            LevelComplete = false;
        }
    }
}
