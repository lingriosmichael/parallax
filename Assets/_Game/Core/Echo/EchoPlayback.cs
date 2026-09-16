using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    public sealed class EchoPlayback
    {
        readonly EchoRecording recording;
        int nextEvent;

        public int Cursor { get; private set; }
        public int FrameCount => recording.Frames.Count;
        public bool IsHolding => Cursor >= FrameCount;
        public EchoFrame Current { get; private set; }

        public EchoPlayback(EchoRecording recording)
        {
            this.recording = recording ?? throw new ArgumentNullException(nameof(recording));
            if (recording.Frames.Count == 0) throw new ArgumentException("Recording must have at least one frame.", nameof(recording));
            Current = recording.Frames[0];
        }

        public EchoFrame Advance(List<EchoAnchorEvent> due)
        {
            due.Clear();
            if (IsHolding) return Current;
            while (nextEvent < recording.AnchorEvents.Count && recording.AnchorEvents[nextEvent].TickOffset <= Cursor)
                due.Add(recording.AnchorEvents[nextEvent++]);
            Current = recording.Frames[Cursor++];
            return Current;
        }
    }
}
