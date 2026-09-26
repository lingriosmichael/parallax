using NUnit.Framework;
using Parallax.Gameplay.Presentation;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-087 (D-089): the interim climb pose (CatVisualPresenter.ClimbPose). The sprite's own centre, relative to its
    // pivot, is off-centre on purpose (the tail makes the frame wider behind the body); the collider's centre is (0, -0.12).
    // The sprite faces +x at localScale.x = 1.
    public sealed class ClimbPoseTests
    {
        static readonly Vector2 SpriteCentre = new(-.15f, .35f), Centre = new(0f, -.12f);

        // The world direction of the sprite's head: root rotation (0° gravity down, 180° gravity up) x pose x facing.
        static Vector2 Head(float facing, bool gravityDown)
        {
            float angle = CatVisualPresenter.ClimbPose(facing, gravityDown, SpriteCentre, Centre, out _);
            Quaternion root = Quaternion.Euler(0f, 0f, gravityDown ? 0f : 180f);
            return root * Quaternion.Euler(0f, 0f, angle) * new Vector3(Mathf.Sign(facing), 0f, 0f);
        }

        [TestCase(1f, true)]
        [TestCase(-1f, true)]
        [TestCase(1f, false)]
        [TestCase(-1f, false)]
        public void TheHeadPointsScreenUp(float facing, bool gravityDown)
        {
            Vector2 head = Head(facing, gravityDown);
            Assert.AreEqual(0f, head.x, 1e-5f, head.ToString());
            Assert.AreEqual(1f, head.y, 1e-5f, head.ToString());
        }

        [TestCase(1f, true)]
        [TestCase(-1f, true)]
        [TestCase(1f, false)]
        [TestCase(-1f, false)]
        public void TheSpritesCentre_LandsOnTheCollidersCentre(float facing, bool gravityDown)
        {
            float angle = CatVisualPresenter.ClimbPose(facing, gravityDown, SpriteCentre, Centre, out Vector2 position);
            // The visual's transform: position, then rotation, then the facing flip (Unity's T·R·S).
            Vector2 flipped = new(SpriteCentre.x * Mathf.Sign(facing), SpriteCentre.y);
            Vector2 middle = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * flipped);
            Assert.AreEqual(Centre.x, middle.x, 1e-5f);
            Assert.AreEqual(Centre.y, middle.y, 1e-5f);
        }
    }
}
