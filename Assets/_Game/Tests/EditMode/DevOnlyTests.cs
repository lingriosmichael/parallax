using NUnit.Framework;

namespace Parallax.Gameplay.Tests.EditMode
{
    public sealed class DevOnlyTests
    {
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void ShouldRemainInBuild_UsesDebugBuildFlag(bool isDebugBuild, bool expected)
        {
            Assert.That(DevOnly.ShouldRemainInBuild(isDebugBuild), Is.EqualTo(expected));
        }
    }
}
