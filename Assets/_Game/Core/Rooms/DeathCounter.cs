using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-047 D-058: per-room death count. Never IRoomResettable, so a room reset never touches it.</summary>
    public sealed class DeathCounter
    {
        readonly Dictionary<int, int> counts = new Dictionary<int, int>();

        public int DeathsIn(int roomId) => counts.TryGetValue(roomId, out int count) ? count : 0;

        public void Record(int roomId) => counts[roomId] = DeathsIn(roomId) + 1;
    }
}
