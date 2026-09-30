namespace Parallax.Core
{
    /// <summary>PAX-V07 (§5): what the cat's animation table reads on one presentation frame. Values the presenter derived
    /// from the motor and its drawn transform; the state machine never reaches back into gameplay. Later items add fields.</summary>
    public readonly struct CatAnimInput
    {
        public CatAnimInput(bool grounded, bool climbing, float surfaceSpeed, float velocityAlongGravity, float dt,
            bool jumpedThisStep = false, float gravitySign = -1f)
        {
            Grounded = grounded;
            Climbing = climbing;
            SurfaceSpeed = surfaceSpeed;
            VelocityAlongGravity = velocityAlongGravity;
            Dt = dt;
            JumpedThisStep = jumpedThisStep;
            GravitySign = gravitySign;
        }

        /// <summary>The motor's grounded flag.</summary>
        public bool Grounded { get; }
        /// <summary>The motor is climbing a vine.</summary>
        public bool Climbing { get; }
        /// <summary>Speed along the surface, in units/second; positive = the cat's local right (+1 facing).</summary>
        public float SurfaceSpeed { get; }
        /// <summary>Velocity along gravity, in units/second; positive = falling.</summary>
        public float VelocityAlongGravity { get; }
        /// <summary>This presentation frame's length, in seconds (frame dt, not the tick).</summary>
        public float Dt { get; }
        /// <summary>The motor's JumpedThisStep on the latest tick (item 2 reads it).</summary>
        public bool JumpedThisStep { get; }
        /// <summary>The sign of the gravity direction's y: -1 gravity down, +1 gravity up.</summary>
        public float GravitySign { get; }
    }

    /// <summary>PAX-V07: the tunables of the animation table, in units/second and seconds. Built by the presenter from
    /// CatVisualConfig and the clip table (no numbers live in code).</summary>
    public readonly struct CatAnimSettings
    {
        public CatAnimSettings(float riseExit, float fallEnter, float walkEnter, float walkExit, float landDuration,
            float runEnter, float runExit, float turnDuration, float flipHysteresis, float snapAcceleration = float.PositiveInfinity,
            int minStateFrames = 1)
        {
            MinStateFrames = minStateFrames;
            SnapAcceleration = snapAcceleration;
            RiseExit = riseExit;
            FallEnter = fallEnter;
            WalkEnter = walkEnter;
            WalkExit = walkExit;
            LandDuration = landDuration;
            RunEnter = runEnter;
            RunExit = runExit;
            TurnDuration = turnDuration;
            FlipHysteresis = flipHysteresis;
        }

        public float RiseExit { get; }
        public float FallEnter { get; }
        /// <summary>Surface speed above which Idle becomes Walk (and a reversal on the ground plays Turn).</summary>
        public float WalkEnter { get; }
        /// <summary>Surface speed below which Walk (or Run) becomes Idle.</summary>
        public float WalkExit { get; }
        public float LandDuration { get; }
        /// <summary>Surface speed at or above which the cat shows Run (runFraction × MaxSpeed).</summary>
        public float RunEnter { get; }
        /// <summary>Surface speed below which Run goes back to Walk (runExitFraction × MaxSpeed).</summary>
        public float RunExit { get; }
        /// <summary>How long Turn shows (its clip's length); the facing flips at its end.</summary>
        public float TurnDuration { get; }
        /// <summary>Surface speed a reversal must exceed, against the facing, before it counts.</summary>
        public float FlipHysteresis { get; }
        /// <summary>Braking faster than this (u/s²) is a digital stop or reversal: the cat walks the brake out (Run hands
        /// over to Walk at once, Walk holds to the stop).</summary>
        public float SnapAcceleration { get; }
        /// <summary>The fewest presentation frames a ground state (Idle, Walk, Run, Turn) shows before another ground state
        /// may replace it: no single-frame flicker.</summary>
        public int MinStateFrames { get; }
    }
}
