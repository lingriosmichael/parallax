using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-082 (D-082): the committed movement numbers. §0: the jump was twice as high as it should be
    // (jumpHeight 3.2 -> 1.6); run, fall and gravity unchanged.
    public sealed class MovementConfigPinTests
    {
        [Test]
        public void CatMotorConfig_HoldsThePax082Values()
        {
            var config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(config);
            Assert.AreEqual(1.6f, config.JumpHeight, 1e-5f, "jumpHeight");
            Assert.AreEqual(6f, config.MaxSpeed, 1e-5f, "maxSpeed");
            Assert.AreEqual(60f, config.Acceleration, 1e-5f, "acceleration");
            Assert.AreEqual(80f, config.Deceleration, 1e-5f, "deceleration");
            Assert.AreEqual(20f, config.MaxFallSpeed, 1e-5f, "maxFallSpeed");
        }

        [Test]
        public void CatGravity_IsUnchanged()
        {
            var cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab");
            Assert.NotNull(cat);
            Assert.AreEqual(30f, cat.GetComponent<GravityReceiver>().Strength, 1e-5f);
        }
    }
}
