using System.Text;

namespace Parallax.Core
{
    /// <summary>PAX-049 (D-061): PARALLAX_STATS line formatting, kept as pure string building
    /// (no UnityEngine.Debug call here) so the exact format is EditMode-testable without a
    /// console.</summary>
    public static class LevelStatsLog
    {
        public const string Prefix = "PARALLAX_STATS";

        public static string RoomClear(int roomNumber, int deaths, int ticks) =>
            $"{Prefix} room_clear room={roomNumber} deaths={deaths} ticks={ticks}";

        public static string LevelComplete(RoomSummaryRow[] rows, int total)
        {
            var sb = new StringBuilder();
            sb.Append(Prefix).Append(" level_complete total=").Append(total).Append(" deaths=");
            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(rows[i].Deaths);
            }
            return sb.ToString();
        }
    }
}
