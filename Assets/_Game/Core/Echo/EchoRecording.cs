using System.Collections.Generic;
using System.Linq;

namespace Parallax.Core
{
    public sealed class EchoRecording
    {
        public ObserverId Observer { get; }
        public IReadOnlyList<EchoFrame> Frames { get; }
        public IReadOnlyList<EchoAnchorEvent> AnchorEvents { get; }

        public EchoRecording(ObserverId observer, IReadOnlyList<EchoFrame> frames, IReadOnlyList<EchoAnchorEvent> anchorEvents)
        {
            Observer = observer;
            Frames = new List<EchoFrame>(frames).AsReadOnly();
            AnchorEvents = anchorEvents.OrderBy(e => e.TickOffset).ToList().AsReadOnly();
        }
    }
}
