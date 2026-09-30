using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
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

        // PAX-V07 items 4 and 5: inputs pressed during Flip (walking on through the roll, a flip at a jump's apex) and during
        // the fidgets (a walk out of the sit, a move during look around, a jump during the ear twitch), both gravities.
        [TestCase("flip_walk_down", "Flip")]
        [TestCase("flip_jump_down", "Flip")]
        [TestCase("ground_fidget_down", "IdleFidget")]
        [TestCase("ground_fidget_up", "IdleFidget")]
        public void PresenterStepped_VsNot_IdenticalOnEveryTick_InputsDuringFlipAndFidgets(string scenario, string state)
        {
            string difference = Parity(scenario, out int ticks, out string statesSeen);
            Assert.Greater(ticks, 50, "the scenario ran");
            Assert.IsNull(difference, difference);
            StringAssert.Contains(state, statesSeen, $"no {state} was shown: the script doesn't press inputs during it");
        }

        // PAX-V07 item 2: inputs pressed during TakeOff, Land and HardLand (on the air bench's tower, both gravities).
        [TestCase("parity_air_down")]
        [TestCase("parity_air_up")]
        public void PresenterStepped_VsNot_IdenticalOnEveryTick_InputsDuringTheAirStates(string scenario)
        {
            string difference = Parity(scenario, out int ticks, out string statesSeen);
            Assert.Greater(ticks, 50, "the scenario ran");
            Assert.IsNull(difference, difference);
            foreach (string state in new[] { "TakeOff", "Land", "HardLand" })
                StringAssert.Contains(state, statesSeen, $"no {state} was shown: the parity script doesn't press inputs during {state}");
        }

        // PAX-V07 item 3: inputs pressed during Climb, Hang and Leap (moves and reversals on the vine, a leap from each, a
        // reversal, a climb and a jump pressed during the Leap), in the climb bench, both gravities.
        [TestCase("parity_climb_down")]
        [TestCase("parity_climb_up")]
        public void PresenterStepped_VsNot_IdenticalOnEveryTick_InputsDuringTheClimbStates(string scenario)
        {
            string difference = Parity(scenario, out int ticks, out string statesSeen);
            Assert.Greater(ticks, 50, "the scenario ran");
            Assert.IsNull(difference, difference);
            foreach (string state in new[] { "Climb", "Hang", "Leap" })
                StringAssert.Contains(state, statesSeen, $"no {state} was shown: the parity script doesn't press inputs during {state}");
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

        // ---------- The three pops (ruled 2026-09-30), on the prefab's clip table ----------

        const float PhonePxPerUnit = 80f;

        static CatClipSet PrefabClips()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            CatVisualPresenter presenter = prefab != null ? prefab.GetComponentInChildren<CatVisualPresenter>(true) : null;
            Assert.NotNull(presenter, CatPlayerPath);
            var set = (CatClipSet)typeof(CatVisualPresenter).GetField("clips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
            Assert.NotNull(set?.ForState(CatAnimState.Walk), "no clip table (run PARALLAX/Setup/Cat Visual)");
            return set;
        }

        static float JumpPp(CatClip a, int i, CatClip b, int j) => (a.PoseCentroid(i) - b.PoseCentroid(j)).magnitude * PhonePxPerUnit;

        // Walk↔Run switch at the matching foot position: each gait hands over only on its switch frames, into the other gait's
        // leg-matched frame. The body height still differs (art), so the jump is bounded, not removed.
        [TestCase(CatAnimState.Walk, CatAnimState.Run)]
        [TestCase(CatAnimState.Run, CatAnimState.Walk)]
        public void GaitSwitch_HasFootMatchedFrames_IntoTheOtherGait(CatAnimState from, CatAnimState to)
        {
            CatClipSet set = PrefabClips();
            CatClip a = set.ForState(from), b = set.ForState(to);
            Assert.IsTrue(a.HasSwitchFrames, $"{from} has no switch frames");
            int switches = 0;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a.IsSwitchFrame(i)) continue;
                switches++;
                int j = a.SwitchTarget(i);
                Assert.That(j, Is.InRange(0, b.Count - 1), $"{from}[{i}] -> {to}[{j}]");
                Assert.LessOrEqual(JumpPp(a, i, b, j), 9f, $"{from}[{i}] -> {to}[{j}] centroid jump (phone px)");
            }
            Assert.Less(switches, a.Count, "a switch frame set, not every frame");
        }

        // The turn flips on the most symmetrical frame: from any Walk, Run or Idle frame the flip frame's mirror, plus the step
        // to it, moves the silhouette no more than flipping the frame on screen would, and Walk's never more than 4.5 phone px.
        [TestCase(CatAnimState.Walk, 4.5f)]
        [TestCase(CatAnimState.Run, 4.5f)]
        [TestCase(CatAnimState.Idle, 2f)]
        public void TurnFlipFrame_IsTheMostSymmetrical(CatAnimState state, float limitPp)
        {
            CatClip c = PrefabClips().ForState(state);
            for (int i = 0; i < c.Count; i++)
            {
                int f = c.FlipFrame(i);
                float cost = JumpPp(c, i, c, f) + c.MirrorShift(f) * PhonePxPerUnit;
                Assert.LessOrEqual(cost, c.MirrorShift(i) * PhonePxPerUnit + 1e-3f, $"{state}[{i}] flips on [{f}]");
                Assert.LessOrEqual(cost, limitPp, $"{state}[{i}] flips on [{f}] (phone px)");
            }
        }

        // A landing enters on the frame matching the fall's last pose: Land 2 after a normal fall (6 phone px, against 15 for
        // Land 0); after a long one, HardLand's impact crouch (1; its frame 0, a tall pre-impact stand, isn't an entry).
        [TestCase(CatAnimState.Land, 2, 6.5f)]
        [TestCase(CatAnimState.HardLand, 1, 19f)]
        public void Landing_EntersOnTheFrameMatchingTheFall(CatAnimState landing, int expected, float limitPp)
        {
            CatClipSet set = PrefabClips();
            CatClip fall = set.ForState(CatAnimState.Fall), land = set.ForState(landing);
            for (int i = 0; i < fall.Count; i++)
            {
                int j = land.ClosestPose(fall.PoseCentroid(i), 1, 1);
                Assert.AreEqual(expected, j, $"Fall[{i}] -> {landing}");
                Assert.LessOrEqual(JumpPp(fall, i, land, j), limitPp, $"Fall[{i}] -> {landing}[{j}] (phone px)");
            }
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
