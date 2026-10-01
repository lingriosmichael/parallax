using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: an arrow or spear launcher and its projectile. A disguised launcher's body is its host's skin, always
    /// (P10); from the fire tick a carved slot (TRAP-01) shows over it as the reveal (§12 R8: the reveal frame differs only by
    /// effects that start on it). An honest launcher's body is the slot. The arrow (TRAP-02) or the spear (TRAP-05: a head and
    /// a repeating shaft) is drawn exactly where the grey-box projectile is, facing its lane: the sprites face right and are
    /// flipped for a left lane, and the trap material (Parallax/2D/Sprite-Lit-Flip) turns their normal maps with them. Over the
    /// declared tell the slot warms and a glint pulses at the head; on stopping, the projectile shivers inside its box and
    /// puffs a little dust.</summary>
    public sealed class ArrowArt : TrapArt, IHostSkinned
    {
        // The art projectile is drawn a little short of the grey-box so its shiver stays inside the grey-box's bounds.
        public const float ArrowFill = 0.94f;
        const int ShiverTicks = 10, ImpactLife = 18;
        const float ShiverAmplitude = 0.018f;
        static readonly Color TellWarm = new(1f, 0.72f, 0.38f, 1f);

        [SerializeField] ArrowTrap trap;
        [SerializeField] SpriteRenderer launcherArt, greyboxLauncher;
        [SerializeField] SpriteRenderer arrowArt, greyboxArrow;
        [Tooltip("A spear's repeating shaft (the head is arrowArt); null for an arrow.")]
        [SerializeField] SpriteRenderer shaftArt;
        [SerializeField] SpriteRenderer slot;
        [SerializeField] bool disguised, spear;
        [SerializeField] HostSkin hostSkin;
        [SerializeField] Sprite launcherSprite, arrowSprite, spearHeadSprite, spearShaftSprite;
        [SerializeField] Material trapMaterial;
        [SerializeField] Vector2 launcherSize, arrowSize;
        [SerializeField] ArrowDirection direction;
        [SerializeField] int tellTicks, flightTicks;
        [SerializeField] SpriteRenderer glint;
        [SerializeField] SpriteRenderer[] impact = new SpriteRenderer[0];

        public override RoomTrap Trap => trap;
        public bool Disguised => disguised;
        public bool Spear => spear;
        public HostSkin HostSkin => hostSkin;
        public SpriteRenderer LauncherArt => launcherArt;
        public int TellTicks => tellTicks;
        bool FacesLeft => direction == ArrowDirection.Left;

        public void Reskin(HostSkin skin)
        {
            if (!disguised) return;
            hostSkin = skin;
            skin.ApplyTo(launcherArt, launcherSize);
            launcherArt.flipX = false;
        }

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(launcherArt, greyboxLauncher));
            into.Add(new Body(arrowArt, greyboxArrow));
            if (shaftArt != null) into.Add(new Body(shaftArt, greyboxArrow));
        }

        protected override void CollectEffects(List<Effect> into)
        {
            if (slot != null) into.Add(new Effect("reveal", slot));
            into.Add(new Effect("glint", glint));
            foreach (SpriteRenderer s in impact) into.Add(new Effect("impact", s));
        }

        protected override void Render()
        {
            bool fired = Shows(greyboxArrow);
            int s = TicksSinceFire;
            bool telling = fired && TrapArtMath.ArrowGlint(s, tellTicks);
            float pulse = telling ? 0.6f + 0.4f * Mathf.Cos(s * 0.9f) : 0f;

            if (Mirror(launcherArt, greyboxLauncher))
            {
                if (disguised)
                {
                    if (hostSkin.WorldTiled) hostSkin.SetSampling(launcherArt, launcherArt.transform.position, launcherArt.transform.lossyScale);
                }
                else
                {
                    DrawFitted(launcherArt, launcherSprite, launcherSize * bodyScale);
                    launcherArt.color = Color.Lerp(Color.white, TellWarm, pulse * 0.8f);
                }
            }
            if (slot != null)
            {
                bool show = disguised && fired && Shows(greyboxLauncher);
                slot.enabled = show;
                if (show)
                {
                    slot.transform.position = new Vector3(greyboxLauncher.transform.position.x, greyboxLauncher.transform.position.y, slot.transform.position.z);
                    DrawFitted(slot, launcherSprite, launcherSize * bodyScale);
                    slot.color = Color.Lerp(Color.white, TellWarm, pulse * 0.8f);
                }
            }

            int stopTick = tellTicks + flightTicks;
            float shiver = 0f;
            int since = s - stopTick;
            if (fired && since >= 0 && since < ShiverTicks)
            {
                float room = (spear ? arrowSize.y * 0.08f : arrowSize.x * (1f - ArrowFill) * 0.5f) * 0.9f;   // spare inside the grey-box
                shiver = Mathf.Min(ShiverAmplitude, room) * (1f - since / (float)ShiverTicks) * (since % 2 == 0 ? 1f : -1f);
            }
            if (!spear)
            {
                if (Mirror(arrowArt, greyboxArrow))
                {
                    DrawFitted(arrowArt, arrowSprite, new Vector2(arrowSize.x * ArrowFill, arrowSize.y) * bodyScale);
                    arrowArt.transform.position += new Vector3(shiver, 0f, 0f);
                }
            }
            else DrawSpear(shiver);

            Vector2 head = HeadPosition();
            if (glint != null)
            {
                glint.enabled = telling;
                if (telling)
                {
                    glint.transform.position = new Vector3(head.x, head.y, glint.transform.position.z);
                    glint.transform.localScale = Vector3.one * (2.4f * pulse);
                    Color c = glint.color; c.a = 0.5f + 0.5f * pulse; glint.color = c;
                }
            }

            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);
            for (int i = 0; i < impact.Length; i++)
            {
                SpriteRenderer r = impact[i];
                if (r == null) continue;
                PuffPose puff = fired && s >= stopTick ? TrapArtMath.Puff(seed, 200 + i, s - stopTick, ImpactLife, 0.25f) : default;
                r.enabled = puff.Visible;
                if (!puff.Visible) continue;
                float back = FacesLeft ? 1f : -1f;
                r.transform.position = new Vector3(head.x + back * puff.Offset.x * 0.3f, head.y + puff.Offset.y * 0.6f, r.transform.position.z);
                r.transform.localScale = Vector3.one * (0.35f * puff.Scale);
                Color c = r.color; c.a = 0.8f * puff.Alpha; r.color = c;
            }
        }

        // A spear: its head at the leading end of the grey-box shaft, the repeating shaft behind it, both inside the box.
        void DrawSpear(float shiver)
        {
            bool shown = Mirror(arrowArt, greyboxArrow);
            if (shaftArt != null) Mirror(shaftArt, greyboxArrow);
            if (!shown) return;
            Vector2 size = arrowSize * bodyScale;   // D-101
            float sign = FacesLeft ? -1f : 1f, thick = size.y * 0.85f;
            float headLen = Mathf.Min(size.x * 0.45f, thick * 2.2f), shaftLen = size.x * 0.98f - headLen;
            Vector3 centre = greyboxArrow.transform.position + new Vector3(0f, shiver, 0f);
            DrawFitted(arrowArt, spearHeadSprite, new Vector2(headLen, thick));
            arrowArt.transform.position = centre + new Vector3(sign * (size.x * 0.49f - headLen * 0.5f), 0f, 0f);
            if (shaftArt == null) return;
            shaftArt.sprite = spearShaftSprite;
            if (trapMaterial != null) shaftArt.sharedMaterial = trapMaterial;
            shaftArt.drawMode = SpriteDrawMode.Tiled;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
            shaftArt.tileMode = SpriteTileMode.Continuous;
            shaftArt.transform.localScale = Vector3.one;
            shaftArt.flipX = FacesLeft;
            shaftArt.color = Color.white;
            shaftArt.size = new Vector2(shaftLen, thick);
            shaftArt.transform.position = centre + new Vector3(-sign * (size.x * 0.49f - shaftLen * 0.5f), 0f, 0f);
        }

        // The head: the projectile's leading end, where the glint and the impact dust sit.
        Vector2 HeadPosition()
        {
            Transform t = greyboxArrow != null ? greyboxArrow.transform : transform;
            float sign = FacesLeft ? -1f : 1f;
            return (Vector2)t.position + new Vector2(sign * arrowSize.x * 0.5f * ArrowFill, 0f);
        }

        /// <summary>The sprite fitted uniformly inside `box` units, facing the lane.</summary>
        void DrawFitted(SpriteRenderer r, Sprite sprite, Vector2 box)
        {
            if (r.sprite != sprite) r.sprite = sprite;
            if (trapMaterial != null && r.sharedMaterial != trapMaterial) r.sharedMaterial = trapMaterial;
            r.drawMode = SpriteDrawMode.Simple;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
            r.color = Color.white;
            r.flipX = FacesLeft;
            if (sprite == null) return;
            Vector2 native = sprite.bounds.size;
            float k = Mathf.Min(box.x / Mathf.Max(native.x, 1e-4f), box.y / Mathf.Max(native.y, 1e-4f));
            r.transform.localScale = new Vector3(k, k, 1f);
        }
    }
}
