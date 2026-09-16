using System;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Transport;

namespace Parallax.Tests.EditMode
{
    public class AnchorInteractionTests
    {
        sealed class FakeTransport : IRealityTransport
        {
            public AnchorRequest[] Requests = new AnchorRequest[8];
            public int Count;
            public bool IsSessionAuthority => true;
            public int Tick => 0;
            public event Action<ControlSample> ControlReceived { add { } remove { } }
            public event Action<SpectacleCue> CueReceived { add { } remove { } }
            public void RequestAnchor(in AnchorRequest request) => Requests[Count++] = request;
            public void PublishControl(in ControlSample sample) { }
            public void SendCue(in SpectacleCue cue) { }
        }

        [Test]
        public void EventOrigins_MapObserversToHumans()
        {
            Assert.AreEqual(EventOrigin.HumanA, EventOrigins.Human(ObserverId.A));
            Assert.AreEqual(EventOrigin.HumanB, EventOrigins.Human(ObserverId.B));
        }

        [TestCase(0f, 1f, 0f, 1f)]
        [TestCase(1f, 0f, 1f, 0f)]
        [TestCase(0.4f, 1f, 0f, 1f)]
        [TestCase(0.5f, 0f, 1f, 0f)]
        public void AnchorToggle_UsesLastRequestedValue(float initial, float first, float second, float third)
        {
            var toggle = new AnchorToggle(initial);
            Assert.AreEqual(first, toggle.Next());
            Assert.AreEqual(second, toggle.Next());
            Assert.AreEqual(third, toggle.Next());
        }

        [Test]
        public void Requester_StampsOriginSequencesAndTargets()
        {
            var fake = new FakeTransport();
            var requester = new AnchorRequester(fake, new EventSequencer(), EventOrigin.HumanA);
            requester.Request(new AnchorId(1), 0.2f);
            requester.Request(new AnchorId(2), 0.7f);
            requester.Request(new AnchorId(1), 1f);
            Assert.AreEqual(3, fake.Count);
            Assert.AreEqual(EventOrigin.HumanA, fake.Requests[0].Origin);
            Assert.AreEqual(1u, fake.Requests[0].Sequence);
            Assert.AreEqual(2u, fake.Requests[1].Sequence);
            Assert.AreEqual(3u, fake.Requests[2].Sequence);
            Assert.AreEqual(0.7f, fake.Requests[1].TargetValue);
        }

        [Test]
        public void SharedSequencer_SeparatesOriginsAndContinuesSameOrigin()
        {
            var fake = new FakeTransport();
            var sequence = new EventSequencer();
            var a = new AnchorRequester(fake, sequence, EventOrigin.HumanA);
            var b = new AnchorRequester(fake, sequence, EventOrigin.HumanB);
            var aAgain = new AnchorRequester(fake, sequence, EventOrigin.HumanA);
            a.Request(new AnchorId(1), 1f);
            b.Request(new AnchorId(1), 1f);
            aAgain.Request(new AnchorId(1), 0f);
            Assert.AreEqual(1u, fake.Requests[0].Sequence);
            Assert.AreEqual(1u, fake.Requests[1].Sequence);
            Assert.AreEqual(2u, fake.Requests[2].Sequence);
        }

        [Test]
        public void Requester_RejectsNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new AnchorRequester(null, new EventSequencer(), EventOrigin.HumanA));
            Assert.Throws<ArgumentNullException>(() => new AnchorRequester(new FakeTransport(), null, EventOrigin.HumanA));
        }

        [Test]
        public void LocalTransport_DeliversNextTickAndTwoLatencyTogglesReturnToInitial()
        {
            int tick = 5;
            var registry = new AnchorRegistry();
            var id = new AnchorId(1);
            registry.Register(id, 0f);
            var transport = new LocalTransport(registry, 0.02f, () => tick);
            var requester = new AnchorRequester(transport, new EventSequencer(), EventOrigin.HumanA);
            requester.Request(id, 1f);
            transport.Pump(5);
            registry.TryGet(id, out AnchorState state);
            Assert.AreEqual(0f, state.Value);
            transport.Pump(6);
            registry.TryGet(id, out state);
            Assert.AreEqual(1f, state.Value);

            var latencyRegistry = new AnchorRegistry();
            latencyRegistry.Register(id, 0f);
            var latencyTransport = new LocalTransport(latencyRegistry, 0.02f, () => tick) { LatencyMs = 300f };
            var toggle = new AnchorToggle(0f);
            var latencyRequester = new AnchorRequester(latencyTransport, new EventSequencer(), EventOrigin.HumanA);
            latencyRequester.Request(id, toggle.Next());
            latencyRequester.Request(id, toggle.Next());
            latencyTransport.Pump(19);
            latencyRegistry.TryGet(id, out state);
            Assert.AreEqual(0f, state.Value);
            latencyTransport.Pump(20);
            latencyRegistry.TryGet(id, out state);
            Assert.AreEqual(0f, state.Value);
            Assert.AreEqual(2u, state.Revision);
        }
    }
}
