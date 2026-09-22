using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-049 (D-061) §5 tests 1-2: pure row/total math.</summary>
    public sealed class LevelSummaryTests
    {
        [Test]
        public void BuildRows_FollowsLevelOrder_IncludesZeroDeathRooms_OneBasedRoomNumbersNotIds()
        {
            // Ids are deliberately non-contiguous and not 1-based, so a row's RoomNumber must
            // come from its POSITION in roomIdsInLevelOrder, never from the id itself.
            var roomIds = new List<int> { 2, 5, 9 };
            var deaths = new Dictionary<int, int> { { 2, 3 }, { 9, 7 } }; // 5 has no entry -> 0 deaths

            RoomSummaryRow[] rows = LevelSummary.BuildRows(roomIds, id => deaths.TryGetValue(id, out int d) ? d : 0);

            Assert.AreEqual(3, rows.Length);
            Assert.AreEqual(1, rows[0].RoomNumber); Assert.AreEqual(2, rows[0].RoomId); Assert.AreEqual(3, rows[0].Deaths);
            Assert.AreEqual(2, rows[1].RoomNumber); Assert.AreEqual(5, rows[1].RoomId); Assert.AreEqual(0, rows[1].Deaths);
            Assert.AreEqual(3, rows[2].RoomNumber); Assert.AreEqual(9, rows[2].RoomId); Assert.AreEqual(7, rows[2].Deaths);
        }

        [Test]
        public void Total_EqualsSumOfRows()
        {
            var roomIds = new List<int> { 0, 1, 2, 3 };
            var deaths = new Dictionary<int, int> { { 0, 4 }, { 1, 0 }, { 2, 2 }, { 3, 1 } };
            RoomSummaryRow[] rows = LevelSummary.BuildRows(roomIds, id => deaths[id]);

            Assert.AreEqual(7, LevelSummary.Total(rows));
        }
    }
}
