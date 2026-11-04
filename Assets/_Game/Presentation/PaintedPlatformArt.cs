using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-292: an Obsidian platform with painted states, drawn over its collider. A phase platform (OT-20) shows lit, then fading
    /// through its warning, and only its end caps while it's gone. A glitch platform (OT-22) shows solid, breaking up through its warning,
    /// and a ghost where it will land. Each state is the same size.</summary>
    public sealed class PaintedPlatformArt : TrapArt
    {
        [SerializeField] MovingTrap trap;
        [SerializeField] SpriteRenderer body, greyboxBody, ghost, greyboxGhost, caps;
        [SerializeField] Sprite steady, warning;
        [Tooltip("Top-aligned: a flat top on the collider's, its rough underside hanging below (the glitch platform).")]
        [SerializeField] bool hangs;

        public override RoomTrap Trap => trap;
        public override Vector2 GrowthOf(Body b) => new(.2f, 3f);

        protected override void CollectBodies(List<Body> into)
        {
            into.Add(new Body(body, greyboxBody));
            if (ghost != null) into.Add(new Body(ghost, greyboxGhost));
        }

        protected override void CollectEffects(List<Effect> into) { if (caps != null) into.Add(new Effect("caps", caps)); }

        void Pose(SpriteRenderer r, Sprite sprite, SpriteRenderer over)
        {
            r.sprite = sprite;
            Vector2 native = sprite.bounds.size, size = over.size;
            float x = size.x / native.x;
            r.transform.localScale = new Vector3(x, hangs ? x : size.y / native.y, 1f);
            if (hangs)
            {
                Vector3 at = r.transform.position;
                r.transform.position = new Vector3(at.x, at.y + size.y * .5f, at.z);
            }
        }

        protected override void Render()
        {
            bool shown = Mirror(body, greyboxBody);
            if (shown) Pose(body, trap.InWarning ? warning : steady, greyboxBody);
            if (ghost != null && Mirror(ghost, greyboxGhost)) Pose(ghost, ghost.sprite, greyboxGhost);
            if (caps == null) return;
            caps.enabled = !shown && !trap.InWarning;
            if (caps.enabled)
            {
                caps.transform.position = new Vector3(greyboxBody.transform.position.x, greyboxBody.transform.position.y, caps.transform.position.z);
                Vector2 native = caps.sprite.bounds.size, size = greyboxBody.size;
                caps.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            }
        }
    }
}
