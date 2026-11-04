using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: a spiked crusher (GT-05): one painting of the block and its spikes, its top on the block's, scaled to its width.</summary>
    public sealed class CrusherArt : TrapArt
    {
        [SerializeField] MovingTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(.05f, 3f);

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (!Mirror(body, greyboxBody)) return;
            Vector2 size = greyboxBody.size;
            float k = size.x / body.sprite.bounds.size.x;
            body.transform.localScale = new Vector3(k, k, 1f);
            body.transform.position += Vector3.up * size.y * .5f;
        }
    }
}
