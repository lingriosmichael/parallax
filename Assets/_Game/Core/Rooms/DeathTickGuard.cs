using System.Collections.Generic;

namespace Parallax.Core
{
    public sealed class DeathTickGuard
    {
        readonly Dictionary<ObserverId, int> acceptedTicks = new Dictionary<ObserverId, int>();

        public bool TryAccept(ObserverId observer, int tick)
        {
            if (acceptedTicks.TryGetValue(observer, out int acceptedTick) && acceptedTick == tick) return false;
            acceptedTicks[observer] = tick;
            return true;
        }
    }
}
