namespace Parallax.Core
{
    // Pure presentation state. It consumes already-derived motion values and owns
    // only animation timing; gameplay never reads it.
    public sealed partial class CatAnimStateMachine
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

        /// <summary>Item 3: +1 while the cat moves up the vine (against gravity), -1 down it; held while it hangs still. Climb
        /// plays its frames forward or reversed by it.</summary>
        public int ClimbDirection { get; private set; } = 1;

        /// <summary>Item 5: which fidget IdleFidget shows (0 look around, 1 ear twitch, 2 sit down).</summary>
        public int FidgetIndex { get; private set; }

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
            if (next != CatAnimState.Idle && next != CatAnimState.IdleFidget) ResetFidgets();   // movement, a landing, a death...
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
            bool wallJumpEdge = WallJumpEdge(input);
            float v = input.VelocityAlongGravity;
            // Item 4: the gravity sign, seen every frame (a respawn or a reset forgets it, so it never rolls).
            bool flipped = gravityKnown && input.GravitySign != lastGravitySign;
            lastGravitySign = input.GravitySign;
            gravityKnown = !input.Respawned;
            // The body turns 180 degrees with gravity, so the same local facing would point the other way on screen: the facing
            // flips with it, and the cat keeps facing where it faced (D-049: screen-relative; final critic).
            if (flipped) { CompleteTurn(); Facing = -Facing; }

