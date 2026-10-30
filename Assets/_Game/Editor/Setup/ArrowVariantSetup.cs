using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>D-236: bakes a level's arrow variations into an ArrowVariantPicker on its room, after the room and its trap art are
    /// built (LevelSetup): for each variant, the launchers it doesn't fire and their art (Art/&lt;name&gt;). Levels with no
    /// variations get no picker (a stale one is removed).</summary>
    public static class ArrowVariantSetup
    {
        static readonly Dictionary<string, System.Func<string[][]>> ByLevel = new()
        {
            ["L028"] = Parallax.Editor.Levels.L028Layout.VariantOff,
        };

        public static void Wire(Transform roomsParent, string levelId, RoomDeath death, List<string> changes)
        {
            if (levelId == null || !ByLevel.TryGetValue(levelId, out System.Func<string[][]> off))
            {
                foreach (ArrowVariantPicker stale in roomsParent.GetComponentsInChildren<ArrowVariantPicker>(true))
                {
                    changes.Add("removed ArrowVariantPicker on " + stale.name);
                    Object.DestroyImmediate(stale);
                }
                return;
            }
            string[][] names = off();
            Transform roomRoot = null;
            var variants = new ArrowVariantPicker.Variant[names.Length];
            for (int v = 0; v < names.Length; v++)
            {
                var objects = new List<GameObject>();
                foreach (string n in names[v])
                {
                    ArrowTrap trap = roomsParent.GetComponentsInChildren<ArrowTrap>(true).FirstOrDefault(t => t.name == n);
                    if (trap == null) { Debug.LogError($"ArrowVariantSetup: {levelId} has no launcher '{n}'; variant {v} left incomplete."); continue; }
                    roomRoot ??= trap.transform.parent;
                    objects.Add(trap.gameObject);
                    Transform art = trap.transform.parent.Find(Parallax.Editor.Art.TrapArtSetup.ArtRootName)?.Find(n);
                    if (art != null) objects.Add(art.gameObject);
                }
                variants[v] = new ArrowVariantPicker.Variant { off = objects.ToArray() };
            }
            if (roomRoot == null) { Debug.LogError($"ArrowVariantSetup: {levelId} has none of its variants' launchers; no picker baked."); return; }
            ArrowVariantPicker picker = SetupUtility.Ensure<ArrowVariantPicker>(roomRoot.gameObject, changes);
            picker.SetVariants(variants, death);   // D-238: a death re-rolls
            EditorUtility.SetDirty(picker);
            changes.Add($"baked {variants.Length} arrow variants on {roomRoot.name}");
        }
    }
}
