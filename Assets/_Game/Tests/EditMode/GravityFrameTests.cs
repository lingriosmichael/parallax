using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class GravityFrameTests
    {
        static readonly Vector2 Down = new Vector2(0f, -1f);
        static readonly Vector2 Up = new Vector2(0f, 1f);
        static readonly Vector2 Left = new Vector2(-1f, 0f);
        static readonly Vector2 Right = new Vector2(1f, 0f);

        [Test]
        public void Along_GravityDown_ProjectsOntoWorldRight()
        {
            Assert.AreEqual(5f, GravityFrame.Along(new Vector2(5f, 3f), Down), 0.001f);
        }

        [Test]
        public void UpSpeed_GravityDown_ProjectsOntoWorldUp()
        {
            Assert.AreEqual(3f, GravityFrame.UpSpeed(new Vector2(5f, 3f), Down), 0.001f);
        }

        [Test]
        public void Along_GravityUp_FlipsRightAxis()
        {
            Assert.AreEqual(-5f, GravityFrame.Along(new Vector2(5f, 3f), Up), 0.001f);
        }

        [Test]
        public void UpSpeed_GravityUp_FlipsUpAxis()
        {
            Assert.AreEqual(-3f, GravityFrame.UpSpeed(new Vector2(5f, 3f), Up), 0.001f);
        }

        [Test]
        public void Along_GravityLeft_ProjectsOntoWorldDown()
        {
            Assert.AreEqual(-3f, GravityFrame.Along(new Vector2(5f, 3f), Left), 0.001f);
        }

        [Test]
        public void Along_GravityRight_ProjectsOntoWorldUp()
        {
            Assert.AreEqual(3f, GravityFrame.Along(new Vector2(5f, 3f), Right), 0.001f);
        }

        [Test]
        public void RotationAngle_MatchesFourCardinalDirections()
        {
            Assert.AreEqual(0f, GravityFrame.RotationAngle(Down), 0.001f);
            Assert.AreEqual(180f, Mathf.Abs(GravityFrame.RotationAngle(Up)), 0.001f);
            Assert.AreEqual(-90f, GravityFrame.RotationAngle(Left), 0.001f);
            Assert.AreEqual(90f, GravityFrame.RotationAngle(Right), 0.001f);
        }

        [Test]
        public void RotationAngle_FortyFiveDegrees()
        {
            Vector2 diagonal = new Vector2(1f, -1f).normalized;
            Assert.AreEqual(45f, GravityFrame.RotationAngle(diagonal), 0.001f);
        }
    }
}
