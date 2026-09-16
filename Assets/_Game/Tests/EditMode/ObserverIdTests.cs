using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class ObserverIdTests
    {
        [Test]
        public void Other_A_ReturnsB()
        {
            Assert.AreEqual(ObserverId.B, ObserverId.A.Other());
        }

        [Test]
        public void Other_B_ReturnsA()
        {
            Assert.AreEqual(ObserverId.A, ObserverId.B.Other());
        }

        [Test]
        public void OtherOther_ReturnsOriginal_ForA()
        {
            Assert.AreEqual(ObserverId.A, ObserverId.A.Other().Other());
        }

        [Test]
        public void OtherOther_ReturnsOriginal_ForB()
        {
            Assert.AreEqual(ObserverId.B, ObserverId.B.Other().Other());
        }
    }
}
