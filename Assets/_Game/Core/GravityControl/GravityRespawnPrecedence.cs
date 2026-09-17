using UnityEngine;

namespace Parallax.Core
{
    public static class GravityRespawnPrecedence
    {
        public static Vector2 GravityOnRespawn(Vector2 checkpointDirection, Vector2 heldStreamDirection, bool hasHeldStreamValue)
        {
            return hasHeldStreamValue ? heldStreamDirection : checkpointDirection;
        }
    }
}
