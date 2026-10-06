using System.Collections.Generic;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-093 (D-095): the floors that move. Movers, slide-aways, drop-and-return floors and push walls are MovingTrap Solids
    // (BuildMovingTrapCore) with a MovingFloorSettings; the shrinking floor is its own trap.
    public static partial class TrapKitSetup
    {
        // A MovingTrap's PAX-093 fields: its SurfaceMotion, its push opt-in and, for a PAX-093 floor only, its trigger delay
        // (a MovingTrap before PAX-093 never had one, and keeps none). The crush partner is wired by WireCrushPartners once
        // every element of the room is built.
        internal static void ConfigureMovingFloor(MovingTrap trap, SoloRoomTrapSettings settings, List<string> changes)
        {
            Write(trap, changes, ("surfaceMotion", (int)settings.Floor.Motion), ("pushes", settings.Floor.Pushes),
                ("delayTicks", settings.Floor.IsConfigured ? settings.DelayTicks : 0));
            // D-119: a hinge floor's hinge (its top corner at the hinged end) and turn; nothing for every other mover.
            if (!settings.Floor.Hinges) return;
            Vector2 half = trap.GetComponent<BoxCollider2D>().size * .5f;
            Write(trap, changes, ("hingeTicks", settings.Floor.HingeTicks), ("hingeSign", settings.Floor.HingeAtRight ? -1f : 1f),
                ("hingePivot", new Vector2(settings.Floor.HingeAtRight ? half.x : -half.x, half.y)));
        }

        internal static void WireCrushPartners(Transform roomRoot, SoloRoomDefinition room, List<string> changes)
        {
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Kind != SoloRoomElementKind.MovingTrap || string.IsNullOrEmpty(e.Settings.Floor.CrushPartner)) continue;
                MovingTrap trap = roomRoot.Find(e.Name)?.GetComponent<MovingTrap>();
                Collider2D partner = roomRoot.Find(e.Settings.Floor.CrushPartner)?.GetComponent<Collider2D>();
                if (trap == null) continue;
                if (partner == null) { Debug.LogError($"TrapKitSetup: push wall '{e.Name}' names crush partner '{e.Settings.Floor.CrushPartner}', which has no collider in {roomRoot.name}."); continue; }
                Write(trap, changes, ("crushPartner", partner));
            }
        }

        // The collider on the element's object (full size, not a trigger); the look on a child "<name>_Visual", so a one-sided
        // shrink can slide the look with the collider's offset.
        internal static ShrinkingFloorTrap BuildShrinkingFloorCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<ShrinkingFloorTrap>(parent, root, name, position, size, Color.clear, roomId, rooms, death, observers, false, sortingOrder, changes);
            Transform visualTransform = SetupUtility.EnsureChild(go.transform, name + "_Visual", root.gameObject.layer, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(visualTransform.gameObject, root, size, color, changes);
            if (sortingOrder.HasValue && visual.sortingOrder != sortingOrder.Value) { visual.sortingOrder = sortingOrder.Value; changes.Add("set " + visual.name + ".sortingOrder"); }
            BoxCollider2D trigger = triggerSize != Vector2.zero ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            ShrinkingFloorTrap trap = go.GetComponent<ShrinkingFloorTrap>();
            Write(trap, changes, ("visual", visual), ("trigger", trigger), ("delayTicks", settings.DelayTicks), ("shrinkTicks", settings.Shrink.ShrinkTicks),
                ("minWidth", settings.Shrink.MinWidth), ("shrinkFrom", (int)settings.Shrink.From));
            // D-116: a crush ledge also drops; it moves on a kinematic body, as a MovingTrap does. A plain shrinking floor
            // gets nothing new (every pin identical).
            if (settings.Shrink.Drops)
            {
                Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes); SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
                ShrinkSettings k = settings.Shrink;
                Write(trap, changes, ("dropDistance", k.DropDistance), ("dropTellTicks", k.DropTellTicks), ("dropMoveTicks", k.DropMoveTicks),
                    ("dropHoldTicks", k.DropHoldTicks), ("dropReturnTicks", k.DropReturnTicks),
                    ("crushConfig", UnityEditor.AssetDatabase.LoadAssetAtPath<CrushConfig>("Assets/_Game/Data/CrushConfig_Default.asset")));
            }
            return trap;
        }
    }
}
