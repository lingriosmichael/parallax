using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A20: an Obsidian Void laser. A lethal band (a static Hazard, a pit's floor, or a MovingTrap hazard: a
    /// flood, a purge, a descending beam) draws as a crimson beam (OT-05) tiled along its grey-box's long side, exactly its
    /// length and BodyScale thick, following the grey-box as it moves. The beam's tile is one unit tall, so it is scaled to
    /// the band's thickness and tiled `length / thickness` tiles long. A scanner (a mover with a harmless warning) draws dim
    /// until it fires and flickers through its warning, so a beam that can't kill yet doesn't look like one that can (the
    /// developer, 2026-10-31: "am I running through a laser"). Every other band shimmers a little on the room clock.
    /// A pulse gate (PAX-112) is a vertical beam between two emitters (OT-16): the beam follows the gate's own grey-box,
    /// shown in its warning and on phases at the grey-box's alpha relative to its full alpha, so it flickers dim exactly as
    /// the gate warns; the emitters are fittings, drawn always, at the gate's floor and ceiling ends.
    /// D-294 (the developer: "a rendering glitch"): every beam has a soft glow around it, tinted to the beam, drawn over whatever's
    /// behind it, so it reads on a bright sky and on black stone. A line from a source also has a flare at its source and a burn
    /// where it lands, and a thin one (aiming) draws as a hot core, not a shrunken beam.</summary>
    public sealed class LaserArt : TrapArt
    {
        /// <summary>A scanner's alpha before it fires, and the two alphas its warning flickers between (every FlickerTicks).</summary>
        public const float IdleAlpha = 0.28f, WarnLow = 0.35f, WarnHigh = 0.95f;
        public const int FlickerTicks = 3;

        [SerializeField] RoomTrap trap;                 // a MovingTrap hazard, or null for a static Hazard
        [SerializeField] SpriteRenderer body, greyboxBody;
        [SerializeField] Sprite beam;
        [Tooltip("The scanner's harmless warning from each fire (MovingTrap.warnTicks); 0 = not a scanner.")]
        [SerializeField, Min(0)] int warnTicks;
        [Tooltip("A pulse gate's emitters (fittings, always drawn) at its top and bottom ends; null for any other laser.")]
        [SerializeField] SpriteRenderer emitterTop, emitterBottom;
        [Tooltip("A pulse gate's grey-box alpha when on: its beam draws at the grey-box's alpha over this (the warning's flicker). 0 = not a gate.")]
        [SerializeField, Min(0f)] float gateAlpha;
        [Tooltip("D-294: the soft glow around the beam, the burn at its far end and the flare at its source; null for none.")]
        [SerializeField] SpriteRenderer glow, burn, flare;
        [Tooltip("D-294: what a thin line (narrower than ThinLine, aiming) draws instead of the beam; null to always draw the beam.")]
        [SerializeField] Sprite thinBeam;

        /// <summary>D-294: a line narrower than this is aiming; it draws at least ThinDrawn wide, so a phone shows it.</summary>
        const float ThinLine = 0.15f, ThinDrawn = 0.09f;
        /// <summary>D-294: the glow's width (over the beam's, and at least GlowMin), and how far its strength follows the beam's.</summary>
        const float GlowWide = 3.2f, GlowMin = 0.42f, GlowStrength = 0.6f;
        /// <summary>D-294: the burn and the flare, over the beam's width and at least EndMin.</summary>
        const float EndWide = 2.6f, EndMin = 0.42f;

        /// <summary>How far an emitter stands out of the stone it's set in, as a fraction of its own height.</summary>
        public const float EmitterOut = 0.8f;

        public override RoomTrap Trap => trap;
        protected override bool NeedsTrap => false;
        public override Vector2 GrowthOf(Body b) => thinBeam != null ? new Vector2(1f, 1f) : BodyGrowth;   // a thin line's core is drawn wider, at any angle

        /// <summary>The band runs up and down (a purge), so the beam turns a quarter.</summary>
        bool Vertical => greyboxBody != null && greyboxBody.size.y > greyboxBody.size.x;

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into)
        {
            if (glow != null) into.Add(new Effect("beam", glow));
            if (burn != null) into.Add(new Effect("beam", burn));
            if (flare != null) into.Add(new Effect("beam", flare));
        }

        protected override void Render()
        {
            PlaceEmitters();
            if (!Mirror(body, greyboxBody))
            {
                if (glow != null) glow.enabled = false;
                if (burn != null) burn.enabled = false;
                if (flare != null) flare.enabled = false;
                return;
            }
            Vector2 size = greyboxBody.size;
            bool thin = thinBeam != null && Mathf.Min(size.x, size.y) < ThinLine;
            Sprite drawn = thin ? thinBeam : beam;
            if (body.sprite != drawn) body.sprite = drawn;
            float length = Vertical ? size.y : size.x, thick = Mathf.Max(1e-3f, (Vertical ? size.x : size.y) * bodyScale);
            if (thin) thick = ThinDrawn;
            body.drawMode = SpriteDrawMode.Tiled;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
            body.tileMode = SpriteTileMode.Continuous;
            body.size = new Vector2(length / thick, 1f);
            body.transform.localScale = new Vector3(thick, thick, 1f);
            body.transform.rotation = greyboxBody.transform.rotation * Quaternion.Euler(0f, 0f, Vertical ? 90f : 0f);
            float alpha = gateAlpha > 0f ? Mathf.Clamp01(greyboxBody.color.a / gateAlpha) : Alpha(TicksSinceFire);
            Color c = body.color; c.a = thin ? Mathf.Lerp(0.65f, 1f, alpha) : alpha; body.color = c;
            Dress(length, thick, alpha);
        }

        /// <summary>D-294: the glow along the beam, the burn at its far end and the flare at its source, moving by the room clock only.</summary>
        void Dress(float length, float thick, float alpha)
        {
            Quaternion turn = body.transform.rotation;
            Vector3 centre = body.transform.position;
            if (glow != null)
            {
                float wide = Mathf.Max(thick * GlowWide, GlowMin);
                glow.enabled = true;
                glow.drawMode = SpriteDrawMode.Tiled;
                glow.tileMode = SpriteTileMode.Continuous;
                glow.size = new Vector2(length / wide, 1f);
                glow.transform.localScale = new Vector3(wide, wide, 1f);
                glow.transform.SetPositionAndRotation(new Vector3(centre.x, centre.y, glow.transform.position.z), turn);
                Color g = glow.color; g.a = alpha * GlowStrength; glow.color = g;
            }
            float end = Mathf.Max(thick * EndWide, EndMin), flicker = 1f + 0.14f * Mathf.Sin(RoomTick * 1.9f);
            Vector2 half = turn * new Vector3(length * 0.5f, 0f, 0f);
            Place(burn, true, (Vector2)centre + half, end * flicker, Mathf.Lerp(0.55f, 1f, alpha));
            Place(flare, true, (Vector2)centre - half, end * 0.8f * (2f - flicker), Mathf.Lerp(0.45f, 1f, alpha));
            if (burn != null) burn.transform.rotation = Quaternion.Euler(0f, 0f, RoomTick * 47f);
            if (flare != null) flare.transform.rotation = Quaternion.Euler(0f, 0f, RoomTick * -31f);
        }

        /// <summary>The emitters at the grey-box's top and bottom, each EmitterOut of its height out into the room.</summary>
        void PlaceEmitters()
        {
            if (greyboxBody == null) return;
            // From the grey-box's transform and size (a disabled renderer's bounds are empty, and the gate is off between pulses).
            Vector2 at = greyboxBody.transform.position;
            float half = greyboxBody.size.y * Mathf.Abs(greyboxBody.transform.lossyScale.y) * 0.5f;
            Place(emitterTop, emitterTop != null && emitterTop.sprite != null, new Vector2(at.x, at.y + half - Half(emitterTop) * (2f * EmitterOut - 1f)), 1f, 1f);
            Place(emitterBottom, emitterBottom != null && emitterBottom.sprite != null, new Vector2(at.x, at.y - half + Half(emitterBottom) * (2f * EmitterOut - 1f)), 1f, 1f);
        }

        static float Half(SpriteRenderer r) => r != null && r.sprite != null ? r.sprite.bounds.extents.y : 0f;

        /// <summary>The beam's opacity this tick: a pure function of the trap's ticks since fire and the room clock.</summary>
        float Alpha(int sinceFire)
        {
            if (warnTicks > 0)
            {
                if (sinceFire < 0) return IdleAlpha;
                if (sinceFire < warnTicks) return (sinceFire / FlickerTicks) % 2 == 0 ? WarnHigh : WarnLow;
            }
            return 0.88f + 0.12f * Mathf.Sin(RoomTick * 0.55f);
        }
    }
}
