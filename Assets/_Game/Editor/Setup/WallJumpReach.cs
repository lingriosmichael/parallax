using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-105 (D-110): a wall jump's arc from the motor's numbers, pure, tick by tick as CatMotor2D integrates it (the wall
    // jump's launch on its own step, then the normal path with the move lock: Move back toward the wall ignored and the side
    // speed kept for WallJumpMoveLockTicks steps; physics moves the body by the velocity the step set). Gravity down.
    // Positions are the collider's from where it left the wall, in units: Y is its bottom's rise, XAway and XBack how far its
    // centre has moved away from the wall with the stick held away the whole time and held back toward the wall (after the
    // lock: the ordinary acceleration), the two ends of every input in between. The vertical motion doesn't depend on the
    // input, so at tick k the cat is at height Y[k], anywhere from XBack[k] to XAway[k]. Measured against the route harness by
    // WallJumpShortcutTests (the arc) and WallClingHarnessTests (the apex).
    public sealed class WallJumpReach
    {
        public const int MaxTicks = 400;
        public readonly float[] Y, XAway, XBack;
        public readonly int ApexTick;
        public readonly float Width, HalfWidth, Height, HalfHeight, MinFaceHeight;

        public WallJumpReach(CatMotorConfig m, float gravity, float dropLimit = 40f)
        {
            Width = m.ColliderSize.x; HalfWidth = Width * .5f; Height = m.ColliderSize.y; HalfHeight = Height * .5f; MinFaceHeight = m.MinClingFaceHeight;
            float dt = TickTime.SecondsPerTick, launch = JumpMath.SpeedForHeight(m.JumpHeight, gravity);
            var y = new System.Collections.Generic.List<float> { 0f };
            var away = new System.Collections.Generic.List<float> { 0f };
            var back = new System.Collections.Generic.List<float> { 0f };
            float vy = launch, py = 0f, va = m.WallJumpSideSpeed, xa = 0f, vb = m.WallJumpSideSpeed, xb = 0f;
            int apex = 0;
            for (int k = 1; k <= MaxTicks; k++)
            {
                if (k > 1)
                {
                    // The normal path's fall (velocity along gravity: -vy), capped at the max fall speed.
                    vy = -Mathf.Min(-vy + gravity * dt, m.MaxFallSpeed);
                    va = Mathf.MoveTowards(va, m.MaxSpeed, m.Acceleration * dt);                  // held away: toward +MaxSpeed
                    if (k - 1 > m.WallJumpMoveLockTicks) vb = Mathf.MoveTowards(vb, -m.MaxSpeed, m.Acceleration * dt);   // held back, after the lock
                }
                py += vy * dt; xa += va * dt; xb += vb * dt;
                y.Add(py); away.Add(xa); back.Add(xb);
                if (vy > 0f) apex = k;
                if (py < -dropLimit) break;
            }
            Y = y.ToArray(); XAway = away.ToArray(); XBack = back.ToArray();
            ApexTick = Mathf.Min(apex + 1, Y.Length - 1);
        }

        /// <summary>The wall jump's apex: its rise above the cling (units).</summary>
        public float Apex
        {
            get { float best = 0f; foreach (float h in Y) best = Mathf.Max(best, h); return best; }
        }

        /// <summary>The widest gap between two facing faces where a wall jump still latches the far one in latch mode (no new
        /// Grab): the leading edge's travel, held away, plus the collider's width, up to the first tick below the height it
        /// left from (the step that reads that tick's position still latches before latch mode ends).</summary>
        public float CrossingReach
        {
            get
            {
                float best = 0f;
                for (int k = 1; k < Y.Length; k++) if (Y[k - 1] >= 0f) best = Mathf.Max(best, XAway[k] + Width);
                return best;
            }
        }

        /// <summary>The latch height gained on the far face of a shaft `gap` wide (face to face), or NaN out of reach: the
        /// leading edge meets it at tick k; rising, the cat keeps rising against it and latches at the top of its rise.</summary>
        public float HeightPerHop(float gap)
        {
            for (int k = 1; k < Y.Length; k++)
            {
                if (XAway[k] + Width < gap - 1e-4f) continue;
                float h = k < ApexTick ? Y[ApexTick] : Y[k];
                return h >= 0f ? h : float.NaN;
            }
            return float.NaN;
        }

        /// <summary>How far above the cling the top of the cat's own wall can be for a wall jump (with the stick held back
        /// after the lock) to come back over it: the highest Y at which the collider's near edge is back past the face, plus
        /// CornerAllowance (measured in the route harness, WallClingHarnessTests: 1.08 u lands, 1.13 u doesn't).</summary>
        public float ReturnOntoOwnTop
        {
            get
            {
                float best = float.NegativeInfinity;
                for (int k = 1; k < Y.Length; k++) if (XBack[k] < 0f) best = Mathf.Max(best, Y[k]);
                return best + CornerAllowance;
            }
        }

        /// <summary>The capsule's rounded end catches a top's corner a little below it and rides up onto it: a landing counts
        /// from half the collider's height below the top (an over-estimate, so the reach model errs toward reach).</summary>
        public float CornerAllowance => HalfHeight;
    }
}
