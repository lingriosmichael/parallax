using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-093 (D-095): MovingFloorMath, the carry (Q2), the push (Q6) and the shrink (Q5).
    public sealed class MovingFloorMathTests
    {
        static readonly Vector2 Down = Vector2.down, Up = Vector2.up;

        [Test] public void Carry_Sideways_IsCarriedWhole() => Assert.AreEqual(new Vector2(.12f, 0f), MovingFloorMath.Carry(new Vector2(.12f, 0f), Down));
        [Test] public void Carry_AwayFromTheGround_IsCarried() => Assert.AreEqual(new Vector2(0f, -.1f), MovingFloorMath.Carry(new Vector2(0f, -.1f), Down));
        [Test] public void Carry_IntoTheCat_IsLeftToPhysics() => Assert.AreEqual(Vector2.zero, MovingFloorMath.Carry(new Vector2(0f, .1f), Down));
        [Test] public void Carry_Diagonal_KeepsTheSidewaysPartOfARise() => Assert.AreEqual(new Vector2(.2f, 0f), MovingFloorMath.Carry(new Vector2(.2f, .1f), Down));

        // Gravity up: "away from the ground" is up, and screen-right is still +x (D-049).
        [Test] public void Carry_GravityUp_AwayIsUp_AndIntoIsDown()
        {
            Assert.AreEqual(new Vector2(0f, .1f), MovingFloorMath.Carry(new Vector2(0f, .1f), Up));
            Assert.AreEqual(Vector2.zero, MovingFloorMath.Carry(new Vector2(0f, -.1f), Up));
            Assert.AreEqual(.2f, MovingFloorMath.Carry(new Vector2(.2f, 0f), Up).x, 1e-6f);
        }

        [Test] public void PushOut_Right_MovesTheCatFlush() => Assert.AreEqual(.25f, MovingFloorMath.PushOut(5.25f, 5f, 1), 1e-6f);
        [Test] public void PushOut_Right_NotYetReached_IsZero() => Assert.AreEqual(0f, MovingFloorMath.PushOut(4.9f, 5f, 1));
        [Test] public void PushOut_Left_MovesTheCatFlush() => Assert.AreEqual(-.3f, MovingFloorMath.PushOut(4.7f, 5f, -1), 1e-6f);
        [Test] public void PushOut_Left_NotYetReached_IsZero() => Assert.AreEqual(0f, MovingFloorMath.PushOut(5.1f, 5f, -1));

        [TestCase(0, 4f)] [TestCase(15, 2.5f)] [TestCase(30, 1f)] [TestCase(45, 1f)] [TestCase(-5, 4f)]
        public void ShrinkWidth_EasesFromFullToMin(int ticks, float expected) => Assert.AreEqual(expected, MovingFloorMath.ShrinkWidth(ticks, 30, 4f, 1f), 1e-5f);

        [Test] public void ShrinkWidth_ClampsMinIntoZeroToFull()
        {
            Assert.AreEqual(0f, MovingFloorMath.ShrinkWidth(30, 30, 4f, -1f), 1e-6f);
            Assert.AreEqual(4f, MovingFloorMath.ShrinkWidth(30, 30, 4f, 9f), 1e-6f);
        }

        [TestCase(ShrinkFrom.Left, 1.5f)] [TestCase(ShrinkFrom.Right, -1.5f)] [TestCase(ShrinkFrom.Both, 0f)]
        public void ShrinkCentreShift_KeepsTheStayingEdge(ShrinkFrom from, float expected) => Assert.AreEqual(expected, MovingFloorMath.ShrinkCentreShift(from, 4f, 1f), 1e-6f);
    }
}
