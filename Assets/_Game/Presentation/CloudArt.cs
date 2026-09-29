using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: the storm cloud. Asleep it passes for a sky cloud (TRAP-09: a little denser, a darker belly; §11 R2).
    /// From the wake tick it darkens; through the 25-tick charge it darkens further and flickers, a glow gathers under it and
    /// the target line shows exactly where the grey-box's does. The strike is a jagged bolt drawn inside the grey-box's strike
    /// column (seeded by the strike's number, so every strike differs and a rewind replays the same one), with a flash and a
    /// scorch where it lands, for the strike's ticks.</summary>
    public sealed class CloudArt : TrapArt
    {
        static readonly Color Asleep = Color.white, Woken = new(0.86f, 0.83f, 0.84f, 1f), Charged = new(0.58f, 0.54f, 0.58f, 1f);

        [SerializeField] StormCloudTrap trap;
        [SerializeField] SpriteRenderer cloud, greyboxCloud, target, greyboxTarget, greyboxBolt;
        [SerializeField] SpriteRenderer[] bolt = new SpriteRenderer[0];
        [SerializeField] Sprite cloudSprite, targetSprite;
        [SerializeField] Vector2 cloudSize;
        [SerializeField] int firstStrikeDelay, strikePeriod, tellTicks;
        [SerializeField] SpriteRenderer chargeGlow, flash, scorch;

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(cloud, greyboxCloud));
            into.Add(new Body(target, greyboxTarget));
            foreach (SpriteRenderer b in bolt) into.Add(new Body(b, greyboxBolt));
        }

        protected override void CollectEffects(List<Effect> into)
        {
            into.Add(new Effect("charge", chargeGlow));
            into.Add(new Effect("flash", flash));
            into.Add(new Effect("scorch", scorch));
        }

        protected override void Render()
        {
            StormCloudPhase phase = trap.Phase;
            int s = TicksSinceFire;
            int inCycle = s >= firstStrikeDelay && strikePeriod > 0 ? (s - firstStrikeDelay) % strikePeriod : -1;
            int strike = s >= firstStrikeDelay && strikePeriod > 0 ? (s - firstStrikeDelay) / strikePeriod : -1;

            if (Mirror(cloud, greyboxCloud))
            {
                Fit(cloud, cloudSprite, cloudSize * 0.96f);
                float churn = phase == StormCloudPhase.Charge ? 0.5f + 0.5f * Mathf.Sin(inCycle * 1.4f) : 0f;
                cloud.color = phase switch
                {
                    StormCloudPhase.Dormant => Asleep,
                    StormCloudPhase.Follow => Woken,
                    StormCloudPhase.Charge => Color.Lerp(Charged, Woken, 0.35f * churn),
                    _ => Color.Lerp(Charged, Color.white, 0.5f),
                };
            }
            if (Mirror(target, greyboxTarget))
            {
                target.sprite = targetSprite; target.drawMode = SpriteDrawMode.Sliced; target.transform.localScale = Vector3.one;
                target.size = greyboxTarget.size;
                Color c = target.color; c.a = 0.35f + 0.25f * Mathf.Sin(inCycle * 0.9f); target.color = c;
            }

            // The bolt: a zig-zag of segments from the cloud's belly to the strike's bottom, inside the column.
            bool striking = greyboxBolt != null && Shows(greyboxBolt);
            Bounds column = greyboxBolt != null ? greyboxBolt.bounds : new Bounds();
            uint seed = TrapArtMath.Seed(SeedName, strike);
            int n = bolt.Length;
            for (int i = 0; i < n; i++)
            {
                if (!Mirror(bolt[i], greyboxBolt) || !striking) continue;
                // Inset by half a segment's drawn thickness, so a short strike (onto a roof) keeps every segment in its column.
                float inset = Mathf.Min(column.extents.y * 0.45f, (bolt[i].sprite != null ? bolt[i].sprite.bounds.size.x * 1.1f * 0.5f : 0.05f) + 0.02f);
                float top = column.max.y - inset, span = column.size.y - 2f * inset;
                float y0 = top - span * i / n, y1 = top - span * (i + 1) / n;
                float x0 = column.center.x + (i == 0 ? 0f : TrapArtMath.HashSigned(seed, i) * column.extents.x * 0.45f);
                float x1 = column.center.x + (i == n - 1 ? TrapArtMath.HashSigned(seed, 99) * column.extents.x * 0.2f : TrapArtMath.HashSigned(seed, i + 1) * column.extents.x * 0.45f);
                Vector2 a = new(x0, y0), b = new(x1, y1), d = b - a;
                Transform t = bolt[i].transform;
                t.SetPositionAndRotation(new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, t.position.z), Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f));
                float native = bolt[i].sprite != null ? bolt[i].sprite.bounds.size.y : 1f;
                t.localScale = new Vector3(1.1f, d.magnitude / native * 1.02f, 1f);
            }

            bool charging = phase == StormCloudPhase.Charge && greyboxCloud != null;
            Vector2 belly = greyboxCloud != null ? (Vector2)greyboxCloud.transform.position - new Vector2(0f, cloudSize.y * 0.45f) : (Vector2)transform.position;
            float gather = charging ? Mathf.Clamp01((inCycle + 1f) / Mathf.Max(1, tellTicks)) : 0f;
            Place(chargeGlow, charging, belly, 0.8f + 1.2f * gather, charging ? 0.3f + 0.6f * gather : 0f);
            Place(flash, striking, striking ? (Vector2)column.center : belly, striking ? Mathf.Max(2f, column.size.y * 0.9f) : 1f, striking ? 0.55f : 0f);
            Place(scorch, striking, striking ? new Vector2(column.center.x, column.min.y + 0.1f) : belly, 1f, striking ? 0.9f : 0f);
        }
    }
}
