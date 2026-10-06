using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    // PAX-093 (D-095): a floor that shrinks once it fires: its collider and its look narrow together, from one side or both,
    // over shrinkTicks, as a pure function of the ticks since the fire (MovingFloorMath). A cat on a part that disappears
    // falls. Its trigger is its own box widened by touchSkin (as a collapsing floor), or the child `trigger` when set. Rearm
    // and a reset restore the full floor; a checkpoint rewind restores the width by the same formula.
    // D-116 (the crush ledge): with dropDistance > 0 the floor also drops onto a cat under it. A cat in the space under what
    // is left of it (its width, dropDistance deep) starts the drop: the floor sinks DropTellNudge for dropTellTicks (the
    // tell), drops dropDistance over dropMoveTicks, holds, comes back over dropReturnTicks, and can drop again. A cat it
    // presses into by the crush depth dies (Crushed). The drop has its own start tick (snapshot ExtraInt), so a rewind
    // restores the pose by the same formula; the shrink keeps the trap's timing.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ShrinkingFloorTrap : RoomTrap
    {
        public const float DropTellNudge = .06f;

        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] int delayTicks;
        [SerializeField] int shrinkTicks = 30;
        [SerializeField] float minWidth;
        [SerializeField] ShrinkFrom shrinkFrom;
        [SerializeField] float touchSkin = .05f;
        [Header("D-116 the crush ledge (off at dropDistance 0)")]
        [SerializeField] float dropDistance;
        [SerializeField] int dropTellTicks = 8, dropMoveTicks = 6, dropHoldTicks = 40, dropReturnTicks = 30;
        [SerializeField] CrushConfig crushConfig;
        BoxCollider2D box; Rigidbody2D body; Vector2 fullSize, fullOffset, home; Vector3 visualHome; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        int dropStart = -1;

        public float Width => box != null ? box.size.x : 0f;
        public bool Drops => dropDistance > 0f;
        /// <summary>D-116: the room tick the drop's tell began, or -1 while it waits at home.</summary>
        public int DropStartTick => dropStart;

        protected override CatDeathKind DeclaredDeathKind => Drops ? CatDeathKind.Crushed : CatDeathKind.Default;

        protected override void Awake()
        {
            base.Awake(); box = GetComponent<BoxCollider2D>(); body = GetComponent<Rigidbody2D>();
            if (box == null || visual == null || Reality == null || observers == null || (Drops && (body == null || crushConfig == null)))
            { Debug.LogError($"ShrinkingFloorTrap '{name}': missing box, visual, RealityRoot or ObserverSet (or, for a drop, its Rigidbody2D or CrushConfig).", this); enabled = false; return; }
            fullSize = box.size; fullOffset = box.offset; visualHome = visual.transform.localPosition;
            if (Drops) { body.bodyType = RigidbodyType2D.Kinematic; home = body.position; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
        }

        protected override int DelayTicks => delayTicks;

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            bool touched;
            if (trigger != null) touched = IsLocalHumanOverlapping(trigger, observers, filter, results, out _);
            else { Bounds bounds = box.bounds; bounds.Expand(touchSkin * 2f); touched = IsLocalHumanOverlapping(bounds, observers, filter, results, out _); }
            StepTiming(touched);
            Apply(IsTimingEffectActive ? RoomLifeTick - LatestFireTick : 0);
            if (Drops) StepDrop();
        }

        // D-116: physics has resolved the cat against this tick's pose; a cat pressed into it by the crush depth dies before
        // the next MovePosition (as MovingTrap). A cat under what is left of the floor starts the drop.
        void StepDrop()
        {
            if (!box.enabled) return;
            Bounds pose = new(body.position + box.offset, box.size);
            if (dropStart >= 0 && TryGetLocalHumanColliderBounds(observers, out Bounds cat) && TrapMotion.Crushes(cat, pose, crushConfig.DefaultCrushDepth))
            { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            if (dropStart >= 0 && RoomLifeTick - dropStart >= DropTicks) dropStart = -1;
            if (dropStart < 0)
            {
                Bounds under = new(new Vector3(pose.center.x, pose.min.y - dropDistance * .5f), new Vector3(pose.size.x, dropDistance));
                if (IsLocalHumanOverlapping(under, observers, filter, results, out _)) dropStart = RoomLifeTick;
            }
            body.MovePosition(home + DropOffset(dropStart < 0 ? -1 : RoomLifeTick - dropStart));
        }

        int DropTicks => dropTellTicks + dropMoveTicks + Mathf.Max(0, dropHoldTicks) + Mathf.Max(0, dropReturnTicks);

        /// <summary>D-116: the drop's offset `ticks` after its tell began (none before it): the tell's nudge, then the drop,
        /// the hold and the return (TrapMotion.MovingOffset), the nudge easing out with the return.</summary>
        public Vector2 DropOffset(int ticks)
        {
            if (ticks < 0 || ticks >= DropTicks) return Vector2.zero;
            if (ticks < dropTellTicks) return new Vector2(0f, -DropTellNudge);
            return TrapMotion.MovingOffset(new Vector2(0f, -dropDistance), ticks - dropTellTicks, dropMoveTicks, dropHoldTicks, dropReturnTicks);
        }

        // The width `ticks` after the fire: the collider and the look narrow together, the staying edge in place; at no width
        // both are off.
        void Apply(int ticks)
        {
            if (box == null || visual == null) return;
            float width = MovingFloorMath.ShrinkWidth(ticks, shrinkTicks, fullSize.x, minWidth);
            float shift = MovingFloorMath.ShrinkCentreShift(shrinkFrom, fullSize.x, width);
            bool present = width > 1e-3f;
            box.size = new Vector2(Mathf.Max(width, 1e-3f), fullSize.y);
            box.offset = fullOffset + new Vector2(shift, 0f);
            box.enabled = present;
            visual.size = new Vector2(width, fullSize.y);
            visual.transform.localPosition = visualHome + new Vector3(shift, 0f, 0f);
            visual.enabled = present;
        }

        void PlaceDrop(int roomTick)
        {
            if (!Drops || body == null) return;
            Vector2 pose = home + DropOffset(dropStart < 0 ? -1 : roomTick - dropStart);
            body.position = pose;
            body.MovePosition(pose);
        }

        protected override void OnReset() { Apply(0); dropStart = -1; PlaceDrop(0); }
        protected override void OnTimingRearmed() => Apply(0);
        protected override void CaptureExtra(ref TrapSnapshot snapshot) => snapshot.ExtraInt = dropStart;
        // PAX-090 (D-091): the width at roomTick from its formula, as the live step leaves it; D-116: the drop's pose too.
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            Apply(IsTimingEffectActive ? roomTick - LatestFireTick : 0);
            dropStart = snapshot.ExtraInt;
            PlaceDrop(roomTick);
        }
    }
}
