using UnityEngine;

namespace Parallax.Core
{
    // PAX-093 (D-095): which edge of a shrinking floor moves in. Left: the left edge moves right (the right edge stays);
    // Right: the mirror; Both: both edges move in toward the centre.
    public enum ShrinkFrom { Left, Right, Both }

    // PAX-093 (D-095): the pure maths of floors that move: the carry, the push and the shrink.
    public static class MovingFloorMath
    {
        // What a Carry floor's move does to a cat standing on it (Q2): the part along the ground and the part away from it
        // (down, for gravity down) are carried; the part pushing into the cat is left to physics, as before (D-056 (3)'s
        // launch-on-stop). `down` is the cat's gravity direction.
        public static Vector2 Carry(Vector2 displacement, Vector2 down)
        {
            var right = new Vector2(-down.y, down.x);
            float along = Vector2.Dot(displacement, right), away = Vector2.Dot(displacement, down);
            return right * along + down * Mathf.Max(0f, away);
        }

        // Q6: the shift along x that puts a cat's trailing edge flush with a push wall's leading edge. `sign` is the push
        // direction (+1 right, -1 left); 0 when the wall's leading edge hasn't reached the cat.
        public static float PushOut(float wallLeadingEdge, float catTrailingEdge, int sign) =>
            sign > 0 ? Mathf.Max(0f, wallLeadingEdge - catTrailingEdge) : Mathf.Min(0f, wallLeadingEdge - catTrailingEdge);

        // Q5: a shrinking floor's width `ticks` after it fired, easing linearly from full to min over shrinkTicks.
        public static float ShrinkWidth(int ticks, int shrinkTicks, float fullWidth, float minWidth) =>
            Mathf.Lerp(fullWidth, Mathf.Clamp(minWidth, 0f, fullWidth), TrapMotion.Progress(Mathf.Max(0, ticks), shrinkTicks));

        // How far the shrunk floor's centre sits from the full floor's: the edge that stays keeps its place.
        public static float ShrinkCentreShift(ShrinkFrom from, float fullWidth, float width) =>
            from == ShrinkFrom.Left ? (fullWidth - width) * .5f : from == ShrinkFrom.Right ? -(fullWidth - width) * .5f : 0f;
    }
}
