namespace Parallax.Core
{
    /// <summary>PAX-V07 (§5): what the cat's animation table reads on one presentation frame. Values the presenter derived
    /// from the motor and its drawn transform; the state machine never reaches back into gameplay. Later items add fields.</summary>
    public readonly struct CatAnimInput
    {
        public CatAnimInput(bool grounded, bool climbing, float surfaceSpeed, float velocityAlongGravity, float dt,
            bool jumpedThisStep = false, float gravitySign = -1f, float heightAgainstGravity = 0f, bool holding = false, bool rollingOff = false,
            float bodySurfaceSpeed = float.NaN)
        {
            BodySurfaceSpeed = float.IsNaN(bodySurfaceSpeed) ? surfaceSpeed : bodySurfaceSpeed;
            RollingOff = rollingOff;
            HeightAgainstGravity = heightAgainstGravity;
            Holding = holding;
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
        /// <summary>Item 2: the drawn root's position against gravity (world units; up for gravity down, down for gravity up):
        /// the fall distance is the drop in it from the highest point since leaving the ground.</summary>
        public float HeightAgainstGravity { get; }
        /// <summary>Item 2 (the priority input; item 6 feeds it): this cat's room is holding after its death.</summary>
        public bool Holding { get; }
        /// <summary>Item 2: the motor is grounded, but the cat's rounded collider is rolling off its ground's corner (Grounded is
        /// then false): no ground pose holds through it, since its paws would draw into the corner.</summary>
        public bool RollingOff { get; }
        /// <summary>Round 2: the body's own speed along the surface after the latest tick (the motor's velocity, read only; the
        /// drawn speed trails it by up to a tick). It says what the player just did: a jump's direction, a release on landing
        /// (the body already slower than drawn), a move pressed during Land (the body already faster).</summary>
        public float BodySurfaceSpeed { get; }
    }

    /// <summary>PAX-V07: the tunables of the animation table, in units/second and seconds. Built by the presenter from
    /// CatVisualConfig and the clip table (no numbers live in code).</summary>
    public readonly struct CatAnimSettings
    {
        public CatAnimSettings(float riseExit, float fallEnter, float walkEnter, float walkExit, float landDuration,
            float runEnter, float runExit, float turnDuration, float flipHysteresis, float snapAcceleration = float.PositiveInfinity,
            int minStateFrames = 1, float hardLandDistance = float.PositiveInfinity, float takeOffDuration = 0f,
            float hardLandDuration = 0f, float airGraceDrop = 0f, float movingLandDuration = 0f, float movingImpactDuration = 0f,
            float speedLeadTolerance = float.PositiveInfinity)
        {
            MovingLandDuration = movingLandDuration;
            MovingImpactDuration = movingImpactDuration;
            SpeedLeadTolerance = speedLeadTolerance;
            HardLandDistance = hardLandDistance;
            TakeOffDuration = takeOffDuration;
            HardLandDuration = hardLandDuration;
            AirGraceDrop = airGraceDrop;
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
        /// <summary>Item 2: a touchdown whose fall distance along gravity is at least this shows HardLand, otherwise Land.</summary>
        public float HardLandDistance { get; }
        /// <summary>Item 2: how long TakeOff shows after a jump (its clip's length).</summary>
        public float TakeOffDuration { get; }
        /// <summary>Item 2: how long HardLand shows (its clip's length from its entry frame); Land's recovery follows it.</summary>
        public float HardLandDuration { get; }
        /// <summary>Item 2: leaving the ground without a jump, the ground state holds while the cat has dropped less than this
        /// (units) and isn't moving along gravity faster than the apex band: a one-tick ground blip (a floor seam) never
        /// flashes an air state, and a walk-off shows Fall once it really falls.</summary>
        public float AirGraceDrop { get; }
        /// <summary>Round 2: a landing still moving at walking speed plays a quick Land this long (the crouch absorbing the
        /// landing) before the gait; 0 = straight into the gait.</summary>
        public float MovingLandDuration { get; }
        /// <summary>Round 2: a hard landing still moving shows HardLand's impact this long before the gait.</summary>
        public float MovingImpactDuration { get; }
        /// <summary>Round 2: how far (u/s) the body's speed must lead the drawn speed to count as the player's doing: slower = a
        /// release (the landing skids into Land), faster = a move pressed (acting during Land cancels it).</summary>
        public float SpeedLeadTolerance { get; }
    }
}
