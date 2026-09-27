using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    // PAX-093 (D-095): a floor that shrinks once it fires: its collider and its look narrow together, from one side or both,
    // over shrinkTicks, as a pure function of the ticks since the fire (MovingFloorMath). A cat on a part that disappears
    // falls. Its trigger is its own box widened by touchSkin (as a collapsing floor), or the child `trigger` when set. Rearm
    // and a reset restore the full floor; a checkpoint rewind restores the width by the same formula.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ShrinkingFloorTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] int delayTicks;
        [SerializeField] int shrinkTicks = 30;
        [SerializeField] float minWidth;
        [SerializeField] ShrinkFrom shrinkFrom;
        [SerializeField] float touchSkin = .05f;
        BoxCollider2D box; Vector2 fullSize, fullOffset; Vector3 visualHome; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];

        public float Width => box != null ? box.size.x : 0f;

        protected override void Awake()
        {
            base.Awake(); box = GetComponent<BoxCollider2D>();
            if (box == null || visual == null || Reality == null || observers == null)
            { Debug.LogError($"ShrinkingFloorTrap '{name}': missing box, visual, RealityRoot or ObserverSet.", this); enabled = false; return; }
            fullSize = box.size; fullOffset = box.offset; visualHome = visual.transform.localPosition;
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

        protected override void OnReset() => Apply(0);
        protected override void OnTimingRearmed() => Apply(0);
        // PAX-090 (D-091): the width at roomTick from its formula, as the live step leaves it.
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Apply(IsTimingEffectActive ? roomTick - LatestFireTick : 0);
    }
}
