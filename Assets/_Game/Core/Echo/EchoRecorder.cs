using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    public sealed class EchoRecorder
    {
        readonly ObserverId observer;
        readonly int maxFrames;
        readonly List<EchoFrame> frames = new List<EchoFrame>();
        readonly List<EchoAnchorEvent> events = new List<EchoAnchorEvent>();
        bool finished;

        public int FrameCount => frames.Count;
        public bool IsFull => FrameCount == maxFrames;

        public EchoRecorder(ObserverId observer, int maxFrames)
        {
            if (maxFrames < 1) throw new ArgumentOutOfRangeException(nameof(maxFrames));
            this.observer = observer;
            this.maxFrames = maxFrames;
        }

        public void AddFrame(in EchoFrame frame)
        {
            EnsureOpen();
            if (!IsFull) frames.Add(frame);
        }

        public void AddAnchorEvent(AnchorId anchor, float targetValue)
        {
            EnsureOpen();
            if (!IsFull) events.Add(new EchoAnchorEvent(FrameCount, anchor, targetValue));
        }

        public EchoRecording Finish()
        {
            EnsureOpen();
            finished = true;
            return new EchoRecording(observer, frames.ToArray(), events.ToArray());
        }

        void EnsureOpen()
        {
            if (finished) throw new InvalidOperationException("EchoRecorder has already been finished.");
        }
    }
}
