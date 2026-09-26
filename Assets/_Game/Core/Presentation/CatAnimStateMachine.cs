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
            State = state;
            landTimeRemaining = 0f;
        }

        CatAnimState SetState(CatAnimState state)
        {
            State = state;
            return State;
        }
    }
}
