using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-053 §3.4/§5.8 (Phase 1 finding): _LevelTemplate's Camera_A carries a LevelCameraFollow
    // whose frame is only ever baked at level-regeneration time (LevelSetup.BuildRoomInScene),
    // never on the template itself. Entering Play mode with the template open therefore runs
    // Resolve() with frameSize == (0,0): CameraMath.ResolveViewHeight(Vector2.zero, ...) == 0,
    // which used to be written straight to Camera.orthographicSize - a degenerate view volume
    // that a Screen Space - Camera canvas' raycasting logs every frame as "Screen position out
    // of view frustum (screen pos 0,0)". LevelCameraFollow has no [ExecuteAlways]/
    // [ExecuteInEditMode], so this never runs merely from the scene being open in Edit mode -
    // only Awake/Start/OnEnable/LateUpdate, which all require Play mode.
    public sealed class LevelCameraFollowTests
    {
        GameObject cameraGo;
        GameObject targetGo;

        [TearDown]
        public void Cleanup()
        {
            if (cameraGo != null) Object.DestroyImmediate(cameraGo);
            if (targetGo != null) Object.DestroyImmediate(targetGo);
        }

        LevelCameraFollow BuildUnbaked()
        {
            cameraGo = new GameObject("Camera_A", typeof(Camera));
            var cam = cameraGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f; // a plausible pre-existing size; must never become 0

            targetGo = new GameObject("Target");
            targetGo.transform.position = Vector2.zero;

            LevelCameraFollow follow = cameraGo.AddComponent<LevelCameraFollow>();
            var config = ScriptableObject.CreateInstance<LevelCameraConfig>();
            SetPrivate(follow, "target", targetGo.transform);
            SetPrivate(follow, "config", config);
            // frameCenter/frameSize left at their serialized default: Vector2.zero (unbaked).
            Invoke(follow, "Awake");
            return follow;
        }

        static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"{target.GetType().Name}.{method} not found");
            m.Invoke(target, null);
        }

        [Test]
        public void SnapToTarget_WithZeroFrame_NeverWritesZeroOrthographicSize_LogsWarningOnce()
        {
            LevelCameraFollow follow = BuildUnbaked();
            Camera cam = cameraGo.GetComponent<Camera>();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Camera_A.*"));
            follow.SnapToTarget();

            Assert.AreNotEqual(0f, cam.orthographicSize, "a zero (unbaked) frame must never zero the camera's orthographic size");
            Assert.AreEqual(5f, cam.orthographicSize, "the camera's prior size must be left untouched when the frame isn't baked yet");
        }

        [Test]
        public void Step_WithZeroFrame_NeverWritesZeroOrthographicSize_LogsWarningExactlyOnce_AcrossMultipleCalls()
        {
            LevelCameraFollow follow = BuildUnbaked();
            Camera cam = cameraGo.GetComponent<Camera>();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Camera_A.*"));
            follow.Step();
            follow.Step();
            follow.Step();

            Assert.AreNotEqual(0f, cam.orthographicSize);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
