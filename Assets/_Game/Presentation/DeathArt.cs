using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    public enum DeathKind { Spikes, Crush, Pierce, Lightning, Fall }

    /// <summary>PAX-A13: a room's death effects: while the room holds a death (D-058), one short impact at the cat by what
    /// killed it, so the killer reads (§3 rule 6): spikes, sparks and grit; crush, a dust burst; an arrow or spear, sparks; the
    /// storm, a flash and a scorch; a pit, a splash. Nothing delays control: it shows only inside the hold. Pure function of
    /// the held death (its killer, cause and tick) and the observer clock.</summary>
    public sealed class DeathArt : TrapArt
    {
        const int Life = 26;

        [SerializeField] RoomDeath death;
        [SerializeField] ObserverSet observers;
        [Tooltip("Hazards that are pit floors (a fall, drawn as a splash), not spikes.")]
        [SerializeField] Object[] pits = new Object[0];
        [SerializeField] Sprite spark, grit, dust, splash, flashSprite, scorchSprite;
        [SerializeField] SpriteRenderer[] puffs = new SpriteRenderer[0];

        public override RoomTrap Trap => null;
        protected override bool NeedsTrap => false;
        public DeathKind? Showing { get; private set; }

        protected override void CollectBodies(List<Body> into) { }
        protected override void CollectEffects(List<Effect> into) { foreach (SpriteRenderer p in puffs) into.Add(new Effect("death", p)); }

        public DeathKind KindOf(Object killer, DeathCause cause)
        {
            if (killer == null) return DeathKind.Fall;
            foreach (Object pit in pits) if (pit == killer) return DeathKind.Fall;
            return killer switch
            {
                StormCloudTrap => DeathKind.Lightning,
                ArrowTrap => DeathKind.Pierce,
                FallingBlockTrap => DeathKind.Crush,
                MovingTrap m => m.GetComponent<Hazard>() != null ? DeathKind.Spikes : DeathKind.Crush,
                _ => DeathKind.Spikes,
            };
        }

        protected override void Render()
        {
            bool holding = death != null && death.IsHolding && death.HoldObserver != null && death.HoldObserver.Cat != null;
            Showing = null;
            int t = holding && observers != null ? observers.Tick - death.HoldTick : -1;
            if (!holding || t < 0 || t >= Life) { foreach (SpriteRenderer p in puffs) if (p != null) p.enabled = false; return; }
            DeathKind kind = KindOf(death.HoldKiller, death.HoldCause);
            Showing = kind;
            Vector2 at = death.HoldObserver.Cat.transform.position;
            uint seed = TrapArtMath.Seed("death", death.HoldTick);
            for (int i = 0; i < puffs.Length; i++)
            {
                SpriteRenderer r = puffs[i];
                if (r == null) continue;
                (Sprite sprite, float spread, float scale, Color tint) = Look(kind, i);
                r.sprite = sprite; r.color = tint;
                PuffPose p = TrapArtMath.Puff(seed, 1200 + i, t, Life, spread);
                Vector2 offset = kind == DeathKind.Fall ? new Vector2(p.Offset.x, Mathf.Abs(p.Offset.y)) : p.Offset;
                Place(r, p.Visible && sprite != null, at + offset, scale * p.Scale, p.Alpha * tint.a);
            }
        }

        (Sprite, float, float, Color) Look(DeathKind kind, int i) => kind switch
        {
            DeathKind.Spikes => i % 2 == 0 ? (spark, 0.5f, 1.2f, new Color(1f, 0.9f, 0.6f, 1f)) : (grit, 0.45f, 0.35f, new Color(1f, 0.95f, 0.85f, 0.9f)),
            DeathKind.Crush => (dust, 0.9f, 0.8f, new Color(1f, 0.95f, 0.85f, 0.9f)),
            DeathKind.Pierce => (spark, 0.4f, 1.0f, new Color(1f, 0.92f, 0.65f, 1f)),
            DeathKind.Lightning => i == 0 ? (flashSprite, 0.05f, 2.4f, new Color(1f, 0.97f, 0.85f, 0.8f)) : i == 1 ? (scorchSprite, 0.02f, 1f, Color.white) : (spark, 0.55f, 1.1f, new Color(1f, 0.95f, 0.7f, 1f)),
            _ => (splash, 0.6f, 0.9f, new Color(1f, 1f, 1f, 0.9f)),
        };
    }
}
