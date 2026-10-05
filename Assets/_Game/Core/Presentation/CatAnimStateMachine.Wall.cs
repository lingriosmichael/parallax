namespace Parallax.Core
{
    // PAX-105 (D-110, §2.4): the wall rows, after the vine (row 5) and before the climb's end (row 6). Clinging, the cat faces
    // the wall (the art's paws reach to its right at facing +1): WallCling while it hangs still, WallSlide while it slides
    // down; a stop holds WallSlide until it has been still for MinStateFrames frames, so a slide that stops never flashes
    // WallCling. A wall jump shows WallJump (the Leap clip) facing away from the wall, for the leap's length, then the air
    // rows; landing ends it like any air state. A wall jump right after the latch (the cling shown fewer than MinStateFrames
    // frames) holds the cling pose until it has had them, then shows WallJump, as a vine's stop holds Climb: no one-frame state.
    public sealed partial class CatAnimStateMachine
    {
        bool wasClinging;       // the previous frame was on a wall
        int slideStillFrames;   // consecutive still frames on the wall, this one included
        bool wallJumpSeen;
        bool pendingWallJump;   // a wall jump waiting for the cling pose's MinStateFrames

        static bool IsWall(CatAnimState s) => s == CatAnimState.WallCling || s == CatAnimState.WallSlide || s == CatAnimState.WallJump;

        // A wall jump is seen once: WallJumped stays set on every frame until the next tick.
        bool WallJumpEdge(in CatAnimInput input)
        {
            bool edge = input.WallJumped && !wallJumpSeen;
            wallJumpSeen = input.WallJumped;
            return edge;
        }

        bool WallRow(in CatAnimInput input, bool onGround, bool wallJumpEdge, out CatAnimState state)
        {
            state = State;
            if (input.Clinging && !onGround)
            {
                CompleteTurn();
                landTimeRemaining = 0f;
                contact = false;
                wasClinging = true;
                if (input.ClingSide != 0) Facing = input.ClingSide;
                float fall = input.VelocityAlongGravity;
                bool still = fall <= settings.ClimbStillSpeed;
                slideStillFrames = still ? slideStillFrames + 1 : 0;
                if (State == CatAnimState.WallSlide && still && slideStillFrames < settings.MinStateFrames) { state = CatAnimState.WallSlide; return true; }
                state = still ? CatAnimState.WallCling : CatAnimState.WallSlide;
                return true;
            }
            bool left = wasClinging;
            if ((wallJumpEdge || pendingWallJump) && left && (State == CatAnimState.WallCling || State == CatAnimState.WallSlide) && FramesInState < settings.MinStateFrames)
            {
                pendingWallJump = true;
                return true;
            }
            wasClinging = false;
            slideStillFrames = 0;
            if (wallJumpEdge || pendingWallJump)
            {
                pendingWallJump = false;
                // Away from the wall: the facing of the body's own motion (a latch and a jump on one step never showed a cling).
                CompleteTurn();
                float body = input.BodySurfaceSpeed;
                if ((body < 0f ? -body : body) > settings.FlipHysteresis) Facing = body > 0f ? 1 : -1;
                landTimeRemaining = 0f;
                contact = false;
                rose = true;
                state = CatAnimState.WallJump;
                return true;
            }
            if (left) rose = false;   // let go of the wall: the ordinary air rows (a fall)
            if (State == CatAnimState.WallJump && !onGround && StateTime < settings.LeapDuration - Epsilon) { state = CatAnimState.WallJump; return true; }
            return false;
        }

        void ResetWall()
        {
            wasClinging = false;
            slideStillFrames = 0;
            wallJumpSeen = false;
            pendingWallJump = false;
        }
    }
}
