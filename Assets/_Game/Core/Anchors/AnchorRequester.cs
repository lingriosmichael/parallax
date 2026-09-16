using System;

namespace Parallax.Core
{
    public sealed class AnchorRequester : IAnchorRequester
    {
        readonly IRealityTransport transport;
        readonly EventSequencer sequencer;

        public EventOrigin Origin { get; }

        public AnchorRequester(IRealityTransport transport, EventSequencer sequencer, EventOrigin origin)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            Origin = origin;
        }

        public void Request(AnchorId anchor, float targetValue)
        {
            transport.RequestAnchor(new AnchorRequest(anchor, targetValue, Origin, sequencer.Next(Origin)));
        }
    }
}
