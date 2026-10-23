using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>D-165 (PAX-110). Idempotent: gives the cat's voice sounds (Tools/Audio/cat_voice.py's {Role}_{NN}.wav in Folder)
    /// their import settings (mono, decompressed on load, preloaded: short sounds started often) and writes them, by role and
    /// in file order, into the AudioCueBank's cat-voice list, saving the bank. No scene changes.</summary>
    public static class CatVoiceImport
    {
        public const string Folder = "Assets/_Game/Audio/CatVoice";

        [MenuItem("PARALLAX/Setup/Cat Voice (PAX-110)")]
        public static void Menu()
        {
            var changes = new List<string>();
            Import(AudioSetup.EnsureBank(changes), changes);
            Debug.Log("CatVoiceImport:\n" + (changes.Count > 0 ? string.Join("\n", changes) : "no changes"));
        }

        /// <summary>The role's files in Folder, in name order (Role_00.wav, Role_01.wav, ...).</summary>
        public static string[] FilesFor(CatVoiceRole role)
        {
            if (!Directory.Exists(Folder)) return new string[0];
            string prefix = role + "_";
            return Directory.GetFiles(Folder, prefix + "*.wav")
                .Select(path => path.Replace('\\', '/'))
                .Where(path => int.TryParse(Path.GetFileNameWithoutExtension(path).Substring(prefix.Length), out _))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        public static void Import(AudioCueBank bank, List<string> changes)
        {
            if (bank == null) { Debug.LogError("CatVoiceImport: no AudioCueBank; run PARALLAX/Setup/Audio (PAX-064) first."); return; }
            if (!Directory.Exists(Folder)) { Debug.LogError("CatVoiceImport: " + Folder + " is missing; run Tools/Audio/cat_voice.py."); return; }

            var serialized = new SerializedObject(bank);
            SerializedProperty list = serialized.FindProperty("catVoice");
            var roles = (CatVoiceRole[])Enum.GetValues(typeof(CatVoiceRole));
            bool changed = list.arraySize != roles.Length;
            list.arraySize = roles.Length;
            for (int r = 0; r < roles.Length; r++)
            {
                string[] files = FilesFor(roles[r]);
                if (files.Length == 0) Debug.LogError("CatVoiceImport: no " + roles[r] + "_NN.wav in " + Folder + "; that voice stays silent.");
                SerializedProperty entry = list.GetArrayElementAtIndex(r);
                SerializedProperty role = entry.FindPropertyRelative("Role");
                if (role.enumValueIndex != r) { role.enumValueIndex = r; changed = true; }
                SerializedProperty variants = entry.FindPropertyRelative("Variants");
                if (variants.arraySize != files.Length) { variants.arraySize = files.Length; changed = true; }
                for (int i = 0; i < files.Length; i++)
                {
                    Configure(files[i], changes);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(files[i]);
                    if (clip == null) { Debug.LogError("CatVoiceImport: " + files[i] + " didn't import as an AudioClip."); continue; }
                    SerializedProperty slot = variants.GetArrayElementAtIndex(i);
                    if (slot.objectReferenceValue != clip) { slot.objectReferenceValue = clip; changed = true; }
                }
            }
            if (!changed) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            changes.Add("wrote the cat voice into " + AudioCueBank.AssetPath + " (" + string.Join(", ", roles.Select(r => r + " " + FilesFor(r).Length)) + ")");
        }

        static void Configure(string path, List<string> changes)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = !importer.forceToMono || importer.loadInBackground || settings.loadType != AudioClipLoadType.DecompressOnLoad
                || !settings.preloadAudioData;
            if (!changed) return;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            changes.Add("import settings: " + Path.GetFileName(path));
        }
    }
}
