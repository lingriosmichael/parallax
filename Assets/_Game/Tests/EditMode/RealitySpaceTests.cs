using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class RealitySpaceTests
    {
        [Test]
        public void Origin_A_IsZero()
        {
            Assert.AreEqual(Vector2.zero, RealitySpace.Origin(ObserverId.A));
        }

        [Test]
        public void Origin_B_IsOffsetB()
        {
            Assert.AreEqual(RealitySpace.OffsetB, RealitySpace.Origin(ObserverId.B));
        }

        [Test]
        public void MapTo_AToBThenBToA_RoundTrips()
        {
            Vector2 original = new Vector2(3f, -2f);
            Vector2 toB = RealitySpace.MapTo(ObserverId.A, ObserverId.B, original);
            Vector2 backToA = RealitySpace.MapTo(ObserverId.B, ObserverId.A, toB);

            Assert.AreEqual(original.x, backToA.x, 1e-4f);
            Assert.AreEqual(original.y, backToA.y, 1e-4f);
        }

        [Test]
        public void MapTo_SameToSame_IsIdentity()
        {
            Vector2 p = new Vector2(5f, 7f);
            Assert.AreEqual(p, RealitySpace.MapTo(ObserverId.A, ObserverId.A, p));
            Assert.AreEqual(p, RealitySpace.MapTo(ObserverId.B, ObserverId.B, p));
        }

        [Test]
        public void LocalCoordinates_UnderA_EqualMappedPointLocalCoordinates_UnderB()
        {
            Vector2 worldA = new Vector2(2f, 1f);
            Vector2 localA = worldA - RealitySpace.Origin(ObserverId.A);

            Vector2 worldB = RealitySpace.MapTo(ObserverId.A, ObserverId.B, worldA);
            Vector2 localB = worldB - RealitySpace.Origin(ObserverId.B);

            Assert.AreEqual(localA.x, localB.x, 1e-4f);
            Assert.AreEqual(localA.y, localB.y, 1e-4f);
        }

        [Test]
        public void PhysicsLayerName_IsExact()
        {
            Assert.AreEqual("RealityA", RealitySpace.PhysicsLayerName(ObserverId.A));
            Assert.AreEqual("RealityB", RealitySpace.PhysicsLayerName(ObserverId.B));
        }

        [Test]
        public void SortingLayerName_IsExact()
        {
            Assert.AreEqual("A_Background", RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Background));
            Assert.AreEqual("A_Middle", RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Middle));
            Assert.AreEqual("A_Gameplay", RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay));
            Assert.AreEqual("A_Foreground", RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Foreground));
            Assert.AreEqual("B_Background", RealitySpace.SortingLayerName(ObserverId.B, SortingBand.Background));
            Assert.AreEqual("B_Middle", RealitySpace.SortingLayerName(ObserverId.B, SortingBand.Middle));
            Assert.AreEqual("B_Gameplay", RealitySpace.SortingLayerName(ObserverId.B, SortingBand.Gameplay));
            Assert.AreEqual("B_Foreground", RealitySpace.SortingLayerName(ObserverId.B, SortingBand.Foreground));
        }
    }
}
