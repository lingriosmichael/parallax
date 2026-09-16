using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Transport;

namespace Parallax.Tests
{
    public class LocalTransportTests
    {
        const float Dt = 0.02f;
        static readonly AnchorId Id = new AnchorId(1);

        int now;
        AnchorRegistry registry;
        LocalTransport transport;

        [SetUp]
        public void SetUp()
        {
            now = 0;
            registry = new AnchorRegistry();
            registry.Register(Id, 0f);
            transport = new LocalTransport(registry, Dt, () => now);
        }

        // Mirrors ObserverSet: the tick advances, then the transport is pumped at the end of that tick.
        void Step(int tick)
        {
            now = tick;
            transport.Pump(tick);
        }

        [Test]
        public void ZeroLatency_DoesNotCommitBeforePump_CommitsOnTickPlusOne()
        {
            Step(0);

            transport.RequestAnchor(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            transport.Pump(0);
            registry.TryGet(Id, out AnchorState beforeState);
            Assert.AreEqual(0f, beforeState.Value);

            Step(1);
            registry.TryGet(Id, out AnchorState afterState);
            Assert.AreEqual(1f, afterState.Value);
        }

        [Test]
        public void Latency100ms_DeliveredAtTickPlus5_NotTickPlus4()
        {
            Step(0);
            transport.LatencyMs = 100f;

            transport.RequestAnchor(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            Step(4);
            registry.TryGet(Id, out AnchorState atFour);
            Assert.AreEqual(0f, atFour.Value);

            Step(5);
            registry.TryGet(Id, out AnchorState atFive);
            Assert.AreEqual(1f, atFive.Value);
        }

        [Test]
        public void Latency400ms_DeliveredAtTickPlus20()
        {
            Step(0);
            transport.LatencyMs = 400f;

            transport.RequestAnchor(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            Step(19);
            registry.TryGet(Id, out AnchorState atNineteen);
            Assert.AreEqual(0f, atNineteen.Value);

            Step(20);
            registry.TryGet(Id, out AnchorState atTwenty);
            Assert.AreEqual(1f, atTwenty.Value);
        }

        [Test]
        public void LatencyMs_ClampsToRange()
        {
            transport.LatencyMs = 1000f;
            Assert.AreEqual(400f, transport.LatencyMs);

            transport.LatencyMs = -5f;
            Assert.AreEqual(0f, transport.LatencyMs);
        }

        [Test]
        public void TwoRequestsSameTick_DeliveredInEnqueueOrder_SecondTargetWins()
        {
            Step(0);

            transport.RequestAnchor(new AnchorRequest(Id, 0.3f, EventOrigin.HumanA, 1));
            transport.RequestAnchor(new AnchorRequest(Id, 0.7f, EventOrigin.HumanA, 2));

            Step(1);

            registry.TryGet(Id, out AnchorState state);
            Assert.AreEqual(0.7f, state.Value);
        }

        [Test]
        public void LatencyChangeAfterEnqueue_DoesNotReschedule_LaterZeroLatencyItemMayArriveFirst()
        {
            Step(0);

            transport.LatencyMs = 200f;
            transport.RequestAnchor(new AnchorRequest(Id, 0.5f, EventOrigin.HumanA, 1)); // delivery tick 10

            transport.LatencyMs = 0f;
            transport.RequestAnchor(new AnchorRequest(Id, 0.9f, EventOrigin.HumanB, 1)); // delivery tick 1

            Step(1);
            registry.TryGet(Id, out AnchorState afterFirstStep);
            Assert.AreEqual(0.9f, afterFirstStep.Value); // the 0ms item arrived first
            Assert.AreEqual(1, transport.PendingCount); // the 200ms item is still pending

            Step(9);
            registry.TryGet(Id, out AnchorState afterNinthStep);
            Assert.AreEqual(0.9f, afterNinthStep.Value); // the 200ms item still hasn't arrived
            Assert.AreEqual(1, transport.PendingCount);

            Step(10);
            registry.TryGet(Id, out AnchorState afterTenthStep);
            Assert.AreEqual(0.5f, afterTenthStep.Value); // the 200ms item kept its original delivery tick
            Assert.AreEqual(0, transport.PendingCount);
        }

        [Test]
        public void PublishControl_RaisesControlReceivedAfterDelay()
        {
            Step(0);
            transport.LatencyMs = 100f;

            ControlSample? received = null;
            transport.ControlReceived += sample => received = sample;

            transport.PublishControl(new ControlSample(ControlChannel.GravityAngle, ObserverId.A, 0.5f, 0));

            Step(4);
            Assert.IsNull(received);

            Step(5);
            Assert.IsNotNull(received);
        }

        [Test]
        public void SendCue_RaisesCueReceivedAfterDelay()
        {
            Step(0);
            transport.LatencyMs = 100f;

            SpectacleCue? received = null;
            transport.CueReceived += cue => received = cue;

            transport.SendCue(new SpectacleCue(7, 0, 42));

            Step(4);
            Assert.IsNull(received);

            Step(5);
            Assert.IsNotNull(received);
        }

        [Test]
        public void IsSessionAuthority_IsAlwaysTrue()
        {
            Assert.IsTrue(transport.IsSessionAuthority);
        }

        [Test]
        public void Tick_ReflectsTickSource()
        {
            now = 3;
            Assert.AreEqual(3, transport.Tick);
        }

        [Test]
        public void PendingCount_DecreasesOnDelivery()
        {
            Step(0);
            transport.LatencyMs = 100f;

            transport.RequestAnchor(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));
            Assert.AreEqual(1, transport.PendingCount);

            Step(4);
            Assert.AreEqual(1, transport.PendingCount);

            Step(5);
            Assert.AreEqual(0, transport.PendingCount);
        }

        [Test]
        public void RequestDuringTick_NotDeliveredBySameTicksPump()
        {
            now = 5; // mid-tick, before the pump
            transport.LatencyMs = 0f;

            transport.RequestAnchor(new AnchorRequest(Id, 1f, EventOrigin.HumanA, 1));

            transport.Pump(5);
            registry.TryGet(Id, out AnchorState afterSameTickPump);
            Assert.AreEqual(0f, afterSameTickPump.Value);
            Assert.AreEqual(1, transport.PendingCount);

            Step(6);
            registry.TryGet(Id, out AnchorState afterNextStep);
            Assert.AreEqual(1f, afterNextStep.Value);
        }
    }
}
