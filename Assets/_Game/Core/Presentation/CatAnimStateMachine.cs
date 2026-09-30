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

        /// <summary>Item 2: the fall distance along gravity of the last touchdown (units): from the highest point since the
        /// cat left the ground (a ledge's height for a walk-off; reset by a gravity flip in the air) to where it landed.</summary>
        public float LastFallDistance { get; private set; }

        /// <summary>Round 2: the Land or HardLand on screen is a moving landing's quick clip (the presenter plays it faster).</summary>
        public bool MovingLanding => movingLanding && (State == CatAnimState.Land || State == CatAnimState.HardLand);

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
            bool onGround = input.Grounded && !input.Climbing;
            bool wasOnGround = groundedLastFrame;
            groundedLastFrame = onGround;
            float drop = TrackHeight(input);
            // A jump is seen once: JumpedThisStep stays set on every frame until the next tick.
            bool jumpEdge = input.JumpedThisStep && !jumpSeen;
            jumpSeen = input.JumpedThisStep;
            float v = input.VelocityAlongGravity;

            // Priority 2 (item 6 wires the signal): this cat's death hold beats everything below.
            if (input.Holding)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                return CatAnimState.Death;
            }

            if (input.Climbing)
            {
                CompleteTurn();
                if (against) Facing = motion;
                landTimeRemaining = 0f;
                return CatAnimState.Climb;
            }

            // Row 7, TakeOff: a jump from the ground (or from a ground or landing state: a coyote jump, a jump during Land),
            // or (§11 R16) a frame long enough for two ticks that starts grounded and ends rising above airThreshold, the
            // jump tick's flag unseen. At 60 fps every jump tick is seen, so rising without one is a launch: Rise (§2).
            bool fromGround = GroundOrLanding(State) || State == CatAnimState.IdleFidget;
            bool missedJump = !input.JumpedThisStep && wasOnGround && !onGround && v < -riseExit && dt > TickTime.SecondsPerTick;
            if ((jumpEdge && (input.Grounded || fromGround)) || (missedJump && fromGround))
            {
                // The facing is decided at the jump, from the body's own motion (the drawn cat hasn't moved yet on the jump
                // frame): a jump pressed against the facing turns the cat on the gather, never later in the push.
                CompleteTurn();
                float body = input.BodySurfaceSpeed;
                if ((body < 0f ? -body : body) > settings.FlipHysteresis) Facing = body > 0f ? 1 : -1;
                landTimeRemaining = 0f;
                movingLanding = false;
                rose = true;
                return CatAnimState.TakeOff;
            }
            if (State == CatAnimState.TakeOff && v <= fallEnter
                && (StateTime < settings.TakeOffDuration - Epsilon || (input.Grounded && input.JumpedThisStep)))
            {
                // The push plays out while the cat rises (and while the jump tick's grounded flag is still set, never a
                // landing), with the facing it took at the jump. Falling (a head bump) ends it on that frame.
                return CatAnimState.TakeOff;
            }

            if (!onGround)
            {
                // No Turn off the ground: an interrupted Turn flips the facing at once; a reversal in the air flips it once the
                // air motion has clearly turned (above walkEnter), not on the first frame of a drift.
                CompleteTurn();
                if (against && abs > walkEnter) Facing = motion;
                landTimeRemaining = 0f;
                // A one-tick ground blip (a floor seam), or the first moment off a ledge: the ground state holds while the cat
                // has dropped less than airGraceDrop and moves along gravity inside the apex band.
                if (GroundOrLanding(State) && !input.JumpedThisStep && !input.RollingOff && v >= -riseExit && v <= fallEnter && drop < settings.AirGraceDrop)
                    return State == CatAnimState.Turn ? CatAnimState.Walk : State;
                contact = false;
                if (GroundOrLanding(State) || State == CatAnimState.IdleFidget) rose = false;
                return Airborne(v);
            }

            // Touchdown (row 9): Land, or HardLand past hardLandDistance along gravity. Moving at touchdown, the motion already
            // implies the gait (§3): the cat lands into Walk or Run and no crouch slides along the floor.
            if (Air(State))
            {
                // The first grounded frame keeps the air pose (its paws meet the ground): a jump on the next tick then goes
                // straight to TakeOff instead of flashing one frame of Land. The fall distance is the one at contact.
                if (!contact) { contact = true; contactDrop = drop; return State; }
                contact = false;
                drop = contactDrop;
                LastFallDistance = drop;
                bool hard = drop >= settings.HardLandDistance;
                float bodyAbs = input.BodySurfaceSpeed < 0f ? -input.BodySurfaceSpeed : input.BodySurfaceSpeed;
                // Still moving on (not released: the body isn't already slower than the drawn cat), the landing is quick and
                // the gait follows: a hard landing shows HardLand's impact, a walking one a quick Land, a running one goes
                // straight into Run. Standing, or released on touchdown (the landing skids to a stop), it plays in full.
                // The body's speed counts too: a move pressed on the landing tick is a moving landing (§3), not a Land cut short.
                movingLanding = (abs > bodyAbs ? abs : bodyAbs) > walkEnter && bodyAbs >= abs - settings.SpeedLeadTolerance && !against;
                int bodyMotion = input.BodySurfaceSpeed > 0f ? 1 : -1;
                if (bodyAbs > walkEnter && bodyMotion != Facing)
                {
                    // A reversal pressed on the landing: the motion implies Turn at once (no Land cut short after a frame).
                    speed = input.BodySurfaceSpeed; abs = bodyAbs; motion = bodyMotion; against = true;
                    movingLanding = false;
                    landTimeRemaining = 0f;
                }
                else if (!movingLanding)
                {
                    if (against) Facing = motion;
                    landTimeRemaining = hard ? settings.HardLandDuration : landDuration;
                    return hard ? CatAnimState.HardLand : CatAnimState.Land;
                }
                else
                {
                    if (hard && settings.MovingImpactDuration > 0f) { landTimeRemaining = settings.MovingImpactDuration; return CatAnimState.HardLand; }
                    if ((abs > bodyAbs ? abs : bodyAbs) < settings.RunEnter && settings.MovingLandDuration > 0f) { landTimeRemaining = settings.MovingLandDuration; return CatAnimState.Land; }
                    movingLanding = false;
                    landTimeRemaining = 0f;
                }
            }
            else if (State == CatAnimState.Land || State == CatAnimState.HardLand)
            {
                // Plays to its end unless the player acts (§11 R5: the motion changes because of input): a move pressed (the
                // body already faster than the drawn cat) or a reversal leaves it on that frame for the gait or Turn below.
                // HardLand's deep crouch recovers through Land; a moving landing's quick clip hands over to the gait.
                landTimeRemaining -= dt;
                float bodyAbs = input.BodySurfaceSpeed < 0f ? -input.BodySurfaceSpeed : input.BodySurfaceSpeed;
                // A moving landing's quick clip already follows the motion: only a reversal cuts it short.
                bool acting = (!movingLanding && bodyAbs > abs + settings.SpeedLeadTolerance) || (against && (abs > bodyAbs ? abs : bodyAbs) > walkEnter);
                if (!acting)
                {
                    if (landTimeRemaining > Epsilon) return State;
                    if (State == CatAnimState.HardLand && !movingLanding)
                    {
                        landTimeRemaining = landDuration;
                        return CatAnimState.Land;
                    }
                }
                if (acting && bodyAbs > abs)
                {
                    // The drawn cat hasn't caught up with the input yet: the state the motion implies is the body's (a move
                    // pressed from the crouch walks or turns at once, no Idle between).
                    speed = input.BodySurfaceSpeed; abs = bodyAbs; motion = speed > 0f ? 1 : -1;
                    against = abs > settings.FlipHysteresis && motion != Facing;
                }
                movingLanding = false;
                landTimeRemaining = 0f;
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

        const float Epsilon = 1e-4f;
        bool groundedLastFrame = true;
        bool jumpSeen;
        bool movingLanding;     // Land / HardLand is a moving landing's quick clip (the gait follows it)
        bool contact;           // the air pose is showing its one frame of contact with the ground
        float contactDrop;      // the fall distance at that contact
        bool rose;              // this air phase went up (a jump or a launch): its apex band shows Apex; a walk-off's shows Fall
        bool heightKnown;
        float gravitySign;
        float highPoint;        // the highest height against gravity since the cat left the ground (while grounded: this frame's)

        // The fall distance's bookkeeping (§2 details): returns how far the cat is below its high point along gravity. A
        // gravity flip starts the count again from where the cat is; on the ground (or a vine) the high point is the cat.
        float TrackHeight(in CatAnimInput input)
        {
            float h = input.HeightAgainstGravity;
            if (!heightKnown || input.GravitySign != gravitySign)
            {
                heightKnown = true;
                gravitySign = input.GravitySign;
                highPoint = h;
            }
            float drop = highPoint - h;
            if ((input.Grounded && !input.JumpedThisStep) || input.Climbing || h > highPoint) highPoint = h;
            return drop;
        }

        static bool Air(CatAnimState s) =>
            s == CatAnimState.Rise || s == CatAnimState.Apex || s == CatAnimState.Fall || s == CatAnimState.TakeOff;

        static bool GroundOrLanding(CatAnimState s) =>
            Ground(s) || s == CatAnimState.Land || s == CatAnimState.HardLand;

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

        // Row 8, by velocity along gravity: Rise above airThreshold against it, Fall above it along it, and the band between
        // is Apex when this air phase went up; a walk-off (it never rose) shows Fall there.
        CatAnimState Airborne(float velocityAlongGravity)
        {
            if (velocityAlongGravity < -riseExit) { rose = true; return CatAnimState.Rise; }
            if (velocityAlongGravity > fallEnter) return CatAnimState.Fall;
            return rose ? CatAnimState.Apex : CatAnimState.Fall;
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
            heightKnown = false;
            rose = false;
            contact = false;
            movingLanding = false;
            jumpSeen = false;
            groundedLastFrame = true;
        }

        CatAnimState SetState(CatAnimState state)
        {
            State = state;
            return State;
        }
    }
}
