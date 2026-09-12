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
    }
}
