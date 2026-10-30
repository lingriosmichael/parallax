using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // PAX-089: the PAX-042/PAX-045 trap builders (collapsing floor, fake platform, gravity flip, hidden spikes, door retreat, falling block, moving trap), split out of TrapKitSetup.cs unchanged.
    public static partial class TrapKitSetup
    {
        internal static CollapsingFloorTrap BuildCollapsingFloorCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, int delayTicks, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<CollapsingFloorTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            CollapsingFloorTrap trap = go.GetComponent<CollapsingFloorTrap>();
            Write(trap, changes, ("delayTicks", delayTicks));
            return trap;
        }

        // PAX-080 (D-080): a fake platform is a CollapsingFloorTrap whose body is a trigger, so it never holds the
        // cat up; with delay 0 it vanishes on the first tick the cat touches it (the collapse's touchSkin). The
        // builder forces Overlap, Once, delay 0; ValidateFakePlatformSettings keeps the data from disagreeing.
        internal static readonly SoloRoomTrapSettings FakePlatformSettings = new(delayTicks: 0);

        internal static CollapsingFloorTrap BuildFakePlatformCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<CollapsingFloorTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (!box.isTrigger) { box.isTrigger = true; changes.Add("set " + name + ".isTrigger"); }
            CollapsingFloorTrap trap = go.GetComponent<CollapsingFloorTrap>();
            Write(trap, changes, ("delayTicks", 0));
            return trap;
        }

        internal static GravityFlipTrap BuildGravityFlipCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, GravityFlipMode mode, int delayTicks, bool rearmOnExit, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<GravityFlipTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            GravityFlipTrap trap = go.GetComponent<GravityFlipTrap>();
            Write(trap, changes, ("mode", (int)mode), ("delayTicks", delayTicks), ("rearmOnExit", rearmOnExit));
            return trap;
        }

        internal static HiddenSpikesTrap BuildHiddenSpikesCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, string triggerName, Vector2 triggerLocalPosition, Vector2 triggerSize, int revealDelayTicks, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<HiddenSpikesTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            Hazard hazard = SetupUtility.Ensure<Hazard>(go, changes);
            Write(hazard, changes, ("observers", (Object)observers), ("roomDeath", death), ("rooms", rooms), ("armed", false), ("deathKind", (int)CatDeathKind.Spiked));
            BoxCollider2D trigger = CreateTrigger(go.transform, root, triggerName, triggerLocalPosition, triggerSize, changes);
            HiddenSpikesTrap trap = go.GetComponent<HiddenSpikesTrap>();
            Write(trap, changes, ("hazard", hazard), ("trigger", trigger), ("revealDelayTicks", revealDelayTicks));
            return trap;
        }

        // PAX-112 (D-176): the pulse gate. Its body is the barrier (a trigger box with a Hazard, disarmed until On), drawn in
        // `color` while on and flickering faint through the warning; nothing shows while it is off. Periodic only, so it has no
        // trigger, and its cooldown is its warning plus its on time (the base timing rearms it then). Placeholder art.
        internal static PulseGateTrap BuildPulseGateCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, PulseGateSettings gate, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<PulseGateTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            Hazard hazard = SetupUtility.Ensure<Hazard>(go, changes);
            Write(hazard, changes, ("observers", (Object)observers), ("roomDeath", death), ("rooms", rooms), ("armed", false), ("deathKind", (int)CatDeathKind.Zapped));
            PulseGateTrap trap = go.GetComponent<PulseGateTrap>();
            Write(trap, changes, ("hazard", hazard), ("warnTicks", gate.WarnTicks), ("onTicks", gate.OnTicks));
            return trap;
        }

        // PAX-112 (D-176): ConfigureTiming, then the cooldown a gate's cycle implies (warn + on), whatever the layout gave.
        internal static void ConfigurePulseGateTiming(PulseGateTrap trap, SoloRoomTrapSettings settings, Transform roomRoot, List<string> changes)
        {
            ConfigureTiming(trap, settings, roomRoot, changes);
            Write(trap, changes, ("cooldownTicks", settings.Gate.WarnTicks + settings.Gate.OnTicks));
        }

        internal static DoorRetreatTrap BuildDoorRetreatCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, Transform doorRoot, Vector2 offset, int moveTicks, int delayTicks, List<string> changes)
        {
            GameObject go = CreateTrap<DoorRetreatTrap>(parent, root, name, position, size, Color.clear, roomId, rooms, death, observers, true, null, changes);
            DoorRetreatTrap trap = go.GetComponent<DoorRetreatTrap>();
            Write(trap, changes, ("trigger", go.GetComponent<BoxCollider2D>()), ("doorRoot", doorRoot), ("offset", offset), ("moveTicks", moveTicks), ("delayTicks", delayTicks));
            return trap;
        }

        internal static FallingBlockTrap BuildFallingBlockCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, string triggerName, Vector2 triggerLocalPosition, Vector2 triggerSize, FallingBlockDirection direction, int delayTicks, float unitsPerTick, float travelDistance, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<FallingBlockTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes);
            SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D block = go.GetComponent<BoxCollider2D>();
            block.isTrigger = false;
            BoxCollider2D trigger = CreateTrigger(go.transform, root, triggerName, triggerLocalPosition, triggerSize, changes);
            FallingBlockTrap trap = go.GetComponent<FallingBlockTrap>();
            Write(trap, changes, ("trigger", trigger), ("direction", (int)direction), ("delayTicks", delayTicks), ("unitsPerTick", unitsPerTick), ("travelDistance", travelDistance));
            return trap;
        }

        internal static MovingTrap BuildMovingTrapCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, Vector2 triggerLocalPosition, Vector2 triggerSize, SoloRoomTrapSettings settings, CrushConfig crushConfig, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<MovingTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            BoxCollider2D bodyBox = go.GetComponent<BoxCollider2D>();
            bool shouldBeTrigger = settings.MovingKind == MovingTrapKind.Hazard;
            if (bodyBox.isTrigger != shouldBeTrigger) { bodyBox.isTrigger = shouldBeTrigger; changes.Add("set " + name + ".isTrigger"); }
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes); SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D trigger = settings.TriggerSource == TrapTriggerSource.Overlap ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            MovingTrap trap = go.GetComponent<MovingTrap>();
            Write(trap, changes, ("trigger", trigger), ("kind", (int)settings.MovingKind), ("offset", settings.Offset), ("moveTicks", settings.MoveTicks), ("holdTicks", settings.HoldTicks), ("returnTicks", settings.ReturnTicks), ("crushDepth", settings.CrushDepth), ("crushConfig", crushConfig));
            ConfigureMovingFloor(trap, settings, changes);   // PAX-093 (D-095)
            Write(trap, changes, ("warnTicks", settings.Scan.IsConfigured ? settings.Scan.WarnTicks : 0));   // PAX-113 (D-178)
            ConfigurePath(trap, root, size, settings, changes);   // PAX-114 (D-187)
            return trap;
        }
    }
}
