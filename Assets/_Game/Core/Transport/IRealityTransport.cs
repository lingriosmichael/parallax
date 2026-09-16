using System;

namespace Parallax.Core
{
    public interface IRealityTransport
    {
        bool IsSessionAuthority { get; } // may this device commit anchors/puzzle state?
        int Tick { get; }                // shared tick (network tick in co-op, local fixed-step count in solo)

        void RequestAnchor(in AnchorRequest request); // routed to session authority
        void PublishControl(in ControlSample sample);  // owner-published stream
        void SendCue(in SpectacleCue cue);

        event Action<ControlSample> ControlReceived;
        event Action<SpectacleCue> CueReceived;
        // Anchor state arrives via AnchorRegistry.Changed (authority) or ApplyReplicated (others).
    }
}
