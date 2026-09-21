using System.Collections.Generic;

namespace Parallax.Core
{
    public readonly struct AnchorReset
    {
        public readonly AnchorId AnchorId;
        public readonly float InitialValue;

        public AnchorReset(AnchorId anchorId, float initialValue)
        {
            AnchorId = anchorId;
            InitialValue = initialValue;
        }
    }

    public readonly struct RoomResetResult
    {
        public readonly int TrapsReset;
        public readonly int AnchorsReset;

        public RoomResetResult(int trapsReset, int anchorsReset)
        {
            TrapsReset = trapsReset;
            AnchorsReset = anchorsReset;
        }
    }

    public sealed class RoomResetRegistry
    {
        readonly Dictionary<int, List<IRoomResettable>> itemsByRoom = new Dictionary<int, List<IRoomResettable>>();
        readonly Dictionary<AnchorId, AnchorResetBinding> anchors = new Dictionary<AnchorId, AnchorResetBinding>();

        struct AnchorResetBinding
        {
            public int RoomId;
            public float InitialValue;
        }

        public bool Register(IRoomResettable item)
        {
            if (item == null) return false;
            if (!itemsByRoom.TryGetValue(item.RoomId, out List<IRoomResettable> items))
            {
                items = new List<IRoomResettable>();
                itemsByRoom.Add(item.RoomId, items);
            }
            if (items.Contains(item)) return false;
            items.Add(item);
            return true;
        }

        public bool Unregister(IRoomResettable item)
        {
            if (item == null || !itemsByRoom.TryGetValue(item.RoomId, out List<IRoomResettable> items)) return false;
            if (!items.Remove(item)) return false;
            if (items.Count == 0) itemsByRoom.Remove(item.RoomId);
            return true;
        }

        public bool RegisterAnchor(int roomId, AnchorId id, float initialValue)
        {
            if (anchors.ContainsKey(id)) return false;
            anchors.Add(id, new AnchorResetBinding { RoomId = roomId, InitialValue = initialValue });
            return true;
        }

        public RoomResetResult Reset(int roomId, List<AnchorReset> anchorResetsOut)
        {
            int trapCount = 0;
            if (itemsByRoom.TryGetValue(roomId, out List<IRoomResettable> items))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].ResetToInitial();
                    trapCount++;
                }
            }

            int anchorCount = 0;
            foreach (KeyValuePair<AnchorId, AnchorResetBinding> entry in anchors)
            {
                if (entry.Value.RoomId != roomId) continue;
                anchorResetsOut?.Add(new AnchorReset(entry.Key, entry.Value.InitialValue));
                anchorCount++;
            }
            return new RoomResetResult(trapCount, anchorCount);
        }
    }
}
