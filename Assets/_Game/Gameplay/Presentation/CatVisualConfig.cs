using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    [CreateAssetMenu(menuName = "PARALLAX/Cat Visual Config")]
    public sealed class CatVisualConfig : ScriptableObject
    {
        [Tooltip("Pixel width/height of one sprite sheet cell, as exported.")]
        [SerializeField] int cellSize = 256;

        [Tooltip("Lowest allowed Walk clip fps, regardless of speed.")]
        [SerializeField] float minFps = 2f;

        [Tooltip("Highest allowed Walk clip fps, regardless of speed.")]
        [SerializeField] float maxFps = 18f;

        [Tooltip("Along-speed, in units/second, at which the Walk clip plays at its authored fps.")]
        [SerializeField] float referenceSpeed = 6f;

        [Tooltip("Absolute surface speed, in units/second, above which Idle enters Walk.")]
        [SerializeField] float idleSpeedThreshold = 0.15f;

        [Tooltip("Absolute surface speed, in units/second, below which Walk exits to Idle.")]
        [SerializeField] float walkExit = 0.1f;

        [Tooltip("Gravity-axis speed magnitude delimiting the symmetric Rise/Fall apex band.")]
        [SerializeField] float airThreshold = 1.0f;

        [Tooltip("Seconds Land is held after an air-to-ground transition.")]
        [SerializeField] float landDuration = 0.12f;

        [Tooltip("Along-speed magnitude a new facing direction must exceed, beyond the opposite of the current facing, before flipping.")]
        [SerializeField] float flipHysteresis = 0.05f;

        [Tooltip("Exponential velocity smoothing time constant used only for visual facing.")]
        [SerializeField] float velocitySmoothingTime = 0.08f;

        [Tooltip("Root movement in one frame at or beyond this distance is treated as a teleport.")]
        [SerializeField] float teleportDistance = 1f;

        [Tooltip("Body alpha while this cat is driven by EchoReplay.")]
        [SerializeField, Range(0f, 1f)] float echoAlpha = 0.5f;

        [Tooltip("Outline width, in texels of the sprite texture.")]
        [SerializeField] float outlineWidth = 1.5f;

        [Tooltip("Fine-tune offset, in world units, added to Visual's computed ground-contact Y position.")]
        [SerializeField] float groundOffset = 0f;

        [Tooltip("Unlit outline colour for Reality A.")]
        [SerializeField] Color outlineColorA = new Color(1f, 0.75f, 0.3f, 1f);

        [Tooltip("Unlit outline colour for Reality B.")]
        [SerializeField] Color outlineColorB = new Color(0.35f, 0.85f, 1f, 1f);

        public int CellSize => cellSize;
        public float MinFps => minFps;
        public float MaxFps => maxFps;
        public float ReferenceSpeed => referenceSpeed;
        public float WalkEnter => idleSpeedThreshold;
        public float WalkExit => walkExit;
        public float RiseExit => airThreshold;
        public float FallEnter => airThreshold;
        public float LandDuration => landDuration;
        public float FlipHysteresis => flipHysteresis;
        public float VelocitySmoothingTime => velocitySmoothingTime;
        public float TeleportDistance => teleportDistance;
        public float EchoAlpha => echoAlpha;
        public float OutlineWidth => outlineWidth;
        public float GroundOffset => groundOffset;
        public Color OutlineColorA => outlineColorA;
        public Color OutlineColorB => outlineColorB;
    }
}
