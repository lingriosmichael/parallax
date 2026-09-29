using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: the inverter. An honest one draws a warm gold orb (TRAP-06, layer 05 glow; §11 R6: no cyan) inside its
    /// zone; a disguised one has no orb (§11 R4). While the controls are swapped, its cue shows on the cat: a ring behind it and
    /// a mark over it (TRAP-07), drawn exactly where and when the grey-box cue is, the 30-tick blink included. On the fire tick
    /// the orb flares.</summary>
    // PAX-096: after InverterTrap (default order), which moves the grey-box cue onto the cat in its own LateUpdate; the
    // art mirrors that cue, so it must see this frame's position, not the last one.
    [DefaultExecutionOrder(100)]
    public sealed class InverterArt : TrapArt
    {
        [SerializeField] InverterTrap trap;
        [SerializeField] SpriteRenderer orb, greyboxOrb;
        [SerializeField] SpriteRenderer ring, greyboxRing, mark, greyboxMark;
        [SerializeField] Sprite orbSprite, ringSprite, markSprite;
        [SerializeField] Vector2 zoneSize, ringSize, markSize;
        [SerializeField] SpriteRenderer flare;

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into)
        {
            if (orb != null) into.Add(new Body(orb, greyboxOrb));
            into.Add(new Body(ring, greyboxRing));
            into.Add(new Body(mark, greyboxMark));
        }

        protected override void CollectEffects(List<Effect> into) => into.Add(new Effect("flare", flare));

        protected override void Render()
        {
            float side = Mathf.Min(zoneSize.x, zoneSize.y);
            if (orb != null && Mirror(orb, greyboxOrb))
            {
                Fit(orb, orbSprite, new Vector2(side, side));
                float breathe = 0.92f + 0.08f * Mathf.Sin(RoomTick * 0.08f);
                orb.transform.localScale *= breathe;
            }
            if (Mirror(ring, greyboxRing)) Fit(ring, ringSprite, ringSize);
            if (Mirror(mark, greyboxMark)) Fit(mark, markSprite, markSize);
            int s = TicksSinceFire;
            bool on = orb != null && s >= 0 && s < TrapArtMath.InverterFlareTicks;
            Vector2 at = greyboxOrb != null ? (Vector2)greyboxOrb.transform.position : (Vector2)transform.position;
            Place(flare, on, at, on ? side * (0.8f + 1.6f * s / TrapArtMath.InverterFlareTicks) : 1f, on ? 1f - s / (float)TrapArtMath.InverterFlareTicks : 0f);
        }
    }
}
