using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Transport
{
    public abstract class TransportHost : MonoBehaviour
    {
        public abstract AnchorRegistry Registry { get; }
        public abstract IRealityTransport Transport { get; }
        public abstract EventSequencer Sequencer { get; }
    }
}