            // Row 1 (item 7): level complete, the celebration; it never leaves (held under the level-complete screen).
            if (input.LevelComplete || State == CatAnimState.Door)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                return CatAnimState.Door;
            }

            // Row 2 (item 6): this cat's death hold beats everything below.
            if (input.Holding)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                return CatAnimState.Death;
            }

            // Row 3 (item 6): the respawn, until its clip ends or any input moves the cat. It snaps gravity and position: the
            // gravity sign and the high point start again, so no Flip and no landing follows.
            if (input.Respawned)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                contact = false;
                gravityKnown = false;
                heightKnown = false;
                return CatAnimState.Respawn;
            }
            if (State == CatAnimState.Respawn)
            {
                float moved = input.BodySurfaceSpeed < 0f ? -input.BodySurfaceSpeed : input.BodySurfaceSpeed;
                if (StateTime < settings.RespawnDuration - Epsilon && moved <= walkExit && !input.JumpedThisStep && onGround) return CatAnimState.Respawn;
            }

            // Row 4 (item 4): the sign of gravity changed: the tucked roll, over Climb and everything below. A reset (a respawn,
            // which snaps gravity) forgets the sign, so it never rolls.
            if (settings.FlipDuration > 0f)
            {
                if (flipped)
                {
                    CompleteTurn();
                    landTimeRemaining = 0f;
                    contact = false;
                    return CatAnimState.Flip;
                }
                if (State == CatAnimState.Flip && StateTime < settings.FlipDuration - Epsilon) return CatAnimState.Flip;
            }

            // Row 5 (item 3): on the vine, Climb while moving along it, Hang when still. The facing is the one the cat grabbed
            // with (the grab's x snap to the vine's centre is not a reversal). A stop holds Climb until it has been still for
            // MinStateFrames frames, so a reversal (the speed passing through zero) never flashes Hang.
            if (input.Climbing)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                contact = false;
                wasClimbing = true;
                float up = -input.VelocityAlongGravity;
                bool still = (up < 0f ? -up : up) <= settings.ClimbStillSpeed;
                climbStillFrames = still ? climbStillFrames + 1 : 0;
                if (!still) ClimbDirection = up > 0f ? 1 : -1;
                if (State == CatAnimState.Climb && still && climbStillFrames < settings.MinStateFrames) return CatAnimState.Climb;
                return still ? CatAnimState.Hang : CatAnimState.Climb;
            }

            // Row 5b (PAX-105): on a wall, and the wall jump.
            if (WallRow(input, onGround, wallJumpEdge, out CatAnimState wall)) return wall;

            // Row 6 (item 3): climbing ends. With the leap's launch (the body leaving against gravity at about the jump speed:
            // the motor's climber doesn't raise JumpedThisStep, so the launch speed is the signal, read only) it's Leap, with the
            // facing of the leap's own direction; otherwise a release, and the ordinary rows below (Fall, or Idle on the ground).
            if (wasClimbing)
            {
                wasClimbing = false;
                climbStillFrames = 0;
                float launch = -input.BodyVelocityAlongGravity;
                if (input.JumpedThisStep || (launch >= settings.LeapSpeedMin && launch <= settings.LeapSpeedMax))
                {
                    float body = input.BodySurfaceSpeed;
                    if ((body < 0f ? -body : body) > settings.FlipHysteresis) Facing = body > 0f ? 1 : -1;
                    rose = true;
                    return CatAnimState.Leap;
                }
                rose = false;
            }
            // Leap plays for its duration (its clip), then the air rows; landing ends it like any air state.
            if (State == CatAnimState.Leap && !onGround && StateTime < settings.LeapDuration - Epsilon) return CatAnimState.Leap;

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

            // Touchdown (row 9): Land, or HardLand past hardLandDistance along gravity. §3 (ruled 2026-09-30): any movement input
            // cancels the landing on the same frame, so a cat moving on at touchdown goes straight into its gait or Turn.
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
                if (Acting(abs, bodyAbs, against))
                {
                    // Moving on: the motion's state, on this frame. A reversal pressed on the landing is a Turn.
                    if (bodyAbs > abs) { speed = input.BodySurfaceSpeed; abs = bodyAbs; motion = speed > 0f ? 1 : -1; }
                    against = abs > settings.FlipHysteresis && motion != Facing;
                    landTimeRemaining = 0f;
                }
                else
                {
                    if (against) Facing = motion;
                    landTimeRemaining = hard ? LandingDuration(input.HardLandDuration, settings.HardLandDuration)
                                             : LandingDuration(input.LandDuration, landDuration);
                    return hard ? CatAnimState.HardLand : CatAnimState.Land;
                }
            }
            else if (State == CatAnimState.Land || State == CatAnimState.HardLand)
            {
                // Plays to its end unless the player acts (§3): any movement input leaves it on that frame for the gait or Turn
                // below. HardLand recovers through Land, entered on the frame matching HardLand's last pose.
                landTimeRemaining -= dt;
                float bodyAbs = input.BodySurfaceSpeed < 0f ? -input.BodySurfaceSpeed : input.BodySurfaceSpeed;
                // From a standstill a movement input shows as the body pulling ahead of the drawn cat (a steady drift isn't one:
                // with no input the motor brakes), or a reversal.
                bool acting = bodyAbs > abs + settings.SpeedLeadTolerance || (against && (abs > bodyAbs ? abs : bodyAbs) > walkEnter);
                if (!acting)
                {
                    if (landTimeRemaining > Epsilon) return State;
                    if (State == CatAnimState.HardLand)
                    {
                        landTimeRemaining = LandingDuration(input.LandDuration, landDuration);
                        if (landTimeRemaining > Epsilon) return CatAnimState.Land;
                    }
                }
                else if (bodyAbs > abs)
                {
                    // The drawn cat hasn't caught up with the input yet: the state the motion implies is the body's.
                    speed = input.BodySurfaceSpeed; abs = bodyAbs; motion = speed > 0f ? 1 : -1;
                    against = abs > settings.FlipHysteresis && motion != Facing;
                }
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
            return Fidget(Locomotion(abs, against, input), input, dt);
        }

        // Row 12 (item 5): idling fidgetDelay seconds plays the next fidget of the fixed cycle (look around -> ear twitch -> sit
        // down); look around and ear twitch go back to Idle at their clip's end (the timer starts again), sit down holds.
        // Any input cancels any fidget on this frame (§11 R5: read from the body: moving faster than walkExit, which a pressed
        // stick passes on its first tick, or a jump), and restarts the cycle.
        CatAnimState Fidget(CatAnimState loco, in CatAnimInput input, float dt)
        {
            float[] durations = settings.FidgetDurations;
            if (durations == null || durations.Length == 0 || float.IsInfinity(settings.FidgetDelay)) return loco;
            float body = input.BodySurfaceSpeed < 0f ? -input.BodySurfaceSpeed : input.BodySurfaceSpeed;
            if (loco != CatAnimState.Idle || body > walkExit || input.JumpedThisStep)
            {
                ResetFidgets();
                // The drawn cat hasn't moved yet on the input's frame: the state the motion implies is the body's.
                return loco == CatAnimState.Idle && State == CatAnimState.IdleFidget && body > walkEnter ? CatAnimState.Walk : loco;
            }
            if (State == CatAnimState.IdleFidget)
            {
                if (FidgetIndex >= durations.Length - 1 || StateTime < durations[FidgetIndex] - Epsilon) return CatAnimState.IdleFidget;
                nextFidget = FidgetIndex + 1;
                idleTime = 0f;
                return CatAnimState.Idle;
            }
            idleTime += dt;
            if (idleTime < settings.FidgetDelay - Epsilon) return CatAnimState.Idle;
            FidgetIndex = nextFidget;
            idleTime = 0f;
            return CatAnimState.IdleFidget;
        }

        void ResetFidgets()
        {
            nextFidget = 0;
            idleTime = 0f;
        }

        // §3, at touchdown: a movement input keeps the body moving on at walking speed without braking off the drawn speed (a
        // release skids: the body is already slower than drawn), or reverses it.
        bool Acting(float abs, float bodyAbs, bool against) =>
            ((abs > bodyAbs ? abs : bodyAbs) > walkEnter && bodyAbs >= abs - settings.SpeedLeadTolerance)
            || (against && (abs > bodyAbs ? abs : bodyAbs) > walkEnter);

        // A landing's length from the frame it enters on (the presenter's pose match), or the clip's full length.
        static float LandingDuration(float fromPose, float full) => fromPose > 0f ? fromPose : full;

        const float Epsilon = 1e-4f;
        bool wasClimbing;       // the previous frame was on the vine (item 3)
        int climbStillFrames;   // consecutive still frames on the vine, this one included
        bool groundedLastFrame = true;
        float idleTime;         // item 5: seconds idling since the last fidget (or since moving)
        int nextFidget;         // item 5: the next fidget in the cycle
        bool gravityKnown;      // item 4: the gravity sign has been seen (a reset forgets it)
        float lastGravitySign;
        bool jumpSeen;
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
            if ((input.Grounded && !input.JumpedThisStep) || input.Climbing || input.Clinging || h > highPoint) highPoint = h;
            return drop;
        }

        static bool Air(CatAnimState s) =>
            s == CatAnimState.Rise || s == CatAnimState.Apex || s == CatAnimState.Fall || s == CatAnimState.TakeOff || s == CatAnimState.Leap
            || s == CatAnimState.Flip || IsWall(s);

        static bool GroundOrLanding(CatAnimState s) =>
            Ground(s) || s == CatAnimState.Land || s == CatAnimState.HardLand || s == CatAnimState.IdleFidget;

        // Run / Walk / Idle by surface speed, each boundary with its hysteresis band. A creep against the facing below
        // walkEnter stays Idle (no Turn below walkEnter, §2 row 10).
        CatAnimState Locomotion(float abs, bool against, in CatAnimInput input)
        {
            bool running = State == CatAnimState.Run;
            bool moving = running || State == CatAnimState.Walk || State == CatAnimState.Turn;
            // Moving against the facing below walkEnter: a creep from rest stays Idle; a reversal in progress (the speed
            // passing through zero before Turn) stays on its planted Walk frame, so no Idle flashes before the Turn.
            if (against) return moving && settings.MinStateFrames > 1 ? CatAnimState.Walk : CatAnimState.Idle;
            // A digital stop or reversal brakes in a few ticks. Braking hard, the cat walks it out: Run hands over to Walk at
            // once (the presenter enters Walk on the frame that brings the paws to a stance just as the body stops), and
            // Walk holds to the stop, so no gallop frame freezes while the body slides and nothing flashes before a Turn.
            // A reversal at a run (the motor brakes at its acceleration, not its deceleration: input.Stopping false) keeps the
            // gallop into the Turn, which flips on a symmetrical Run frame; only a real stop walks the brake out.
            if (Braking && running && !input.Stopping) return CatAnimState.Run;
            if (Braking && (running || State == CatAnimState.Walk)) return CatAnimState.Walk;
            bool wantsRun = abs >= settings.RunEnter || (running && abs >= settings.RunExit);
            // Walk↔Run switch only where the paws of both clips match (the presenter's switch frames), not on the first frame
            // over the threshold; a stop (below walkEnter) or a hard brake doesn't wait.
            if (wantsRun) return running || input.GaitSwitchReady ? CatAnimState.Run : CatAnimState.Walk;
            if (abs > walkEnter) return running && !input.GaitSwitchReady ? CatAnimState.Run : CatAnimState.Walk;
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
            jumpSeen = false;
            groundedLastFrame = true;
            wasClimbing = false;
            climbStillFrames = 0;
            ClimbDirection = 1;
            gravityKnown = false;
            ResetWall();
            ResetFidgets();
        }

        CatAnimState SetState(CatAnimState state)
        {
            State = state;
            return State;
        }
    }
}
