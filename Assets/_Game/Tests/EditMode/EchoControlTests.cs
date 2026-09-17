using System;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Transport;

namespace Parallax.Tests.EditMode
{
    public class EchoControlTests
    {
        static EchoFrame Frame(int x) => new EchoFrame { Position = new UnityEngine.Vector2(x, 0f) };

        [Test]
        public void Recorder_ControlEventsUseFrameOffsetIgnoreFullAndThrowAfterFinish()
        {
            var recorder = new EchoRecorder(ObserverId.A, 2);
            recorder.AddControlEvent(ControlChannel.GravityAngle, ObserverId.B, 0.5f);
            recorder.AddFrame(Frame(0));
            recorder.AddControlEvent(ControlChannel.GravityAngle, ObserverId.B, -0.5f);
            recorder.AddFrame(Frame(1));
            recorder.AddControlEvent(ControlChannel.GravityAngle, ObserverId.B, 1f);
            EchoRecording recording = recorder.Finish();
            Assert.AreEqual(2, recording.ControlEvents.Count);
            Assert.AreEqual(0, recording.ControlEvents[0].TickOffset);
            Assert.AreEqual(1, recording.ControlEvents[1].TickOffset);
            Assert.Throws<InvalidOperationException>(() => recorder.AddControlEvent(ControlChannel.GravityAngle, ObserverId.B, 0f));
        }

        [Test]
        public void Recording_SortsControlEventsStably()
        {
            var recording = new EchoRecording(ObserverId.A, new[] { Frame(0) }, Array.Empty<EchoAnchorEvent>(), new[] { new EchoControlEvent(2, ControlChannel.GravityAngle, ObserverId.B, 1f), new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, 0f), new EchoControlEvent(2, ControlChannel.GravityAngle, ObserverId.B, -1f) });
            Assert.AreEqual(0f, recording.ControlEvents[0].Value);
            Assert.AreEqual(1f, recording.ControlEvents[1].Value);
            Assert.AreEqual(-1f, recording.ControlEvents[2].Value);
        }

        [Test]
        public void Playback_ReturnsDueControlsAndAnchorsAndNoneWhileHolding()
        {
            var playback = new EchoPlayback(new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1) }, new[] { new EchoAnchorEvent(0, new AnchorId(1), 1f) }, new[] { new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, 0.5f), new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, -0.5f) }));
            var anchors = new List<EchoAnchorEvent>();
            var controls = new List<EchoControlEvent>();
            playback.Advance(anchors, controls);
            Assert.AreEqual(1, anchors.Count);
            Assert.AreEqual(2, controls.Count);
            playback.Advance(anchors, controls);
            playback.Advance(anchors, controls);
            Assert.AreEqual(0, anchors.Count);
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void Playback_ControlOffsetsZeroZeroTwoAreDueOnTheirFrames()
        {
            var playback = new EchoPlayback(new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, Array.Empty<EchoAnchorEvent>(), new[]
            {
                new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, 0.1f),
                new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, 0.2f),
                new EchoControlEvent(2, ControlChannel.GravityAngle, ObserverId.B, 0.3f),
            }));
            var anchors = new List<EchoAnchorEvent>();
            var controls = new List<EchoControlEvent>();
            playback.Advance(anchors, controls);
            Assert.AreEqual(2, controls.Count);
            playback.Advance(anchors, controls);
            Assert.AreEqual(0, controls.Count);
            playback.Advance(anchors, controls);
            Assert.AreEqual(1, controls.Count);
        }

        [Test]
        public void LocalTransport_ControlPublishedOnTickFiveArrivesOnTickTenAt100Ms()
        {
            int tick = 5;
            var registry = new AnchorRegistry();
            var transport = new LocalTransport(registry, 0.02f, () => tick) { LatencyMs = 100f };
            ControlSample? received = null;
            transport.ControlReceived += sample => received = sample;
            transport.PublishControl(new ControlSample(ControlChannel.GravityAngle, ObserverId.B, 0.5f, tick));
            tick = 9;
            transport.Pump(tick);
            Assert.IsNull(received);
            tick = 10;
            transport.Pump(tick);
            Assert.IsNotNull(received);
            Assert.AreEqual(0.5f, received.Value.Value);
        }

        [Test]
        public void ReplayControlEvents_DeliverInOrderAndDeliverAgainForNewPlayback()
        {
            int tick = 0;
            var transport = new LocalTransport(new AnchorRegistry(), 0.02f, () => tick);
            var values = new List<float>();
            transport.ControlReceived += sample => values.Add(sample.Value);
            var recording = new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, Array.Empty<EchoAnchorEvent>(), new[] { new EchoControlEvent(0, ControlChannel.GravityAngle, ObserverId.B, 0.5f), new EchoControlEvent(2, ControlChannel.GravityAngle, ObserverId.B, -0.5f) });
            for (int replay = 0; replay < 2; replay++)
            {
                var playback = new EchoPlayback(recording);
                var anchors = new List<EchoAnchorEvent>();
                var controls = new List<EchoControlEvent>();
                for (int i = 0; i < 3; i++)
                {
                    playback.Advance(anchors, controls);
                    for (int j = 0; j < controls.Count; j++) transport.PublishControl(new ControlSample(controls[j].Channel, controls[j].Target, controls[j].Value, tick));
                    tick++;
                    transport.Pump(tick);
                }
            }
            CollectionAssert.AreEqual(new[] { 0.5f, -0.5f, 0.5f, -0.5f }, values);
        }
    }
}
