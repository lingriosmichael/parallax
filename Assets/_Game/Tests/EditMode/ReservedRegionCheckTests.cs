using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Tests
{
    public class ReservedRegionCheckTests
    {
        sealed class FakeRegion : ITouchReservedRegion
        {
            readonly Rect rect;
            public FakeRegion(Rect rect) { this.rect = rect; }
            public bool ContainsScreenPoint(Vector2 screenPos) => rect.Contains(screenPos);
        }

        [Test]
        public void IsReserved_PointInsideRegion_ReturnsTrue()
        {
            var regions = new List<ITouchReservedRegion> { new FakeRegion(new Rect(0f, 0f, 100f, 100f)) };
            Assert.IsTrue(ReservedRegionCheck.IsReserved(regions, new Vector2(50f, 50f)));
        }

        [Test]
        public void IsReserved_PointOutsideAllRegions_ReturnsFalse()
        {
            var regions = new List<ITouchReservedRegion> { new FakeRegion(new Rect(0f, 0f, 100f, 100f)) };
            Assert.IsFalse(ReservedRegionCheck.IsReserved(regions, new Vector2(500f, 500f)));
        }

        [Test]
        public void IsReserved_EmptyRegionList_ReturnsFalse()
        {
            var regions = new List<ITouchReservedRegion>();
            Assert.IsFalse(ReservedRegionCheck.IsReserved(regions, new Vector2(50f, 50f)));
        }
    }
}
