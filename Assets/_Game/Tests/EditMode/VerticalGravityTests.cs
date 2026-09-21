using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class VerticalGravityTests
    {
        [Test]
        public void SideOf_DownVector_ReturnsDown()
        {
            Assert.AreEqual(GravitySide.Down, VerticalGravity.SideOf(Vector2.down, GravitySide.Up));
        }

        [Test]
        public void SideOf_UpVector_ReturnsUp()
        {
            Assert.AreEqual(GravitySide.Up, VerticalGravity.SideOf(Vector2.up, GravitySide.Down));
        }

        [Test]
        public void SideOf_DownwardDiagonal_ReturnsDown()
        {
            Assert.AreEqual(GravitySide.Down, VerticalGravity.SideOf(new Vector2(0.3f, -0.9f), GravitySide.Up));
        }

        [Test]
        public void SideOf_UpwardDiagonal_ReturnsUp()
        {
            Assert.AreEqual(GravitySide.Up, VerticalGravity.SideOf(new Vector2(-0.3f, 0.9f), GravitySide.Down));
        }

        [Test]
        public void SideOf_FlatDirection_KeepsCurrentSide()
        {
            Assert.AreEqual(GravitySide.Down, VerticalGravity.SideOf(Vector2.right, GravitySide.Down));
            Assert.AreEqual(GravitySide.Up, VerticalGravity.SideOf(Vector2.right, GravitySide.Up));
        }

        [Test]
        public void SideOf_BelowThreshold_KeepsCurrentSide()
        {
            Assert.AreEqual(GravitySide.Up, VerticalGravity.SideOf(new Vector2(0.99f, -0.05f), GravitySide.Up));
        }

        [Test]
        public void Quantize_ZeroVector_KeepsCurrentDirection()
        {
            Assert.AreEqual(Vector2.up, VerticalGravity.Quantize(Vector2.zero, Vector2.up));
        }

        [Test]
        public void Flip_SwapsBothSides()
        {
            Assert.AreEqual(GravitySide.Up, VerticalGravity.Flip(GravitySide.Down));
            Assert.AreEqual(GravitySide.Down, VerticalGravity.Flip(GravitySide.Up));
        }

        [Test]
        public void IsVertical_AcceptsOnlyExactVerticalDirections()
        {
            Assert.IsTrue(VerticalGravity.IsVertical(Vector2.down));
            Assert.IsTrue(VerticalGravity.IsVertical(Vector2.up));
            Assert.IsFalse(VerticalGravity.IsVertical(new Vector2(0.01f, -1f)));
            Assert.IsFalse(VerticalGravity.IsVertical(Vector2.right));
        }
    }
}
