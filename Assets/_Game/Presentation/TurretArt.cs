using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A20, D-292: a turret: a fixed mount (GT-10 or OT-18) and an eye that turns with the turret's aim, shown with its grey-box:
    /// dark while it aims or rests, lit crimson while it locks and fires (TrapPath.TurretPhase on its ticks since fire). Both draw at their
    /// painted size. Its aim and shot line is a LaserArt of its own over the turret's line renderer.</summary>
    public sealed class TurretArt : TrapArt
    {
        [SerializeField] TurretTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody, mount;
        [SerializeField] float mountAngle;
        [SerializeField] Sprite idle, locked;
        [SerializeField] int aimTicks, lockTicks, fireTicks;

        public override RoomTrap Trap => trap;

        public override Vector2 GrowthOf(Body b) => new(.5f, 1f);   // the mount and eye are wider than the grey-box

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(body, greyboxBody));
            if (mount != null) into.Add(new Body(mount, greyboxBody));
        }
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (mount != null && Mirror(mount, greyboxBody)) mount.transform.rotation = Quaternion.Euler(0f, 0f, mountAngle);
            if (!Mirror(body, greyboxBody)) return;
            int phase = TrapPath.TurretPhase(TicksSinceFire, aimTicks, lockTicks, fireTicks);
            body.sprite = phase is 1 or 2 && locked != null ? locked : idle;
            body.transform.rotation = Quaternion.Euler(0f, 0f, trap.AimAngle);
        }
    }
}
