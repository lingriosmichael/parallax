using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: a pressure plate (GT-08), following the trap's look as it dips.</summary>
    public sealed class PlateArt : TrapArt
    {
        [SerializeField] PlateTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody;
        [SerializeField] float width;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(.2f, 2f);

        protected override void CollectBodies(List<Body> into) => into.Add(new Body(body, greyboxBody));
        protected override void CollectEffects(List<Effect> into) { }

        protected override void Render()
        {
            if (!Mirror(body, greyboxBody)) return;
            Fit(body, body.sprite, new Vector2(width, width));
        }
    }
}
