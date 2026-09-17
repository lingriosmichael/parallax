using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    [CreateAssetMenu(menuName = "PARALLAX/Cat Visual Config")]
    public sealed class CatVisualConfig : ScriptableObject
    {
        [Tooltip("Pixel width/height of one sprite sheet cell, as exported.")]
        [SerializeField] int cellSize = 256;

        [Tooltip("First frame index (inclusive) of the walk loop.")]
        [SerializeField] int walkLoopStart = 0;

        [Tooltip("Last frame index (inclusive) of the walk loop.")]
        [SerializeField] int walkLoopEnd = 9;

        [Tooltip("Frame shown while idle (below idleSpeedThreshold).")]
        [SerializeField] int idleFrame = 0;

        [Tooltip("Frame shown while airborne.")]
        [SerializeField] int airFrame = 0;

        [Tooltip("Walk cycle fps at referenceSpeed.")]
        [SerializeField] float walkFps = 10f;

        [Tooltip("Lowest allowed walk fps, regardless of speed.")]
        [SerializeField] float minFps = 2f;

        [Tooltip("Highest allowed walk fps, regardless of speed.")]
        [SerializeField] float maxFps = 18f;

        [Tooltip("Along-speed, in units/second, at which walkFps plays at its authored rate.")]
        [SerializeField] float referenceSpeed = 6f;

        [Tooltip("Along-speed, in units/second, below which the cat shows Idle instead of Walk.")]
        [SerializeField] float idleSpeedThreshold = 0.15f;

        [Tooltip("Up-speed magnitude, in units/second, above which the cat shows Air instead of Walk/Idle.")]
        [SerializeField] float airThreshold = 1.0f;

        [Tooltip("Along-speed magnitude a new facing direction must exceed, beyond the opposite of the current facing, before flipping.")]
        [SerializeField] float flipHysteresis = 0.05f;

        [Tooltip("Outline width, in texels of the sprite texture.")]
        [SerializeField] float outlineWidth = 1.5f;

        [Tooltip("Fine-tune offset, in world units, added to Visual's computed ground-contact Y position.")]
        [SerializeField] float groundOffset = 0f;

        [Tooltip("Unlit outline colour for Reality A.")]
        [SerializeField] Color outlineColorA = new Color(1f, 0.75f, 0.3f, 1f);

        [Tooltip("Unlit outline colour for Reality B.")]
        [SerializeField] Color outlineColorB = new Color(0.35f, 0.85f, 1f, 1f);

        public int CellSize => cellSize;
        public int WalkLoopStart => walkLoopStart;
        public int WalkLoopEnd => walkLoopEnd;
        public int IdleFrame => idleFrame;
        public int AirFrame => airFrame;
        public float WalkFps => walkFps;
        public float MinFps => minFps;
        public float MaxFps => maxFps;
        public float ReferenceSpeed => referenceSpeed;
        public float IdleSpeedThreshold => idleSpeedThreshold;
        public float AirThreshold => airThreshold;
        public float FlipHysteresis => flipHysteresis;
        public float OutlineWidth => outlineWidth;
        public float GroundOffset => groundOffset;
        public Color OutlineColorA => outlineColorA;
        public Color OutlineColorB => outlineColorB;
    }
}
