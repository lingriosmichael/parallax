using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: a gravity flip. A visible flip draws its glyph ring (TRAP-08) inside its zone, with a few warm motes
    /// drifting inside the zone as part of its body (so they show exactly when the zone does, and never past it); a hidden flip
    /// draws nothing (§11 R4). On every fire, a ring pulses out from the zone's centre (a re-arming flip's
    /// pulse ends early if it re-arms).</summary>
    public sealed class FlipArt : TrapArt
    {
        [SerializeField] GravityFlipTrap trap;
        [SerializeField] SpriteRenderer ring, greyboxZone;
        [SerializeField] SpriteRenderer[] motes = new SpriteRenderer[0];
        [SerializeField] Sprite glyphRing;
        [SerializeField] SpriteRenderer pulse;
        [SerializeField] Vector2 zoneSize;

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into)
        {
            if (ring != null) into.Add(new Body(ring, greyboxZone));
            foreach (SpriteRenderer m in motes) into.Add(new Body(m, greyboxZone));
        }

        protected override void CollectEffects(List<Effect> into) => into.Add(new Effect("pulse", pulse));

        protected override void Render()
        {
            Vector2 centre = greyboxZone != null ? (Vector2)greyboxZone.transform.position : (Vector2)transform.position;
            float side = Mathf.Min(zoneSize.x, zoneSize.y) * 0.9f;
            if (ring != null && Mirror(ring, greyboxZone)) Fit(ring, glyphRing, new Vector2(side, side));
            // Motes: slow drift inside the zone, on render ticks (ambient, D-094 allows); a pure function of the room clock.
            for (int i = 0; i < motes.Length; i++)
            {
                if (!Mirror(motes[i], greyboxZone)) continue;
                float t = RoomTick * 0.02f + i * 1.7f;
                var local = new Vector2(Mathf.Sin(t * 1.3f + i) * zoneSize.x * 0.35f, Mathf.Repeat(t * 0.35f + i * 0.31f, 1f) * zoneSize.y * 0.8f - zoneSize.y * 0.4f);
                motes[i].transform.position = new Vector3(centre.x + local.x, centre.y + local.y, motes[i].transform.position.z);
                motes[i].transform.localScale = Vector3.one * 0.9f;
                Color c = motes[i].color; c.a = 0.35f + 0.35f * Mathf.Sin(t * 2f); motes[i].color = c;
            }
            // A re-arming flip's fire comes from its own countdown; the pulse is cut short when it re-arms.
            int s = trap != null && trap.RearmTicksSinceFire >= 0 ? trap.RearmTicksSinceFire : TicksSinceFire;
            bool on = s >= 0 && s < TrapArtMath.FlipPulseTicks;
            Place(pulse, on, centre, on ? side * (0.6f + 1.2f * s / TrapArtMath.FlipPulseTicks) : 1f, on ? 1f - s / (float)TrapArtMath.FlipPulseTicks : 0f);
        }
    }
}
