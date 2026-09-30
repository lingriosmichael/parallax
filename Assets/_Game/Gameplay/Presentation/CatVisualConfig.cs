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

        [Tooltip("PAX-V07: a touchdown whose fall distance along gravity (from the highest point since leaving the ground) is at least this, in units, shows HardLand; otherwise Land (set by the setup menu, from the levels' drops).")]
        [SerializeField, Min(0f)] float hardLandDistance = 2.75f;

        [Tooltip("PAX-V07: leaving the ground without a jump, the ground pose holds while the cat has dropped less than this (units) and moves along gravity inside the apex band: a one-tick ground blip never flashes an air pose (set by the setup menu).")]
        [SerializeField, Min(0f)] float airGraceDrop = 0.03f;

        [Tooltip("PAX-V07: a grounded cat whose paws are further than this (units) below the top of the collider it stands on (its rounded collider rolling off a ledge's corner) is shown off the ground (set by the setup menu).")]
        [SerializeField, Min(0f)] float groundSinkTolerance = 0.01f;

        [Tooltip("PAX-V07: how far (u/s) the body's speed must lead the drawn speed to count as the player's doing: a release on landing, a move pressed during Land (set by the setup menu).")]
        [SerializeField, Min(0f)] float speedLeadTolerance = 0.5f;

        [Tooltip("PAX-V07 (ruled 2026-09-30): a ground reversal holds the turn's flip frame this long (seconds) before the facing flips: one display frame (set by the setup menu).")]
        [SerializeField, Min(0f)] float turnHoldTime = 1f / 60f;

        [Tooltip("PAX-V07 item 5: seconds of idling before the next fidget of the cycle (look around -> ear twitch -> sit down; set by the setup menu).")]
        [SerializeField, Min(0f)] float fidgetDelay = 3f;

        [Tooltip("PAX-V07 item 3: on the vine, a speed along it (u/s) at or below this is still: Hang (set by the setup menu).")]
        [SerializeField, Min(0f)] float climbStillSpeed = 0.02f;

        [Tooltip("PAX-V07 item 3: climbing ends with a leap when the body leaves against gravity within this fraction of the way from the jump speed toward the climb speed, either side of the jump speed (set by the setup menu).")]
        [SerializeField, Range(0f, 1f)] float leapSpeedFraction = 0.5f;

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
        public float HardLandDistance => hardLandDistance;
        public float AirGraceDrop => airGraceDrop;
        public float GroundSinkTolerance => groundSinkTolerance;
        public float SpeedLeadTolerance => speedLeadTolerance;
        public float TurnHoldTime => turnHoldTime;
        public float FidgetDelay => fidgetDelay;
        public float ClimbStillSpeed => climbStillSpeed;
        /// <summary>Item 3: the band of launch speeds against gravity (u/s) that make the end of a climb a leap: the jump speed
        /// less / plus leapSpeedFraction of its difference from the climb speed (a release keeps at most the climb speed; a
        /// geyser launch is faster). Infinite without a motor config (no leap is recognized).</summary>
        public float LeapSpeedMin(float gravityStrength) => motorConfig != null ? JumpSpeed(gravityStrength) - leapSpeedFraction * (JumpSpeed(gravityStrength) - motorConfig.ClimbSpeed) : float.PositiveInfinity;
        public float LeapSpeedMax(float gravityStrength) => motorConfig != null ? JumpSpeed(gravityStrength) + leapSpeedFraction * (JumpSpeed(gravityStrength) - motorConfig.ClimbSpeed) : float.PositiveInfinity;
        /// <summary>The motor's jump speed, u/s, at `gravityStrength` (0 without a motor config): the air clips' frames run
        /// over the velocity range of a normal jump.</summary>
        public float JumpSpeed(float gravityStrength) => motorConfig != null ? Parallax.Core.JumpMath.SpeedForHeight(motorConfig.JumpHeight, gravityStrength) : 0f;
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
