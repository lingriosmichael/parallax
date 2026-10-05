using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: spikes. Static spikes and pit floors (Hazard), hidden and periodic spikes (HiddenSpikesTrap) and
    /// sliding spikes (a MovingTrap hazard) draw the spike strip (TRAP-04) tiled to exactly their grey-box's size, pointing
    /// away from their host (down when they hang from a ceiling). Hidden and periodic spikes draw nothing while down (§11 R4);
    /// from their reveal tick they rise over three ticks while already lethal, anchored at their base, with a puff of grit.</summary>
    public sealed class SpikeArt : TrapArt
    {
        public const int RiseTicks = 3;
        const int GritLife = 12;

        [SerializeField] RoomTrap trap;                 // HiddenSpikesTrap or MovingTrap; null for a static Hazard
        [SerializeField] SpriteRenderer body, greyboxBody;
        [SerializeField] Sprite strip;
        [SerializeField] Material trapMaterial;
        [SerializeField] bool pointsDown, rises;
        [Tooltip("Spikes on a wall's face point sideways, away from it: +1 east, -1 west, 0 up or down (pointsDown).")]
        [SerializeField, Range(-1, 1)] int pointsSide;
        [SerializeField] SpriteRenderer[] grit = new SpriteRenderer[0];
        [Tooltip("D-105: the drawn height as a multiple of the grey-box's (from the base outward); the hitbox is unchanged.")]
        [SerializeField, Min(1f)] float heightScale = 1f;
        [Tooltip("The teeth in one tile of the strip; the strip is drawn a whole number of teeth wide.")]
        [SerializeField, Min(1)] int stripTeeth = 11;

        /// <summary>D-105: only a thin strip (a floor's or a ceiling's spikes) is drawn longer; a taller grey-box, like a sweep
        /// that waits inside a post, already reads as long blades, and lengthening it would push it out of its host.</summary>
        public const float MaxStripHeight = 0.5f;

        float HeightScale => greyboxBody != null && Strip(greyboxBody.size).y > MaxStripHeight ? 1f : heightScale;

        public override Vector2 BodyGrowth => pointsSide != 0 ? new(bodyScale * HeightScale - 1f, bodyScale - 1f) : new(bodyScale - 1f, bodyScale * HeightScale - 1f);

        /// <summary>A grey-box's size in the strip's frame: x along the row of teeth, y from base to tips.</summary>
        Vector2 Strip(Vector2 size) => pointsSide != 0 ? new Vector2(size.y, size.x) : size;

        /// <summary>The way the tips point.</summary>
        Vector2 Outward => pointsSide != 0 ? new Vector2(pointsSide, 0f) : new Vector2(0f, pointsDown ? -1f : 1f);

        public override RoomTrap Trap => trap;
        protected override bool NeedsTrap => false;

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { foreach (SpriteRenderer g in grit) into.Add(new Effect("grit", g)); }

        protected override void Render()
        {
            int s = TicksSinceFire;
            if (Mirror(body, greyboxBody))
            {
                if (body.sprite != strip) body.sprite = strip;
                if (trapMaterial != null && body.sharedMaterial != trapMaterial) body.sharedMaterial = trapMaterial;
                body.drawMode = SpriteDrawMode.Tiled;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
                body.tileMode = SpriteTileMode.Continuous;
                // D-101: each tooth bodyScale bigger, the strip as wide as the grey-box, taller by bodyScale.
                // Tiled across only: one row of blades, stretched to the drawn height, so a tall grey-box never stacks a
                // second, cut-off row and a short one never loses its tips.
                body.flipY = pointsSide == 0 && pointsDown;
                // Sideways spikes: the strip turned a quarter, its tips away from the wall.
                if (pointsSide != 0) body.transform.rotation = greyboxBody.transform.rotation * Quaternion.Euler(0f, 0f, -90f * pointsSide);
                Vector2 size = Strip(greyboxBody.size);
                float rise = rises && s >= 0 ? Mathf.Clamp01((s + 1f) / RiseTicks) : 1f;
                float drawn = size.y * rise * bodyScale * HeightScale;
                float row = strip != null ? strip.bounds.size.y : size.y;
                // A whole number of teeth (the developer: spikes "appear somewhat cut"), each at most bodyScale big, squeezed a
                // little so they span exactly the grey-box's width.
                float tooth = strip != null ? strip.bounds.size.x / stripTeeth : size.x / bodyScale;
                int teeth = Mathf.Max(1, Mathf.CeilToInt(size.x / (tooth * bodyScale) - 0.01f));
                body.transform.localScale = new Vector3(size.x / (teeth * tooth), drawn / row, 1f);
                body.size = new Vector2(teeth * tooth, row);
                // Anchored at its base: the host side (the bottom, the top for spikes hanging from a ceiling, a wall's face).
                float shift = (drawn - size.y) * 0.5f;
                body.transform.position += (Vector3)(Outward * shift);
            }
            Vector2 at = greyboxBody != null ? (Vector2)greyboxBody.transform.position : (Vector2)transform.position;
            Vector2 half = greyboxBody != null ? Strip(greyboxBody.size) * 0.5f : Vector2.zero;
            Vector2 outward = Outward, across = pointsSide != 0 ? Vector2.up : Vector2.right;   // along the row of teeth
            uint seed = TrapArtMath.Seed(SeedName, trap != null ? trap.LatestFireTick : 0);
            for (int i = 0; i < grit.Length; i++)
            {
                bool on = rises && Shows(greyboxBody) && s >= 0 && s < GritLife;
                PuffPose p = on ? TrapArtMath.Puff(seed, 600 + i, s, GritLife, 0.3f) : default;
                float along = grit.Length > 1 ? (i / (grit.Length - 1f) - 0.5f) * half.x * 1.8f : 0f;
                Vector2 seat = at - outward * half.y;
                Place(grit[i], p.Visible, seat + across * (along + p.Offset.x * 0.5f) + outward * (p.Offset.y * 0.5f), 0.3f * p.Scale, 0.8f * p.Alpha);
            }
        }
    }
}
