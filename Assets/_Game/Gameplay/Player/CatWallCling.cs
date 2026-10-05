using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    /// <summary>PAX-105 (D-110 and its amendment): the motor's wall branch, a plain class CatMotor2D owns (not a component,
    /// so the cat prefab is unchanged), like CatClimber. Without a Grab press remembered, latch mode, a cling or a move lock
    /// (WallClingState.Idle), Step casts nothing and returns false having touched no body, so the motor's normal path runs
    /// exactly as before. A face is a grip wall's collider (it carries GripSurface, D-110 amendment 2: every other surface
    /// ignores Grab), solid and in the cat's own reality (the ground cast's filter), with a wall normal and at least
    /// MinClingFaceHeight tall. A face whose
    /// collider's bounds change while clung (it moves, resizes or is switched off) lets go; a collider with a body (a falling
    /// block, a moving trap) latches only when its bounds match the previous step's.</summary>
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

        public CatWallCling(Rigidbody2D body, Collider2D collider, CatMotorConfig config, ContactFilter2D filter)
        {
            this.body = body; this.collider = collider; this.config = config; this.filter = filter;
        }

        public bool IsClinging => state.IsClinging;
        /// <summary>+1 clinging to a wall on the right, -1 on the left, 0 when not clinging.</summary>
        public int ClingSide => state.IsClinging ? state.Current.Side : 0;
        public bool LatchMode => state.LatchMode;
        public bool WallJumpedThisStep { get; private set; }
        public Collider2D Face => state.IsClinging ? face : null;
        /// <summary>The motor's normal path, once per step: true while the move lock after a wall jump lasts (one of its
        /// steps used); then LockedMove applies and, with no Move left, the sideways speed is kept.</summary>
        public bool TakeMoveLockStep() => state.ConsumeMoveLock();

        /// <summary>During the move lock, Move toward the face just left reads 0.</summary>
        public float LockedMove(float move) => WallClingState.LockedMove(move, state.MoveLockSide);

        /// <summary>A launch, a push, a moving face or a vine grab: lets go (the face is locked) and ends latch mode.</summary>
        public void Release() => state.Release(endLatchMode: true);

        /// <summary>A death, room reset or respawn: nothing clung, remembered or locked.</summary>
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
            state.BeginStep(command.GrabPressed, config.GrabBufferTicks);
            try
            {
                // Landing and gravity up only change the rules' own state (the face lock clears on any grounded step).
                if (collider == null) return false;
                if (grounded) { state.Landed(); seen.Clear(); return false; }
                if (down.y > 0f) { state.GravityUp(); seen.Clear(); return false; }
                if (state.Idle) { seen.Clear(); return false; }

                float fall = Vector2.Dot(body.linearVelocity, down);
                if (state.IsClinging)
                {
                    if (!StillOnFace()) { state.Release(endLatchMode: true); return false; }
                    if (command.JumpPressed) return WallJump(jumpSpeed);
                    if (WallClingState.PushesAway(command.Move, state.Current.Side, config.WallReleaseThreshold))
                    { state.Release(endLatchMode: true); return false; }
                    Slide(fall, gravity, dt);
                    return true;
                }

                if (!state.WantsLatch) { seen.Clear(); return false; }
                bool rising = fall < 0f;
                if (!rising && FindFace(out RaycastHit2D hit, out WallClingState.Face found))
                {
                    // x snaps onto the face on the latch step, as a vine grab snaps to the vine's centre.
                    body.position += new Vector2(found.Side * hit.distance, 0f);
                    face = hit.collider;
                    faceBounds = face.bounds;
                    state.Latch(found);
                    if (command.JumpPressed || buffered) return WallJump(jumpSpeed);
                    Slide(fall, gravity, dt);
                    return true;
                }
                state.AirStep(collider.bounds.center.y, fall > 0f);
                return false;
            }
            finally { state.EndStep(); }
        }

        void Slide(float fall, float gravity, float dt) =>
            body.linearVelocity = new Vector2(0f, -WallClingState.SlideFall(Mathf.Max(0f, fall), gravity, dt, config.WallSlideSpeed));

        bool WallJump(float jumpSpeed)
        {
            body.linearVelocity = WallClingState.WallJumpVelocity(state.Current.Side, config.WallJumpSideSpeed, jumpSpeed);
            state.WallJump(collider.bounds.center.y, config.WallJumpMoveLockTicks);
            face = null;
            WallJumpedThisStep = true;
            return true;
        }

        // The clung face is still beside the cat, still a wall, and hasn't moved, resized or been switched off.
        bool StillOnFace()
        {
            if (face == null || !face.enabled || !face.gameObject.activeInHierarchy || face.bounds != faceBounds) return false;
            int side = state.Current.Side;
            int count = body.Cast(new Vector2(side, 0f), filter, hits, config.WallProbeDistance);
            for (int i = 0; i < count; i++)
                if (hits[i].collider == face && WallClingState.IsWallNormal(hits[i].normal, config.WallNormalThreshold) && WallClingState.SideOf(hits[i].normal) == side)
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
                int count = body.Cast(new Vector2(side, 0f), filter, hits, config.WallProbeDistance);
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = hits[i];
                    Collider2D c = hit.collider;
                    if (c == null || !WallClingState.IsWallNormal(hit.normal, config.WallNormalThreshold) || WallClingState.SideOf(hit.normal) != side) continue;
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
    }
}
