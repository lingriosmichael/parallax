using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: the inverter. An honest one draws the direction orb (TRAP-06, layer 05 glow; D-284: one art in every world) at its
    /// zone's centre; a disguised one has no orb (§11 R4). While the controls are swapped, its cue shows on the cat: a ring behind it and
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
        [SerializeField] float orbOffsetY;   // D-286: where the orb draws, from the zone's centre (TrapArtSetup.OrbOffset)
        [SerializeField] SpriteRenderer flare;

        public override RoomTrap Trap => trap;

        public override Vector2 GrowthOf(Body b)
        {
            if (b.Art != orb || orb == null || greyboxOrb == null) return BodyGrowth;
            return FlipArt.OrbGrowth(BodyGrowth, greyboxOrb.bounds.size, orbOffsetY);
        }

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
                // D-286: the same size as the gravity orb, in every zone, and it holds still as that one does.
                Fit(orb, orbSprite, new Vector2(FlipArt.RingSide, FlipArt.RingSide));
                orb.transform.position += Vector3.up * orbOffsetY;
            }
            if (Mirror(ring, greyboxRing)) Fit(ring, ringSprite, ringSize * bodyScale);
            if (Mirror(mark, greyboxMark)) Fit(mark, markSprite, markSize * bodyScale);
            int s = TicksSinceFire;
            bool on = orb != null && s >= 0 && s < TrapArtMath.InverterFlareTicks;
            Vector2 at = (greyboxOrb != null ? (Vector2)greyboxOrb.transform.position : (Vector2)transform.position) + Vector2.up * orbOffsetY;
            Place(flare, on, at, on ? side * (0.8f + 1.6f * s / TrapArtMath.InverterFlareTicks) : 1f, on ? 1f - s / (float)TrapArtMath.InverterFlareTicks : 0f);
        }
    }
}
