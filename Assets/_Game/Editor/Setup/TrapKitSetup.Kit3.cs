using System.Collections.Generic;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-123/124 (D-276): the trident and rotating chaser builders. Placeholder looks (PAX-A20 draws them in Obsidian levels): a
    // trident is a grey body hosted in its solid with three "Beam<i>" lines the trap sizes and turns; a chaser's object stays put
    // (its trigger a child of it), its "Pivot" child carries the hub, and its two "<name>_Arm<i>" lines are placed by the trap.
    public static partial class TrapKitSetup
    {
        static readonly Color TridentGrey = new(.25f, .25f, .3f, 1f), TridentRed = new(1f, .2f, .2f, .6f);
        static readonly Color ChaserWhite = new(.88f, .96f, 1f, 1f), ChaserHub = new(.55f, .7f, .8f, 1f);

        internal static TridentTrap BuildTridentCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, TridentSettings k, List<string> changes)
        {
            GameObject go = CreateTrap<TridentTrap>(parent, root, name, position, size, TridentGrey, roomId, rooms, death, observers, false, 0, changes, box: false);
            var lines = new Object[TridentTrap.Beams];
            for (int i = 0; i < TridentTrap.Beams; i++)
            {
                Transform t = SetupUtility.EnsureChild(go.transform, "Beam" + i, root.gameObject.layer, changes);
                SpriteRenderer line = SetupUtility.SetVisual(t.gameObject, root, new Vector2(1f, k.BeamWidth), TridentRed, changes);
                if (line.sortingOrder != 2) { line.sortingOrder = 2; changes.Add($"set {name}.Beam{i}.sortingOrder"); }
                line.enabled = false;
                lines[i] = line;
            }
            TridentTrap trap = go.GetComponent<TridentTrap>();
            SetupUtility.SetArray(trap, "lines", lines, changes);
            Write(trap, changes, ("warnTicks", k.WarnTicks), ("fireTicks", k.FireTicks), ("downAngle", k.DownAngle), ("spreadDegrees", k.SpreadDegrees),
                ("beamWidth", k.BeamWidth), ("range", k.Range));
            return trap;
        }

        // D-276 amendment 2: the chaser: as the rotating chaser's body (a pivot with its hub, two arm lines the trap places), and its
        // run in world x (the layout's are the room's local x).
        internal static ChaserTrap BuildChaserCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 localPosition, Vector2 hubSize, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            ChaserSettings k = settings.Chaser;
            GameObject go = CreateTrap<ChaserTrap>(parent, root, name, position, hubSize, Color.clear, roomId, rooms, death, observers, false, null, changes, box: false);
            Transform pivot = SetupUtility.EnsureChild(go.transform, "Pivot", root.gameObject.layer, changes);
            pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity; pivot.localScale = Vector3.one;
            Transform hub = SetupUtility.EnsureChild(pivot, name + "_Hub", root.gameObject.layer, changes);
            hub.localPosition = Vector3.zero; hub.localRotation = Quaternion.identity; hub.localScale = Vector3.one;
            SpriteRenderer hubLook = SetupUtility.SetVisual(hub.gameObject, root, hubSize, ChaserHub, changes);
            if (hubLook.sortingOrder != 1) { hubLook.sortingOrder = 1; changes.Add("set " + name + "_Hub.sortingOrder"); }
            var arms = new Object[ChaserTrap.Arms];
            for (int i = 0; i < ChaserTrap.Arms; i++)
            {
                Transform t = SetupUtility.EnsureChild(go.transform, name + "_Arm" + i, root.gameObject.layer, changes);
                SpriteRenderer arm = SetupUtility.SetVisual(t.gameObject, root, new Vector2(1f, k.ArmWidth), ChaserWhite, changes);
                if (arm.sortingOrder != 0) { arm.sortingOrder = 0; changes.Add($"set {name}_Arm{i}.sortingOrder"); }
                arms[i] = arm;
            }
            BoxCollider2D trigger = settings.TriggerSource == Parallax.Core.TrapTriggerSource.Overlap && triggerSize != Vector2.zero
                ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            float dx = position.x - localPosition.x;   // local x to world x
            var zones = new float[k.ZoneX?.Length ?? 0];
            for (int i = 0; i < zones.Length; i++) zones[i] = k.ZoneX[i] + dx;
            ChaserTrap trap = go.GetComponent<ChaserTrap>();
            SetupUtility.SetArray(trap, "arms", arms, changes);
            WriteFloats(trap, "zoneX", zones, changes);
            WriteFloats(trap, "zoneSpeed", k.ZoneSpeed ?? new float[0], changes);
            WriteFloats(trap, "swingTilts", k.SwingTilts ?? new float[0], changes);
            Write(trap, changes, ("minLag", k.MinLag), ("closeScale", k.CloseScale), ("swingSpeed", k.SwingSpeed),
                ("swingFromX", k.SwingFromX >= float.MaxValue * .5f ? float.MaxValue : k.SwingFromX + dx),
                ("parkFromX", k.ParkFromX >= float.MaxValue * .5f ? float.MaxValue : k.ParkFromX + dx), ("parkSpeed", k.ParkSpeed), ("afterSpeed", k.AfterSpeed));
            Write(trap, changes, ("trigger", trigger), ("pivot", pivot), ("warnTicks", k.WarnTicks), ("armWidth", k.ArmWidth), ("range", k.Range), ("endX", k.EndX + dx),
                ("maxLag", k.MaxLag), ("catchUpSpeed", k.CatchUpSpeed), ("catchUpFromX", k.CatchUpFromX + dx), ("spinX", k.SpinX >= float.MaxValue * .5f ? float.MaxValue : k.SpinX + dx), ("spinTurn", k.SpinTurn));
            return trap;
        }

        static void WriteFloats(Object target, string field, float[] values, List<string> changes)
        {
            var so = new UnityEditor.SerializedObject(target);
            UnityEditor.SerializedProperty p = so.FindProperty(field);
            bool same = p.arraySize == values.Length;
            for (int i = 0; same && i < values.Length; i++) same = Mathf.Approximately(p.GetArrayElementAtIndex(i).floatValue, values[i]);
            if (same) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).floatValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"set {target.name}.{field}");
        }

        internal static SweeperTrap BuildSweeperCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 hubSize, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            SweeperSettings k = settings.Sweeper;
            GameObject go = CreateTrap<SweeperTrap>(parent, root, name, position, hubSize, Color.clear, roomId, rooms, death, observers, false, null, changes, box: false);
            Transform pivot = SetupUtility.EnsureChild(go.transform, "Pivot", root.gameObject.layer, changes);
            pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity; pivot.localScale = Vector3.one;
            Transform hub = SetupUtility.EnsureChild(pivot, name + "_Hub", root.gameObject.layer, changes);
            hub.localPosition = Vector3.zero; hub.localRotation = Quaternion.identity; hub.localScale = Vector3.one;
            SpriteRenderer hubLook = SetupUtility.SetVisual(hub.gameObject, root, hubSize, ChaserHub, changes);
            if (hubLook.sortingOrder != 1) { hubLook.sortingOrder = 1; changes.Add("set " + name + "_Hub.sortingOrder"); }
            var arms = new Object[SweeperTrap.Arms];
            for (int i = 0; i < SweeperTrap.Arms; i++)
            {
                Transform t = SetupUtility.EnsureChild(go.transform, name + "_Arm" + i, root.gameObject.layer, changes);
                SpriteRenderer arm = SetupUtility.SetVisual(t.gameObject, root, new Vector2(k.ArmLength, k.ArmWidth), ChaserWhite, changes);
                if (arm.sortingOrder != 0) { arm.sortingOrder = 0; changes.Add($"set {name}_Arm{i}.sortingOrder"); }
                arms[i] = arm;
            }
            BoxCollider2D trigger = settings.TriggerSource == Parallax.Core.TrapTriggerSource.Overlap && triggerSize != Vector2.zero
                ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            SweeperTrap trap = go.GetComponent<SweeperTrap>();
            SetupUtility.SetArray(trap, "arms", arms, changes);
            Write(trap, changes, ("trigger", trigger), ("pivot", pivot), ("offset", settings.Offset), ("warnTicks", k.WarnTicks), ("moveTicks", settings.MoveTicks),
                ("delayTicks", settings.DelayTicks), ("armLength", k.ArmLength), ("armWidth", k.ArmWidth), ("degreesPerTick", k.DegreesPerTick), ("startAngle", k.StartAngle),
                ("spinFromTicks", k.SpinFromTicks), ("spinRampTicks", k.SpinRampTicks));
            return trap;
        }
    }
}
