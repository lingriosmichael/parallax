using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: a geyser. The carved vent (TRAP-03) always, flipped for a vent in a ceiling; through the tell,
    /// droplets bubble at the mouth; while erupting, the water column (T04) exactly as big as the gameplay column, its
    /// texture scrolling with the ticks, a burst at the mouth for its first ticks and spray at the column's far end. The
    /// tell and the burst are code effects over the kit's spray (T06): AutoSprite's animations of the vent came back with a
    /// person in them (style gate, 2026-09-29).</summary>
    public sealed class GeyserArt : TrapArt
    {
        const int TellCycle = 12, BurstTicks = 14, SprayLife = 16;

        [SerializeField] GeyserTrap trap;
        [SerializeField] SpriteRenderer ventArt, greyboxVent;
        [SerializeField] SpriteRenderer columnArt, greyboxColumn;
        [SerializeField] Sprite ventSprite;
        [SerializeField] Sprite[] columnFrames = new Sprite[0];
        [SerializeField] Material trapMaterial;
        [SerializeField] Vector2 ventSize;
        [SerializeField] SpriteRenderer[] tellDrops = new SpriteRenderer[0];
        [Tooltip("The tell's warm glow at the mouth (layer 05: vents may glow).")]
        [SerializeField] SpriteRenderer tellGlow;
        [Tooltip("Steam rising from the mouth through the tell (TRAP-12; §12 R9).")]
        [SerializeField] SpriteRenderer[] steam = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] burst = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] spray = new SpriteRenderer[0];
        [SerializeField] int tellTicks;
        [SerializeField] GeyserDirection direction;

        public override RoomTrap Trap => trap;
        public GeyserTrap Geyser => trap;
        Vector2 Push => GeyserMath.Push(direction);

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(ventArt, greyboxVent));
            into.Add(new Body(columnArt, greyboxColumn));
        }

        protected override void CollectEffects(List<Effect> into)
        {
            into.Add(new Effect("tell", tellGlow));
            foreach (SpriteRenderer s in steam) into.Add(new Effect("tell", s));
            foreach (SpriteRenderer s in tellDrops) into.Add(new Effect("tell", s));
            foreach (SpriteRenderer s in burst) into.Add(new Effect("burst", s));
            foreach (SpriteRenderer s in spray) into.Add(new Effect("burst", s));
        }

        protected override void Render()
        {
            int s = TicksSinceFire;
            GeyserPhase phase = trap.Phase;
            if (Mirror(ventArt, greyboxVent))
            {
                FitVent(ventArt, ventSprite, ventSize * bodyScale);
                // The vent warms through the tell and the eruption, as the grey-box vent turns its tell colour; pulsing in the tell.
                float warm = phase == GeyserPhase.Tell ? 0.55f + 0.35f * Mathf.Sin(Mathf.Max(0, s) * 0.7f) : phase == GeyserPhase.Erupt ? 0.6f : 0f;
                ventArt.color = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.3f, 1f), warm);
            }

            int intoErupt = s - tellTicks;
            if (Mirror(columnArt, greyboxColumn) && columnFrames.Length > 0)
            {
                // Rows roll down as the frame index grows, so an upward jet plays the frames backwards.
                int k = Mathf.Max(0, intoErupt) % columnFrames.Length;
                columnArt.sprite = columnFrames[(columnFrames.Length - k) % columnFrames.Length];
                columnArt.flipY = direction == GeyserDirection.Down;
                columnArt.size = new Vector2(greyboxColumn.size.x * bodyScale, greyboxColumn.size.y);   // D-101: wider, never longer
            }

            Vector2 push = Push;
            Vector2 mouth = greyboxVent != null ? (Vector2)greyboxVent.transform.position + push * (greyboxVent.size.y * 0.5f) : (Vector2)transform.position;
            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);

            // The tell's steam: wisps rising from the mouth and fading, staggered through the tell.
            for (int i = 0; i < steam.Length; i++)
            {
                bool telling = phase == GeyserPhase.Tell && s >= 0;
                float u = telling ? Mathf.Repeat((s + i * 7f) / 20f, 1f) : 0f;
                float x = (i - (steam.Length - 1) * 0.5f) * 0.22f;
                Place(steam[i], telling, mouth + new Vector2(x, 0f) + push * (0.15f + 0.5f * u), 0.55f + 0.35f * u, telling ? 0.75f * Mathf.Sin(u * Mathf.PI) + 0.05f : 0f);
                if (telling && direction == GeyserDirection.Down) steam[i].flipY = true;
            }

            // The tell: a warm glow pulsing at the mouth, and droplets popping out of it, staggered so one always shows.
            if (tellGlow != null)
            {
                bool telling = phase == GeyserPhase.Tell && s >= 0;
                float pulse = telling ? 0.75f + 0.25f * Mathf.Sin(s * 0.7f) : 0f;
                Place(tellGlow, telling, mouth, 1f, pulse);
                if (telling) tellGlow.transform.localScale = new Vector3(2.4f * pulse, 1.1f * pulse, 1f);
            }
            for (int i = 0; i < tellDrops.Length; i++)
            {
                SpriteRenderer r = tellDrops[i];
                if (r == null) continue;
                bool on = phase == GeyserPhase.Tell && s >= 0;
                int t = s + i * (TellCycle / Mathf.Max(1, tellDrops.Length));
                PuffPose p = on ? TrapArtMath.Puff(seed, 300 + i * 31 + t / TellCycle, t % TellCycle, TellCycle, 0.22f) : default;
                Place(r, p.Visible, mouth + new Vector2((i - (tellDrops.Length - 1) * 0.5f) * 0.18f + p.Offset.x * 0.4f, 0f) + push * (p.Offset.y * 0.7f), 0.55f * p.Scale, p.Alpha);
            }

            // The burst: spray thrown out of the mouth as it starts.
            for (int i = 0; i < burst.Length; i++)
            {
                SpriteRenderer r = burst[i];
                if (r == null) continue;
                PuffPose p = phase == GeyserPhase.Erupt ? TrapArtMath.Puff(seed, 400 + i, intoErupt, BurstTicks, 0.6f) : default;
                float side = i % 2 == 0 ? -1f : 1f;
                Place(r, p.Visible, mouth + new Vector2(side * Mathf.Abs(p.Offset.x) * 0.9f, 0f) + push * (p.Offset.y * 0.5f), 0.55f * p.Scale, p.Alpha);
            }

            // The spray at the column's far end, cycling while it erupts.
            Vector2 top = greyboxColumn != null ? (Vector2)greyboxColumn.transform.position + push * (greyboxColumn.size.y * 0.5f) : mouth;
            for (int i = 0; i < spray.Length; i++)
            {
                SpriteRenderer r = spray[i];
                if (r == null) continue;
                int t = intoErupt + i * (SprayLife / Mathf.Max(1, spray.Length));
                PuffPose p = phase == GeyserPhase.Erupt && intoErupt >= 0 ? TrapArtMath.Puff(seed, 500 + i * 37 + t / SprayLife, t % SprayLife, SprayLife, 0.75f) : default;
                // §12 R9: larger and denser at the top: the spray fans out and falls back around the column's end.
                Place(r, p.Visible, top + new Vector2(p.Offset.x * 1.1f, 0f) + push * (p.Offset.y * 0.5f), 0.95f * p.Scale, Mathf.Min(1f, p.Alpha * 1.2f));
            }
        }


        void FitVent(SpriteRenderer r, Sprite sprite, Vector2 box)
        {
            if (r.sprite != sprite) r.sprite = sprite;
            if (trapMaterial != null && r.sharedMaterial != trapMaterial) r.sharedMaterial = trapMaterial;
            r.drawMode = SpriteDrawMode.Simple;
            r.flipY = direction == GeyserDirection.Down;
            if (sprite == null) return;
            Vector2 native = sprite.bounds.size;
            float k = Mathf.Min(box.x / Mathf.Max(native.x, 1e-4f), box.y / Mathf.Max(native.y, 1e-4f));
            r.transform.localScale = new Vector3(k, k, 1f);
        }
    }
}
