using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Parallax.Tests.EditMode
{
    // D-104 amendment (the developer, 2026-10-02): every level keeps L001's 1.8× camera, including the traps that set off
    // out of view at that zoom. These are the camera tell (D-083) and chaos (D-093) findings accepted with it, each pinned to
    // its level and, for the tell, to its betrayal and trap; any other finding, or the same rule failing on another trap or
    // level, still fails. A layout fix that clears one should remove its entry.
    public static class LevelZoomAccepted
    {
        // (level, betrayal, trap) for the camera tell.
        static readonly (string Level, string Betrayal, string Trap)[] Tells =
        {
            ("L004", "Dead end: the flip toward the door drops a cat onto spikes under the slab", "Spikes_L"),
            ("L005", "T1: Arrow_A hits a cat that runs along S2", "Arrow_A"),
            ("L005", "T4: Arrow_C hits a cat that jumps over the nook", "Arrow_C"),
            ("L005", "T5: Arrow_D hits a cat that leaves the nook once Arrow_C has passed", "Arrow_D"),
            ("L008", "Dead end: the hole behind the start", "Floor_9"),
            ("L009", "T2: Arrow_2 catches a cat that walked under the floating flip and goes on", "Arrow_2"),
            ("L010", "T7: the floor before the floor flip drops away under a cat that stops on it", "Drop_10"),
            ("L011", "T3c [OL]: a cat that hops onto Ledge_M instead of firing the next step falls to the foot of the shaft", "Ledge_M"),
            ("L012", "Dead end D1 [OL]: the Slot, a shaft straight down from hall 5", "Slot_Floor"),
            ("L012", "T5 [NW]: a cat that stops where the hop lands is caught by Spikes_D", "Orb_A"),
            ("L014", "T4 [B]: a cat that climbs on down V3 into the lane is struck by Spear_4", "Spear_4"),
            ("L016", "T3 [SS]: a cat that jumps out of the Dip at once meets Arrow_3", "Arrow_3"),
            ("L016", "T3b [W]: the arrow from under the Bed comes down on a cat that walks on along the floor", "Arrow_U"),
            ("L016", "T7 [LW]: a cat that jumps Recess_7 meets the hidden Flip_H7 and falls onto Spikes_7", "Spikes_7"),
            ("L017", "T8 [W]: a cat that jumps the hole as Arrow_8 comes is struck", "Arrow_8"),
            ("L018", "Dead end D1 [LW]: a cat that steps east off the lowered drop walks onto Spikes_3", "Spikes_3"),
            ("L018", "T14 [OL]: a cat that steers G_C's launch short misses the Crown and comes down on Thorns_C", "G_C"),
            ("L018", "T5 [J]: a cat that stands on SL_1 when Spear_S comes is struck", "Spear_S"),
            ("L019", "T1 [W]: a cat that steps off the cliff before Spear_1 has stuck falls through the volley", "Spear_4"),
            ("L019", "T4 [W]: a cat that steps straight down from step 3 meets Sweeper_4", "Sweeper_4"),
            ("L019", "T8 [SS]: a cat that keeps holding right as the inversion ends falls short of P4", "Cat.Inverted"),
            ("L020", "T9 [BAIT]: a cat that chases the door runs off the Loft into Shrink_8's well", "Door"),
        };

        // Levels whose chaos moment has fewer than band 2's five elements in view at 1.8×.
        static readonly HashSet<string> Chaos = new() { "L014", "L015", "L016", "L017", "L019", "L020" };

        public static bool IsAccepted(string error)
        {
            if (error.Contains("(D-083 camera tell rule)"))
                return Tells.Any(t => error.StartsWith(t.Level + ": ") && error.Contains($"'{t.Betrayal}'") && error.Contains($": {t.Trap} @ "));
            if (error.Contains("the chaos moment has") && error.Contains("(D-093)"))
                return Chaos.Any(level => error.StartsWith(level + ": "));
            return false;
        }

        /// <summary>The errors minus the accepted findings; the accepted ones are written to the test's output.</summary>
        public static List<string> Unaccepted(IEnumerable<string> errors)
        {
            var kept = new List<string>();
            foreach (string e in errors)
            {
                if (IsAccepted(e)) TestContext.Out.WriteLine("accepted at the 1.8× zoom (D-104 amendment): " + e);
                else kept.Add(e);
            }
            return kept;
        }
    }
}
