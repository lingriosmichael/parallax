using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Setup
{
    // PAX-089 B, R4: small synthetic rooms used only by the EditMode chain-kit tests (next to SpearFixtures, the existing
    // convention). Not used by any menu and not part of LevelLayouts or the Trap Lab.
    // 16 wide, floor top 0, ceiling underside 7, checkpoint x 2, door at x 14.5. A full-height source (x 5.7-6.3, y 0-7)
    // is a cut: walking right from the checkpoint, the cat can't pass it without touching it.
    public static class ChainKitFixtures
    {
        public const int BlockDelay = 10, SpikesDelay = 6;

        // Configured timing (Overlap, Once, no delay), as Trap Lab room 7 writes it; `new SoloRoomTrapSettings()` is the
        // unconfigured struct default.
        static SoloRoomTrapSettings Inverter() => new(new InverterSettings(), new SoloRoomTrapSettings(delayTicks: 0));
        static SoloRoomTrapSettings Snap() => new(delayTicks: 0);
        static SoloRoomTrapSettings BlockAfter(string source) => new(delayTicks: BlockDelay, unitsPerTick: .36f, travelDistance: 5.5f, triggerSource: TrapTriggerSource.Chain, chainSource: source);
        static SoloRoomTrapSettings SpikesAfter(string source) => new(revealDelayTicks: SpikesDelay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // B: an inverter chaining a falling block beyond it (R4: a covered chain).
        public static SoloRoomDefinition InverterChainsBlock() => Room(
            E(SoloRoomElementKind.Inverter, "Inverter", (6f, 3.5f), (.6f, 7f), settings: Inverter()),
            E(SoloRoomElementKind.FallingBlock, "Block", (10f, 6f), (1f, 1f), settings: BlockAfter("Inverter")));

        // R4: the chained spikes lie before the inverter, so the cat reaches them without touching it.
        public static SoloRoomDefinition InverterChainsSpikesBefore() => Room(
            E(SoloRoomElementKind.Inverter, "Inverter", (6f, 3.5f), (.6f, 7f), settings: Inverter()),
            E(SoloRoomElementKind.HiddenSpikes, "Spikes", (4f, .15f), (1f, .3f), settings: SpikesAfter("Inverter")));

        // B: a full-height snap vine chaining hidden spikes beyond it.
        public static SoloRoomDefinition SnapVineChainsSpikes() => Room(
            E(SoloRoomElementKind.Vine, "Vine", (6f, 3.5f), (.6f, 7f), settings: Snap()),
            E(SoloRoomElementKind.HiddenSpikes, "Spikes", (10f, .15f), (1f, .3f), settings: SpikesAfter("Vine")));

        // R4: a snap vine that stops 1.9 u short of the ceiling isn't a cut; a jump passes over it.
        public static SoloRoomDefinition ShortSnapVineChainsSpikes() => Room(
            E(SoloRoomElementKind.Vine, "Vine", (6f, 2.55f), (.6f, 5.1f), settings: Snap()),
            E(SoloRoomElementKind.HiddenSpikes, "Spikes", (10f, .15f), (1f, .3f), settings: SpikesAfter("Vine")));

        // B: a plain vine (no snap settings) never fires, so it can't be a chain source.
        public static SoloRoomDefinition PlainVineChainsSpikes() => Room(
            E(SoloRoomElementKind.Vine, "Vine", (6f, 3.5f), (.6f, 7f)),
            E(SoloRoomElementKind.HiddenSpikes, "Spikes", (10f, .15f), (1f, .3f), settings: SpikesAfter("Vine")));

        // Harness cases: walk right from the checkpoint until the chained target fires, then two more ticks.
        public static RouteCase InverterFiresBlock() => new("inverter chains a falling block", InverterChainsBlock(),
            new Route("walk into the inverter", Hold(Right), Until(Fired("Block")), For(2)));

        public static RouteCase SnapVineFiresSpikes() => new("snap vine chains hidden spikes", SnapVineChainsSpikes(),
            new Route("walk into the vine", Hold(Right), Until(Fired("Spikes")), For(2)));

        static SoloRoomDefinition Room(params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (8f, 7.5f), (16f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor", (8f, -.5f), (16f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (14.5f, .75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 16f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }
    }
}
