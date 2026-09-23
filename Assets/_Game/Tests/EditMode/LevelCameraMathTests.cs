using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Core.Cameras;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests
{
    // PAX-052 (D-071): the level camera's pure fit/follow/clamp/look-ahead math. L001-L004's
    // baked content is 32x12; with ViewMargin 0.5 that's a 33x13 frame, which is the fixture
    // the aspect test (5.5) uses so its fit/follow split matches the real levels.
    public class LevelCameraMathTests
    {
        const float MaxViewHeight = 16f;
        const float Aspect20x9 = 20f / 9f;
        const float Aspect16x9 = 16f / 9f;
        const float Aspect4x3 = 4f / 3f;
        static readonly Vector2 RoomFrame = new Vector2(33f, 13f); // 32x12 content + 0.5 margin/side

        // ---------- 5.1 Fit mode ----------

        [Test]
        public void SmallRoom_At20x9_IsFitMode()
        {
            Assert.IsTrue(CameraMath.IsFitMode(RoomFrame, MaxViewHeight, Aspect20x9));
        }

        [Test]
        public void FitMode_ViewHeightIsMinimumThatShowsTheWholeFrame()
        {
            float viewHeight = CameraMath.ResolveViewHeight(RoomFrame, MaxViewHeight, Aspect20x9);
            float expected = Mathf.Max(RoomFrame.y, RoomFrame.x / Aspect20x9);

            Assert.AreEqual(expected, viewHeight, 1e-5f);
            // The view must actually contain the frame at this aspect (no dimension clipped).
            Assert.GreaterOrEqual(viewHeight, RoomFrame.y);
            Assert.GreaterOrEqual(viewHeight * Aspect20x9, RoomFrame.x);
        }

        // ---------- 5.2 Follow mode ----------

        [Test]
        public void WideRoom_BeyondFitLimit_IsFollowMode()
        {
            var wideFrame = new Vector2(80f, 13f);
            Assert.IsFalse(CameraMath.IsFitMode(wideFrame, MaxViewHeight, Aspect16x9));
        }

        [Test]
        public void FollowMode_ViewHeightIsMinOfFrameHeightAndMaxViewHeight()
        {
            var shortWideFrame = new Vector2(80f, 13f);
            Assert.AreEqual(13f, CameraMath.ResolveViewHeight(shortWideFrame, MaxViewHeight, Aspect16x9), 1e-5f);

            var tallWideFrame = new Vector2(80f, 30f);
            Assert.AreEqual(MaxViewHeight, CameraMath.ResolveViewHeight(tallWideFrame, MaxViewHeight, Aspect16x9), 1e-5f);
        }

        // ---------- 5.3 Clamp ----------

        [Test]
        public void FollowCentre_CatAtFarRightEdge_NeverShowsPastFramePlusMargin()
        {
            Vector2 halfView = new Vector2(8f, 6.5f);
            Vector2 frameCentre = Vector2.zero;
            Vector2 frameMin = frameCentre - RoomFrame * 0.5f;
            Vector2 frameMax = frameCentre + RoomFrame * 0.5f;
            Vector2 target = new Vector2(frameMax.x + 50f, 0f); // cat way past the right edge

            Vector2 result = CameraMath.ResolveFollowCentre(Vector2.zero, target, 1f,
                new Vector2(2f, 1.6f), 2.5f, halfView, frameCentre, frameMin, frameMax, verticalFollow: false);

            Assert.LessOrEqual(result.x + halfView.x, frameMax.x + 1e-4f);
            Assert.AreEqual(frameCentre.y, result.y, 1e-5f);
        }

        [Test]
        public void FollowCentre_CatAtFarLeftEdge_NeverShowsPastFramePlusMargin()
        {
            Vector2 halfView = new Vector2(8f, 6.5f);
            Vector2 frameCentre = Vector2.zero;
            Vector2 frameMin = frameCentre - RoomFrame * 0.5f;
            Vector2 frameMax = frameCentre + RoomFrame * 0.5f;
            Vector2 target = new Vector2(frameMin.x - 50f, 0f);

            Vector2 result = CameraMath.ResolveFollowCentre(Vector2.zero, target, -1f,
                new Vector2(2f, 1.6f), 2.5f, halfView, frameCentre, frameMin, frameMax, verticalFollow: false);

            Assert.GreaterOrEqual(result.x - halfView.x, frameMin.x - 1e-4f);
        }

        // ---------- 5.4 Look-ahead ----------

        [Test]
        public void ApplyLookAhead_MovingRight_ShiftsTargetRightByLookAhead()
        {
            Vector2 target = new Vector2(10f, 0f);
            Vector2 result = CameraMath.ApplyLookAhead(target, 1f, 2.5f);
            Assert.AreEqual(12.5f, result.x, 1e-5f);
        }

        [Test]
        public void ApplyLookAhead_MovingLeft_ShiftsTargetLeftByLookAhead()
        {
            Vector2 target = new Vector2(10f, 0f);
            Vector2 result = CameraMath.ApplyLookAhead(target, -1f, 2.5f);
            Assert.AreEqual(7.5f, result.x, 1e-5f);
        }

        [Test]
        public void FollowCentre_LookAheadStaysClampedNearFrameEdge()
        {
            Vector2 halfView = new Vector2(8f, 6.5f);
            Vector2 frameCentre = Vector2.zero;
            Vector2 frameMin = frameCentre - RoomFrame * 0.5f;
            Vector2 frameMax = frameCentre + RoomFrame * 0.5f;
            // Cat just inside the right edge, moving right: look-ahead alone would push the
            // desired centre past the frame; the clamp must still hold.
            Vector2 target = new Vector2(frameMax.x - 1f, 0f);

            Vector2 result = CameraMath.ResolveFollowCentre(Vector2.zero, target, 1f,
                new Vector2(2f, 1.6f), 2.5f, halfView, frameCentre, frameMin, frameMax, verticalFollow: false);

            Assert.LessOrEqual(result.x + halfView.x, frameMax.x + 1e-4f);
        }

        // ---------- 5.5 Aspect ----------

        [Test]
        public void SameRoom_At20x9_IsFit_At16x9And4x3_IsFollow()
        {
            Assert.IsTrue(CameraMath.IsFitMode(RoomFrame, MaxViewHeight, Aspect20x9), "20:9 should fit.");
            Assert.IsFalse(CameraMath.IsFitMode(RoomFrame, MaxViewHeight, Aspect16x9), "16:9 should follow.");
            Assert.IsFalse(CameraMath.IsFitMode(RoomFrame, MaxViewHeight, Aspect4x3), "4:3 should follow.");
        }

        // ---------- 5.6 Snap (component-level: LevelCameraFollow itself, not just its math) ----------

        static (GameObject camGO, GameObject targetGO, LevelCameraConfig config, LevelCameraFollow follow) BuildCamera(params System.Type[] extraComponents)
        {
            var types = new System.Collections.Generic.List<System.Type> { typeof(Camera), typeof(LevelCameraFollow) };
            types.AddRange(extraComponents);
            var camGO = new GameObject("TestLevelCamera", types.ToArray());
            var targetGO = new GameObject("TestTarget");
            var config = ScriptableObject.CreateInstance<LevelCameraConfig>();

            var cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.aspect = Aspect20x9; // fits the 33x13 RoomFrame fixture (see SameRoom_At20x9_...)

            var follow = camGO.GetComponent<LevelCameraFollow>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = targetGO.transform;
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("frameCenter").vector2Value = new Vector2(16f, 2f);
            so.FindProperty("frameSize").vector2Value = RoomFrame;
            so.ApplyModifiedPropertiesWithoutUndo();

            return (camGO, targetGO, config, follow);
        }

        static void DestroyCamera((GameObject camGO, GameObject targetGO, LevelCameraConfig config, LevelCameraFollow follow) built)
        {
            Object.DestroyImmediate(built.camGO);
            Object.DestroyImmediate(built.targetGO);
            Object.DestroyImmediate(built.config);
        }

        [Test]
        public void Snap_FromFarAway_LandsExactlyOnTheFitModeFrameCentre_InOneCall()
        {
            var built = BuildCamera();
            try
            {
                built.camGO.transform.position = new Vector3(-500f, 500f, -10f);

                built.follow.SnapToTarget();

                Assert.AreEqual(16f, built.camGO.transform.position.x, 1e-4f);
                Assert.AreEqual(2f, built.camGO.transform.position.y, 1e-4f);
            }
            finally { DestroyCamera(built); }
        }

        [Test]
        public void DuringDeathHold_Step_ChangesNeitherPositionNorOrthographicSize()
        {
            var built = BuildCamera(typeof(RoomDeath));
            try
            {
                var roomDeath = built.camGO.GetComponent<RoomDeath>();
                var holdingHold = new DeathHold(30);
                holdingHold.Begin();
                typeof(RoomDeath).GetField("hold", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(roomDeath, holdingHold);
                Assert.IsTrue(roomDeath.IsHolding, "test setup: DeathHold must report holding.");

                var so = new SerializedObject(built.follow);
                so.FindProperty("roomDeath").objectReferenceValue = roomDeath;
                so.ApplyModifiedPropertiesWithoutUndo();

                built.camGO.transform.position = new Vector3(500f, -500f, -10f);
                built.camGO.GetComponent<Camera>().orthographicSize = 3f;

                built.follow.Step();

                Assert.AreEqual(500f, built.camGO.transform.position.x, 1e-4f, "camera must not move during the death hold.");
                Assert.AreEqual(-500f, built.camGO.transform.position.y, 1e-4f, "camera must not move during the death hold.");
                Assert.AreEqual(3f, built.camGO.GetComponent<Camera>().orthographicSize, 1e-4f, "camera size must not change during the death hold either.");
            }
            finally { DestroyCamera(built); }
        }

        [Test]
        public void AfterDeathHoldEnds_StepRecomputesOrthographicSize()
        {
            // Step()'s SmoothDamp position update depends on Time.deltaTime, which is 0 in an
            // EditMode test (no frame has actually elapsed), so it cannot be used to prove the
            // hold gate let execution through. orthographicSize is set unconditionally by Resolve
            // before any SmoothDamp call, so it's a deltaTime-independent proxy for "the gate did
            // not early-return": DuringDeathHold_... proves it stays put while holding; this proves
            // it gets recomputed the moment IsHolding goes false.
            var built = BuildCamera(typeof(RoomDeath));
            try
            {
                var roomDeath = built.camGO.GetComponent<RoomDeath>();
                var liveHold = new DeathHold(30); // never Begin() -> IsHolding false
                typeof(RoomDeath).GetField("hold", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(roomDeath, liveHold);
                Assert.IsFalse(roomDeath.IsHolding);

                var so = new SerializedObject(built.follow);
                so.FindProperty("roomDeath").objectReferenceValue = roomDeath;
                so.ApplyModifiedPropertiesWithoutUndo();

                var cam = built.camGO.GetComponent<Camera>();
                cam.orthographicSize = 3f;

                built.follow.Step();

                Assert.That(cam.orthographicSize, Is.Not.EqualTo(3f).Within(1e-4f), "orthographicSize should be recomputed once the hold has ended.");
            }
            finally { DestroyCamera(built); }
        }
    }
}
