using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: a spiked underside (GT-04), tiled along a mover's face and turning with it, its band against the mover.</summary>
    public sealed class FaceStripArt : TrapArt
    {
        [SerializeField] MovingTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(.1f, 3f);

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (!Mirror(body, greyboxBody)) return;
            Vector2 size = greyboxBody.size;
            body.size = new Vector2(size.x, body.sprite.bounds.size.y);
            body.transform.position += greyboxBody.transform.up * size.y * .5f;
        }
    }
}
