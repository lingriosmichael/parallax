using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    public sealed class EchoPlayback
    {
        readonly EchoRecording recording;
        int nextAnchorEvent;
        int nextControlEvent;

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

        public EchoFrame Advance(List<EchoAnchorEvent> dueAnchors, List<EchoControlEvent> dueControls)
        {
            dueAnchors.Clear();
            dueControls.Clear();
            if (IsHolding) return Current;
            while (nextAnchorEvent < recording.AnchorEvents.Count && recording.AnchorEvents[nextAnchorEvent].TickOffset <= Cursor)
                dueAnchors.Add(recording.AnchorEvents[nextAnchorEvent++]);
            while (nextControlEvent < recording.ControlEvents.Count && recording.ControlEvents[nextControlEvent].TickOffset <= Cursor)
                dueControls.Add(recording.ControlEvents[nextControlEvent++]);
            Current = recording.Frames[Cursor++];
            return Current;
        }
    }
}
