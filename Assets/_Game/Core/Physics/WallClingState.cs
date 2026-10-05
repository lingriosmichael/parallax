using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-105 (D-110 and its amendment): the wall-cling rules, pure. CatWallCling owns one per cat and feeds it
    /// motor steps (gravity down only; the caller never latches with gravity up).
    /// Grab: a press is remembered for bufferTicks steps (its own and the next bufferTicks - 1, as JumpWindows counts the
    /// jump buffer). A remembered press, or latch mode, latches the cat onto a clingable face it touches while not rising.
    /// Latching starts latch mode; while it lasts, the next face reached latches without another press. A face is a
    /// collider and the side of the cat it is on (+1 a wall on the right, -1 on the left).
    /// Face lock: the face last left can't be latched again until the cat is grounded or latches a different face.
    /// Latch mode ends on landing, a release (push-away, sliding off the face's end, the face moving, a flip, a launch or a
    /// push) and, after a wall jump, on the first airborne, falling step below the height it jumped from (it ran out of
    /// walls). A wall jump keeps latch mode and starts the move lock: moveLockTicks steps in which Move toward the face
    /// just left is ignored and, with no Move away, the sideways speed is kept.</summary>
    public sealed class WallClingState
    {
        public const float DefaultSlideSpeed = 2f;
        public const float DefaultWallJumpSideSpeed = 6f;
        public const int DefaultMoveLockTicks = 8;
        public const int DefaultGrabBufferTicks = 6;
        public const float DefaultMinFaceHeight = 1f;
        public const float DefaultProbeDistance = .05f;
        public const float DefaultNormalThreshold = .9f;
        public const float DefaultReleaseThreshold = .5f;

        public readonly struct Face : System.IEquatable<Face>
        {
            public readonly int Collider;
            public readonly int Side;
            public Face(int collider, int side) { Collider = collider; Side = side; }
            public bool Equals(Face other) => Collider == other.Collider && Side == other.Side;
            public override bool Equals(object obj) => obj is Face f && Equals(f);
            public override int GetHashCode() => Collider * 3 + Side;
        }

        bool locked;
        Face lockedFace;

        public bool IsClinging { get; private set; }
        /// <summary>The face clung to (meaningful only while clinging).</summary>
        public Face Current { get; private set; }
        public bool LatchMode { get; private set; }
        /// <summary>Steps left in which a Grab press is still remembered (0: none).</summary>
        public int GrabBuffer { get; private set; }
        /// <summary>Steps left of the move lock after a wall jump (0: none), and the side of the face it locks Move toward.</summary>
        public int MoveLock { get; private set; }
        public int MoveLockSide { get; private set; }
        /// <summary>A wall jump started this airborne stretch of latch mode, from this height (the collider centre's).</summary>
        public bool WallJumped { get; private set; }
        public float LaunchHeight { get; private set; }

        /// <summary>A press remembered, or latch mode: a face touched while not rising latches.</summary>
        public bool WantsLatch => GrabBuffer > 0 || LatchMode;

        /// <summary>Nothing to do this step: no cling, no press remembered, no latch mode, no move lock.</summary>
        public bool Idle => !IsClinging && GrabBuffer == 0 && !LatchMode && MoveLock == 0;

        public bool IsLocked(Face face) => locked && lockedFace.Equals(face);

        /// <summary>Once per motor step, before any decision: a press opens the buffer.</summary>
        public void BeginStep(bool grabPressed, int bufferTicks)
        {
            if (grabPressed && bufferTicks > 0) GrabBuffer = bufferTicks;
        }

        /// <summary>Once per motor step, after every decision: the buffer counts down.</summary>
        public void EndStep()
        {
            if (GrabBuffer > 0) GrabBuffer--;
        }

        /// <summary>The motor's normal path, once per step: true (and one step used) while the move lock lasts, so it covers
        /// the moveLockTicks steps after the wall jump.</summary>
        public bool ConsumeMoveLock()
        {
            if (MoveLock == 0) return false;
            MoveLock--;
            return true;
        }

        /// <summary>True when `face` may be latched now: a press remembered or latch mode, not clinging, not rising, not the
        /// locked face.</summary>
        public bool CanLatch(Face face, bool rising) => !IsClinging && WantsLatch && !rising && !IsLocked(face);

        public void Latch(Face face)
        {
            if (locked && !lockedFace.Equals(face)) locked = false;   // a different face clears the lock
            IsClinging = true;
            Current = face;
            LatchMode = true;
            GrabBuffer = 0;
            WallJumped = false;
            MoveLock = 0;
        }

        /// <summary>Lets go of the face and locks it; `endLatchMode` also ends latch mode. Nothing if not clinging (latch mode
        /// still ends when asked).</summary>
        public void Release(bool endLatchMode)
        {
            if (IsClinging)
            {
                locked = true;
                lockedFace = Current;
                IsClinging = false;
            }
            if (endLatchMode) EndLatchMode();
        }

        /// <summary>The wall jump: lets go (the face is locked), keeps latch mode, starts the move lock against that face's
        /// side and remembers the launch height for the out-of-walls test.</summary>
        public void WallJump(float launchHeight, int moveLockTicks)
        {
            int side = Current.Side;
            Release(endLatchMode: false);
            WallJumped = true;
            LaunchHeight = launchHeight;
            MoveLock = moveLockTicks;
            MoveLockSide = side;
        }

        /// <summary>A grounded step: any cling ends, latch mode ends, the face lock clears and the move lock stops.</summary>
        public void Landed()
        {
            IsClinging = false;
            EndLatchMode();
            locked = false;
            MoveLock = 0;
        }

        /// <summary>An airborne step without a cling: after a wall jump, falling below the launch height ends latch mode.</summary>
        public void AirStep(float centreHeight, bool falling)
        {
            if (LatchMode && WallJumped && !IsClinging && falling && centreHeight < LaunchHeight) EndLatchMode();
        }

        public void EndLatchMode()
        {
            LatchMode = false;
            WallJumped = false;
        }

        /// <summary>Gravity up: no cling, no latch mode, no press remembered (D-110 amendment (2)).</summary>
        public void GravityUp()
        {
            Release(endLatchMode: true);
            GrabBuffer = 0;
        }

        /// <summary>A death, room reset or respawn: nothing clung, remembered or locked.</summary>
        public void Clear()
        {
            IsClinging = false;
            Current = default;
            LatchMode = false;
            WallJumped = false;
            GrabBuffer = 0;
            MoveLock = 0;
            MoveLockSide = 0;
            locked = false;
            lockedFace = default;
        }

        /// <summary>A hit normal that counts as a wall: within threshold of horizontal (|normal.x| ≥ threshold).</summary>
        public static bool IsWallNormal(Vector2 normal, float threshold) => Mathf.Abs(normal.x) >= threshold;

        /// <summary>The side the face is on, from its normal (which points from the face toward the cat).</summary>
        public static int SideOf(Vector2 normal) => normal.x < 0f ? 1 : -1;

        /// <summary>|Move| at least threshold away from the face on `side`.</summary>
        public static bool PushesAway(float move, int side, float threshold) => -side * move >= threshold;

        /// <summary>The fall speed while clinging: normal gravity, capped at the slide speed (a faster fall is cut at once).</summary>
        public static float SlideFall(float fall, float gravity, float dt, float slideSpeed) => Mathf.Min(fall + gravity * dt, slideSpeed);

        /// <summary>The wall jump (gravity down): the normal jump launch up plus sideSpeed away from the face on `side`.</summary>
        public static Vector2 WallJumpVelocity(int side, float sideSpeed, float jumpSpeed) => new(-side * sideSpeed, jumpSpeed);

        /// <summary>During the move lock: Move toward the locked side reads 0.</summary>
        public static float LockedMove(float move, int lockedSide) => move * lockedSide > 0f ? 0f : move;
    }
}
