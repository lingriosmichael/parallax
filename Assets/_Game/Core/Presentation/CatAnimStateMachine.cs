namespace Parallax.Core
{
    // Pure presentation state. It consumes already-derived motion values and owns
    // only animation timing; gameplay never reads it.
    public sealed class CatAnimStateMachine
    {
        readonly float riseExit;
        readonly float fallEnter;
        readonly float walkEnter;
        readonly float walkExit;
        readonly float landDuration;

        float landTimeRemaining;

        public CatAnimStateMachine(
            float riseExit,
            float fallEnter,
            float walkEnter,
            float walkExit,
            float landDuration)
        {
            this.riseExit = riseExit;
            this.fallEnter = fallEnter;
            this.walkEnter = walkEnter;
            this.walkExit = walkExit;
            this.landDuration = landDuration;
            State = CatAnimState.Idle;
        }

        public CatAnimState State { get; private set; }

        // ---------- PAX-V07: the new table (Step(in CatAnimInput)) ----------
        // The legacy overloads below keep their pre-V07 behaviour exactly (§11 R4); only this path shows Run and Turn.

        readonly CatAnimSettings settings;
        bool pendingFlip;   // Turn is playing: the facing flips when it ends, or at once if another state interrupts it
        float lastSpeed;    // the previous frame's absolute surface speed (the acceleration test)
        float acceleration; // this frame's change of absolute surface speed, u/s²
        int slowFrames;     // consecutive frames below walkExit, this one included

        public CatAnimStateMachine(in CatAnimSettings settings)
            : this(settings.RiseExit, settings.FallEnter, settings.WalkEnter, settings.WalkExit, settings.LandDuration)
        {
            this.settings = settings;
            FramesInState = settings.MinStateFrames;   // the starting state counts as settled
        }

        /// <summary>+1 facing the cat's local right, -1 its left. The new table owns it: a ground reversal flips it at the
        /// end of Turn; in the air (and whenever Turn is interrupted) it flips at once.</summary>
        public int Facing { get; private set; } = 1;

        /// <summary>Seconds the current state has shown, this frame included (frame dt; a state change starts it at this
        /// frame's dt).</summary>
        public float StateTime { get; private set; }

        /// <summary>Presentation frames the current state has shown, this one included.</summary>
        public int FramesInState { get; private set; }

        /// <summary>This frame's change of absolute surface speed, u/s² (negative = slowing down).</summary>
        public float Acceleration => acceleration;

        /// <summary>Braking harder than SnapAcceleration: a digital stop or reversal.</summary>
        public bool Braking => acceleration < -settings.SnapAcceleration;

        static bool Ground(CatAnimState s) =>
            s == CatAnimState.Idle || s == CatAnimState.Walk || s == CatAnimState.Run || s == CatAnimState.Turn;

        /// <summary>PAX-V07 §2: the priority table. Item 1 implements the ground rows (Turn, Run / Walk, Idle) on top of the
        /// pre-V07 air rows (Rise / Fall / Land) and Climb; later items add their rows.</summary>
        public CatAnimState Step(in CatAnimInput input)
        {
            float dt = input.Dt > 0f ? input.Dt : 0f;
            CatAnimState before = State;
            CatAnimState next = Next(input, dt);
            // No flicker: Idle, Walk or Run shown for fewer than MinStateFrames frames holds while the next state is another
            // ground state (leaving the ground or climbing always wins at once; Turn keeps its own clip length and cancel).
            if (next != before && before != CatAnimState.Turn && Ground(before) && Ground(next) && FramesInState < settings.MinStateFrames)
            {
                if (next == CatAnimState.Turn) pendingFlip = false;
                next = before;
            }
            FramesInState = next != before ? 1 : FramesInState + 1;
            StateTime = next != before ? dt : StateTime + dt;
            State = next;
            return next;
        }

        CatAnimState Next(in CatAnimInput input, float dt)
        {
            float speed = input.SurfaceSpeed;
            float abs = speed < 0f ? -speed : speed;
            acceleration = dt > 0f ? (abs - lastSpeed) / dt : 0f;
            lastSpeed = abs;
            slowFrames = abs < walkExit ? slowFrames + 1 : 0;
            int motion = speed > 0f ? 1 : -1;
            bool against = abs > settings.FlipHysteresis && motion != Facing;

            if (input.Climbing || !input.Grounded)
            {
                // No Turn off the ground: an interrupted Turn, or a reversal in the air, flips the facing at once.
                CompleteTurn();
                if (against) Facing = motion;
                if (input.Climbing)
                {
                    landTimeRemaining = 0f;
                    return CatAnimState.Climb;
                }
                return Airborne(input.VelocityAlongGravity);
            }

            if (State == CatAnimState.Rise || State == CatAnimState.Fall)
            {
                if (against) Facing = motion;
                landTimeRemaining = landDuration;
                return CatAnimState.Land;
            }
            if (State == CatAnimState.Land)
            {
                if (against) Facing = motion;
                landTimeRemaining -= dt;
                if (landTimeRemaining > 0f) return CatAnimState.Land;
            }

            if (State == CatAnimState.Turn)
            {
                // Moving the old way again cancels the Turn (the facing never flipped); otherwise it plays to its end.
                if (abs > settings.FlipHysteresis && motion == Facing) pendingFlip = false;
                else if (StateTime < settings.TurnDuration) return CatAnimState.Turn;
                else CompleteTurn();
                against = abs > settings.FlipHysteresis && motion != Facing;
            }

            if (against && abs > settings.WalkEnter)
            {
                pendingFlip = true;
                return CatAnimState.Turn;
            }
            return Locomotion(abs, against);
        }

        // Run / Walk / Idle by surface speed, each boundary with its hysteresis band. A creep against the facing below
        // walkEnter stays Idle (no Turn below walkEnter, §2 row 10).
        CatAnimState Locomotion(float abs, bool against)
        {
            bool running = State == CatAnimState.Run;
            bool moving = running || State == CatAnimState.Walk || State == CatAnimState.Turn;
            // Moving against the facing below walkEnter: a creep from rest stays Idle; a reversal in progress (the speed
            // passing through zero before Turn) stays on its planted Walk frame, so no Idle flashes before the Turn.
            if (against) return moving && settings.MinStateFrames > 1 ? CatAnimState.Walk : CatAnimState.Idle;
            // A digital stop or reversal brakes in a few ticks. Braking hard, the cat walks it out: Run hands over to Walk at
            // once (the presenter enters Walk on the frame that brings the paws to a stance just as the body stops), and
            // Walk holds to the stop, so no gallop frame freezes while the body slides and nothing flashes before a Turn.
            if (Braking && (running || State == CatAnimState.Walk)) return CatAnimState.Walk;
            if (abs >= settings.RunEnter || (running && abs >= settings.RunExit)) return CatAnimState.Run;
            if (abs > walkEnter) return CatAnimState.Walk;
            if (abs < walkExit)
            {
                // Coming to rest, the cat stands on a Walk frame for MinStateFrames frames before Idle: a reversal (the speed
                // passes through zero) goes straight from Walk to Turn, and two turns meet on a planted Walk frame, with no
                // Idle flashing between them.
                if (moving && settings.MinStateFrames > 1 && slowFrames <= settings.MinStateFrames) return CatAnimState.Walk;
                return CatAnimState.Idle;
            }
            return moving ? CatAnimState.Walk : CatAnimState.Idle;
        }

        CatAnimState Airborne(float velocityAlongGravity)
        {
            landTimeRemaining = 0f;
            if (velocityAlongGravity < -riseExit) return CatAnimState.Rise;
            if (velocityAlongGravity > fallEnter) return CatAnimState.Fall;
            if (State == CatAnimState.Rise || State == CatAnimState.Fall) return State;
            return CatAnimState.Fall;
        }

        void CompleteTurn()
        {
            if (!pendingFlip) return;
            pendingFlip = false;
            Facing = -Facing;
        }

        /// <summary>PAX-087 (D-089): Climb while the motor climbs; leaving it, the ordinary rules pick the next state
        /// (a leap reads as Rise, a release as Fall, a cat on the ground as Idle or Walk).</summary>
        public CatAnimState Step(
            bool climbing,
            bool grounded,
            float speedAlongSurface,
            float velocityAlongGravity,
            float dt)
        {
            if (!climbing) return Step(grounded, speedAlongSurface, velocityAlongGravity, dt);
            landTimeRemaining = 0f;
            return SetState(CatAnimState.Climb);
        }

        public CatAnimState Step(
            bool grounded,
            float speedAlongSurface,
            float velocityAlongGravity,
            float dt)
        {
            if (!grounded)
            {
                landTimeRemaining = 0f;

                if (velocityAlongGravity < -riseExit)
                    return SetState(CatAnimState.Rise);

                if (velocityAlongGravity > fallEnter)
                    return SetState(CatAnimState.Fall);

                if (State == CatAnimState.Rise || State == CatAnimState.Fall)
                    return State;

                // A real jump crossed riseExit above. With no previous air state,
                // any velocity inside the ambiguous apex band is treated as Fall.
                return SetState(CatAnimState.Fall);
            }

            if (State == CatAnimState.Rise || State == CatAnimState.Fall)
            {
                landTimeRemaining = landDuration;
                return SetState(CatAnimState.Land);
            }

            if (State == CatAnimState.Land)
            {
                landTimeRemaining -= dt > 0f ? dt : 0f;
                if (landTimeRemaining > 0f)
                    return State;
            }

            float surfaceSpeed = speedAlongSurface < 0f
                ? -speedAlongSurface
                : speedAlongSurface;

            if (surfaceSpeed > walkEnter)
                return SetState(CatAnimState.Walk);

            if (surfaceSpeed < walkExit)
                return SetState(CatAnimState.Idle);

            if (State != CatAnimState.Idle && State != CatAnimState.Walk)
                return SetState(CatAnimState.Idle);

            return State;
        }

        public void Reset(CatAnimState state = CatAnimState.Idle)
        {
            CompleteTurn();
            State = state;
            landTimeRemaining = 0f;
            StateTime = 0f;
            FramesInState = settings.MinStateFrames;
            slowFrames = 0;
            lastSpeed = 0f;
            acceleration = 0f;
        }

        CatAnimState SetState(CatAnimState state)
        {
            State = state;
            return State;
        }
    }
}
