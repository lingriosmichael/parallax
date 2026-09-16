using System.Collections.Generic;

namespace Parallax.Core
{
    // Per-origin monotonic counter. Starts at 1 so 0 can never pass the
    // duplicate check against a fresh registry (lastAppliedSequence defaults to 0).
    public sealed class EventSequencer
    {
        readonly Dictionary<EventOrigin, uint> next = new Dictionary<EventOrigin, uint>();

        public uint Next(EventOrigin origin)
        {
            uint value = next.TryGetValue(origin, out uint current) ? current : 1;
            next[origin] = value + 1;
            return value;
        }
    }
}
