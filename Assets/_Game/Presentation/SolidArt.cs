using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    public enum SolidArtKind { FallingBlock, Mover, Shrinker }

    /// <summary>PAX-A13: a solid that passes for geometry and then moves or changes: a falling block, a moving floor (carry,
    /// slip, drop-and-return, the crusher, a push wall) or a shrinking floor. Its body is its host's skin (P10), drawn exactly
    /// over its grey-box's pose and size. On a world-tiled host (§12 R8) a block or a mover samples the tile at its authored
    /// pose, so it carries its own pixels as it moves; a shrinker samples where it stands, so it erodes in place. A block
    /// shows a crack from its release tick and a dust burst when it lands; a mover puffs dust at its leading edge while it
    /// moves; a shrinker sheds chips at its shrinking edges.</summary>
    public sealed class SolidArt : TrapArt, IHostSkinned
    {
        [SerializeField] RoomTrap trap;
        [SerializeField] SolidArtKind kind;
        [SerializeField] SpriteRenderer skin, greyboxBody;
        [SerializeField] HostSkin hostSkin;
        [Tooltip("The authored pose's pivot in world space: where a moving solid samples its host's tile.")]
        [SerializeField] Vector2 restPivot;
        [SerializeField] Vector2 moveDirection;
        [SerializeField] int landTick, moveTicks, holdTicks, returnTicks, shrinkTicks;
        [SerializeField] SpriteRenderer crack;
        [SerializeField] SpriteRenderer[] dust = new SpriteRenderer[0];

        public override RoomTrap Trap => trap;
        public HostSkin HostSkin => hostSkin;
        public SpriteRenderer Skin => skin;
        public SolidArtKind Kind => kind;

        public void Reskin(HostSkin newSkin)
        {
            hostSkin = newSkin;
            newSkin.ApplyTo(skin, greyboxBody != null ? greyboxBody.size : skin.size);
        }

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(skin, greyboxBody));

        protected override void CollectEffects(List<Effect> into)
        {
            if (crack != null) into.Add(new Effect("crack", crack));
            string group = kind == SolidArtKind.FallingBlock ? "land" : kind == SolidArtKind.Shrinker ? "erode" : "move";
            foreach (SpriteRenderer d in dust) into.Add(new Effect(group, d));
        }

        protected override void Render()
        {
            int s = TicksSinceFire;
            if (Mirror(skin, greyboxBody))
            {
                Vector2 size = greyboxBody.size;
                if (skin.drawMode != SpriteDrawMode.Simple) skin.size = size;
                if (hostSkin.WorldTiled)
                    hostSkin.SetSampling(skin, kind == SolidArtKind.Shrinker ? (Vector2)skin.transform.position : restPivot, skin.transform.lossyScale);
            }
            Vector2 at = greyboxBody != null ? (Vector2)greyboxBody.transform.position : (Vector2)transform.position;
            Vector2 half = greyboxBody != null ? greyboxBody.size * 0.5f : Vector2.one * 0.5f;
            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);

            // The block's crack: on it from the release tick until it has landed and settled.
            if (crack != null)
            {
                bool on = kind == SolidArtKind.FallingBlock && Shows(greyboxBody) && s >= 0 && s < landTick + TrapArtMath.CrackFadeTicks;
                crack.enabled = on;
                if (on)
                {
                    crack.transform.position = new Vector3(at.x, at.y, crack.transform.position.z);
                    Vector2 native = crack.sprite != null ? (Vector2)crack.sprite.bounds.size : Vector2.one;
                    crack.transform.localScale = new Vector3(half.x * 1.9f / native.x, half.y * 1.9f / native.y, 1f);
                    Color c = crack.color; c.a = s >= landTick ? Mathf.Clamp01(1f - (s - landTick) / (float)TrapArtMath.CrackFadeTicks) : 1f; crack.color = c;
                }
            }

            for (int i = 0; i < dust.Length; i++)
            {
                PuffPose p = default;
                Vector2 from = at;
                switch (kind)
                {
                    case SolidArtKind.FallingBlock:   // a burst at its landing face
                        if (s >= landTick && s < landTick + TrapArtMath.LandDustTicks) p = TrapArtMath.Puff(seed, 700 + i, s - landTick, TrapArtMath.LandDustTicks, 0.7f);
                        from = at + moveDirection * half.y + new Vector2((i / Mathf.Max(1f, dust.Length - 1f) - 0.5f) * half.x * 1.8f, 0f);
                        break;
                    case SolidArtKind.Mover:          // at the leading edge on the ticks it moves out or back (TrapMotion.MovingOffset)
                        int holdEnd = moveTicks + holdTicks;
                        bool goingOut = s >= 1 && s <= moveTicks, goingBack = returnTicks > 0 && s > holdEnd && s <= holdEnd + returnTicks;
                        int into = goingOut ? s - 1 : s - holdEnd - 1;
                        if (goingOut || goingBack) p = TrapArtMath.Puff(seed, 800 + i * 31 + into / TrapArtMath.SolidDustTicks, into % TrapArtMath.SolidDustTicks, TrapArtMath.SolidDustTicks, 0.35f);
                        Vector2 lead = goingBack ? -moveDirection : moveDirection;
                        from = at + new Vector2(lead.x * half.x, lead.y * half.y) + new Vector2(0f, -half.y * 0.8f);
                        break;
                    case SolidArtKind.Shrinker:       // chips at both shrinking edges
                        if (s >= 0 && s < shrinkTicks) p = TrapArtMath.Puff(seed, 900 + i * 31 + s / TrapArtMath.SolidDustTicks, s % TrapArtMath.SolidDustTicks, TrapArtMath.SolidDustTicks, 0.3f);
                        from = at + new Vector2((i % 2 == 0 ? -1f : 1f) * half.x, 0f);
                        break;
                }
                Place(dust[i], p.Visible, from + new Vector2(p.Offset.x, kind == SolidArtKind.Shrinker ? -p.Offset.y : p.Offset.y * 0.5f), 0.4f * p.Scale, 0.75f * p.Alpha);
            }
        }
    }
}
