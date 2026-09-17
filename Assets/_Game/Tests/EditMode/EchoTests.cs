using System;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Transport;

namespace Parallax.Tests.EditMode
{
    public class EchoTests
    {
        static EchoFrame Frame(float x) => new EchoFrame { Position = new UnityEngine.Vector2(x, 0f), FacingRight = true };

        [Test]
        public void EventOrigins_EchoMapsObservers()
        {
            Assert.AreEqual(EventOrigin.EchoA, EventOrigins.Echo(ObserverId.A));
            Assert.AreEqual(EventOrigin.EchoB, EventOrigins.Echo(ObserverId.B));
        }

        [Test]
        public void Recorder_CapsFramesAndCapturesEventOffsets()
        {
            var recorder = new EchoRecorder(ObserverId.A, 3);
            recorder.AddAnchorEvent(new AnchorId(1), 1f);
            recorder.AddFrame(Frame(0));
            recorder.AddFrame(Frame(1));
            recorder.AddAnchorEvent(new AnchorId(1), 0f);
            recorder.AddFrame(Frame(2));
            recorder.AddFrame(Frame(3));
            recorder.AddAnchorEvent(new AnchorId(1), 1f);
            Assert.IsTrue(recorder.IsFull);
            EchoRecording recording = recorder.Finish();
            Assert.AreEqual(3, recording.Frames.Count);
            Assert.AreEqual(2, recording.AnchorEvents.Count);
            Assert.AreEqual(0, recording.AnchorEvents[0].TickOffset);
            Assert.AreEqual(2, recording.AnchorEvents[1].TickOffset);
            Assert.Throws<InvalidOperationException>(() => recorder.Finish());
            Assert.Throws<ArgumentOutOfRangeException>(() => new EchoRecorder(ObserverId.A, 0));
        }

        [Test]
        public void Playback_OrdersFramesEventsAndHolds()
        {
            var recording = new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, new[]
            {
                new EchoAnchorEvent(1, new AnchorId(1), 1f),
                new EchoAnchorEvent(1, new AnchorId(2), 0f),
            }, Array.Empty<EchoControlEvent>());
            var playback = new EchoPlayback(recording);
            var due = new List<EchoAnchorEvent>();
            var controls = new List<EchoControlEvent>();
            Assert.AreEqual(0f, playback.Current.Position.x);
            Assert.AreEqual(0f, playback.Advance(due, controls).Position.x);
            Assert.AreEqual(0, due.Count);
            Assert.AreEqual(1f, playback.Advance(due, controls).Position.x);
            Assert.AreEqual(2, due.Count);
            Assert.AreEqual((ushort)1, due[0].Anchor.Value);
            Assert.AreEqual((ushort)2, due[1].Anchor.Value);
            Assert.AreEqual(2f, playback.Advance(due, controls).Position.x);
            Assert.IsTrue(playback.IsHolding);
            Assert.AreEqual(2f, playback.Advance(due, controls).Position.x);
            Assert.AreEqual(0, due.Count);
        }

        [Test]
        public void Playback_RejectsZeroFramesAndIsDeterministic()
        {
            Assert.Throws<ArgumentException>(() => new EchoPlayback(new EchoRecording(ObserverId.A, Array.Empty<EchoFrame>(), Array.Empty<EchoAnchorEvent>(), Array.Empty<EchoControlEvent>())));
            var recording = new EchoRecording(ObserverId.B, new[] { Frame(0), Frame(1) }, new[] { new EchoAnchorEvent(0, new AnchorId(1), 1f) }, Array.Empty<EchoControlEvent>());
            var a = new EchoPlayback(recording);
            var b = new EchoPlayback(recording);
            var dueA = new List<EchoAnchorEvent>();
            var dueB = new List<EchoAnchorEvent>();
            var controlsA = new List<EchoControlEvent>();
            var controlsB = new List<EchoControlEvent>();
            for (int i = 0; i < recording.Frames.Count + 5; i++)
            {
                Assert.AreEqual(a.Advance(dueA, controlsA).Position, b.Advance(dueB, controlsB).Position);
                Assert.AreEqual(dueA.Count, dueB.Count);
            }
        }

