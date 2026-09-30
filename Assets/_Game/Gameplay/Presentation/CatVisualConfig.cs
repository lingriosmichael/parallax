using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    [CreateAssetMenu(menuName = "PARALLAX/Cat Visual Config")]
    public sealed class CatVisualConfig : ScriptableObject
    {
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

        [Tooltip("PAX-V07: the motor config whose MaxSpeed the run thresholds are fractions of (set by the setup menu).")]
        [SerializeField] CatMotorConfig motorConfig;

        [Tooltip("PAX-V07: Walk becomes Run at this fraction of the motor's MaxSpeed (set by the setup menu, from the art's strides).")]
        [SerializeField, Range(0f, 1f)] float runFraction = 1f;

        [Tooltip("PAX-V07: Run goes back to Walk below this fraction of the motor's MaxSpeed (hysteresis; set by the setup menu).")]
        [SerializeField, Range(0f, 1f)] float runExitFraction = 1f;

        [Tooltip("PAX-V07: braking faster than this (u/s², a digital stop or reversal) holds the gait on screen until the cat stops or turns (set by the setup menu).")]
        [SerializeField] float snapAcceleration = 20f;

        [Tooltip("PAX-V07: the fewest presentation frames Idle, Walk or Run shows before another ground state replaces it (no single-frame flicker; set by the setup menu).")]
        [SerializeField, Min(1)] int minStateFrames = 1;

        [Tooltip("Unlit outline colour for Reality A.")]
        [SerializeField] Color outlineColorA = new Color(1f, 0.75f, 0.3f, 1f);

        [Tooltip("Unlit outline colour for Reality B.")]
        [SerializeField] Color outlineColorB = new Color(0.35f, 0.85f, 1f, 1f);

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
        public CatMotorConfig MotorConfig => motorConfig;
        public float RunFraction => runFraction;
        public float RunExitFraction => runExitFraction;
        public float SnapAcceleration => snapAcceleration;
        public int MinStateFrames => minStateFrames;
        /// <summary>The motor's release deceleration, u/s² (0 without a motor config): how far a braking cat still travels.</summary>
        public float BrakeDeceleration => motorConfig != null ? motorConfig.Deceleration : 0f;
        /// <summary>Surface speed, u/s, at which Walk becomes Run (infinite without a motor config: Run never shows).</summary>
        public float RunEnterSpeed => motorConfig != null ? runFraction * motorConfig.MaxSpeed : float.PositiveInfinity;
        /// <summary>Surface speed, u/s, below which Run goes back to Walk.</summary>
        public float RunExitSpeed => motorConfig != null ? Mathf.Min(runExitFraction, runFraction) * motorConfig.MaxSpeed : float.PositiveInfinity;
        public Color OutlineColorA => outlineColorA;
        public Color OutlineColorB => outlineColorB;
    }
}
