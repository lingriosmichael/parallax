using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Core
{
    public enum StormCloudPhase { Dormant, Follow, Charge, Strike }

    /// <summary>PAX-088 (D-090): the storm cloud as pure functions. Ticks count from the wake W (the trap's fire tick):
    /// dormant through W, following from W+1, the first charge at W + firstStrikeDelay, then a charge every strikePeriod
    /// counted from the first; each charge is tellTicks (locked, harmless) then strikeTicks (lethal). A strike runs from the
    /// cloud's bottom down to the highest static top at or below it that overlaps any part of the column's width (ruling
    /// A). Positions are in one frame the caller chooses (the trap uses offsets from its authored pose).</summary>
    public static class StormCloudMath
    {
        public const float DefaultFollowSpeed = .08f, DefaultStrikeWidth = .8f;
        public const int DefaultFirstStrikeDelay = 50, DefaultStrikePeriod = 100, DefaultTellTicks = 25, DefaultStrikeTicks = 6;
        /// <summary>D-116: how fast a cloud with floors rises or sinks to the cat's floor, u per tick.</summary>
        public const float DefaultClimbSpeed = .2f;

        /// <summary>D-116: the floor a cat whose centre is at `catY` is on: the last of `floors` (x the lowest cat centre y of
        /// each, ascending) whose x is at or below catY; the first when none is. Each floor is (catMinY, cloudY, minX, maxX).</summary>
        public static Vector4 FloorFor(IReadOnlyList<Vector4> floors, float catY)
        {
            Vector4 floor = floors[0];
            for (int i = 1; i < floors.Count; i++) if (floors[i].x <= catY) floor = floors[i];
            return floor;
        }

        /// <summary>D-116: one follow tick with floors: x toward the cat's x clamped to the floor's range, y toward the floor's
        /// height, each by at most its speed (a cloud changing floors comes back into the new range as it moves).</summary>
        public static Vector2 FollowFloor(Vector2 cloud, Vector2 cat, Vector4 floor, float speed, float climbSpeed) =>
            new(Follow(cloud.x, Mathf.Clamp(cat.x, floor.z, floor.w), speed, float.NegativeInfinity, float.PositiveInfinity),
                Mathf.MoveTowards(cloud.y, floor.y, climbSpeed));

        /// <summary>One follow tick: toward the cat by at most `speed`, clamped to [minX, maxX].</summary>
        public static float Follow(float x, float catX, float speed, float minX, float maxX) =>
            Mathf.Clamp(x + Mathf.Clamp(catX - x, -speed, speed), minX, maxX);

        /// <summary>The phase `ticksSinceWake` ticks after the wake; negative = never woken.</summary>
        public static StormCloudPhase PhaseSince(int ticksSinceWake, int firstStrikeDelay, int strikePeriod, int tellTicks, int strikeTicks)
        {
            if (ticksSinceWake < 1) return StormCloudPhase.Dormant;
            if (ticksSinceWake < firstStrikeDelay || strikePeriod < 1) return StormCloudPhase.Follow;
            int inCycle = (ticksSinceWake - firstStrikeDelay) % strikePeriod;
            if (inCycle < tellTicks) return StormCloudPhase.Charge;
            return inCycle < tellTicks + strikeTicks ? StormCloudPhase.Strike : StormCloudPhase.Follow;
        }

        /// <summary>The strike's bottom under a column centred on x: the highest top at or below `cloudBottom` among the
        /// profile's (xMin, xMax, top) segments that strictly overlap [x - halfWidth, x + halfWidth]. False when none does.</summary>
        public static bool StrikeBottom(IReadOnlyList<Vector3> profile, float x, float halfWidth, float cloudBottom, out float bottom)
        {
            bottom = float.NegativeInfinity;
            bool found = false;
            if (profile == null) return false;
            for (int i = 0; i < profile.Count; i++)
            {
                Vector3 s = profile[i];
                if (s.z > cloudBottom || s.x >= x + halfWidth || s.y <= x - halfWidth) continue;
                if (!found || s.z > bottom) bottom = s.z;
                found = true;
            }
            return found;
        }

        public static Rect Column(float x, float halfWidth, float bottom, float cloudBottom) =>
            Rect.MinMaxRect(x - halfWidth, bottom, x + halfWidth, cloudBottom);

        /// <summary>Strict overlap: boxes that only touch don't hit.</summary>
        public static bool Hits(Rect column, Rect cat) =>
            column.xMin < cat.xMax && column.xMax > cat.xMin && column.yMin < cat.yMax && column.yMax > cat.yMin;

        /// <summary>The cat's collider box from its body (ruling C): the collider offset turned by the body's rotation (the root
        /// turns 180 degrees with gravity up, D-052), and the axis-aligned bounds of the turned box.</summary>
        public static Rect CatBox(Vector2 bodyPosition, float rotationDegrees, Vector2 colliderOffset, Vector2 colliderSize)
        {
            float r = rotationDegrees * Mathf.Deg2Rad, cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            Vector2 centre = bodyPosition + new Vector2(colliderOffset.x * cos - colliderOffset.y * sin, colliderOffset.x * sin + colliderOffset.y * cos);
            Vector2 size = new(Mathf.Abs(cos) * colliderSize.x + Mathf.Abs(sin) * colliderSize.y, Mathf.Abs(sin) * colliderSize.x + Mathf.Abs(cos) * colliderSize.y);
            return new Rect(centre - size * .5f, size);
        }

        /// <summary>The dodge rule's distance (§2.4): a cat at rest under the column's centre moves this far to be clear.</summary>
        public static float ClearDistance(float strikeWidth, float colliderWidth) => (strikeWidth + colliderWidth) * .5f;
    }
}
