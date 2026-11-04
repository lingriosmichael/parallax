using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: an Obsidian flip plate (OT-21). It's painted danger side up, so one sprite is right both ways: turned over at rest
    /// (blades down, cyan on top) and upright once it has flipped. Its pivot is the plate's centre, under the blades.</summary>
    public sealed class FlipPlateArt : TrapArt
    {
        [SerializeField] MovingTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(.4f, 4f);

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (!Mirror(body, greyboxBody)) return;
            body.transform.rotation = greyboxBody.transform.rotation * Quaternion.Euler(0f, 0f, 180f);
            Vector2 native = body.sprite.bounds.size, size = greyboxBody.size;
            float x = size.x / native.x;
            body.transform.localScale = new Vector3(x, Mathf.Max(x, Mathf.Min(1.6f, size.y / (native.y * .6f))), 1f);
        }
    }
}
