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
        };

        public static bool TryGet(string id, out RoomRoutes routes) => ById.TryGetValue(id, out routes);
    }
}
