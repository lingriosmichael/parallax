using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public sealed class DeathTickGuardTests
    {
        [Test]
        public void FirstDeathPerObserverAndTickIsAccepted()
        {
            var guard = new DeathTickGuard();
            Assert.IsTrue(guard.TryAccept(ObserverId.A, 10));
            Assert.IsFalse(guard.TryAccept(ObserverId.A, 10));
            Assert.IsTrue(guard.TryAccept(ObserverId.A, 11));
            Assert.IsTrue(guard.TryAccept(ObserverId.B, 10));
            Assert.IsFalse(guard.TryAccept(ObserverId.B, 10));
        }
    }
}
