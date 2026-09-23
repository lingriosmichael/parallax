using System.Collections.Generic;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-052 (D-071): camera-frame baking shared by LevelCameraSetup (template-only tuning of
    // background coverage) and LevelSetup's per-level regeneration (frame + background vertical
    // position). Kept separate from SoloRoomBuilder since it's camera/background-specific, not
    // room geometry.
    public static class LevelCameraBuilder
    {
        public static LevelCameraConfig EnsureConfig(string assetPath, List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(assetPath);
            if (config != null) return config;
            var created = ScriptableObject.CreateInstance<LevelCameraConfig>();
            AssetDatabase.CreateAsset(created, assetPath);
            changes.Add("created LevelCameraConfig asset");
            return created;
        }

        // worldFrame is the room's content bounds (SoloRoomBuilder.ComputeRoomBounds with
        // LevelCameraConfig.ViewMargin) translated to world space - same baking pattern D-059
        // uses for RoomManager.bounds, just a different margin and a different destination field.
        public static void BakeFrame(LevelCameraFollow camera, Bounds worldFrame, List<string> changes)
        {
            var serialized = new SerializedObject(camera);
            SerializedProperty centerProp = serialized.FindProperty("frameCenter");
            SerializedProperty sizeProp = serialized.FindProperty("frameSize");
            var centerValue = new Vector2(worldFrame.center.x, worldFrame.center.y);
            var sizeValue = new Vector2(worldFrame.size.x, worldFrame.size.y);
            if (centerProp.vector2Value == centerValue && sizeProp.vector2Value == sizeValue) return;
            centerProp.vector2Value = centerValue;
            sizeProp.vector2Value = sizeValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("baked " + camera.name + " camera frame");
        }

        // §2.3: regeneration only repositions the background/foreground layers vertically to the
        // baked frame. Horizontal placement and the tiled layers' height/tileOffsetY are set once
        // in the template by LevelCameraSetup, not here.
        public static void SetBackgroundVerticalPosition(RealityRoot root, Bounds worldFrame, List<string> changes)
        {
            // worldFrame is world space; these layers are direct children of RealityRoot_A, so
            // their localPosition must be converted through the root, not read off worldFrame
            // directly (only true by coincidence today since RealityRoot_A sits at world origin).
            float localCentreY = root.ToLocal(new Vector2(worldFrame.center.x, worldFrame.center.y)).y;
            string[] paths = { "Background/BG_00_Sky", "Background/BG_01_Far", "Background/MG_01_Mid", "Foreground/FG_01" };
            for (int i = 0; i < paths.Length; i++)
            {
                Transform layer = root.transform.Find(paths[i]);
                if (layer == null) continue;
                var position = new Vector2(layer.localPosition.x, localCentreY);
                SetupUtility.SetLocalPosition(layer, position, changes);
            }
        }
    }
}
