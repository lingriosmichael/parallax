using UnityEngine;

namespace Parallax.Core.Cameras
{
    public static class CameraMath
    {
        public static Vector2 ResolveDeadZone(Vector2 currentCentre, Vector2 target, Vector2 halfExtents)
        {
            Vector2 result = currentCentre;

            float dx = target.x - currentCentre.x;
            if (dx > halfExtents.x) result.x = target.x - halfExtents.x;
            else if (dx < -halfExtents.x) result.x = target.x + halfExtents.x;

            float dy = target.y - currentCentre.y;
            if (dy > halfExtents.y) result.y = target.y - halfExtents.y;
            else if (dy < -halfExtents.y) result.y = target.y + halfExtents.y;

            return result;
        }

        public static Vector2 ClampToBounds(Vector2 centre, Vector2 halfView, Vector2 boundsMin, Vector2 boundsMax)
        {
            Vector2 result = centre;

            float widthX = boundsMax.x - boundsMin.x;
            result.x = widthX < halfView.x * 2f
                ? (boundsMin.x + boundsMax.x) * 0.5f
                : Mathf.Clamp(centre.x, boundsMin.x + halfView.x, boundsMax.x - halfView.x);

            float widthY = boundsMax.y - boundsMin.y;
            result.y = widthY < halfView.y * 2f
                ? (boundsMin.y + boundsMax.y) * 0.5f
                : Mathf.Clamp(centre.y, boundsMin.y + halfView.y, boundsMax.y - halfView.y);

            return result;
        }

        // PAX-052 (D-071): the level camera's fit/follow decision and sizing. frameSize is the
        // room's content bounds plus ViewMargin (baked per level at regeneration time, never a
        // runtime layout read, D-066). Fit mode requires the whole frame - width AND height - to
        // fit in a view no taller than maxViewHeight at the current aspect; follow mode's view
        // height then only depends on the frame's own height, since horizontal coverage comes
        // from panning, not from sizing wider.
        public static float RequiredFitViewHeight(Vector2 frameSize, float aspect)
        {
            float safeAspect = Mathf.Max(aspect, 0.0001f);
            return Mathf.Max(frameSize.y, frameSize.x / safeAspect);
        }

        public static bool IsFitMode(Vector2 frameSize, float maxViewHeight, float aspect) =>
            RequiredFitViewHeight(frameSize, aspect) <= maxViewHeight;

        public static float FollowViewHeight(Vector2 frameSize, float maxViewHeight) =>
            Mathf.Min(frameSize.y, maxViewHeight);

        public static float ResolveViewHeight(Vector2 frameSize, float maxViewHeight, float aspect) =>
            IsFitMode(frameSize, maxViewHeight, aspect)
                ? RequiredFitViewHeight(frameSize, aspect)
                : FollowViewHeight(frameSize, maxViewHeight);

        // direction > 0 shifts the dead-zone target right by lookAhead, < 0 shifts it left, 0
        // leaves it unchanged (the caller holds the last non-zero direction across frames so the
        // shift doesn't snap back to centre the instant the cat stops).
        public static Vector2 ApplyLookAhead(Vector2 target, float direction, float lookAhead)
        {
            if (direction > 0f) target.x += lookAhead;
            else if (direction < 0f) target.x -= lookAhead;
            return target;
        }

        // The look-ahead direction with hysteresis. The direction changes only once the target has
        // moved more than flipDistance from anchorX, the point where the direction last changed (or the
        // furthest point reached since, in the current direction). A cat at rest, whose interpolated
        // position can still differ by a float step between frames, never flips it. Frame-rate
        // independent: it measures distance travelled, not per-frame deltas.
        public static float ResolveLookDirection(float targetX, ref float anchorX, float lastDirection, float flipDistance)
        {
            float delta = targetX - anchorX;
            if (delta > flipDistance) { anchorX = targetX; return 1f; }
            if (delta < -flipDistance) { anchorX = targetX; return -1f; }
            if ((lastDirection > 0f && delta > 0f) || (lastDirection < 0f && delta < 0f)) anchorX = targetX;
            return lastDirection;
        }

        // Composes dead zone -> look-ahead -> (optional) vertical lock -> bounds clamp into the
        // one follow-mode position the level camera resolves every step. verticalFollow is false
        // whenever the room isn't taller than the current view (§2.2.2): the camera then stays
        // locked to the frame's vertical centre instead of tracking the cat's height.
        public static Vector2 ResolveFollowCentre(Vector2 currentCentre, Vector2 target, float direction,
            Vector2 deadZoneHalfExtents, float lookAhead, Vector2 halfView, Vector2 frameCentre,
            Vector2 frameMin, Vector2 frameMax, bool verticalFollow)
        {
            Vector2 desired = ResolveDeadZone(currentCentre, target, deadZoneHalfExtents);
            desired = ApplyLookAhead(desired, direction, lookAhead);
            if (!verticalFollow) desired.y = frameCentre.y;
            return ClampToBounds(desired, halfView, frameMin, frameMax);
        }

        // PAX-076 (D-083): LevelCameraFollow's whole per-step state and tunables, so the level camera and the
        // validator's camera tell rule run the same step.
        public struct FollowState
        {
            public Vector2 Centre, Velocity;
            public float AnchorX, LastDirection;
        }

        public readonly struct FollowParams
        {
            public readonly float MaxViewHeight, LookAhead, LookAheadFlipDistance, SmoothTime, MaxSpeed;
            public readonly Vector2 DeadZoneHalfExtents;
            public FollowParams(float maxViewHeight, float lookAhead, float lookAheadFlipDistance, Vector2 deadZoneHalfExtents, float smoothTime, float maxSpeed)
            { MaxViewHeight = maxViewHeight; LookAhead = lookAhead; LookAheadFlipDistance = lookAheadFlipDistance; DeadZoneHalfExtents = deadZoneHalfExtents; SmoothTime = smoothTime; MaxSpeed = maxSpeed; }
        }

        // PAX-076 (D-083): one level-camera step, moved verbatim out of LevelCameraFollow.Resolve (after D-084). Fit
        // mode centres on the frame; follow mode resolves the look direction, the dead zone, the look-ahead and the
        // clamp. immediate (a snap) skips SmoothDamp and leaves Velocity alone. Returns the view height.
        public static float Step(ref FollowState state, Vector2 target, Vector2 frameCentre, Vector2 frameSize, float aspect, in FollowParams p, bool immediate, float deltaTime)
        {
            float viewHeight = ResolveViewHeight(frameSize, p.MaxViewHeight, aspect);
            Vector2 halfView = new Vector2(viewHeight * 0.5f * aspect, viewHeight * 0.5f);
            Vector2 frameMin = frameCentre - frameSize * 0.5f;
            Vector2 frameMax = frameCentre + frameSize * 0.5f;

            Vector2 desired;
            if (IsFitMode(frameSize, p.MaxViewHeight, aspect))
            {
                desired = frameCentre;
            }
            else
            {
                float direction = ResolveLookDirection(target.x, ref state.AnchorX, state.LastDirection, p.LookAheadFlipDistance);
                state.LastDirection = direction;
                bool verticalFollow = frameSize.y > viewHeight + 0.001f;

                desired = ResolveFollowCentre(state.Centre, target, direction,
                    p.DeadZoneHalfExtents, p.LookAhead, halfView, frameCentre, frameMin, frameMax, verticalFollow);
            }

            state.Centre = immediate ? desired : Vector2.SmoothDamp(state.Centre, desired, ref state.Velocity, p.SmoothTime, p.MaxSpeed, deltaTime);
            return viewHeight;
        }
    }
}
