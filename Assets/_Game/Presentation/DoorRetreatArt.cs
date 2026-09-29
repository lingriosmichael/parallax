using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: the door retreating. No door art of its own (the door is the environment's): stone-scrape dust at the
    /// door's base from the tick it starts to move until just after it stops.</summary>
    public sealed class DoorRetreatArt : TrapArt
    {
        [SerializeField] DoorRetreatTrap trap;
        [SerializeField] Transform door;
        [SerializeField] Vector2 doorSize;
        [SerializeField] int moveTicks;
        [SerializeField] SpriteRenderer[] dust = new SpriteRenderer[0];

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into) { }
        protected override void CollectEffects(List<Effect> into) { foreach (SpriteRenderer d in dust) into.Add(new Effect("scrape", d)); }

        protected override void Render()
        {
            int s = TicksSinceFire;
            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);
            Vector2 at = door != null ? (Vector2)door.position : (Vector2)transform.position;
            for (int i = 0; i < dust.Length; i++)
            {
                int t = s - i * 3;
                bool on = s >= 0 && s < moveTicks + TrapArtMath.DoorDustTicks && t >= 0;
                PuffPose p = on ? TrapArtMath.Puff(seed, 1000 + i, t % TrapArtMath.DoorDustTicks, TrapArtMath.DoorDustTicks, 0.4f) : default;
                Vector2 from = at + new Vector2((i % 2 == 0 ? -1f : 1f) * doorSize.x * 0.5f, -doorSize.y * 0.5f);
                Place(dust[i], p.Visible, from + p.Offset * 0.6f, 0.45f * p.Scale, 0.8f * p.Alpha);
            }
        }
    }
}
