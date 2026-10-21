using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: spikes. Static spikes and pit floors (Hazard), hidden and periodic spikes (HiddenSpikesTrap) and
    /// sliding spikes (a MovingTrap hazard) draw the spike strip (TRAP-04) tiled to exactly their grey-box's size, pointing
    /// away from their host (down when they hang from a ceiling). Hidden and periodic spikes draw nothing while down (§11 R4);
    /// from their reveal tick they rise over three ticks while already lethal, anchored at their base, with a puff of grit.
    /// PAX-A18 (D-151): with the painted blades in the kit, a strip draws blade by blade instead (SpikeLayout): every blade
    /// at one fixed size in every level, the irregular gaps absorbing the strip's width.</summary>
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
        [Tooltip("PAX-A18 (D-151): the blades, one renderer each (empty: the tiled strip), with their variant sprite and their centre along the strip (local units, 0 = the middle), laid out by SpikeLayout when built.")]
        [SerializeField] SpriteRenderer[] blades = new SpriteRenderer[0];
        [SerializeField] Sprite[] bladeSprites = new Sprite[0];
        [SerializeField] int[] bladeVariant = new int[0];
        [SerializeField] float[] bladeAlong = new float[0];
        [Tooltip("The tallest blade's drawn height (world units): every strip's blades draw at this one size (a strip thinner than the standard one draws them smaller, a taller one stretches them to its height).")]
        [SerializeField] float bladeHeight = 0.594f;
        [Tooltip("The one growth every thin strip's blades draw at (base to tip, as a multiple of the strip's height): BodyScale x SpikeHeightScale, whatever this strip's own bodyScale (a hinge floor's face draws its blades the same size as any floor's).")]
        [SerializeField] float bladeGrowth = 1.98f;
        [Tooltip("The developer (2026-10-16): per blade, how far it may reach from the strip's base (world units) before it'd show past the solid hiding it or pierce one in front of it; 0 = no limit. A blade that needs it draws smaller (the one exception to D-151's one size).")]
        [SerializeField] float[] bladeReach = new float[0];
        [Tooltip("The developer (2026-10-16, \"Can we overlap them somehow?\"): per blade, whether it stands in the back row (between two front blades, BackScale smaller, drawn behind), so a strip reads as one dense bed.")]
        [SerializeField] bool[] bladeBack = new bool[0];
        [Tooltip("D-161: stout blades draw this much wider (x) and taller (y) than the standard size (TrapArtConfig.StoutBladeScale); (1, 1) = standard.")]
        [SerializeField] Vector2 bladeStretch = Vector2.one;

        /// <summary>The back row's size and tint, relative to the front row's.</summary>
        public const float BackScale = 0.8f;
        /// <summary>2026-10-17 visual review ("about 100 near-identical blades ... monotonous"; the developer: proceed with
        /// varying the blades, never with gaps, since a gap in a lethal row would read as a safe spot): each front blade leans up
        /// to this many degrees about its foot, and BrokenShare of them stand at BrokenScale of their height.</summary>
        public const float BladeLean = 7f, BrokenShare = 0.14f, BrokenScale = 0.72f;
        public static readonly Color BackTint = Color.white;   // t22: dimmed (0.82), its bright edges fell under 3:1 in dark rooms

        /// <summary>Whether blade `i` stands in the back row.</summary>
        public bool IsBack(int i) => i < bladeBack.Length && bladeBack[i];

        /// <summary>Whether blade `i` draws smaller than the standard size to stay inside its cover.</summary>
        public bool Reached(int i) => i < bladeReach.Length && bladeReach[i] > 0f && greyboxBody != null
            && bladeHeight * BladeScale(Strip(greyboxBody.size).y, Growth, bladeHeight).k * bladeStretch.y * (IsBack(i) ? BackScale : 1f) > bladeReach[i] + 1e-4f;

        public IReadOnlyList<SpriteRenderer> Blades => blades;

        /// <summary>D-161: (1, 1) for the standard blades; StoutBladeScale for a stout steel strip.</summary>
        public Vector2 BladeStretch => bladeStretch;

        /// <summary>D-105: only a thin strip (a floor's or a ceiling's spikes) is drawn longer; a taller grey-box, like a sweep
        /// that waits inside a post, already reads as long blades, and lengthening it would push it out of its host.</summary>
        public const float MaxStripHeight = 0.5f;

        /// <summary>D-161: a stout blade's lean lifts its wide corner a little above its upright tip; its growth allows for it.</summary>
        float StoutLean => bladeStretch.x > 1f ? 1.06f : 1f;

        float HeightScale => greyboxBody != null && Strip(greyboxBody.size).y > MaxStripHeight ? 1f : heightScale;

        public override Vector2 BodyGrowth
        {
            get
            {
                // PAX-A18: blades draw at the one standard growth (bladeGrowth), across within the strip.
                // Along the strip the blades stay inside it by construction; a little allowance covers a turning blade's bounds.
                float along = blades.Length > 0 ? Mathf.Max(bodyScale - 1f, 0.1f) : bodyScale - 1f, tips = blades.Length > 0 ? Mathf.Max(bodyScale * HeightScale, Growth * bladeStretch.y * StoutLean) - 1f : bodyScale * HeightScale - 1f;
                return pointsSide != 0 ? new(tips, along) : new(along, tips);
            }
        }

        /// <summary>PAX-A18: the blades' growth for this strip (the standard one on a thin strip; bodyScale on a tall one).</summary>
        float Growth => greyboxBody != null && Strip(greyboxBody.size).y > MaxStripHeight ? bodyScale : bladeGrowth;

        /// <summary>A grey-box's size in the strip's frame: x along the row of teeth, y from base to tips.</summary>
        Vector2 Strip(Vector2 size) => pointsSide != 0 ? new Vector2(size.y, size.x) : size;

        /// <summary>The way the tips point.</summary>
        Vector2 Outward => pointsSide != 0 ? new Vector2(pointsSide, 0f) : new Vector2(0f, pointsDown ? -1f : 1f);

        public override RoomTrap Trap => trap;
        protected override bool NeedsTrap => false;

        protected override void CollectBodies(List<Body> into)
        {
            if (blades.Length == 0) { into.Add(new Body(body, greyboxBody)); return; }
            foreach (SpriteRenderer b in blades) into.Add(new Body(b, greyboxBody));
        }

        /// <summary>PAX-A18: the blades' uniform scale (thin strips) and their extra vertical stretch (taller strips), for a strip
        /// whose base-to-tip height is `stripHeight` (local units).</summary>
        public static (float k, float stretch) BladeScale(float stripHeight, float growth, float bladeHeight)
        {
            // The blades' own size (k 1) wherever the strip allows it: a standard strip (0.3 u at the standard growth) draws them
            // exactly; a thinner one draws them smaller; a taller one stretches them up to its height.
            float drawn = stripHeight * growth;
            if (bladeHeight <= 0f) return (1f, 1f);
            return drawn < bladeHeight - 1e-3f ? (drawn / bladeHeight, 1f) : (1f, Mathf.Max(1f, drawn / bladeHeight));
        }
        /// <summary>The scale and stretch `k`, `stretch` brought within `reach` (world units from the base; 0 = no limit): the
        /// stretch goes first, then the blades shrink whole, keeping their shape.</summary>
        public static (float k, float stretch) Reach(float k, float stretch, float bladeHeight, float reach)
        {
            if (reach <= 0f || bladeHeight <= 0f || bladeHeight * k * stretch <= reach) return (k, stretch);
            stretch = Mathf.Max(1f, reach / (bladeHeight * k));
            if (bladeHeight * k * stretch > reach) k = reach / (bladeHeight * stretch);
            return (k, stretch);
        }

        protected override void CollectEffects(List<Effect> into) { foreach (SpriteRenderer g in grit) into.Add(new Effect("grit", g)); }

        protected override void Render()
        {
            int s = TicksSinceFire;
            if (blades.Length > 0) { if (body != null) body.enabled = false; RenderBlades(s); }
            else if (Mirror(body, greyboxBody))
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
            RenderGrit(s);
        }

        // PAX-A18 (D-151): each blade at the strip's base, its pivot (the blade's foot) on the base line, pointing outward.
        void RenderBlades(int s)
        {
            if (greyboxBody == null) return;
            Vector2 size = Strip(greyboxBody.size);
            float rise = rises && s >= 0 ? Mathf.Clamp01((s + 1f) / RiseTicks) : 1f;
            (float k0, float stretch0) = BladeScale(size.y, Growth, bladeHeight);
            Quaternion turn = greyboxBody.transform.rotation;
            Vector2 across = turn * (pointsSide != 0 ? Vector2.up : Vector2.right), outward = turn * Outward;
            Vector2 baseLine = (Vector2)greyboxBody.transform.position - outward * (size.y * 0.5f);
            for (int i = 0; i < blades.Length; i++)
            {
                SpriteRenderer b = blades[i];
                if (!Mirror(b, greyboxBody)) continue;
                int v = i < bladeVariant.Length ? bladeVariant[i] : 0;
                Sprite sprite = bladeSprites.Length > 0 ? bladeSprites[Mathf.Clamp(v, 0, bladeSprites.Length - 1)] : strip;
                if (b.sprite != sprite) b.sprite = sprite;
                if (trapMaterial != null && b.sharedMaterial != trapMaterial) b.sharedMaterial = trapMaterial;
                b.drawMode = SpriteDrawMode.Simple;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
                b.flipY = pointsSide == 0 && pointsDown;
                bool back = IsBack(i);
                uint look = TrapArtMath.Seed(SeedName, 0);
                float lean = back ? 0f : TrapArtMath.HashSigned(look, 1300 + i) * BladeLean;
                bool broken = !back && TrapArtMath.Hash01(look, 1400 + i) < BrokenShare;
                b.transform.rotation = (pointsSide != 0 ? turn * Quaternion.Euler(0f, 0f, -90f * pointsSide) : turn) * Quaternion.Euler(0f, 0f, lean);
                // D-161: a stout blade's extra height goes through the same reach cap as any other.
                (float k, float stretch) = Reach(k0 * (back ? BackScale : 1f), stretch0 * bladeStretch.y, bladeHeight, i < bladeReach.Length ? bladeReach[i] : 0f);
                if (broken) stretch *= BrokenScale;
                Color tint = back ? BackTint : Color.white;
                if (b.color != tint) b.color = tint;
                b.transform.localScale = new Vector3(k * bladeStretch.x, k * stretch * rise, 1f);
                float along = i < bladeAlong.Length ? bladeAlong[i] : 0f;
                Vector2 foot = baseLine + across * along;
                b.transform.position = new Vector3(foot.x, foot.y, b.transform.position.z);
            }
        }

        void RenderGrit(int s)
        {
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
