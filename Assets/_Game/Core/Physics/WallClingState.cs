using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-105 (D-110 and its amendments): the wall-cling rules, pure. CatWallCling owns one per cat and feeds it
    /// motor steps (gravity down only; the caller never latches with gravity up).
    /// PAX-106 (D-110 amendment 4): the latch is automatic. A clingable face the cat touches while not rising latches; no
    /// button, buffer or latch mode. A face is a collider and the side of the cat it is on (+1 a wall on the right, -1 on
    /// the left).
    /// Face lock: the face last left can't be latched again until the cat is grounded or latches a different face.
    /// A wall jump lets go (the face is locked) and starts the move lock: moveLockTicks steps in which Move toward the face
    /// just left is ignored and, with no Move away, the sideways speed is kept.</summary>
    public sealed class WallClingState
    {
        public const float DefaultSlideSpeed = 2f;
        public const float DefaultWallJumpSideSpeed = 6f;
        public const int DefaultMoveLockTicks = 8;
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
        /// <summary>Steps left of the move lock after a wall jump (0: none), and the side of the face it locks Move toward.</summary>
        public int MoveLock { get; private set; }
        public int MoveLockSide { get; private set; }

        /// <summary>Not clinging and no move lock.</summary>
        public bool Idle => !IsClinging && MoveLock == 0;

        public bool IsLocked(Face face) => locked && lockedFace.Equals(face);

        /// <summary>The motor's normal path, once per step: true (and one step used) while the move lock lasts, so it covers
        /// the moveLockTicks steps after the wall jump.</summary>
        public bool ConsumeMoveLock()
        {
            if (MoveLock == 0) return false;
            MoveLock--;
            return true;
        }

        /// <summary>True when `face` may be latched now: not clinging, not rising, not the locked face.</summary>
        public bool CanLatch(Face face, bool rising) => !IsClinging && !rising && !IsLocked(face);

        public void Latch(Face face)
        {
            if (locked && !lockedFace.Equals(face)) locked = false;   // a different face clears the lock
            IsClinging = true;
            Current = face;
            MoveLock = 0;
        }

        /// <summary>Lets go of the face and locks it. Nothing if not clinging.</summary>
        public void Release()
        {
            if (!IsClinging) return;
            locked = true;
            lockedFace = Current;
            IsClinging = false;
        }

        /// <summary>The wall jump: lets go (the face is locked) and starts the move lock against that face's side.</summary>
        public void WallJump(int moveLockTicks)
        {
            int side = Current.Side;
            Release();
            MoveLock = moveLockTicks;
            MoveLockSide = side;
        }

        /// <summary>A grounded step: any cling ends, the face lock clears and the move lock stops.</summary>
        public void Landed()
        {
            IsClinging = false;
            locked = false;
            MoveLock = 0;
        }

        /// <summary>Gravity up: no cling (D-110 amendment (2)).</summary>
        public void GravityUp() => Release();

        /// <summary>A death, room reset or respawn: nothing clung or locked.</summary>
        public void Clear()
        {
            IsClinging = false;
            Current = default;
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
