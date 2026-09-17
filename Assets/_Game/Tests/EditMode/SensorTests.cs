using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Transport;

namespace Parallax.Tests.EditMode
{
    public class SensorTests
    {
        [Test]
        public void SensorLatch_InitialZero_ChangesOnlyOnEdges()
        {
            var latch = new SensorLatch(0f);
            Assert.IsFalse(latch.Update(false, out _));
            Assert.IsTrue(latch.Update(true, out float on));
            Assert.AreEqual(1f, on);
            Assert.IsFalse(latch.Update(true, out _));
            Assert.IsTrue(latch.Update(false, out float off));
            Assert.AreEqual(0f, off);
        }

        [Test]
        public void SensorLatch_InitialOne_OnlyChangesWhenVacated()
        {
            var latch = new SensorLatch(1f);
            Assert.IsFalse(latch.Update(true, out _));
            Assert.IsTrue(latch.Update(false, out float target));
            Assert.AreEqual(0f, target);
        }

        [Test]
        public void SensorLatch_Flicker_ReportsEachEdge()
        {
            var latch = new SensorLatch(0f);
            var targets = new List<float>();
            if (latch.Update(true, out float first)) targets.Add(first);
            if (latch.Update(false, out float second)) targets.Add(second);
            if (latch.Update(true, out float third)) targets.Add(third);
            CollectionAssert.AreEqual(new[] { 1f, 0f, 1f }, targets);
        }

        [Test]
        public void SensorPolicy_CountsOnlyLocalAndEcho()
        {
            Assert.IsTrue(SensorPolicy.Counts(InputSourceKind.LocalHuman));
            Assert.IsTrue(SensorPolicy.Counts(InputSourceKind.EchoReplay));
            Assert.IsFalse(SensorPolicy.Counts(InputSourceKind.Inactive));
            Assert.IsFalse(SensorPolicy.Counts(InputSourceKind.RemoteHuman));
        }

        [Test]
        public void SensorOrigin_UsesTheOwningHumansOrigin()
        {
            Assert.AreEqual(EventOrigin.HumanA, EventOrigins.Sensor(ObserverId.A));
            Assert.AreEqual(EventOrigin.HumanB, EventOrigins.Sensor(ObserverId.B));
        }

        [Test]
        public void SensorRequester_LocalTransport_CommitsOnFollowingTick()
        {
            int tick = 5;
            var registry = new AnchorRegistry();
            var id = new AnchorId(2);
            registry.Register(id, 0f);
            var transport = new LocalTransport(registry, 0.02f, () => tick);
            var requester = new AnchorRequester(transport, new EventSequencer(), EventOrigins.Sensor(ObserverId.A));
            requester.Request(id, 1f);
            tick = 6;
            transport.Pump(tick);
            Assert.AreEqual(CommitResult.Applied, transport.LastCommitResult);
            Assert.IsTrue(registry.TryGet(id, out AnchorState occupied));
            Assert.AreEqual(1f, occupied.Value);
            tick = 7;
            requester.Request(id, 0f);
            tick = 8;
            transport.Pump(tick);
            Assert.AreEqual(CommitResult.Applied, transport.LastCommitResult);
            Assert.IsTrue(registry.TryGet(id, out AnchorState empty));
            Assert.AreEqual(0f, empty.Value);
            Assert.AreEqual(2u, empty.Revision);
        }

        [Test]
        public void SensorAndCatRequesters_ShareHumanSequenceWithoutDuplicates()
        {
            int tick = 0;
            var registry = new AnchorRegistry();
            var id = new AnchorId(2);
            registry.Register(id, 0f);
            var transport = new LocalTransport(registry, 0.02f, () => tick);
            var sequencer = new EventSequencer();
            var cat = new AnchorRequester(transport, sequencer, EventOrigins.Human(ObserverId.A));
            var sensor = new AnchorRequester(transport, sequencer, EventOrigins.Sensor(ObserverId.A));
            cat.Request(id, 1f);
            sensor.Request(id, 0f);
            cat.Request(id, 1f);
            tick = 1;
            transport.Pump(tick);
            Assert.AreEqual(CommitResult.Applied, transport.LastCommitResult);
            Assert.IsTrue(registry.TryGet(id, out AnchorState state));
            Assert.AreEqual(3u, state.Revision);
            Assert.AreEqual(1f, state.Value);
        }
    }
}
