using System;
using System.Collections.Generic;
using System.IO;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Editor.Setup
{
    /// <summary>D-277 (PAX-125). Idempotent: gives the music loops (Tools/Audio/music.py's {Track}.wav in Folder) their import settings
    /// (stereo, streamed Vorbis: long loops that shouldn't sit in memory) and writes them into the AudioCueBank, saving the bank.
    /// PARALLAX/Setup/Audio (PAX-064) runs it too. Rebuild All Levels bakes each level's track from its world (Wire).</summary>
    public static class MusicImport
    {
        public const string Folder = "Assets/_Game/Audio/Music";
        const float VorbisQuality = 0.5f;

        [MenuItem("PARALLAX/Setup/Music (PAX-125)")]
        public static void Menu()
        {
            var changes = new List<string>();
            Import(AudioSetup.EnsureBank(changes), changes);
            Debug.Log("MusicImport:\n" + (changes.Count > 0 ? string.Join("\n", changes) : "no changes"));
        }

        public static string FileFor(MusicTrack track) => Folder + "/" + track + ".wav";

        public static MusicTrack TrackFor(Biome biome) => biome == Biome.ObsidianVoid ? MusicTrack.ObsidianVoid : MusicTrack.GoldenRuins;

        public static MusicTrack TrackForLevel(string levelId) => TrackFor(Biomes.For(levelId));

        public static void Import(AudioCueBank bank, List<string> changes)
        {
            if (bank == null) { Debug.LogError("MusicImport: no AudioCueBank; run PARALLAX/Setup/Audio (PAX-064) first."); return; }
            var serialized = new SerializedObject(bank);
            SerializedProperty list = serialized.FindProperty("tracks");
            var tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
            bool changed = list.arraySize != tracks.Length;
            list.arraySize = tracks.Length;
            for (int t = 0; t < tracks.Length; t++)
            {
                string path = FileFor(tracks[t]);
                Configure(path, changes);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) Debug.LogError("MusicImport: " + path + " isn't there; run Tools/Audio/music.py. The scene plays the ambient bed instead.");
                SerializedProperty entry = list.GetArrayElementAtIndex(t);
                SerializedProperty track = entry.FindPropertyRelative("Track");
                if (track.enumValueIndex != t) { track.enumValueIndex = t; changed = true; }
                SerializedProperty slot = entry.FindPropertyRelative("Clip");
                if (slot.objectReferenceValue != clip) { slot.objectReferenceValue = clip; changed = true; }
            }
            if (!changed) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            changes.Add("wrote " + tracks.Length + " music tracks into " + AudioCueBank.AssetPath);
        }

        /// <summary>Sets the scene's GameAudio to its world's track. A scene without one has no audio to wire.</summary>
        public static void Wire(Scene scene, Biome biome, List<string> changes)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameAudio gameAudio = root.GetComponentInChildren<GameAudio>(true);
                if (gameAudio == null) continue;
                TrapKitSetup.Write(gameAudio, changes, ("track", (int)TrackFor(biome)));
                return;
            }
        }

        static void Configure(string path, List<string> changes)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = importer.forceToMono || !importer.loadInBackground || settings.loadType != AudioClipLoadType.Streaming
                || settings.compressionFormat != AudioCompressionFormat.Vorbis || !Mathf.Approximately(settings.quality, VorbisQuality);
            if (!changed) return;
            importer.forceToMono = false;
            importer.loadInBackground = true;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            changes.Add("import settings: " + Path.GetFileName(path));
        }
    }
}
