using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    public class GravityControlTests
    {
        [Test]
        public void Mapping_ClampsRotatesClockwiseSnapsAndNormalizes()
        {
            Assert.AreEqual(Vector2.down, GravityControlMapping.ToDirection(0f, 90f, 0f));
            Assert.Less(Vector2.Distance(GravityControlMapping.ToDirection(1f, 90f, 0f), Vector2.left), 0.0001f);
            Assert.Less(Vector2.Distance(GravityControlMapping.ToDirection(-1f, 90f, 0f), Vector2.right), 0.0001f);
            Assert.Less(Vector2.Distance(GravityControlMapping.ToDirection(2f, 90f, 0f), Vector2.left), 0.0001f);
            Assert.Less(Vector2.Distance(GravityControlMapping.ToDirection(0.6f, 90f, 90f), Vector2.left), 0.0001f);
            Assert.Less(Vector2.Distance(GravityControlMapping.ToDirection(0.4f, 90f, 90f), Vector2.down), 0.0001f);
            Assert.AreEqual(1f, GravityControlMapping.ToDirection(0.31f, 83f, 0f).magnitude, 0.0001f);
        }

        [Test]
        public void DialMath_UsesClockwisePositiveAndClamps()
        {
            Vector2 center = Vector2.zero;
            Assert.AreEqual(0f, DialMath.ValueFromPointer(center, Vector2.up * 20f, 135f), 0.0001f);
            Assert.AreEqual(90f / 135f, DialMath.ValueFromPointer(center, Vector2.right * 20f, 135f), 0.0001f);
            Assert.AreEqual(-90f / 135f, DialMath.ValueFromPointer(center, Vector2.left * 20f, 135f), 0.0001f);
            Assert.AreEqual(1f, DialMath.ValueFromPointer(center, new Vector2(20f, -20f), 135f));
            Assert.IsTrue(float.IsNaN(DialMath.ValueFromPointer(center, new Vector2(7f, 0f), 135f)));
        }

        [Test]
        public void SeatCommandFilter_ClearsMovementAndJumpButPreservesInteract()
        {
            var command = new CatCommand { Move = 1f, JumpPressed = true, JumpHeld = true, InteractPressed = true, InteractHeld = true };
            CatCommand filtered = SeatCommandFilter.Apply(command);
            Assert.AreEqual(0f, filtered.Move);
            Assert.IsFalse(filtered.JumpPressed);
            Assert.IsFalse(filtered.JumpHeld);
            Assert.IsTrue(filtered.InteractPressed);
            Assert.IsTrue(filtered.InteractHeld);
        }
    }
}