        [Test]
        public void ReplayTwice_UsesFreshSequencesAndAppliesBothTimes()
        {
            int tick = 0;
            var registry = new AnchorRegistry();
            var id = new AnchorId(1);
            registry.Register(id, 0f);
            var transport = new LocalTransport(registry, 0.02f, () => tick);
            var requester = new AnchorRequester(transport, new EventSequencer(), EventOrigin.EchoA);
            var recording = new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, new[]
            {
                new EchoAnchorEvent(0, id, 1f), new EchoAnchorEvent(2, id, 0f)
            }, Array.Empty<EchoControlEvent>());
            for (int replay = 0; replay < 2; replay++)
            {
                var playback = new EchoPlayback(recording);
                var due = new List<EchoAnchorEvent>();
                var controls = new List<EchoControlEvent>();
                for (int i = 0; i < 3; i++)
                {
                    playback.Advance(due, controls);
                    foreach (EchoAnchorEvent item in due) requester.Request(item.Anchor, item.TargetValue);
                    tick++;
                    transport.Pump(tick);
                    Assert.AreEqual(CommitResult.Applied, transport.LastCommitResult);
                }
            }
            registry.TryGet(id, out AnchorState state);
            Assert.AreEqual(0f, state.Value);
            Assert.AreEqual(4u, state.Revision);
        }

        [Test]
        public void Recorder_EventAfterFull_IsIgnored()
        {
            var recorder = new EchoRecorder(ObserverId.A, 2);
            recorder.AddFrame(Frame(0));
            recorder.AddFrame(Frame(1));
            recorder.AddAnchorEvent(new AnchorId(1), 1f);
            Assert.AreEqual(0, recorder.Finish().AnchorEvents.Count);
        }

        [Test]
        public void Recorder_AnyUseAfterFinish_Throws()
        {
            var recorder = new EchoRecorder(ObserverId.A, 2);
            recorder.Finish();
            Assert.Throws<InvalidOperationException>(() => recorder.AddFrame(Frame(0)));
            Assert.Throws<InvalidOperationException>(() => recorder.AddAnchorEvent(new AnchorId(1), 1f));
            Assert.Throws<InvalidOperationException>(() => recorder.Finish());
        }

        [Test]
        public void Playback_WhileHolding_CurrentIsLastFrame_NoDueEvents()
        {
            var playback = new EchoPlayback(new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1) }, new[] { new EchoAnchorEvent(1, new AnchorId(1), 1f) }, Array.Empty<EchoControlEvent>()));
            var due = new List<EchoAnchorEvent>();
            var controls = new List<EchoControlEvent>();
            playback.Advance(due, controls);
            playback.Advance(due, controls);
            for (int i = 0; i < 3; i++)
            {
                playback.Advance(due, controls);
                Assert.AreEqual(1f, playback.Current.Position.x);
                Assert.AreEqual(0, due.Count);
            }
        }

        [Test]
        public void Playback_PastOffsetsDoNotBlockLaterEvents()
        {
            var playback = new EchoPlayback(new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, new[] { new EchoAnchorEvent(0, new AnchorId(1), 1f), new EchoAnchorEvent(0, new AnchorId(2), 1f), new EchoAnchorEvent(2, new AnchorId(3), 1f) }, Array.Empty<EchoControlEvent>()));
            var due = new List<EchoAnchorEvent>();
            var controls = new List<EchoControlEvent>();
            playback.Advance(due, controls);
            Assert.AreEqual(2, due.Count);
            playback.Advance(due, controls);
            Assert.AreEqual(0, due.Count);
            playback.Advance(due, controls);
            Assert.AreEqual(1, due.Count);
        }

        [Test]
        public void Recording_SortsEventsStably()
        {
            var recording = new EchoRecording(ObserverId.A, new[] { Frame(0), Frame(1), Frame(2) }, new[] { new EchoAnchorEvent(2, new AnchorId(1), 1f), new EchoAnchorEvent(0, new AnchorId(2), 1f), new EchoAnchorEvent(2, new AnchorId(3), 1f) }, Array.Empty<EchoControlEvent>());
            Assert.AreEqual((ushort)2, recording.AnchorEvents[0].Anchor.Value);
            Assert.AreEqual((ushort)1, recording.AnchorEvents[1].Anchor.Value);
            Assert.AreEqual((ushort)3, recording.AnchorEvents[2].Anchor.Value);
        }
    }
}
