using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Tests
{
    public class GravityReceiverTests
    {
        [Test]
        public void Flip_TwiceBeforeFixedTick_ReturnsToOriginalTarget()
        {
            var gameObject = new GameObject("GravityReceiverTest");
            GravityReceiver receiver = gameObject.AddComponent<GravityReceiver>();
            receiver.SetTargetDirection(Vector2.down);

            receiver.Flip();
            receiver.Flip();
            receiver.FixedTick(0.02f);

            Assert.AreEqual(Vector2.down, receiver.Direction);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Flip_ChangesDirectionOnNextTick()
        {
            var gameObject = new GameObject("GravityReceiverTest");
            GravityReceiver receiver = gameObject.AddComponent<GravityReceiver>();
            receiver.SetTargetDirection(Vector2.down);

            receiver.Flip();
            receiver.FixedTick(0.02f);

            Assert.AreEqual(Vector2.up, receiver.Direction);
            Object.DestroyImmediate(gameObject);
        }
    }
}
