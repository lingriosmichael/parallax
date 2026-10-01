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
            if (SoloRoomSkin.Enabled && BuildSeamlessVine(vine, root, size, changes) is SpriteRenderer[] seamless) return seamless;
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
            DropSegmentsFrom(vine, count, changes);
            return renderers;
        }

        // A shorter vine than last time: drop the segments it no longer has.
        static void DropSegmentsFrom(Transform vine, int count, List<string> changes)
        {
            for (int i = count; vine.Find("Segment_" + i) != null; i++)
            { Object.DestroyImmediate(vine.Find("Segment_" + i).gameObject); changes.Add("removed " + vine.name + ".Segment_" + i); }
        }

        /// <summary>PAX-A15 §2.6 (PAX-A08's old item 15): the seamless vine from ENV-18, bottom to top: Segment_0 the tip,
        /// Segment_1 the middle drawn Tiled (the kit sprite is Full Rect), Segment_2 the anchor, all centred on the grab
        /// box, the anchor's top at the box's top. A vine under 3 u shrinks its anchor and tip to fit. Null when the kit's
        /// vine sprites are missing (the stacked A02 vine is used).</summary>
        static SpriteRenderer[] BuildSeamlessVine(Transform vine, RealityRoot root, Vector2 size, List<string> changes)
        {
            Sprite tip = Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_VineTip"), mid = Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_VineMid"), anchor = Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_VineAnchor");
            if (tip == null || mid == null || anchor == null) return null;
            float k = Mathf.Min(1f, size.y / 3.2f);
            float half = size.y * .5f, tipH = tip.bounds.size.y * k, anchorH = anchor.bounds.size.y * k;
            float midBottom = -half + tipH * 0.6f, midTop = half - anchorH * 0.5f;
            var parts = new (Sprite sprite, Vector2 centre, Vector2 drawSize, bool tiled)[]
            {
                (tip, new Vector2(0f, -half + tipH * .5f), tip.bounds.size * k, false),
                (mid, new Vector2(0f, (midBottom + midTop) * .5f), new Vector2(mid.bounds.size.x, Mathf.Max(0.1f, midTop - midBottom)), true),
                (anchor, new Vector2(0f, half - anchorH * .5f), anchor.bounds.size * k, false),
            };
            string layer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            var renderers = new SpriteRenderer[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                Transform t = SetupUtility.EnsureChild(vine, "Segment_" + i, root.gameObject.layer, changes);
                SetupUtility.SetLocalPosition(t, parts[i].centre, changes);
                SpriteRenderer r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                if (r.sprite != parts[i].sprite) { r.sprite = parts[i].sprite; changes.Add("set " + t.name + ".sprite"); }
                SpriteDrawMode mode = parts[i].tiled ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
                if (r.drawMode != mode) { r.drawMode = mode; changes.Add("set " + t.name + ".drawMode"); }
                // Scale after the draw mode: switching Simple/Tiled rewrites the transform's scale.
                Vector3 scale = parts[i].tiled ? Vector3.one : new Vector3(k, k, 1f);
                if (parts[i].tiled && r.size != parts[i].drawSize) { r.size = parts[i].drawSize; changes.Add("set " + t.name + ".size"); }
                if (t.localScale != scale) { t.localScale = scale; changes.Add("set " + t.name + ".localScale"); }
                if (r.sortingLayerName != layer) { r.sortingLayerName = layer; changes.Add("set " + t.name + ".sortingLayer"); }
                if (r.sortingOrder != VineSortingOrder) { r.sortingOrder = VineSortingOrder; changes.Add("set " + t.name + ".sortingOrder"); }
                if (!r.enabled) { r.enabled = true; changes.Add("showed " + t.name); }
                renderers[i] = r;
            }
            DropSegmentsFrom(vine, parts.Length, changes);
            return renderers;
        }
    }
}
