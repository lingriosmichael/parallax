using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: a snap vine. Until it snaps it is the environment's climbable vine, untouched (§11 R1). From the snap
    /// tick its pieces (copies of its own segments) fall and turn, and leaves (TRAP-13) flutter down.</summary>
    public sealed class VineArt : TrapArt
    {
        [SerializeField] ClimbVine trap;
        [SerializeField] SpriteRenderer[] segments = new SpriteRenderer[0];   // the vine's own segments (read only)
        [SerializeField] SpriteRenderer[] pieces = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] leaves = new SpriteRenderer[0];

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into) { }

        protected override void CollectEffects(List<Effect> into)
        {
            foreach (SpriteRenderer p in pieces) into.Add(new Effect("snap", p));
            foreach (SpriteRenderer l in leaves) into.Add(new Effect("snap", l));
        }

        protected override void Render()
        {
            int s = TicksSinceFire;
            bool falling = trap.IsSnapped && s >= 0 && s < TrapArtMath.VineFallTicks;
            uint seed = TrapArtMath.Seed(SeedName, trap.LatestFireTick);
            for (int i = 0; i < pieces.Length; i++)
            {
                SpriteRenderer p = pieces[i], source = i < segments.Length ? segments[i] : null;
                if (p == null) continue;
                p.enabled = falling && source != null;
                if (!p.enabled) continue;
                ShardPose pose = TrapArtMath.Shard(seed, i, s);
                p.sprite = source.sprite; p.drawMode = source.drawMode; p.size = source.size; p.flipX = source.flipX;
                Vector3 home = source.transform.position;
                p.transform.SetPositionAndRotation(new Vector3(home.x + pose.Offset.x, home.y + pose.Offset.y, p.transform.position.z), Quaternion.Euler(0f, 0f, pose.Rotation * 0.6f));
                p.transform.localScale = source.transform.lossyScale;
                Color c = source.color; c.a *= pose.Alpha; p.color = c;
            }
            Vector2 top = segments.Length > 0 && segments[0] != null ? (Vector2)segments[0].transform.position : (Vector2)transform.position;
            for (int i = 0; i < leaves.Length; i++)
            {
                PuffPose l = falling ? TrapArtMath.Puff(seed, 1100 + i, s, TrapArtMath.VineFallTicks, 0.6f) : default;
                float drop = falling ? 0.0012f * s * s : 0f;
                Place(leaves[i], l.Visible, top + new Vector2(l.Offset.x, -l.Offset.y * 0.3f - drop - i * 0.25f), 1f, l.Alpha);
                if (l.Visible) leaves[i].transform.rotation = Quaternion.Euler(0f, 0f, 40f * Mathf.Sin(s * 0.3f + i));
            }
        }
    }
}
