using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using Parallax.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-V07 §3, §6 (§11 R13): the presenter is visual only. A scripted input sequence is replayed through the capture
    // rig (the real Cat_Player.prefab in a room built by SoloRoomBuilder) twice: ticks alone, with the presenter never
    // stepped, and ticks with 60 fps presentation frames between them (the interpolated pose, CatVisualPresenter.Present,
    // the pose restored before the next tick). The cat's position and velocity must be identical on every tick. The
    // scripts press inputs during Turn and the other ground states (item 1); later items extend them.
    // The harness lives in the Editor assembly, which this one doesn't reference: reached by reflection.
    public sealed class CatVisualOnlyParityTests
    {
        internal static object CallEditor(string type, string method, object[] args)
        {
            Type t = Type.GetType($"Parallax.Editor.Art.{type}, Parallax.Editor");
            Assert.NotNull(t, type + " not found");
            MemberInfo m = (MemberInfo)t.GetMethod(method, BindingFlags.Public | BindingFlags.Static) ?? t.GetProperty(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, $"{type}.{method} not found");
            try { return m is MethodInfo mi ? mi.Invoke(null, args) : ((PropertyInfo)m).GetValue(null); }
            catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }

        static string Parity(string scenario, out int ticks, out string states)
        {
            var args = new object[] { scenario, 0, null };
            string difference = (string)CallEditor("CatCapture", "VisualOnlyParity", args);
            ticks = (int)args[1];
            states = (string)args[2] ?? "";
            return difference;
        }

        [TestCase("parity_ground_down")]
        [TestCase("parity_ground_up")]
        [TestCase("ground_turns_down")]
        [TestCase("ground_turns_up")]
        [TestCase("ground_ramp_down")]
        [TestCase("ground_wall_down")]
        public void PresenterStepped_VsNot_IdenticalOnEveryTick(string scenario)
        {
            string difference = Parity(scenario, out int ticks, out string statesSeen);
            Assert.Greater(ticks, 50, "the scenario ran");
            Assert.IsNull(difference, difference);
            // The inputs must actually land in the states they are meant to test.
            StringAssert.Contains("Turn", statesSeen, "no Turn was shown: the parity script doesn't press inputs during Turn");
            StringAssert.Contains("Walk", statesSeen);
            StringAssert.Contains("Idle", statesSeen);
        }

        [TestCase("parity_ground_down")]
        [TestCase("parity_ground_up")]
        public void ParityScript_ShowsRun(string scenario)
        {
            Parity(scenario, out _, out string statesSeen);
            StringAssert.Contains("Run", statesSeen);
        }
    }

    // PAX-V07 item 1 (§5): the clip list is data covering every wired A08 slot, and the prefab's clip table carries it.
    public sealed class CatClipTableTests
    {
        const string ManifestPath = "Art_Source/AutoSprite/Cats/A/_import/manifest.json";
        const string CatPlayerPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";

        [Serializable] sealed class Slot { public string slot; public int frames; public bool wired; }
        [Serializable] sealed class Manifest { public float ppu; public Slot[] slots; }

        static Manifest Load() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), ManifestPath)));

        static IEnumerable<Slot> Wired() => Load().slots.Where(s => s.wired && s.slot != "DoorEnter");

        [Test]
        public void TheSetupTable_CoversEveryWiredSlot_AndNotDoorEnter()
        {
            string[] table = (string[])CatVisualOnlyParityTests.CallEditor("CatVisualSetup", "SlotNames", null);
            foreach (Slot s in Wired()) CollectionAssert.Contains(table, s.slot, $"{s.slot} has no row in CatVisualSetup's clip table");
            CollectionAssert.DoesNotContain(table, "DoorEnter", "the door-enter clip is imported but not wired");
        }

        [Test]
        public void ThePrefabsClipTable_HasEveryWiredSlot_WithTheManifestsFrames()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            Assert.NotNull(prefab, CatPlayerPath);
            CatVisualPresenter presenter = prefab.GetComponentInChildren<CatVisualPresenter>(true);
            Assert.NotNull(presenter);
            var so = new SerializedObject(presenter);
            SerializedProperty clips = so.FindProperty("clips.clips");
            Assert.NotNull(clips, "CatVisualPresenter has no CatClipSet 'clips' (run PARALLAX/Setup/Cat Visual)");
            var found = new Dictionary<string, int>();
            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty c = clips.GetArrayElementAtIndex(i);
                found[c.FindPropertyRelative("slot").stringValue] = c.FindPropertyRelative("frames").arraySize;
            }
            foreach (Slot s in Wired())
            {
                Assert.IsTrue(found.ContainsKey(s.slot), $"the prefab's clip table has no {s.slot}");
                Assert.AreEqual(s.frames, found[s.slot], $"{s.slot} frame count");
            }
        }

        [Test]
        public void TheLocomotionClips_HaveAStrideForEveryFrame()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            var so = new SerializedObject(prefab.GetComponentInChildren<CatVisualPresenter>(true));
            SerializedProperty clips = so.FindProperty("clips.clips");
            Assert.NotNull(clips);
            foreach (string slot in new[] { "Walk", "Run" })
            {
                SerializedProperty clip = null;
                for (int i = 0; i < clips.arraySize; i++)
                    if (clips.GetArrayElementAtIndex(i).FindPropertyRelative("slot").stringValue == slot) clip = clips.GetArrayElementAtIndex(i);
                Assert.NotNull(clip, slot);
                SerializedProperty strides = clip.FindPropertyRelative("strideUnits");
                Assert.AreEqual(clip.FindPropertyRelative("frames").arraySize, strides.arraySize, slot + " strides");
                for (int i = 0; i < strides.arraySize; i++) Assert.Greater(strides.GetArrayElementAtIndex(i).floatValue, 0f, $"{slot} stride {i}");
            }
        }
    }
}
