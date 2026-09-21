using UnityEngine;

namespace Parallax.Core
{
    public enum GravityFlipMode { Flip, ForceUp, ForceDown }

    public static class GravityFlipRule
    {
        public static Vector2 Resolve(GravityFlipMode mode, Vector2 current)
        {
            GravitySide side = VerticalGravity.SideOf(current, GravitySide.Down);
            return mode == GravityFlipMode.Flip ? VerticalGravity.ToVector(VerticalGravity.Flip(side))
                : mode == GravityFlipMode.ForceUp ? Vector2.up : Vector2.down;
        }
    }
}
