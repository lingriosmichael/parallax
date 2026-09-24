using System.Reflection;
using NUnit.Framework;
using Parallax.Core.Cameras;
using Parallax.Gameplay.Cameras;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-082 follow-up: the level camera shook with the cat standing still. The look-ahead side came
    // from the sign of the cat's per-frame x change, so float-step noise in the interpolated position
    // flipped it and swung the camera's target by up to 2 x (look-ahead + dead zone).
    public sealed class CameraLookAheadTests
    {
        const float Flip = .1f; // LevelCameraConfig's default lookAheadFlipDistance

        [Test]
        public void ACatAtRest_WithFloatNoise_NeverFlipsTheDirection()
        {
            float anchor = 13.5679893f, direction = 1f;
            for (int i = 0; i < 1000; i++)
            {
                float x = 13.5679893f + (i % 2 == 0 ? 1e-6f : -1e-6f);
                direction = CameraMath.ResolveLookDirection(x, ref anchor, direction, Flip);
                Assert.AreEqual(1f, direction, $"flipped at frame {i}");
            }
        }

        [Test]
        public void AReversal_FlipsOnlyPastTheFlipDistance()
        {
            float anchor = 10f, direction = 1f;
            direction = CameraMath.ResolveLookDirection(9.95f, ref anchor, direction, Flip);
            Assert.AreEqual(1f, direction, "0.05 u back is not a reversal");
            direction = CameraMath.ResolveLookDirection(9.85f, ref anchor, direction, Flip);
            Assert.AreEqual(-1f, direction, "0.15 u back is a reversal");
        }

        [Test]
        public void AReversal_IsMeasuredFromTheFurthestPointReached()
        {
            float anchor = 10f, direction = 1f;
            for (float x = 10f; x <= 12f; x += .01f) direction = CameraMath.ResolveLookDirection(x, ref anchor, direction, Flip);
            Assert.AreEqual(1f, direction);
            direction = CameraMath.ResolveLookDirection(11.95f, ref anchor, direction, Flip);
            Assert.AreEqual(1f, direction, "0.05 u back from the furthest point is not a reversal");
            direction = CameraMath.ResolveLookDirection(11.85f, ref anchor, direction, Flip);
            Assert.AreEqual(-1f, direction);
        }

        [Test]
        public void TheLevelCamera_WithAStillCat_KeepsItsTargetStill()
        {
            var cameraGo = new GameObject("Camera_A", typeof(Camera));
            var targetGo = new GameObject("Target");
            try
            {
                var cam = cameraGo.GetComponent<Camera>();
                cam.orthographic = true;
                LevelCameraFollow follow = cameraGo.AddComponent<LevelCameraFollow>();
                Set(follow, "target", targetGo.transform);
                Set(follow, "config", ScriptableObject.CreateInstance<LevelCameraConfig>());
                // A room far wider than any view, so the camera is in follow mode at any aspect.
                Set(follow, "frameCenter", new Vector2(100f, 2f));
                Set(follow, "frameSize", new Vector2(200f, 9f));
                Call(follow, "Awake");
                MethodInfo resolve = typeof(LevelCameraFollow).GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Instance);

                // Walk right to set the look-ahead direction, then settle.
                Vector2 centre = new(100f, 2f);
                for (float x = 99f; x <= 100f; x += .05f)
                {
                    targetGo.transform.position = new Vector2(x, 2f);
                    centre = (Vector2)resolve.Invoke(follow, new object[] { centre, true });
                }
                targetGo.transform.position = new Vector2(100f, 2f); // the float walk can stop short of 100
                for (int i = 0; i < 10; i++) centre = (Vector2)resolve.Invoke(follow, new object[] { centre, true });
                float settled = centre.x;

                // Stand still with one-float-step jitter (the ulp at x 100 is ~7.6e-6), as the interpolated Transform does.
                for (int i = 0; i < 200; i++)
                {
                    targetGo.transform.position = new Vector2(100f + (i % 2 == 0 ? 1e-5f : -1e-5f), 2f);
                    centre = (Vector2)resolve.Invoke(follow, new object[] { centre, true });
                    Assert.AreEqual(settled, centre.x, .001f, $"the camera's target moved at frame {i} with the cat still");
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(targetGo);
            }
        }

        static void Set(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    }
}
