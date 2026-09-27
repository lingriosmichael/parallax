using System.Text;

namespace Parallax.Core
{
    /// <summary>PAX-090 (D-091): the current checkpoint section of the live room, and deaths per section for the stats log.
    /// Section 0 is the room's start; gate N makes section N current, only from section N-1 (gates pass in order). A room
    /// with no sections has Count 0: no gate, no count.</summary>
    public sealed class SectionProgress
    {
        int[] deaths = System.Array.Empty<int>();

        public int Count => deaths.Length;
        public int Current { get; private set; }
        /// <summary>A gate has been passed: a death rewinds to it instead of resetting the room.</summary>
        public bool HasCheckpoint => Current > 0;

        public void Begin(int sectionCount)
        {
            deaths = new int[sectionCount < 0 ? 0 : sectionCount];
            Current = 0;
        }

        public bool TryEnter(int index)
        {
            if (index != Current + 1 || index >= Count) return false;
            Current = index;
            return true;
        }

        public void RecordDeath()
        {
            if (Count > 0) deaths[Current]++;
        }

        public int DeathsIn(int index) => index >= 0 && index < Count ? deaths[index] : 0;

        /// <summary>R6: "PARALLAX_SECTIONS room=&lt;id&gt; &lt;name&gt;=&lt;deaths&gt; ... total=&lt;n&gt;", in section order.</summary>
        public string Format(int roomId, string[] names)
        {
            var line = new StringBuilder("PARALLAX_SECTIONS room=").Append(roomId);
            int total = 0;
            for (int i = 0; i < Count; i++)
            {
                line.Append(' ').Append(names != null && i < names.Length ? names[i] : i.ToString()).Append('=').Append(deaths[i]);
                total += deaths[i];
            }
            return line.Append(" total=").Append(total).ToString();
        }
    }
}
