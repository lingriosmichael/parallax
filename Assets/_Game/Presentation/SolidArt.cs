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
    /// shows a crack from its release tick and a dust burst when it lands, then lies as a heap of broken stone (rubble); a mover puffs dust at its leading edge while it
    /// moves; a shrinker sheds chips at its shrinking edges. PAX-102: a block in the room's roof leaves a hole (its socket)
    /// once it has left its place by its own height, until the room resets it.</summary>
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
        [Tooltip("PAX-102: the hole a roof block leaves, at its authored pose; null for any other block.")]
        [SerializeField] SpriteRenderer socket;
        [SerializeField] SpriteRenderer[] dust = new SpriteRenderer[0];
        [Tooltip("PAX-A18 (D-151): a slam's iron striking plate along its bottom edge (a child of the skin, so it moves and turns with it); null otherwise.")]
        [SerializeField] SpriteRenderer plate;
        [Tooltip("PAX-A18: a hinge floor's iron knuckle at its hinge end, inside the floor (a child of the skin); null otherwise.")]
        [SerializeField] SpriteRenderer knuckle;
        [Tooltip("The knuckle's centre along the floor, local units from its centre (the hinge end).")]
        [SerializeField] float knuckleAlong;
        [Tooltip("2026-10-17: a falling block's heap of broken stone (or a slam's that comes down and stays), shown instead of its " +
                 "skin from its landing tick (it stays down until its room resets); null for a grip wall and any other solid.")]
        [SerializeField] SpriteRenderer rubble;
        [Tooltip("The heap's scale: the block's size over the footprint its sprite was drawn for.")]
        [SerializeField] Vector2 rubbleScale = Vector2.one;

        public override RoomTrap Trap => trap;
        public HostSkin HostSkin => hostSkin;
        public SpriteRenderer Skin => skin;
        public SolidArtKind Kind => kind;

        public void Reskin(HostSkin newSkin)
        {
            hostSkin = newSkin;
            newSkin.ApplyTo(skin, greyboxBody != null ? greyboxBody.size : skin.size);
        }

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(skin, greyboxBody));
            if (plate != null) into.Add(new Body(plate, greyboxBody));
            if (knuckle != null) into.Add(new Body(knuckle, greyboxBody));
        }

        protected override void CollectEffects(List<Effect> into)
        {
            if (crack != null) into.Add(new Effect("crack", crack));
            if (socket != null) into.Add(new Effect("hole", socket));
            if (rubble != null) into.Add(new Effect("rubble", rubble));
            string group = kind == SolidArtKind.FallingBlock ? "land" : kind == SolidArtKind.Shrinker ? "erode" : "move";
            foreach (SpriteRenderer d in dust) into.Add(new Effect(group, d));
        }

        // PAX-A18 (D-151): the plate and the knuckle, at their one fixed size, shown exactly with the skin. The plate spans the
        // slam's width along its bottom edge (its foot pivot on that edge); the knuckle sits inside the floor at its hinge end,
        // as tall as the floor allows.
        void Fittings()
        {
            // 2026-10-18 visual review (L011-L018: "a bright warm studded brick strip ... pasted onto the dark ceiling undersides";
            // "the vertical launchers read as copper capsules or pipe fittings stuck on the stone"): the slam's plate and the hinge
            // floor's knuckle show from the trap's fire on; at rest it's the stone it is disguised as.
            bool shown = skin != null && skin.enabled && TicksSinceFire >= 0;
            Vector2 size = greyboxBody != null ? greyboxBody.size : Vector2.zero;
            if (plate != null)
            {
                plate.enabled = shown && plate.sprite != null;
                if (plate.enabled)
                {
                    plate.drawMode = SpriteDrawMode.Tiled;   // before the scale: switching draw mode rewrites a SpriteRenderer's scale
                    plate.tileMode = SpriteTileMode.Continuous;
                    float h = Mathf.Min(plate.sprite.bounds.size.y, size.y * 0.5f);
                    plate.transform.localScale = new Vector3(1f, h / plate.sprite.bounds.size.y, 1f);
                    plate.size = new Vector2(size.x, plate.sprite.bounds.size.y);
                    plate.transform.localPosition = new Vector3(0f, -size.y * 0.5f, plate.transform.localPosition.z);
                    plate.transform.localRotation = Quaternion.identity;
                }
            }
            if (knuckle != null)
            {
                knuckle.enabled = shown && knuckle.sprite != null;
                if (knuckle.enabled)
                {
                    Vector2 native = knuckle.sprite.bounds.size;
                    float k = Mathf.Min(1f, size.y * 0.96f / native.y, size.x * 0.3f / native.x);
                    knuckle.transform.localScale = new Vector3(k, k, 1f);
                    float along = Mathf.Clamp(knuckleAlong, -size.x * 0.5f + native.x * k * 0.5f, size.x * 0.5f - native.x * k * 0.5f);
                    knuckle.transform.localPosition = new Vector3(along, 0f, knuckle.transform.localPosition.z);
                    knuckle.transform.localRotation = Quaternion.identity;
                }
            }
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

            // 2026-10-17 (the developer: "Whatever falls needs to crash and look like a pile of rubble and not like a perfect
            // square"): from its landing tick the block is a heap of broken stone on its landing face (its collider is
            // unchanged: the heap keeps its top as a walkable top); its skin and trims go (the landing's dust covers the change).
            bool heap = rubble != null && rubble.sprite != null && Shows(greyboxBody) && s >= 0 && s >= landTick;
            if (rubble != null)
            {
                rubble.enabled = heap;
                if (heap)
                {
                    skin.enabled = false;
                    bool up = moveDirection.y > 0f;
                    float tall = rubble.sprite.bounds.size.y * rubbleScale.y;
                    rubble.flipY = up;
                    rubble.transform.SetPositionAndRotation(
                        new Vector3(at.x, up ? at.y + half.y - tall * 0.5f : at.y - half.y + tall * 0.5f, rubble.transform.position.z), Quaternion.identity);
                    rubble.transform.localScale = new Vector3(rubbleScale.x, rubbleScale.y, 1f);
                }
            }
            Fittings();   // after the heap: a slam's plate goes with its skin

            // The block's crack: on it from the release tick until it has landed and settled.
            if (crack != null)
            {
                bool on = kind == SolidArtKind.FallingBlock && Shows(greyboxBody) && s >= 0 && s < landTick + TrapArtMath.CrackFadeTicks && !heap;
                crack.enabled = on;
                if (on)
                {
                    crack.transform.position = new Vector3(at.x, at.y, crack.transform.position.z);
                    Vector2 native = crack.sprite != null ? (Vector2)crack.sprite.bounds.size : Vector2.one;
                    crack.transform.localScale = new Vector3(half.x * 1.9f / native.x, half.y * 1.9f / native.y, 1f);
                    Color c = crack.color; c.a = s >= landTick ? Mathf.Clamp01(1f - (s - landTick) / (float)TrapArtMath.CrackFadeTicks) : 1f; crack.color = c;
                }
            }

            // PAX-102: the roof block's hole, once the block is a whole height away from its place (the roof's own stone covers it
            // until then); gone again when the room resets it.
            if (socket != null)
                socket.enabled = kind == SolidArtKind.FallingBlock && greyboxBody != null && Vector2.Distance(at, restPivot) >= greyboxBody.size.y - 1e-3f;

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
