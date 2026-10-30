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
                ("delayTicks", settings.Floor.IsConfigured ? settings.DelayTicks : 0), ("riseFirst", settings.Floor.RiseFirst), ("slams", settings.Floor.Slams),
                ("flipTicks", settings.Floor.FlipTicks), ("flipWarnTicks", settings.Floor.FlipWarnTicks),   // PAX-115 (D-187)
                ("phaseOnTicks", settings.Floor.PhaseOnTicks), ("phaseWarnTicks", settings.Floor.PhaseWarnTicks));   // PAX-122 (D-201)
            ConfigureSpikedFace(trap, settings, changes);
            // D-119: a hinge floor's hinge (its top corner at the hinged end) and turn; nothing for every other mover.
            if (!settings.Floor.Hinges) return;
            Vector2 half = trap.GetComponent<BoxCollider2D>().size * .5f;
            Write(trap, changes, ("hingeTicks", settings.Floor.HingeTicks), ("hingeSign", settings.Floor.HingeAtRight ? -1f : 1f),
                ("hingePivot", new Vector2(settings.Floor.HingeAtRight ? half.x : -half.x, half.y)));
        }

        // PAX-107 (D-124): a spiked hinge floor's face, a grey-box strip (0.3 u) on its top face, a child so it turns with the
        // floor; the trap shows it from the swing and the art mirrors it. Every other mover has none (a stale one is removed).
        static void ConfigureSpikedFace(MovingTrap trap, SoloRoomTrapSettings settings, List<string> changes)
        {
            string name = trap.name + "_Spikes";
            Transform existing = trap.transform.Find(name);
            // PAX-115 (D-187): a flip platform's face is its underside, shown always (the trap keeps it on). D-189: so is a spiked
            // slam's (a falling spiked ceiling).
            bool flips = settings.Floor.Flips || (settings.Floor.Spiked && settings.Floor.Slams);
            if (!flips && (!settings.Floor.Spiked || !settings.Floor.Hinges))
            {
                if (existing != null) { Object.DestroyImmediate(existing.gameObject); changes.Add("removed " + name); }
                Write(trap, changes, ("spikes", (SpriteRenderer)null));
                return;
            }
            RealityRoot root = trap.GetComponentInParent<RealityRoot>();
            Vector2 size = trap.GetComponent<BoxCollider2D>().size;
            Transform face = SetupUtility.EnsureChild(trap.transform, name, trap.gameObject.layer, changes);
            face.localPosition = new Vector3(0f, (flips ? -1f : 1f) * (size.y * .5f + SpikeFaceHeight * .5f), 0f); face.localRotation = Quaternion.identity; face.localScale = Vector3.one;
            SpriteRenderer strip = SetupUtility.SetVisual(face.gameObject, root, new Vector2(size.x, SpikeFaceHeight), new Color(.85f, .15f, .15f), changes);
            SpriteRenderer floor = trap.GetComponent<SpriteRenderer>();
            if (floor != null && strip.sortingOrder != floor.sortingOrder) { strip.sortingOrder = floor.sortingOrder; changes.Add("set " + name + ".sortingOrder"); }
            strip.enabled = flips;
            Write(trap, changes, ("spikes", strip));
        }

        // PAX-114 (D-187): a mover's path and glitch settings, and a glitch wall's ghost: a translucent copy of its box (a child,
        // hidden; the trap moves it to the next point and shows it through each warning). Every other mover has none.
        internal static void ConfigurePath(MovingTrap trap, RealityRoot root, Vector2 size, SoloRoomTrapSettings settings, List<string> changes)
        {
            PathSettings path = settings.Path;
            Write(trap, changes, ("path", path.HasPoints ? path.Points : new Vector2[0]), ("glitch", path.HasPoints && path.Glitch), ("glitchWarnTicks", path.HasPoints && path.Glitch ? path.WarnTicks : 0),
                ("pathFirstLegTicks", path.HasPoints && !path.Glitch ? path.FirstLegTicks : 0));   // D-250
            string name = trap.name + "_Ghost";
            Transform existing = trap.transform.Find(name);
            if (!path.HasPoints || !path.Glitch)
            {
                if (existing != null) { Object.DestroyImmediate(existing.gameObject); changes.Add("removed " + name); }
                Write(trap, changes, ("ghost", (SpriteRenderer)null));
                return;
            }
            Transform ghost = SetupUtility.EnsureChild(trap.transform, name, trap.gameObject.layer, changes);
            ghost.localPosition = Vector3.zero; ghost.localRotation = Quaternion.identity; ghost.localScale = Vector3.one;
            SpriteRenderer look = SetupUtility.SetVisual(ghost.gameObject, root, size, GhostColor, changes);
            SpriteRenderer body = trap.GetComponent<SpriteRenderer>();
            if (body != null && look.sortingOrder != body.sortingOrder + 1) { look.sortingOrder = body.sortingOrder + 1; changes.Add("set " + name + ".sortingOrder"); }
            look.enabled = false;
            Write(trap, changes, ("ghost", look));
        }

        static readonly Color GhostColor = new(.3f, 1f, .9f, .4f);

        internal const float SpikeFaceHeight = .3f;

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
