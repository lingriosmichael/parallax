using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-049 (D-061) §5 test 3: exact PARALLAX_STATS line format, prefix included.</summary>
    public sealed class LevelStatsLogTests
    {
        [Test]
        public void RoomClear_MatchesExactFormat()
        {
            string line = LevelStatsLog.RoomClear(roomNumber: 2, deaths: 5, ticks: 812);
            Assert.AreEqual("PARALLAX_STATS room_clear room=2 deaths=5 ticks=812", line);
        }

        [Test]
        public void LevelComplete_MatchesExactFormat()
        {
            var rows = new[]
            {
                new RoomSummaryRow(1, 0, 3),
                new RoomSummaryRow(2, 1, 0),
                new RoomSummaryRow(3, 2, 7),
            };
            string line = LevelStatsLog.LevelComplete(rows, LevelSummary.Total(rows));
            Assert.AreEqual("PARALLAX_STATS level_complete total=10 deaths=3,0,7", line);
        }
    }
}
