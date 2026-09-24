using UnityEngine;

namespace Parallax.Gameplay.Cameras
{
    // PAX-052 (D-071): named constants for the level camera, in one place per CLAUDE.md. Starting
    // values proposed in Phase 1, confirmed in review; tuned on device later (out of scope here).
    [CreateAssetMenu(menuName = "PARALLAX/Level Camera Config")]
    public sealed class LevelCameraConfig : ScriptableObject
    {
        [Tooltip("Added on every side of a room's content bounds to make the camera frame.")]
        [SerializeField] float viewMargin = 0.5f;

        [Tooltip("The tallest orthographic view height (world units) the level camera ever uses. A room whose frame needs more than this at the current aspect switches to follow mode.")]
        [SerializeField] float maxViewHeight = 16f;

        [Tooltip("Horizontal look-ahead, in the cat's direction of travel, in follow mode.")]
        [SerializeField] float lookAhead = 2.5f;

        [Tooltip("How far (world units) the cat must travel the other way before the look-ahead switches sides. Stops a cat at rest from flipping it on float noise.")]
        [SerializeField] float lookAheadFlipDistance = 0.1f;

        [SerializeField] Vector2 deadZoneHalfExtents = new Vector2(2f, 1.6f);
        [SerializeField] float smoothTime = 0.18f;
        [SerializeField] float maxSpeed = 40f;

        public float ViewMargin => viewMargin;
        public float MaxViewHeight => maxViewHeight;
        public float LookAhead => lookAhead;
        public float LookAheadFlipDistance => lookAheadFlipDistance;
        public Vector2 DeadZoneHalfExtents => deadZoneHalfExtents;
        public float SmoothTime => smoothTime;
        public float MaxSpeed => maxSpeed;
    }
}
