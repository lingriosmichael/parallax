using System.Collections.Generic;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-089 (R1): every KIT-5-KIT-9 rule in one call ----------
        // Separately named, not part of Validate() (the kit convention). A fixed order: Spear, Inverter, Geyser, Vine,
        // StormCloud, Band, Sections (PAX-090); with routes, also GeyserEnvelope and VineRoutes (they read the declared betrayals; no replay).
        // Every error is kept, each prefixed with its rule's name. ValidateStormCloud loads its own PrecisionThresholds.
        public static List<string> ValidateKit(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravity,
            PlatformSizeConfig sizes, LevelListConfig levels, Routes.RoomRoutes routes = null)
        {
            var errors = new List<string>();
            AddPrefixed(errors, nameof(ValidateSpear), ValidateSpear(levelId, room, sizes));
            AddPrefixed(errors, nameof(ValidateInverter), ValidateInverter(levelId, room));
            AddPrefixed(errors, nameof(ValidateGeyser), ValidateGeyser(levelId, room, motor));
            AddPrefixed(errors, nameof(ValidateVine), ValidateVine(levelId, room, motor));
            AddPrefixed(errors, nameof(ValidateStormCloud), ValidateStormCloud(levelId, room, motor));
            AddPrefixed(errors, nameof(ValidateBand), ValidateBand(levelId, room, levels));
            AddPrefixed(errors, nameof(ValidateSections), ValidateSections(levelId, room));   // PAX-090 (D-091)
            if (routes == null) return errors;
            AddPrefixed(errors, nameof(ValidateGeyserEnvelope), ValidateGeyserEnvelope(levelId, room, motor, gravity, routes));
            AddPrefixed(errors, nameof(ValidateVineRoutes), ValidateVineRoutes(levelId, room, routes));
            return errors;
        }

        static void AddPrefixed(List<string> errors, string rule, List<string> found)
        {
            foreach (string error in found) errors.Add(rule + ": " + error);
        }
    }
}
