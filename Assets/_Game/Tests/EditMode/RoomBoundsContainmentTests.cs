using NUnit.Framework;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-047 (D-058) §8 test 7, pure half: point-vs-room-bounds containment, via the
    /// same RoomManager.ContainsXY the runtime out-of-bounds check actually uses (not a raw
    /// Bounds.Contains, which also tests z — room bounds are 2D, z is always [0,0], so a naive
    /// Bounds.Contains rejects any point whose z isn't exactly 0). The rig half (a real kill
    /// with cause OutOfBounds) lives in RoomDeathHoldTests.</summary>
    public sealed class RoomBoundsContainmentTests
    {
        static readonly Bounds Room = new Bounds(new Vector2(5f, 0f), new Vector2(4f, 4f)); // x in [3,7], y in [-2,2]

        [Test] public void Inside_IsContained() => Assert.IsTrue(RoomManager.ContainsXY(Room, new Vector2(5f, 0f)));
        [Test] public void Below_IsNotContained() => Assert.IsFalse(RoomManager.ContainsXY(Room, new Vector2(5f, -3f)));
        [Test] public void Above_IsNotContained() => Assert.IsFalse(RoomManager.ContainsXY(Room, new Vector2(5f, 3f)));
        [Test] public void Left_IsNotContained() => Assert.IsFalse(RoomManager.ContainsXY(Room, new Vector2(0f, 0f)));
        [Test] public void Right_IsNotContained() => Assert.IsFalse(RoomManager.ContainsXY(Room, new Vector2(10f, 0f)));
    }
}
