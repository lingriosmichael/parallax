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
    // PAX-089: the geyser builder (PAX-086), split out of TrapKitSetup.cs unchanged.
    public static partial class TrapKitSetup
    {
        // PAX-086 (D-088): the geyser. Its body is the vent (a trigger: the host Floor or Ceiling holds the cat), drawn over the
        // host in VentGrey; it turns the trap's tell colour through the tell and the eruption. Its child "Column" (the column's
        // box, from the vent's face along the push) shows only while erupting. Placeholder art.
        static readonly Color VentGrey = new(.30f, .30f, .34f, 1f);
        static readonly Color ColumnColor = new(.75f, .92f, 1f, .45f);
        internal const int VentSortingOrder = -1, ColumnSortingOrder = -1;

        internal static GeyserTrap BuildGeyserCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, GeyserSettings geyser, List<string> changes)
        {
            GeyserSettings g = geyser.Resolved;
            GameObject go = CreateTrap<GeyserTrap>(parent, root, name, position, size, VentGrey, roomId, rooms, death, observers, true, VentSortingOrder, changes);
            Vector2 push = GeyserMath.Push(g.Direction);
            Transform columnTransform = SetupUtility.EnsureChild(go.transform, "Column", root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(columnTransform, push * (size.y * .5f + g.ColumnHeight * .5f), changes);
            SpriteRenderer column = SetupUtility.SetVisual(columnTransform.gameObject, root, new Vector2(g.ColumnWidth, g.ColumnHeight), ColumnColor, changes);
            if (column != null)
            {
                if (column.sortingOrder != ColumnSortingOrder) { column.sortingOrder = ColumnSortingOrder; changes.Add("set " + name + ".Column.sortingOrder"); }
                if (column.enabled) { column.enabled = false; changes.Add("hid " + name + ".Column"); }
            }
            GeyserTrap trap = go.GetComponent<GeyserTrap>();
            Write(trap, changes, ("vent", go.GetComponent<SpriteRenderer>()), ("column", column), ("direction", (int)g.Direction),
                ("columnWidth", g.ColumnWidth), ("columnHeight", g.ColumnHeight), ("tellTicks", g.TellTicks), ("eruptTicks", g.EruptTicks), ("launchSpeed", g.LaunchSpeed));
            return trap;
        }
    }
}
