using System.Reflection;
using NUnit.Framework;
using Parallax.Core.Cameras;
using Parallax.Gameplay.Cameras;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §5: LevelCameraFollow's per-step math moved into CameraMath.Step so the camera tell rule can
    // run it. Reference below is the pre-move LevelCameraFollow.Resolve (after D-084), kept verbatim as the oracle:
    // the component and the pure step must reproduce it bit for bit, in fit and follow mode, through turns, rests
    // and snaps.
    public sealed class CameraStepEquivalenceTests
    {
        GameObject cameraGo, targetGo;

        [TearDown]
        public void Cleanup()
        {
            if (cameraGo != null) Object.DestroyImmediate(cameraGo);
            if (targetGo != null) Object.DestroyImmediate(targetGo);
        }

        // The pre-move Resolve, fields and all (LevelCameraFollow.cs at f457bf6, lines 108-134).
        sealed class Reference
        {
            public Vector2 Position, Velocity; public float DirectionAnchorX, LastDirection;
            readonly LevelCameraConfig config; readonly Vector2 frameCenter, frameSize; readonly float aspect;
            public float OrthographicSize;
            public Reference(LevelCameraConfig config, Vector2 frameCenter, Vector2 frameSize, float aspect) { this.config = config; this.frameCenter = frameCenter; this.frameSize = frameSize; this.aspect = aspect; }

            public void Start(Vector2 target) { DirectionAnchorX = target.x; Snap(target); }
            public void Snap(Vector2 target) { Position = Resolve(Position, target, true, 0f); Velocity = Vector2.zero; DirectionAnchorX = target.x; }
            public void Step(Vector2 target, float deltaTime) => Position = Resolve(Position, target, false, deltaTime);

            Vector2 Resolve(Vector2 currentCentre, Vector2 targetPos, bool immediate, float deltaTime)
            {
                float viewHeight = CameraMath.ResolveViewHeight(frameSize, config.MaxViewHeight, aspect);
                OrthographicSize = viewHeight * 0.5f;
                Vector2 halfView = new Vector2(viewHeight * 0.5f * aspect, viewHeight * 0.5f);
                Vector2 frameMin = frameCenter - frameSize * 0.5f;
                Vector2 frameMax = frameCenter + frameSize * 0.5f;

                Vector2 desired;
                if (CameraMath.IsFitMode(frameSize, config.MaxViewHeight, aspect))
                {
                    desired = frameCenter;
                }
                else
                {
                    float direction = CameraMath.ResolveLookDirection(targetPos.x, ref DirectionAnchorX, LastDirection, config.LookAheadFlipDistance);
                    LastDirection = direction;
                    bool verticalFollow = frameSize.y > viewHeight + 0.001f;

                    desired = CameraMath.ResolveFollowCentre(currentCentre, targetPos, direction,
                        config.DeadZoneHalfExtents, config.LookAhead, halfView, frameCenter, frameMin, frameMax, verticalFollow);
                }

                if (immediate) return desired;
                return Vector2.SmoothDamp(currentCentre, desired, ref Velocity, config.SmoothTime, config.MaxSpeed, deltaTime);
            }
        }

        // A cat path with runs both ways, rests, float jitter at rest (D-084), a jump and a respawn snap.
        static Vector2 PathAt(int i)
        {
            if (i < 60) return new Vector2(2f + .12f * i, 0f);
            if (i < 80) return new Vector2(9.2f + (i % 2 == 0 ? 0f : 1e-5f), 0f);
            if (i < 140) return new Vector2(9.2f - .12f * (i - 80), Mathf.Max(0f, 1.6f - Mathf.Pow((i - 110) * .06f, 2f)));
            if (i < 200) return new Vector2(2f + .09f * (i - 140), 0f);
            return new Vector2(30f - .12f * (i - 200), (i - 200) * .02f);
        }

        static LevelCameraConfig Config() => AssetDatabaseConfig() ?? ScriptableObject.CreateInstance<LevelCameraConfig>();
        static LevelCameraConfig AssetDatabaseConfig() => UnityEditor.AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");

        [TestCase(4f / 3f, 16.5f, 13f)]    // follow (L001's 33x13 frame at 4:3)
        [TestCase(16f / 9f, 16.5f, 13f)]   // follow
        [TestCase(20f / 9f, 16.5f, 13f)]   // fit
        [TestCase(16f / 9f, 20f, 24f)]     // follow with vertical follow (a frame taller than the view)
        public void LevelCameraFollow_MatchesThePreMoveResolve_BitForBit(float aspect, float frameCentreX, float frameHeight)
        {
            LevelCameraConfig config = Config();
            var frameCenter = new Vector2(frameCentreX, frameHeight * .5f - 3f);
            var frameSize = new Vector2(frameCentreX * 2f, frameHeight);

            cameraGo = new GameObject("Camera_A", typeof(Camera));
            Camera cam = cameraGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.aspect = aspect;
            targetGo = new GameObject("Target");
            targetGo.transform.position = PathAt(0);
            LevelCameraFollow follow = cameraGo.AddComponent<LevelCameraFollow>();
            SetPrivate(follow, "target", targetGo.transform);
            SetPrivate(follow, "config", config);
            SetPrivate(follow, "frameCenter", frameCenter);
            SetPrivate(follow, "frameSize", frameSize);
            Invoke(follow, "Awake");
            Invoke(follow, "Start");

            var reference = new Reference(config, frameCenter, frameSize, aspect);
            reference.Start(PathAt(0));
            AssertSame(reference, follow, cam, "Start");

            float deltaTime = Time.deltaTime;
            TestContext.Out.WriteLine($"Time.deltaTime in EditMode: {deltaTime}");
            for (int i = 1; i < 260; i++)
            {
                targetGo.transform.position = PathAt(i);
                // Every third step is a snap: EditMode's Time.deltaTime can be 0 (no smoothing at all), and a snap
                // runs the look direction, dead zone, look-ahead and clamp without it.
                if (i == 200 || i % 3 == 0) { follow.SnapToTarget(); reference.Snap(PathAt(i)); }
                else { follow.Step(); reference.Step(PathAt(i), deltaTime); }
                AssertSame(reference, follow, cam, "step " + i);
            }
        }

        // The pure step against the reference at the frame rates the tell rule models (dt is explicit here, so the
        // smoothing is exercised whatever EditMode's Time.deltaTime is).
        [TestCase(1f / 30f, 4f / 3f)]
        [TestCase(1f / 60f, 16f / 9f)]
        [TestCase(1f / 60f, 20f / 9f)]
        public void CameraMathStep_MatchesThePreMoveResolve_BitForBit(float deltaTime, float aspect)
        {
            LevelCameraConfig config = Config();
            var frameCenter = new Vector2(16.5f, 3.5f);
            var frameSize = new Vector2(33f, 13f);
            var reference = new Reference(config, frameCenter, frameSize, aspect);
            var p = new CameraMath.FollowParams(config.MaxViewHeight, config.LookAhead, config.LookAheadFlipDistance, config.DeadZoneHalfExtents, config.SmoothTime, config.MaxSpeed);

            reference.Start(PathAt(0));
            var state = new CameraMath.FollowState { AnchorX = PathAt(0).x };
            CameraMath.Step(ref state, PathAt(0), frameCenter, frameSize, aspect, p, true, 0f);
            state.Velocity = Vector2.zero; state.AnchorX = PathAt(0).x;

            for (int i = 1; i < 260; i++)
            {
                float height;
                if (i == 200)
                {
                    reference.Snap(PathAt(i));
                    height = CameraMath.Step(ref state, PathAt(i), frameCenter, frameSize, aspect, p, true, deltaTime);
                    state.Velocity = Vector2.zero; state.AnchorX = PathAt(i).x;
                }
                else
                {
                    reference.Step(PathAt(i), deltaTime);
                    height = CameraMath.Step(ref state, PathAt(i), frameCenter, frameSize, aspect, p, false, deltaTime);
                }
                Assert.AreEqual(reference.Position, state.Centre, "centre at step " + i);
                Assert.AreEqual(reference.Velocity, state.Velocity, "velocity at step " + i);
                Assert.AreEqual(reference.DirectionAnchorX, state.AnchorX, "anchor at step " + i);
                Assert.AreEqual(reference.LastDirection, state.LastDirection, "direction at step " + i);
                Assert.AreEqual(reference.OrthographicSize, height * .5f, "size at step " + i);
            }
        }

        static void AssertSame(Reference reference, LevelCameraFollow follow, Camera cam, string at)
        {
            Vector2 actual = follow.transform.position;
            Assert.IsTrue(actual.x == reference.Position.x && actual.y == reference.Position.y, $"{at}: camera {actual.x:R},{actual.y:R} vs reference {reference.Position.x:R},{reference.Position.y:R}");
            Assert.AreEqual(reference.OrthographicSize, cam.orthographicSize, at + ": orthographic size");
            Assert.AreEqual(reference.Velocity, (Vector2)GetPrivate(follow, "velocity"), at + ": velocity");
            Assert.AreEqual(reference.DirectionAnchorX, (float)GetPrivate(follow, "directionAnchorX"), at + ": anchor");
            Assert.AreEqual(reference.LastDirection, (float)GetPrivate(follow, "lastDirection"), at + ": direction");
        }

        static void SetPrivate(object target, string field, object value) => Field(target, field).SetValue(target, value);
        static object GetPrivate(object target, string field) => Field(target, field).GetValue(target);
        static FieldInfo Field(object target, string field)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            return f;
        }

        static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"{target.GetType().Name}.{method} not found");
            m.Invoke(target, null);
        }
    }
}
