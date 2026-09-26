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
        };

        public static bool TryGet(string id, out SoloRoomDefinition layout) => ById.TryGetValue(id, out layout);
    }
}
