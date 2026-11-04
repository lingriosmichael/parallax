using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A10: PARALLAX/Setup/App Icon gives Android the icon layers in Assets/_Game/Art/AppIcon/ (made by
    /// Tools/Art/store_icons.py): adaptive, round and legacy, one texture for every density (the build scales them). It only touches
    /// the icon slots in Player Settings, and running it again changes nothing.</summary>
    public static class AppIconSetup
    {
        const string Folder = "Assets/_Game/Art/AppIcon/";

        [MenuItem("PARALLAX/Setup/App Icon")]
        public static void Apply()
        {
            var changes = new List<string>();
            Texture2D foreground = Icon("ICON_foreground", true, changes), background = Icon("ICON_background", false, changes),
                round = Icon("ICON_round", true, changes), legacy = Icon("ICON_legacy", false, changes);
            if (foreground == null || background == null || round == null || legacy == null)
            {
                Debug.LogError("AppIconSetup: an icon is missing from " + Folder + ". Run Tools/Art/store_icons.py first. Stopping.");
                return;
            }
            NamedBuildTarget android = NamedBuildTarget.Android;
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(android))
            {
                string name = kind.ToString();
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (PlatformIcon icon in icons)
                {
                    if (name.StartsWith("Adaptive")) icon.SetTextures(background, foreground);
                    else if (name.StartsWith("Round")) icon.SetTexture(round);
                    else if (name.StartsWith("Legacy")) icon.SetTexture(legacy);
                }
                PlayerSettings.SetPlatformIcons(android, kind, icons);
                changes.Add($"{name}: {icons.Length} sizes");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PARALLAX/Setup/App Icon: " + string.Join("; ", changes));
        }

        static Texture2D Icon(string name, bool alpha, List<string> changes)
        {
            string path = Folder + name + ".png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return null;
            bool stale = importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None || importer.alphaIsTransparency != alpha
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.textureType != TextureImporterType.Default;
            if (stale)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = alpha;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                changes.Add("imported " + name);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
