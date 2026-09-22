using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    public readonly struct RoomSummaryRow
    {
        public readonly int RoomNumber;
        public readonly int RoomId;
        public readonly int Deaths;

        public RoomSummaryRow(int roomNumber, int roomId, int deaths)
        {
            RoomNumber = roomNumber;
            RoomId = roomId;
            Deaths = deaths;
        }
    }

    /// <summary>PAX-049 (D-061): pure summary-row and total math, independent of how deaths are
    /// stored or how room order is discovered.</summary>
    public static class LevelSummary
    {
        public static RoomSummaryRow[] BuildRows(IReadOnlyList<int> roomIdsInLevelOrder, Func<int, int> deathsInRoom)
        {
            var rows = new RoomSummaryRow[roomIdsInLevelOrder.Count];
            for (int i = 0; i < roomIdsInLevelOrder.Count; i++)
            {
                int roomId = roomIdsInLevelOrder[i];
                rows[i] = new RoomSummaryRow(i + 1, roomId, deathsInRoom(roomId));
            }
            return rows;
        }

        public static int Total(RoomSummaryRow[] rows)
        {
            int total = 0;
            for (int i = 0; i < rows.Length; i++) total += rows[i].Deaths;
            return total;
        }
    }
}
