using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13, D-162: a geyser. The carved vent (TRAP-03) always, flipped for a vent in a ceiling, warming through the
    /// tell and the eruption; over it the thermal-vent steam (Tools/Art/geyser_steam.py), one pre-rendered sprite per tick,
    /// each pivoted on the vent's mouth: the tell's hissing wisps and the vent's glow; while erupting, the dense jet inside the
    /// launch column (the column's body, exactly the column art's box) and the plume round it; then the steam dying away for
    /// TrapArtMath.GeyserFadeTicks. A vent in a ceiling has its own frames: its jet fires down and its steam rises back.</summary>
    public sealed class GeyserArt : TrapArt
    {
        /// <summary>The column height the frames were rendered for (GeyserMath's default).</summary>
        const float FramesColumnHeight = GeyserMath.DefaultColumnHeight;

        [SerializeField] GeyserTrap trap;
        [SerializeField] SpriteRenderer ventArt, greyboxVent;
        [SerializeField] SpriteRenderer columnArt, greyboxColumn;
        [SerializeField] Sprite ventSprite;
        [SerializeField] Material trapMaterial;
        [SerializeField] Vector2 ventSize;
        [Tooltip("D-162: the tell's wisps and glow (tell group).")]
        [SerializeField] SpriteRenderer tellSteam;
        [Tooltip("D-162: the eruption's plume round the column (burst group).")]
        [SerializeField] SpriteRenderer plume;
        [Tooltip("D-162: the steam dying away after the eruption (fade group).")]
        [SerializeField] SpriteRenderer fade;
        [SerializeField] Sprite[] tellFrames = new Sprite[0], coreFrames = new Sprite[0], plumeFrames = new Sprite[0], fadeFrames = new Sprite[0];
        [SerializeField] int tellTicks;
        [SerializeField] int eruptTicks = GeyserMath.DefaultEruptTicks;
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
            into.Add(new Effect("tell", tellSteam));
            into.Add(new Effect("burst", plume));
            into.Add(new Effect("fade", fade));
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

            Vector2 mouth = greyboxVent != null ? (Vector2)greyboxVent.transform.position + Push * (greyboxVent.size.y * 0.5f) : (Vector2)transform.position;
            // A column longer or shorter than the frames' scales the steam with it (every level's is 1.5 u).
            float k = greyboxColumn != null ? greyboxColumn.size.y / FramesColumnHeight : 1f;
            int intoErupt = s - tellTicks, afterErupt = intoErupt - eruptTicks;

            bool telling = phase == GeyserPhase.Tell && s >= 0;
            Show(tellSteam, telling, telling ? Frame(tellFrames, s, tellTicks) : null, mouth, k);

            bool erupting = phase == GeyserPhase.Erupt && intoErupt >= 0;
            Show(plume, erupting, erupting ? Frame(plumeFrames, intoErupt, eruptTicks) : null, mouth, k);
            if (Mirror(columnArt, greyboxColumn) && coreFrames.Length > 0)
            {
                // The body: the jet inside the column, drawn at the column art's box (D-101: wider, never longer), from the mouth.
                columnArt.sprite = Frame(coreFrames, Mathf.Max(0, intoErupt), eruptTicks);
                columnArt.drawMode = SpriteDrawMode.Simple;
                columnArt.flipY = false;
                columnArt.transform.position = new Vector3(mouth.x, mouth.y, columnArt.transform.position.z);
                Vector2 native = columnArt.sprite != null ? (Vector2)columnArt.sprite.bounds.size : Vector2.one;
                columnArt.transform.localScale = new Vector3(greyboxColumn.size.x * bodyScale / Mathf.Max(native.x, 1e-4f), greyboxColumn.size.y / Mathf.Max(native.y, 1e-4f), 1f);
            }

            bool fading = phase == GeyserPhase.Idle && s >= 0 && afterErupt >= 0 && afterErupt < TrapArtMath.GeyserFadeTicks && fadeFrames.Length > 0;
            Show(fade, fading, fading ? fadeFrames[Mathf.Min(afterErupt, fadeFrames.Length - 1)] : null, mouth, k);
        }

        /// <summary>The frame for tick `t` of a `length`-tick event; frames made for another length are stretched over it.</summary>
        static Sprite Frame(Sprite[] frames, int t, int length)
        {
            if (frames.Length == 0) return null;
            int i = length == frames.Length || length <= 1 ? t : Mathf.RoundToInt(t * (frames.Length - 1) / (float)(length - 1));
            return frames[Mathf.Clamp(i, 0, frames.Length - 1)];
        }

        static void Show(SpriteRenderer r, bool visible, Sprite sprite, Vector2 mouth, float scale)
        {
            if (r == null) return;
            visible &= sprite != null;
            if (visible) r.sprite = sprite;
            Place(r, visible, mouth, scale, 1f);
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
