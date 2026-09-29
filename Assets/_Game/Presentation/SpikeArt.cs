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
        [SerializeField] SpriteRenderer[] grit = new SpriteRenderer[0];

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
                body.transform.localScale = Vector3.one;
                body.flipY = pointsDown;
                Vector2 size = greyboxBody.size;
                float rise = rises && s >= 0 ? Mathf.Clamp01((s + 1f) / RiseTicks) : 1f;
                body.size = new Vector2(size.x, size.y * rise);
                // Anchored at its base: the host side (the bottom, or the top for spikes hanging from a ceiling).
                float shift = (1f - rise) * size.y * 0.5f * (pointsDown ? 1f : -1f);
                body.transform.position += new Vector3(0f, shift, 0f);
            }
            Vector2 at = greyboxBody != null ? (Vector2)greyboxBody.transform.position : (Vector2)transform.position;
            Vector2 half = greyboxBody != null ? greyboxBody.size * 0.5f : Vector2.zero;
            uint seed = TrapArtMath.Seed(SeedName, trap != null ? trap.LatestFireTick : 0);
            for (int i = 0; i < grit.Length; i++)
            {
                bool on = rises && Shows(greyboxBody) && s >= 0 && s < GritLife;
                PuffPose p = on ? TrapArtMath.Puff(seed, 600 + i, s, GritLife, 0.3f) : default;
                float along = grit.Length > 1 ? (i / (grit.Length - 1f) - 0.5f) * half.x * 1.8f : 0f;
                float baseY = at.y + (pointsDown ? half.y : -half.y);
                Place(grit[i], p.Visible, new Vector2(at.x + along + p.Offset.x * 0.5f, baseY + (pointsDown ? -1f : 1f) * p.Offset.y * 0.5f), 0.3f * p.Scale, 0.8f * p.Alpha);
            }
        }
    }
}
