using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-047 (D-058) §8 tests 9 and 11: CatMotor2D.Freeze/Unfreeze and Step's
    /// early-return while frozen, isolated from RoomDeath/RoomManager (see RoomDeathHoldTests
    /// for the full integration rig).
    ///
    /// Unity does not run Awake/OnEnable synchronously for GameObjects built (or reactivated)
    /// inside a plain EditMode [Test] method — confirmed empirically (CatMotor2D.Freeze NREs on
    /// a null `body` otherwise, even when the GameObject is active throughout). So every
    /// component here has its private fields wired by reflection first, then its Awake is
    /// invoked by reflection too, once, in dependency order — matching exactly what Unity would
    /// have called, just driven by hand instead of by the player loop.</summary>
    public sealed class CatMotor2DFreezeTests
    {
        static readonly FieldInfo JumpBufferField = typeof(CatMotor2D).GetField("jumpBufferTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        static float JumpBuffer(CatMotor2D cat) => (float)JumpBufferField.GetValue(cat);

        internal static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        internal static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"{target.GetType().Name}.{method} not found");
            m.Invoke(target, null);
        }

        static (GameObject root, GameObject cat, CatMotor2D motor, Rigidbody2D body) Build()
        {
            var root = new GameObject("RealityRoot_A");
            RealityRoot realityRoot = root.AddComponent<RealityRoot>();
            SetPrivate(realityRoot, "id", ObserverId.A);
            Invoke(realityRoot, "Awake");

            var cat = new GameObject("Cat");
            cat.transform.SetParent(root.transform, false);
            cat.layer = LayerMask.NameToLayer("RealityA");
            Rigidbody2D body = cat.AddComponent<Rigidbody2D>();
            cat.AddComponent<BoxCollider2D>();
            GravityReceiver gravity = cat.AddComponent<GravityReceiver>();
            Invoke(gravity, "Awake");
            CatMotor2D motor = cat.AddComponent<CatMotor2D>();
            SetPrivate(motor, "config", ScriptableObject.CreateInstance<CatMotorConfig>());
            Invoke(motor, "Awake");

            return (root, cat, motor, body);
        }

        // ---------- §8.9 Input discard ----------

        [Test]
        public void Frozen_JumpCommand_NeverReachesTheJumpBuffer()
        {
            var (root, _, motor, body) = Build();
            try
            {
                motor.Freeze();
                var jump = new CatCommand { JumpPressed = true, Move = 1f };

                motor.Step(in jump, 0.02f);

                Assert.AreEqual(0f, JumpBuffer(motor), "a jump command must never set the jump buffer while frozen");
                Assert.AreEqual(Vector2.zero, body.linearVelocity, "velocity must stay zero while frozen");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void UnfreezeThenResetMotion_ThenStepWithNoCommand_NoJump()
        {
            var (root, _, motor, body) = Build();
            try
            {
                motor.Freeze();
                var jump = new CatCommand { JumpPressed = true };
                motor.Step(in jump, 0.02f); // dropped — see the test above

                motor.Unfreeze();
                motor.ResetMotion();
                CatCommand none = CatCommand.None;
                motor.Step(in none, 0.02f);

                Assert.AreEqual(0f, JumpBuffer(motor), "no jump must fire on the first tick after the reset");
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ---------- §8.11 Unfreeze restores exact pre-freeze constraints ----------

        [Test]
        public void Unfreeze_RestoresExactPreFreezeConstraints()
        {
            var (root, _, motor, body) = Build();
            try
            {
                body.constraints = RigidbodyConstraints2D.FreezeRotation;

                motor.Freeze();
                Assert.AreEqual(RigidbodyConstraints2D.FreezeAll, body.constraints);
                Assert.IsTrue(motor.IsFrozen);

                motor.Unfreeze();
                Assert.AreEqual(RigidbodyConstraints2D.FreezeRotation, body.constraints);
                Assert.IsFalse(motor.IsFrozen);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Unfreeze_NotFrozen_IsNoOp()
        {
            var (root, _, motor, body) = Build();
            try
            {
                body.constraints = RigidbodyConstraints2D.FreezePositionX;
                Assert.IsFalse(motor.IsFrozen);

                motor.Unfreeze();

                Assert.AreEqual(RigidbodyConstraints2D.FreezePositionX, body.constraints);
                Assert.IsFalse(motor.IsFrozen);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
