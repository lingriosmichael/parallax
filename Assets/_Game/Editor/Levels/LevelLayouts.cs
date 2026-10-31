using System.Collections.Generic;
using Parallax.Editor.Setup;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): the single map from level id to its code-as-data layout. Every
    // LevelListConfig entry must have a registered layout, and every registered layout must be
    // listed in LevelListConfig (tested both ways).
    public static class LevelLayouts
    {
        public static readonly IReadOnlyDictionary<string, SoloRoomDefinition> ById = new Dictionary<string, SoloRoomDefinition>
        {
            ["L001"] = L001Layout.Build(),
            ["L002"] = L002Layout.Build(),
            ["L003"] = L003Layout.Build(),
            ["L004"] = L004Layout.Build(),
            ["L005"] = L005Layout.Build(),
            ["L006"] = L006Layout.Build(),
            ["L007"] = L007Layout.Build(),
            ["L008"] = L008Layout.Build(),
            ["L009"] = L009Layout.Build(),
            ["L010"] = L010Layout.Build(),
            ["L011"] = L011Layout.Build(),
            ["L012"] = L012Layout.Build(),
            ["L013"] = L013Layout.Build(),
            ["L014"] = L014Layout.Build(),
            ["L015"] = L015Layout.Build(),
            ["L016"] = L016Layout.Build(),
            ["L017"] = L017Layout.Build(),
            ["L018"] = L018Layout.Build(),
            ["L019"] = L019Layout.Build(),
            ["L020"] = L020Layout.Build(),
            ["L021"] = L021Layout.Build(),
            ["L022"] = L022Layout.Build(),
            ["L023"] = L023Layout.Build(),
            ["L024"] = L024Layout.Build(),
            ["L025"] = L025Layout.Build(),
            ["L026"] = L026Layout.Build(),
            ["L027"] = L027Layout.Build(),
            ["L028"] = L028Layout.Build(),
            ["L029"] = L029Layout.Build(),
            ["L030"] = L030Layout.Build(),
            ["L031"] = L031Layout.Build(),
            ["L032"] = L032Layout.Build(),
            ["L033"] = L033Layout.Build(),
            ["L034"] = L034Layout.Build(),
            ["L035"] = L035Layout.Build(),
            ["L036"] = L036Layout.Build(),
            ["L037"] = L037Layout.Build(),
            ["L038"] = L038Layout.Build(),
            ["L039"] = L039Layout.Build(),
            ["L040"] = L040Layout.Build(),
        };

        public static bool TryGet(string id, out SoloRoomDefinition layout) => ById.TryGetValue(id, out layout);
    }
}
