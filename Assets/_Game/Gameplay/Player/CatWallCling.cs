using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    /// <summary>PAX-105 (D-110 and its amendments): the motor's wall branch, a plain class CatMotor2D owns (not a component,
    /// so the cat prefab is unchanged), like CatClimber. PAX-106 (D-110 amendment 4): the latch is automatic; an airborne,
    /// not-rising, gravity-down step casts for a face (read-only queries) and latches it, and with no face found Step returns
    /// false having touched no body, so the motor's normal path runs exactly as before. A face is a grip wall's collider (it
    /// carries GripSurface, D-110 amendment 2: no other surface can be held), solid and in the cat's own reality (the ground
    /// cast's filter), with a wall normal and at least MinClingFaceHeight tall. A face whose
    /// collider's bounds change while clung (it moves, resizes or is switched off) lets go; a collider with a body (a falling
    /// block, a moving trap) latches only when its bounds match the previous step's.
    /// D-118 (amends D-110 (2)): it works with gravity up too, in the cat's frame: faces are found along the motor's right,
    /// the slide runs along gravity and the wall jump launches against it. WallClingState's sides are in that frame (Move
    /// is); ClingSide reports the world side. A gravity change while clinging lets go.</summary>
    public sealed class CatWallCling
    {
        readonly Rigidbody2D body;
        readonly Collider2D collider;
        readonly CatMotorConfig config;
        readonly WallClingState state = new();
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        Dictionary<int, Bounds> seen = new(), seenNow = new();
        ContactFilter2D filter;
        Collider2D face;
        Bounds faceBounds;
        Vector2 down = Vector2.down, right = Vector2.right;   // D-118: this step's frame (the latch's, while clinging)

        public CatWallCling(Rigidbody2D body, Collider2D collider, CatMotorConfig config, ContactFilter2D filter)
        {
            this.body = body; this.collider = collider; this.config = config; this.filter = filter;
        }

        public bool IsClinging => state.IsClinging;
        /// <summary>+1 clinging to a wall on the right (in the world), -1 on the left, 0 when not clinging.</summary>
        public int ClingSide => state.IsClinging ? state.Current.Side * (right.x < 0f ? -1 : 1) : 0;
        public bool WallJumpedThisStep { get; private set; }
        public Collider2D Face => state.IsClinging ? face : null;
        /// <summary>The motor's normal path, once per step: true while the move lock after a wall jump lasts (one of its
        /// steps used); then LockedMove applies and, with no Move left, the sideways speed is kept.</summary>
        public bool TakeMoveLockStep() => state.ConsumeMoveLock();

        /// <summary>During the move lock, Move toward the face just left reads 0.</summary>
        public float LockedMove(float move) => WallClingState.LockedMove(move, state.MoveLockSide);

        /// <summary>A launch, a push, a moving face or a vine grab: lets go (the face is locked).</summary>
        public void Release() => state.Release();

        /// <summary>A death, room reset or respawn: nothing clung or locked.</summary>
        public void Clear()
        {
            state.Clear();
            seen.Clear();
            face = null;
        }

        /// <summary>One motor step, after the ground test and the climber. True: this step is the wall's (a cling, or a wall
        /// jump) and the body's velocity is set; false: the motor's normal path runs (also on the step a release happens, so
        /// the cat falls from the velocity it had). `buffered`: the motor's jump buffer is open, which wall-jumps on the latch
        /// step only (while clinging, a press wall-jumps at once).</summary>
        public bool Step(in CatCommand command, Vector2 down, bool grounded, bool buffered, float jumpSpeed, float gravity, float dt)
        {
            WallJumpedThisStep = false;
            // Landing only changes the rules' own state (the face lock clears on any grounded step).
            if (collider == null) return false;
            if (grounded) { state.Landed(); seen.Clear(); SetFrame(down); return false; }
            // D-118: a gravity change while clinging lets go; the new frame is this step's.
            if (down != this.down) { state.Release(); seen.Clear(); SetFrame(down); return false; }

            float fall = Vector2.Dot(body.linearVelocity, down);
            if (state.IsClinging)
            {
                if (!StillOnFace()) { state.Release(); return false; }
                if (command.JumpPressed) return WallJump(jumpSpeed);
                if (WallClingState.PushesAway(command.Move, state.Current.Side, config.WallReleaseThreshold))
                { state.Release(); return false; }
                Slide(fall, gravity, dt);
                return true;
            }

            bool rising = fall < 0f;
            if (rising) { seen.Clear(); return false; }   // a body's still check wants two probes in a row
            if (FindFace(out RaycastHit2D hit, out WallClingState.Face found))
            {
                // x snaps onto the face on the latch step, as a vine grab snaps to the vine's centre.
                body.position += right * (found.Side * hit.distance);
                face = hit.collider;
                faceBounds = face.bounds;
                state.Latch(found);
                if (command.JumpPressed || buffered) return WallJump(jumpSpeed);
                Slide(fall, gravity, dt);
                return true;
            }
            return false;
        }

        void SetFrame(Vector2 d) { down = d; right = new Vector2(-d.y, d.x); }

        void Slide(float fall, float gravity, float dt) =>
            body.linearVelocity = down * WallClingState.SlideFall(Mathf.Max(0f, fall), gravity, dt, config.WallSlideSpeed);

        bool WallJump(float jumpSpeed)
        {
            // In the cat's frame (x along right, y against gravity); gravity down, the world's.
            Vector2 v = WallClingState.WallJumpVelocity(state.Current.Side, config.WallJumpSideSpeed, jumpSpeed);
            body.linearVelocity = right * v.x - down * v.y;
            state.WallJump(config.WallJumpMoveLockTicks);
            face = null;
            WallJumpedThisStep = true;
            return true;
        }

        // The clung face is still beside the cat, still a wall, and hasn't moved, resized or been switched off.
        bool StillOnFace()
        {
            if (face == null || !face.enabled || !face.gameObject.activeInHierarchy || face.bounds != faceBounds) return false;
            int side = state.Current.Side;
            int count = body.Cast(right * side, filter, hits, config.WallProbeDistance);
            for (int i = 0; i < count; i++)
                if (hits[i].collider == face && WallClingState.IsWallNormal(hits[i].normal, config.WallNormalThreshold) && FrameSide(hits[i].normal) == side)
                    return true;
            return false;
        }

        // The nearest latchable face either side (the right one on a tie). A collider with a body counts only when its bounds
        // match the previous step's (the one-tick still check).
        bool FindFace(out RaycastHit2D best, out WallClingState.Face bestFace)
        {
            best = default; bestFace = default;
            bool found = false;
            seenNow.Clear();
            for (int side = 1; side >= -1; side -= 2)
            {
                int count = body.Cast(right * side, filter, hits, config.WallProbeDistance);
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = hits[i];
                    Collider2D c = hit.collider;
                    if (c == null || !WallClingState.IsWallNormal(hit.normal, config.WallNormalThreshold) || FrameSide(hit.normal) != side) continue;
                    if (c.bounds.size.y < config.MinClingFaceHeight || !c.TryGetComponent(out GripSurface _)) continue;
                    var candidate = new WallClingState.Face(c.GetInstanceID(), side);
                    if (!state.CanLatch(candidate, rising: false)) continue;
                    if (c.attachedRigidbody != null)
                    {
                        seenNow[candidate.Collider] = c.bounds;
                        if (!seen.TryGetValue(candidate.Collider, out Bounds before) || before != c.bounds) continue;
                    }
                    if (found && hit.distance >= best.distance) continue;
                    best = hit; bestFace = candidate; found = true;
                }
            }
            (seen, seenNow) = (seenNow, seen);
            return found;
        }

        // D-118: the side of the cat a face is on, in its frame (the world's with gravity down).
        int FrameSide(Vector2 normal) => WallClingState.SideOf(normal) * (right.x < 0f ? -1 : 1);
    }
}
