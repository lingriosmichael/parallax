using System.Collections.Generic;
using Parallax.Editor.Routes;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): each level's declared anatomy (a solution route and its betrayal routes), keyed
    // like LevelLayouts. LevelLayoutValidator.ValidateRoutes replays them through the real game code.
    public static class LevelRoutes
    {
        public static readonly IReadOnlyDictionary<string, RoomRoutes> ById = new Dictionary<string, RoomRoutes>
        {
            ["L001"] = L001Routes.Build(),
            ["L002"] = L002Routes.Build(),
            ["L003"] = L003Routes.Build(),
            ["L004"] = L004Routes.Build(),
            ["L005"] = L005Routes.Build(),
            ["L006"] = L006Routes.Build(),
            ["L007"] = L007Routes.Build(),
            ["L008"] = L008Routes.Build(),
            ["L009"] = L009Routes.Build(),
            ["L010"] = L010Routes.Build(),
            ["L011"] = L011Routes.Build(),
            ["L012"] = L012Routes.Build(),
            ["L013"] = L013Routes.Build(),
            ["L014"] = L014Routes.Build(),
            ["L015"] = L015Routes.Build(),
            ["L016"] = L016Routes.Build(),
            ["L017"] = L017Routes.Build(),
            ["L018"] = L018Routes.Build(),
            ["L019"] = L019Routes.Build(),
            ["L020"] = L020Routes.Build(),
            ["L021"] = L021Routes.Build(),
            ["L022"] = L022Routes.Build(),
            ["L023"] = L023Routes.Build(),
            ["L024"] = L024Routes.Build(),
            ["L025"] = L025Routes.Build(),
            ["L026"] = L026Routes.Build(),
            ["L027"] = L027Routes.Build(),
            ["L028"] = L028Routes.Build(),
            ["L029"] = L029Routes.Build(),
            ["L030"] = L030Routes.Build(),
            ["L031"] = L031Routes.Build(),
            ["L032"] = L032Routes.Build(),
            ["L033"] = L033Routes.Build(),
            ["L034"] = L034Routes.Build(),
            ["L035"] = L035Routes.Build(),
            ["L036"] = L036Routes.Build(),
            ["L037"] = L037Routes.Build(),
            ["L038"] = L038Routes.Build(),
            ["L039"] = L039Routes.Build(),
            ["L040"] = L040Routes.Build(),
        };

        public static bool TryGet(string id, out RoomRoutes routes) => ById.TryGetValue(id, out routes);
    }
}
