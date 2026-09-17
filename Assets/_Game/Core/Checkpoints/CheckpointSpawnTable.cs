using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Core
{
    public sealed class CheckpointSpawnTable
    {
        struct Entry
        {
            public Vector2 LocalPosition;
            public Vector2 GravityDirection;
        }

        readonly Dictionary<ObserverId, SortedList<int, Entry>> entriesByObserver = new Dictionary<ObserverId, SortedList<int, Entry>>();

        public void Set(int id, ObserverId observer, Vector2 localPosition, Vector2 gravityDirection)
        {
            if (!entriesByObserver.TryGetValue(observer, out SortedList<int, Entry> entries))
            {
                entries = new SortedList<int, Entry>();
                entriesByObserver.Add(observer, entries);
            }
            entries[id] = new Entry { LocalPosition = localPosition, GravityDirection = gravityDirection };
        }

        public bool TryGet(int current, ObserverId observer, out Vector2 localPosition, out Vector2 gravityDirection)
        {
            localPosition = default;
            gravityDirection = default;

            if (!entriesByObserver.TryGetValue(observer, out SortedList<int, Entry> entries)) return false;

            bool found = false;
            Entry best = default;
            IList<int> keys = entries.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                int id = keys[i];
                if (id > current) break;
                best = entries[id];
                found = true;
            }

            if (!found) return false;

            localPosition = best.LocalPosition;
            gravityDirection = best.GravityDirection;
            return true;
        }
    }
}
