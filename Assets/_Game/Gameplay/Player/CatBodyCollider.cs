using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    /// <summary>PAX-089 E.2: the cat's body is its first non-trigger Collider2D. The one lookup RoomTrap, StormCloudTrap and
    /// the route harness share, so they can't drift apart if the cat ever gets a second collider. Main thread only (a shared
    /// buffer, no per-call allocation).</summary>
    public static class CatBodyCollider
    {
        static readonly List<Collider2D> Buffer = new();

        public static Collider2D Of(Component cat)
        {
            if (cat == null) return null;
            cat.GetComponents(Buffer);
            Collider2D body = null;
            foreach (Collider2D collider in Buffer) if (!collider.isTrigger) { body = collider; break; }
            Buffer.Clear();
            return body;
        }
    }
}
