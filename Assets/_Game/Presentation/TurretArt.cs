using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A20: an Obsidian turret's housing (OT-04), fitted uniformly inside its grey-box at BodyScale and shown with
    /// it: dark while it aims or rests, lit crimson while it locks and fires (TrapPath.TurretPhase on its ticks since fire).
    /// Its aim and shot line is a LaserArt of its own over the turret's line renderer.</summary>
    public sealed class TurretArt : TrapArt
    {
        [SerializeField] TurretTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody;
        [SerializeField] Sprite idle, locked;
        [SerializeField] int aimTicks, lockTicks, fireTicks;

        public override RoomTrap Trap => trap;

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (!Mirror(body, greyboxBody)) return;
            int phase = TrapPath.TurretPhase(TicksSinceFire, aimTicks, lockTicks, fireTicks);
            Fit(body, phase is 1 or 2 && locked != null ? locked : idle, greyboxBody.size * bodyScale);
        }
    }
}
