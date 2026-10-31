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
    /// the gate warns; the emitters are fittings, drawn always, at the gate's floor and ceiling ends.</summary>
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

        /// <summary>How far an emitter stands out of the stone it's set in, as a fraction of its own height.</summary>
        public const float EmitterOut = 0.8f;

        public override RoomTrap Trap => trap;
        protected override bool NeedsTrap => false;

        /// <summary>The band runs up and down (a purge), so the beam turns a quarter.</summary>
        bool Vertical => greyboxBody != null && greyboxBody.size.y > greyboxBody.size.x;

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            PlaceEmitters();
            if (!Mirror(body, greyboxBody)) return;
            if (body.sprite != beam) body.sprite = beam;
            Vector2 size = greyboxBody.size;
            float length = Vertical ? size.y : size.x, thick = Mathf.Max(1e-3f, (Vertical ? size.x : size.y) * bodyScale);
            body.drawMode = SpriteDrawMode.Tiled;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
            body.tileMode = SpriteTileMode.Continuous;
            body.size = new Vector2(length / thick, 1f);
            body.transform.localScale = new Vector3(thick, thick, 1f);
            body.transform.rotation = greyboxBody.transform.rotation * Quaternion.Euler(0f, 0f, Vertical ? 90f : 0f);
            Color c = body.color; c.a = gateAlpha > 0f ? Mathf.Clamp01(greyboxBody.color.a / gateAlpha) : Alpha(TicksSinceFire); body.color = c;
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
