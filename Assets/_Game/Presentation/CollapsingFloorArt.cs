using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: a collapsing floor or fake platform. Until it gives way it draws exactly its host's skin (P10);
    /// from the tick its grey-box hides, it breaks into shards drawn with that same skin, which drop and spin, with dust
    /// (TrapArtMath, seeded by the element and its fire tick, so a rewind replays the same crumble). On a world-tiled host
    /// (§12 R8) the skin and every shard sample the host's tile in world space at their rest pose, so the reveal frame, when
    /// the shards are still in place, is pixel-identical to the frame before, and each shard carries its own pixels.</summary>
    public sealed class CollapsingFloorArt : TrapArt, IHostSkinned
    {
        const int DustLife = 28;
        const float DustSpread = 0.45f;

        [SerializeField] CollapsingFloorTrap trap;
        [SerializeField] SpriteRenderer skin;
        [SerializeField] SpriteRenderer greyboxVisual;
        [SerializeField] HostSkin hostSkin;
        [Tooltip("The shards, drawn with the host skin; each one's home is its centre relative to the floor's centre.")]
        [SerializeField] SpriteRenderer[] shards = new SpriteRenderer[0];
        [SerializeField] Vector2[] shardHomes = new Vector2[0];
        [SerializeField] Vector2[] shardSizes = new Vector2[0];
        [SerializeField] SpriteRenderer[] dust = new SpriteRenderer[0];
        [SerializeField] Vector2 size;


        public override RoomTrap Trap => trap;
        public HostSkin HostSkin => hostSkin;
        public SpriteRenderer Skin => skin;

        public void Reskin(HostSkin newSkin)
        {
            hostSkin = newSkin;
            newSkin.ApplyTo(skin, size);

            for (int i = 0; i < shards.Length; i++) if (shards[i] != null) newSkin.ApplyTo(shards[i], ShardSize(i));
        }

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(skin, greyboxVisual));

        protected override void CollectEffects(List<Effect> into)
        {
            foreach (SpriteRenderer s in shards) into.Add(new Effect("shard", s));

            foreach (SpriteRenderer d in dust) into.Add(new Effect("crumble", d));
        }

        Vector2 ShardSize(int i) => i < shardSizes.Length ? shardSizes[i] : (shards[i] != null ? shards[i].size : Vector2.one);

        protected override void Render()
        {
            Mirror(skin, greyboxVisual);
            Vector2 centre = greyboxVisual != null ? (Vector2)greyboxVisual.transform.position : (Vector2)transform.position;
            if (hostSkin.WorldTiled) hostSkin.SetSampling(skin, skin.transform.position, skin.transform.lossyScale);
            int s = TicksSinceFire;
            bool crumbling = !Shows(greyboxVisual) && s >= 0 && s < TrapArtMath.CrumbleTicks;

            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);
            for (int i = 0; i < shards.Length; i++)
            {
                SpriteRenderer r = shards[i];
                if (r == null) continue;
                ShardPose pose = crumbling ? TrapArtMath.Shard(seed, i, s) : default;
                r.enabled = pose.Visible;
                if (!pose.Visible) continue;
                Vector2 home = centre + (i < shardHomes.Length ? shardHomes[i] : Vector2.zero);
                r.transform.SetPositionAndRotation(new Vector3(home.x + pose.Offset.x, home.y + pose.Offset.y, r.transform.position.z), Quaternion.Euler(0f, 0f, pose.Rotation));
                // Its own pixels: sampled where it sat, frozen from the reveal tick on (§12 R8).
                if (hostSkin.WorldTiled) hostSkin.SetSampling(r, home, r.transform.lossyScale);
                Color c = hostSkin.Color; c.a *= pose.Alpha; r.color = c;
            }
            for (int i = 0; i < dust.Length; i++)
            {
                SpriteRenderer r = dust[i];
                if (r == null) continue;
                PuffPose puff = crumbling ? TrapArtMath.Puff(seed, 100 + i, s, DustLife, DustSpread) : default;
                r.enabled = puff.Visible;
                if (!puff.Visible) continue;
                float along = dust.Length > 1 ? (i / (dust.Length - 1f) - 0.5f) * size.x * 0.9f : 0f;
                r.transform.position = new Vector3(centre.x + along + puff.Offset.x * 0.6f, centre.y - size.y * 0.5f - puff.Offset.y * 0.5f, r.transform.position.z);
                r.transform.localScale = Vector3.one * (0.55f * puff.Scale);
                Color c = r.color; c.a = 0.85f * puff.Alpha; r.color = c;
            }
        }
    }
}
