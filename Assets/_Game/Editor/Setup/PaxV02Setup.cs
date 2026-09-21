using System;
using System.Collections.Generic;
using Parallax.Gameplay;
using Parallax.Gameplay.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class PaxV02Setup
    {
        const byte OpaqueAlphaThreshold = 25;
        const float MaximumSupportDistance = 2f;

        static readonly SeatingTarget[] SeatingTargets =
        {
            new SeatingTarget("RealityRoot_A", "Checkpoints/Checkpoint_0"),
            new SeatingTarget("RealityRoot_A", "Interactables/Station_A"),
            new SeatingTarget("RealityRoot_A", "Anchors/Plate_A"),
            new SeatingTarget("RealityRoot_B", "Checkpoints/Checkpoint_0")
        };

        [MenuItem("PARALLAX/Setup/PAX-V02 Runtime Cleanup (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            ConfigureHudDevOnly(changes);
            DisablePlaceholderSquares(changes);
            DisableTouchDebugOverlay(changes);
            AnchorForeground("RealityRoot_A", changes);
            AnchorForeground("RealityRoot_B", changes);
            SeatGroundedObjectArt(changes);

            if (changes.Count == 0)
            {
                Debug.Log("PaxV02Setup: no changes.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("PaxV02Setup:\n - " + string.Join("\n - ", changes));
        }

        [MenuItem("PARALLAX/Report/PAX-V02b Art Seating (Sandbox)")]
        public static void ReportArtSeating()
        {
            if (!TryMeasureArtSeating(out List<ArtSeatingMeasurement> measurements, out string error))
            {
                Debug.LogError("PaxV02Setup: " + error);
                return;
            }

            Debug.Log("PaxV02Setup art seating: " + measurements.Count + " targets.");
            for (int i = 0; i < measurements.Count; i++)
            {
                Debug.Log("PaxV02Setup art seating | " + FormatMeasurement(measurements[i]));
            }
        }

        static void ConfigureHudDevOnly(List<string> changes)
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogError("PaxV02Setup: HUD is missing.");
                return;
            }

            EnsureDevOnly(hud.transform.Find("SwitchButton"), changes);
            EnsureDevOnly(hud.transform.Find("RecordButton"), changes);
            EnsureDevOnly(hud.transform.Find("EchoTimeline"), changes);
        }

        static void EnsureDevOnly(Transform target, List<string> changes)
        {
            if (target == null)
            {
                Debug.LogError("PaxV02Setup: expected HUD dev control is missing.");
                return;
            }

            SetupUtility.Ensure<DevOnly>(target.gameObject, changes);
        }

        static void DisablePlaceholderSquares(List<string> changes)
        {
            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer.sprite == null || renderer.sprite.name != "Square" || renderer.gameObject.name == "Goal_B") continue;
                if (!renderer.enabled) continue;

                renderer.enabled = false;
                changes.Add("disabled Square renderer at " + RealityIsolationValidator.Path(renderer.transform));
            }
        }

        static void DisableTouchDebugOverlay(List<string> changes)
        {
            TouchStickCatInput input = Object.FindFirstObjectByType<TouchStickCatInput>(FindObjectsInactive.Include);
            if (input == null)
            {
                Debug.LogError("PaxV02Setup: TouchStickCatInput is missing.");
                return;
            }

            var serialized = new SerializedObject(input);
            SerializedProperty property = serialized.FindProperty("showDebugOverlay");
            if (property == null || !property.boolValue) return;

            property.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set TouchStickCatInput.showDebugOverlay = false");
        }

        static void AnchorForeground(string rootName, List<string> changes)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.LogError("PaxV02Setup: " + rootName + " is missing.");
                return;
            }

            Transform ceiling = root.transform.Find("Geometry/GravityArena/Ceiling");
            Transform foreground = root.transform.Find("Foreground");
            SpriteRenderer art = foreground == null ? null : foreground.GetComponentInChildren<SpriteRenderer>(true);
            Collider2D ceilingCollider = ceiling == null ? null : ceiling.GetComponent<Collider2D>();
            if (foreground == null || art == null || ceilingCollider == null)
            {
                Debug.LogError("PaxV02Setup: could not anchor Foreground for " + rootName + ".");
                return;
            }

            float offset = ceilingCollider.bounds.min.y - art.bounds.max.y;
            if (Mathf.Approximately(offset, 0f)) return;

            foreground.position += Vector3.up * offset;
            changes.Add("anchored " + RealityIsolationValidator.Path(foreground) + ".top to Ceiling underside");
        }

        static void SeatGroundedObjectArt(List<string> changes)
        {
            if (!TryMeasureArtSeating(out List<ArtSeatingMeasurement> measurements, out string error))
            {
                Debug.LogError("PaxV02Setup: " + error);
                return;
            }

            for (int i = 0; i < measurements.Count; i++)
            {
                ArtSeatingMeasurement measurement = measurements[i];
                if (Mathf.Abs(measurement.GapWorld) <= measurement.TexelWorld) continue;

                Transform art = measurement.Art;
                float parentScaleY = Mathf.Abs(art.parent.lossyScale.y);
                if (parentScaleY <= Mathf.Epsilon)
                {
                    Debug.LogError("PaxV02Setup: cannot seat " + measurement.Path + "; Art parent has zero y scale.");
                    continue;
                }

                Vector3 local = art.localPosition;
                local.y -= measurement.GapWorld / parentScaleY;
                art.localPosition = local;
                changes.Add("seated " + measurement.Path + ".localPosition.y");
            }
        }

        static bool TryMeasureArtSeating(out List<ArtSeatingMeasurement> measurements, out string error)
        {
            measurements = new List<ArtSeatingMeasurement>();
            for (int i = 0; i < SeatingTargets.Length; i++)
            {
                SeatingTarget target = SeatingTargets[i];
                GameObject realityRoot = GameObject.Find(target.RealityRootName);
                Transform objectRoot = realityRoot == null ? null : realityRoot.transform.Find(target.ObjectPath);
                Transform art = objectRoot == null ? null : objectRoot.Find("Art");
                SpriteRenderer renderer = art == null ? null : art.GetComponent<SpriteRenderer>();
                if (objectRoot == null || art == null || renderer == null || renderer.sprite == null)
                {
                    error = "missing grounded Art target " + target.RealityRootName + "/" + target.ObjectPath + ".";
                    return false;
                }

                Collider2D support = FindSupportingSurface(realityRoot.transform, objectRoot);
                if (support == null)
                {
                    error = "no supporting collider within " + MaximumSupportDistance.ToString("0.###") + " units below " + RealityIsolationValidator.Path(objectRoot) + ".";
                    return false;
                }

                int lowestOpaqueRow = FindLowestOpaqueRow(renderer.sprite, OpaqueAlphaThreshold);
                if (lowestOpaqueRow < 0)
                {
                    error = "no pixel with alpha > " + OpaqueAlphaThreshold + " in " + renderer.sprite.name + ".";
                    return false;
                }

                float texelWorld = Mathf.Abs(art.lossyScale.y) / renderer.sprite.pixelsPerUnit;
                float pivotToLowestOpaque = PivotToLowestOpaqueWorld(renderer.sprite, lowestOpaqueRow, texelWorld);
                float artBottom = art.position.y - pivotToLowestOpaque;
                float supportTop = support.bounds.max.y;
                Camera camera = GameObject.Find(target.RealityRootName == "RealityRoot_A" ? "Camera_A" : "Camera_B")?.GetComponent<Camera>();
                float gapPixels = camera == null ? float.NaN : Mathf.Abs(camera.WorldToScreenPoint(new Vector3(0f, artBottom, 0f)).y - camera.WorldToScreenPoint(new Vector3(0f, supportTop, 0f)).y);

                measurements.Add(new ArtSeatingMeasurement(
                    RealityIsolationValidator.Path(objectRoot),
                    objectRoot.position,
                    art.localPosition,
                    renderer.sprite.pivot / renderer.sprite.rect.size,
                    pivotToLowestOpaque,
                    supportTop,
                    artBottom - supportTop,
                    gapPixels,
                    texelWorld,
                    art));
            }

            error = null;
            return true;
        }

        static Collider2D FindSupportingSurface(Transform realityRoot, Transform objectRoot)
        {
            Collider2D[] colliders = realityRoot.GetComponentsInChildren<Collider2D>(true);
            Collider2D nearest = null;
            float nearestDrop = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D candidate = colliders[i];
                if (!candidate.enabled || candidate.isTrigger || candidate.gameObject.layer != realityRoot.gameObject.layer) continue;
                if (candidate.transform == objectRoot || candidate.transform.IsChildOf(objectRoot)) continue;

                Bounds bounds = candidate.bounds;
                if (objectRoot.position.x < bounds.min.x || objectRoot.position.x > bounds.max.x) continue;
                float drop = objectRoot.position.y - bounds.max.y;
                if (drop < -0.0001f || drop > MaximumSupportDistance || drop >= nearestDrop) continue;

                nearest = candidate;
                nearestDrop = drop;
            }

            return nearest;
        }

        internal static int FindLowestOpaqueRow(Color32[] pixels, int textureWidth, RectInt textureRect, byte alphaThreshold)
        {
            for (int y = textureRect.yMin; y < textureRect.yMax; y++)
            {
                for (int x = textureRect.xMin; x < textureRect.xMax; x++)
                {
                    if (pixels[y * textureWidth + x].a > alphaThreshold) return y;
                }
            }

            return -1;
        }

        static int FindLowestOpaqueRow(Sprite sprite, byte alphaThreshold)
        {
            Texture2D texture = sprite.texture;
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D readableCopy = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                readableCopy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
                readableCopy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                readableCopy.Apply(false, false);
                Rect textureRect = sprite.textureRect;
                var rect = new RectInt(
                    Mathf.RoundToInt(textureRect.x),
                    Mathf.RoundToInt(textureRect.y),
                    Mathf.RoundToInt(textureRect.width),
                    Mathf.RoundToInt(textureRect.height));
                return FindLowestOpaqueRow(readableCopy.GetPixels32(), texture.width, rect, alphaThreshold);
            }
            finally
            {
                RenderTexture.active = previous;
                if (readableCopy != null) UnityEngine.Object.DestroyImmediate(readableCopy);
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        static float PivotToLowestOpaqueWorld(Sprite sprite, int lowestTextureRow, float texelWorld)
        {
            float localLowestRow = lowestTextureRow - sprite.textureRect.y;
            return (sprite.pivot.y - (localLowestRow + 0.5f)) * texelWorld;
        }

        static string FormatMeasurement(ArtSeatingMeasurement measurement)
        {
            return string.Format(
                "{0} | ({1:0.###}, {2:0.###}) | ({3:0.###}, {4:0.###}) | ({5:0.###}, {6:0.###}) | {7:0.####} | {8:0.####} | {9:0.####} | {10:0.##} | {11:0.####}",
                measurement.Path,
                measurement.RootWorld.x, measurement.RootWorld.y,
                measurement.ArtLocal.x, measurement.ArtLocal.y,
                measurement.PivotNormalized.x, measurement.PivotNormalized.y,
                measurement.PivotToLowestOpaqueWorld,
                measurement.SupportTop,
                measurement.GapWorld,
                measurement.GapPixels,
                measurement.TexelWorld);
        }

        readonly struct SeatingTarget
        {
            public readonly string RealityRootName;
            public readonly string ObjectPath;

            public SeatingTarget(string realityRootName, string objectPath)
            {
                RealityRootName = realityRootName;
                ObjectPath = objectPath;
            }
        }

        readonly struct ArtSeatingMeasurement
        {
            public readonly string Path;
            public readonly Vector3 RootWorld;
            public readonly Vector3 ArtLocal;
            public readonly Vector2 PivotNormalized;
            public readonly float PivotToLowestOpaqueWorld;
            public readonly float SupportTop;
            public readonly float GapWorld;
            public readonly float GapPixels;
            public readonly float TexelWorld;
            public readonly Transform Art;

            public ArtSeatingMeasurement(string path, Vector3 rootWorld, Vector3 artLocal, Vector2 pivotNormalized, float pivotToLowestOpaqueWorld, float supportTop, float gapWorld, float gapPixels, float texelWorld, Transform art)
            {
                Path = path;
                RootWorld = rootWorld;
                ArtLocal = artLocal;
                PivotNormalized = pivotNormalized;
                PivotToLowestOpaqueWorld = pivotToLowestOpaqueWorld;
                SupportTop = supportTop;
                GapWorld = gapWorld;
                GapPixels = gapPixels;
                TexelWorld = texelWorld;
                Art = art;
            }
        }
    }
}
