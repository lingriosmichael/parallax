using System.Collections.Generic;

namespace Parallax.Core
{
    public static class RoomSequence
    {
        public static bool HasNext(int current, IReadOnlyCollection<int> doorIds)
        {
            foreach (int id in doorIds)
            {
                if (id == current + 1) return true;
            }
            return false;
        }
    }
}
