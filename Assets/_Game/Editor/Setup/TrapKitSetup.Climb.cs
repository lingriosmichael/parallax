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
    // PAX-089: the climbable vine builder (PAX-087), split out of TrapKitSetup.cs unchanged.
    public static partial class TrapKitSetup
    {
        // PAX-087 (D-089) R3: a climbable vine. Its body is a trigger box, the grab box (width 0.6); no renderer of its own.
        // Its look is the vine sprite stacked in Simple children "Segment_i", each scaled to the box's width, the top one
        // squashed so the vine ends exactly at its top. Not Tiled: the sprite's mesh is Tight and Tiled needs Full Rect
        // (the .meta is the art ticket's). A snap vine (configured settings) gets an Overlap trigger child from the
        // secondary box when it has one; with none it triggers on its own box.
        internal const string VineSpritePath = "Assets/_Game/Art/RealityA/Environment/A_OBJ_Vine.png";
        internal const int VineSortingOrder = -1;
        static readonly Color VinePlaceholder = new(.25f, .55f, .2f, 1f);

        internal static ClimbVine BuildClimbVineCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            GameObject go = CreateTrap<ClimbVine>(parent, root, name, position, size, Color.clear, roomId, rooms, death, observers, true, null, changes);
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            SetupUtility.SetColliderSize(box, size, changes);
            if (!box.isTrigger) { box.isTrigger = true; changes.Add("set " + name + ".isTrigger"); }
            SpriteRenderer[] segments = BuildVineSegments(go.transform, root, size, changes);
            bool snaps = settings.IsConfigured;
            BoxCollider2D trigger = snaps && settings.TriggerSource == TrapTriggerSource.Overlap && triggerSize != Vector2.zero
                ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            ClimbVine vine = go.GetComponent<ClimbVine>();
            Write(vine, changes, ("snaps", snaps), ("trigger", trigger), ("delayTicks", snaps ? settings.DelayTicks : 0));
            SerializedObject serialized = new(vine);
            SerializedProperty list = serialized.FindProperty("segments");
            bool same = list.arraySize == segments.Length;
            for (int i = 0; same && i < segments.Length; i++) same = list.GetArrayElementAtIndex(i).objectReferenceValue == segments[i];
            if (!same)
            {
                list.arraySize = segments.Length;
                for (int i = 0; i < segments.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(vine);
                changes.Add("wired " + name + ".segments");
            }
            return vine;
        }

        static SpriteRenderer[] BuildVineSegments(Transform vine, RealityRoot root, Vector2 size, List<string> changes)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VineSpritePath);
            if (sprite == null) Debug.LogError($"TrapKitSetup: no vine sprite at '{VineSpritePath}'; '{vine.name}' uses a placeholder colour.", vine);
            // Placeholder: one greybox segment the size of the box.
            Vector2 natural = sprite != null ? (Vector2)sprite.bounds.size : size;
            float scale = size.x / natural.x;
            float segment = natural.y * scale;
            int count = sprite != null ? Mathf.Max(1, Mathf.CeilToInt(size.y / segment - 1e-4f)) : 1;
            var renderers = new SpriteRenderer[count];
            float bottom = -size.y * .5f;
            for (int i = 0; i < count; i++)
            {
                float height = i < count - 1 ? segment : size.y - segment * (count - 1);
                Transform t = SetupUtility.EnsureChild(vine, "Segment_" + i, root.gameObject.layer, changes);
                SetupUtility.SetLocalPosition(t, new Vector2(0f, bottom + segment * i + height * .5f), changes);
                SpriteRenderer r;
                if (sprite == null) r = SetupUtility.SetVisual(t.gameObject, root, size, VinePlaceholder, changes);
                else
                {
                    Vector3 local = new(scale, scale * height / segment, 1f);
                    if (t.localScale != local) { t.localScale = local; changes.Add("set " + t.name + ".localScale"); }
                    r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                    if (r.sprite != sprite) { r.sprite = sprite; changes.Add("set " + t.name + ".sprite"); }
                    if (r.drawMode != SpriteDrawMode.Simple) { r.drawMode = SpriteDrawMode.Simple; changes.Add("set " + t.name + ".drawMode"); }
                    string layer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
                    if (r.sortingLayerName != layer) { r.sortingLayerName = layer; changes.Add("set " + t.name + ".sortingLayer"); }
                }
                if (r != null && r.sortingOrder != VineSortingOrder) { r.sortingOrder = VineSortingOrder; changes.Add("set " + t.name + ".sortingOrder"); }
                if (r != null && !r.enabled) { r.enabled = true; changes.Add("showed " + t.name); }
                renderers[i] = r;
            }
            // A shorter vine than last time: drop the segments it no longer has.
            for (int i = count; vine.Find("Segment_" + i) != null; i++)
            { Object.DestroyImmediate(vine.Find("Segment_" + i).gameObject); changes.Add("removed " + vine.name + ".Segment_" + i); }
            return renderers;
        }
    }
}
