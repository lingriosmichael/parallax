using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: a spinner (GT-01's blade on GT-02's hub) or a pendulum (GT-07 in three parts: its ring at the pivot, its shaft
    /// tiled to the arm's length, its head at the end). The art turns with the trap's arms; a blade shorter than the painting is the
    /// same blade cut off at its tip.</summary>
    public sealed class SpinnerArt : TrapArt
    {
        [SerializeField] SpinnerTrap trap;
        [SerializeField] SpriteRenderer hub, greyboxHub;
        [SerializeField] SpriteRenderer[] arms = new SpriteRenderer[0], greyboxArms = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer ring, head;
        [SerializeField] float armLength;
        [SerializeField] bool pendulum;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(1.2f, 3f);   // drawn at its one size around a thin hitbox, and turning

        protected override void CollectBodies(List<Body> into)
        {
            if (hub != null) into.Add(new Body(hub, greyboxHub));
            for (int i = 0; i < arms.Length; i++) into.Add(new Body(arms[i], greyboxArms[i]));
            if (ring != null) into.Add(new Body(ring, greyboxArms[0]));
            if (head != null) into.Add(new Body(head, greyboxArms[0]));
        }

        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (trap == null) return;
            Vector3 pivot = trap.transform.position;
            if (hub != null && Mirror(hub, greyboxHub)) hub.transform.localScale = Vector3.one;
            for (int i = 0; i < arms.Length; i++)
            {
                SpriteRenderer arm = arms[i];
                if (!Mirror(arm, greyboxArms[i])) continue;
                float angle = greyboxArms[i].transform.eulerAngles.z;
                Sprite s = arm.sprite;
                // Its pivot on the trap's; turned so the painting's own length lies along the arm.
                arm.transform.SetPositionAndRotation(new Vector3(pivot.x, pivot.y, arm.transform.position.z), Quaternion.Euler(0f, 0f, pendulum ? angle + 90f : angle));
                arm.size = pendulum ? new Vector2(s.bounds.size.x, armLength) : new Vector2(armLength + s.pivot.x / s.pixelsPerUnit, s.bounds.size.y);
            }
            if (!pendulum) return;
            bool shown = arms.Length > 0 && arms[0].enabled;
            if (ring != null) ring.enabled = shown;
            if (head != null) { head.enabled = shown; head.transform.localPosition = new Vector3(0f, -armLength, 0f); }
        }
    }
}
