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
    // PAX-089: the inverter builder and its cues (PAX-085), split out of TrapKitSetup.cs unchanged.
    public static partial class TrapKitSetup
    {
        // PAX-085 (D-087): the inverter. Its own body is a trigger box (Overlap on its own body), or a separate child box
        // (the element's secondary box), or neither matters (Chain). Honest: a cyan orb on the body; disguised: no body
        // visual (an invisible trigger, D-056 (4)). Two cue children, off until it fires: Cue_Ring (behind the cat) and
        // Cue_Mark (over it); InverterTrap moves them onto the cat while they're on. Placeholder art.
        static readonly Color InverterCyan = new(.20f, .85f, .90f, 1f);
        static readonly Color CueRingColor = new(.20f, .85f, .90f, .45f);
        static readonly Color CueMarkColor = new(.20f, .85f, .90f, 1f);
        internal static readonly Vector2 CueRingSize = new(1.5f, 1.1f), CueMarkSize = new(.7f, .2f);
        internal const int CueRingSortingOrder = -1, CueMarkSortingOrder = 1;

        internal static InverterTrap BuildInverterCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            bool disguised = settings.Inverter.Disguised;
            GameObject go = CreateTrap<InverterTrap>(parent, root, name, position, size, disguised ? Color.clear : InverterCyan, roomId, rooms, death, observers, true, -1, changes);
            BoxCollider2D trigger = triggerSize != Vector2.zero ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : go.GetComponent<BoxCollider2D>();
            SpriteRenderer ring = BuildCue(go.transform, root, "Cue_Ring", CueRingSize, CueRingColor, CueRingSortingOrder, changes);
            SpriteRenderer mark = BuildCue(go.transform, root, "Cue_Mark", CueMarkSize, CueMarkColor, CueMarkSortingOrder, changes);
            InverterTrap trap = go.GetComponent<InverterTrap>();
            Write(trap, changes, ("trigger", trigger), ("orb", disguised ? null : go.GetComponent<SpriteRenderer>()), ("ring", ring), ("mark", mark),
                ("durationTicks", settings.Inverter.Duration), ("delayTicks", settings.DelayTicks));
            return trap;
        }

        static SpriteRenderer BuildCue(Transform parent, RealityRoot root, string name, Vector2 size, Color color, int sortingOrder, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(transform, Vector2.zero, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(transform.gameObject, root, size, color, changes);
            if (visual == null) return null;
            if (visual.sortingOrder != sortingOrder) { visual.sortingOrder = sortingOrder; changes.Add("set " + name + ".sortingOrder"); }
            if (visual.enabled) { visual.enabled = false; changes.Add("hid " + parent.name + "." + name); }
            return visual;
        }
    }
}
