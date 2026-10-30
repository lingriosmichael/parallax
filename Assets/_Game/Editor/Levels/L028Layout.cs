using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-191 (replaces D-181; the developer, 2026-10-24: "Level 28 is AWFUL, way to easy, and short", and "maybe the walls move
    // different and reposition themselves like a glitch in the matrix"): level 28, "Glitch". A spiral: east along the top, down
    // the right shaft, west along the bottom, up the left shaft, and east into the middle to the door. The architecture glitches:
    // walls and blocks flicker, show a ghost of where they'll be, and teleport there.
    // D-209 (the developer, 2026-10-28: the disappearing walls, "Remove it everywhere in the remaining levels"): nothing teleports
    // any more. GW_1 is Press_A, a block slamming down from the corridor's ceiling in sight; the crushers slide down and back up.
    //  - Top (section 0, corridor A, walked east): Press_A, half the corridor's height, slams down from its ceiling on its beat:
    //    pass under it while it's up. Spin_A turns in the corridor;
    //    Loop_A carries the cat up, over Pit_A and down; off the east end, down the right shaft past Spin_B.
    //  - Bottom (section 1, corridor C, walked west): Crush_1-3 slide down out of the ceiling and fill the corridor, each on its
    //    own beat: wait between them. Lift_L carries the cat up the left shaft.
    //  - Inner (section 2, corridor E, walked east): the crushers' homes are holes in E's floor while they're down: cross each
    //    while it's home; past Spin_E to the door.
    // D-234 (the developer, 2026-10-28: "no sense of pressure. You can go so slow without anything ever happening", then "I prefer
    // arrows on each floor and horizontal ones. One at the height of the cat running and another at the height of the cat jumping.
    // They need to be intercalated. So once top and once bottom"): each corridor has two launchers, one at shin height (ArrowLow
    // over the floor: jump it) and one at jump height (ArrowHigh: stay down under it), each every ArrowPeriod, half a period apart:
    // a shot every 60 ticks, low, high, low, high. Waiting for a crusher means dodging arrows.
    // D-235 (the developer: "I want it interchanged and mixed it up a bit. Sometimes the arrow from the front sometimes from the back
    // ... at random"): launchers at both ends of each corridor. Each shot is its own launcher (Arrow_A_1-8, …) firing once a cycle of
    // ShotCount shots, ShotGap apart: low, high, low, high still, but front or back by the corridor's fixed shuffle (Pattern: F
    // from ahead of the cat, B from behind). Deterministic (D-040): the same shuffle on every attempt. A shot from behind has the
    // longer tell (BackTell), as the cat can't see it coming.
    // D-236 (the developer: "on render of this level, the order changes automatically. Like a randomizer. We predefined 5
    // variations, on render we pick 1"; picked on each level load, kept through deaths): every shot has a launcher at both ends
    // (Arrow_A_3F and Arrow_A_3B, …); Variants holds five shuffles per corridor, and ArrowVariantPicker (baked by ArrowVariantSetup)
    // switches off, on load, the launcher of each shot its picked variant doesn't use. An exception to D-040 for this level.
    static class L028Layout
    {
        // The corridors' floors and ceilings: C (bottom), E (inner), A (top).
        internal const float CFloor = 4f, CCeil = 8f, EFloor = 12f, ECeil = 16f, AFloor = 20f, ACeil = 24f, Roof = 26f, Base = -1f;
        // Press_A and the crushers: each is down (away from home) from Step to 2 * Step of its beat, sliding SlideTicks each way;
        // every Period.
        internal const int Step = 60, SlideTicks = 15, Period = 160, SpinARate = 3, LoopMove = 200, LoopPeriod = 250, LiftTicks = 80;
        internal static readonly int[] CrushPhase = { 0, 90, 40 };
        internal static readonly float[] CrushX = { 26.6f, 20.6f, 14.6f };
        // D-234: the arrows: each launcher's lane over its corridor's floor; speed; period (above tell + the longest flight, 108).
        internal const float ArrowLow = .3f, ArrowHigh = 1.5f, ArrowSpeed = .35f, DoorX = 31.9f;   // D-239: ArrowSpeed was .4
        // D-235: a shot every ShotGap, a cycle of ShotCount; cooldown over the tell and the longest flight (20 + 115).
        // D-239 (the developer: "a very hard level. Lets add slightly more time in the intervals and reduce the speed of the arrows
        // just slightly"): ShotGap 75 (was 60), ArrowSpeed 0.35 (was 0.4).
        internal const int ShotGap = 75, ShotCount = 8, ArrowCooldown = 140, FrontTell = 10, BackTell = 20;
        // (name, floor, the front launcher's x, direction and lane end, the back one's, the cycle's first shot).
        // The front is ahead of the cat as it walks the corridor: A east, C west, E east.
        internal static readonly (string name, float floor, float fx, ArrowDirection fdir, float fend, float bx, ArrowDirection bdir, float bend, int phase)[] Arrows = {
            // D-237 (the developer, of the launchers standing out in front of Wall_L: "It should be in the wall"): each launcher sits
            // inside its wall (Wall_L x -1-0, Wall_R 40-41, Wall_E 33-34), its mouth at the face; each lane ends at the far wall's face.
            ("Arrow_A", AFloor, 40.25f, ArrowDirection.Left, 0f, -.25f, ArrowDirection.Right, 40f, 50),
            ("Arrow_C", CFloor, -.25f, ArrowDirection.Right, 40f, 40.25f, ArrowDirection.Left, 0f, 0),
            ("Arrow_E", EFloor, 33.25f, ArrowDirection.Left, 0f, -.25f, ArrowDirection.Right, 33f, 30),
        };
        // D-236: the five variations, one shuffle per corridor (in Arrows' order: A, C, E); F from ahead of the cat, B from behind.
        internal static readonly string[][] Variants = {
            new[] { "FFBFBBFB", "BFFBFBBF", "FBBFFBFB" },
            new[] { "FBFFBBFB", "FFBBFBFF", "BFBFFBBF" },
            new[] { "BFFBFFBB", "FBFBBFFB", "FFBBFBFB" },
            new[] { "FBBFBFFB", "BBFFBFFB", "FBFBFBBF" },
            new[] { "BFBFFBFF", "FBBFFBFB", "BFFBBFBF" },
        };

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(40.5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,AFloor),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,EFloor + .75f),(.6f,1.5f)),   // D-234: was 32.3 (Arrow_E's front launchers fire from Wall_E)
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, 40f, ACeil, Roof);

            // ---- The ground: corridor C's floor, the whole width, Lift_L's seat in it ----
            Block(elements, SoloRoomElementKind.Floor, "Ground_W", 0f, 1.5f, Base, CFloor);
            Block(elements, SoloRoomElementKind.Floor, "Lift_Seat", 1.5f, 6f, Base, CFloor - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Ground", 6f, 40f, Base, CFloor);

            // ---- The central block (x 6-34, y 8-12): C's ceiling and E's floor, with a hole at each crusher's home ----
            float from = 6f;
            for (int i = CrushX.Length - 1; i >= 0; i--)
            {
                Block(elements, SoloRoomElementKind.Floor, "Core_" + i, from, CrushX[i] - .6f, CCeil, EFloor);
                from = CrushX[i] + .6f;
            }
            Block(elements, SoloRoomElementKind.Floor, "Core_E", from, 34f, CCeil, EFloor);
            // E's east end, past the door.
            Block(elements, SoloRoomElementKind.Wall, "Wall_E", 33f, 34f, EFloor, ECeil);

            // ---- The slab between E and A (x 0-34, y 16-20), A's floor, with Pit_A (x 23-29, its spiked floor 2 down) ----
            Block(elements, SoloRoomElementKind.Floor, "Slab_W", 0f, 23f, ECeil, AFloor);
            Block(elements, SoloRoomElementKind.Floor, "Pit_A_Floor", 23f, 29f, ECeil + 1f, ECeil + 2f);
            Block(elements, SoloRoomElementKind.Floor, "Slab_PitBase", 23f, 30f, ECeil, ECeil + 1f);
            Spikes(elements, "Pit_A", 23f, 29f, ECeil + 2f);
            Block(elements, SoloRoomElementKind.Floor, "Slab_E1", 29f, 30f, ECeil + 1f, AFloor);
            Block(elements, SoloRoomElementKind.Floor, "Slab_E2", 30f, 34f, ECeil, AFloor);

            // ---------- Top (corridor A) ----------
            // Press_A: a block from the corridor's ceiling down to its middle, at x 9.5; down onto the floor and back up on its beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Press_A",(9.5f,ACeil - 1f),(1f,2f),settings:Slide(new Vector2(0f, AFloor - (ACeil - 2f)), 0)));
            // Spin_A, one arm in the corridor's middle.
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_A",(19f,(AFloor + ACeil) * .5f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.7f,SpinARate),new SoloRoomTrapSettings())));
            // Loop_A: from Pit_A's west edge, up 2, east 4.5, down 2, and back west, over LoopMove of every LoopPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Loop_A",(24f,AFloor - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(0f, 2f), new Vector2(4f, 2f), new Vector2(4f, 0f), Vector2.zero }),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks:LoopMove,repeatMode:TrapRepeatMode.Periodic,periodTicks:LoopPeriod,phaseTicks:0,cooldownTicks:LoopMove,movingKind:MovingTrapKind.Solid)))));
            // Spin_B, one arm in the right shaft, under A's east end.
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_B",(36f,14f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.8f,-3f),new SoloRoomTrapSettings())));

            // ---------- Bottom (corridor C) ----------
            for (int i = 0; i < CrushX.Length; i++)
                elements.Add(E(SoloRoomElementKind.MovingTrap,"Crush_" + (i + 1),(CrushX[i],(CCeil + EFloor) * .5f),(1.2f,EFloor - CCeil),settings:Slide(new Vector2(0f, CFloor - CCeil), CrushPhase[i])));
            // Lift_L, flush with the ground in the left shaft, rises to E's floor after a cat stands on it (D-234: faster, LiftTicks
            // 80, so it carries the cat through Arrow_C's lanes quickly).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_L",(3.75f,CFloor - .25f),(4.5f,.5f),(3.75f,(CFloor + ECeil) * .5f),(4.5f,ECeil - CFloor),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:15,offset:new Vector2(0f,EFloor - CFloor),moveTicks:LiftTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            // ---------- Inner (corridor E) ----------
            // Spin_E, one arm in front of the door (D-234: at 29.6, was 30, clear of the door's new place).
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_E",(29.6f,(EFloor + ECeil) * .5f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.7f,-3f),new SoloRoomTrapSettings())));

            // ---------- The arrows (D-234) ----------
            // D-235: shot k of a corridor's cycle: low when k is even, high when odd. D-236: from both ends; the picked variant
            // switches one of the two off on load.
            foreach (var (name, floor, fx, fdir, fend, bx, bdir, bend, phase) in Arrows)
                for (int k = 0; k < ShotCount; k++)
                {
                    float y = floor + (k % 2 == 0 ? ArrowLow : ArrowHigh);
                    int at = (phase + k * ShotGap) % (ShotCount * ShotGap);
                    Arrow(elements, Shot(name, k, 'F'), fx, y, fdir, fend, FrontTell, at);
                    Arrow(elements, Shot(name, k, 'B'), bx, y, bdir, bend, BackTell, at);
                }

            var sections = new[] {
                CheckpointSection.Start("Top", new Vector2(2f, AFloor), new[] { "Press_A", "Spin_A", "Loop_A", "Spin_B" }.Concat(Shots("Arrow_A")).ToArray()),
                new CheckpointSection("Bottom", new Vector2(38f, CFloor), new Rect(34.5f, 6f, 5.5f, .2f), new[] { "Crush_1", "Crush_2", "Crush_3", "Lift_L" }.Concat(Shots("Arrow_C")).ToArray()),
                new CheckpointSection("Inner", new Vector2(7.6f, EFloor), new Rect(0f, EFloor - 1f, 6f, .2f), new[] { "Spin_E" }.Concat(Shots("Arrow_E")).ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, 40f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A sliding block (D-209, was a glitch): it slides `away` over SlideTicks so it's there from Step ticks into its beat, holds,
        // and slides home by 2 * Step; a slam (it crushes a cat it comes down on); every Period from `phase`.
        static SoloRoomTrapSettings Slide(Vector2 away, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(offset: away, moveTicks: SlideTicks, holdTicks: Step - 2 * SlideTicks, returnTicks: SlideTicks, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Period, phaseTicks: (phase + Step - SlideTicks) % Period, cooldownTicks: Step, movingKind: MovingTrapKind.Solid));

        // D-234/D-235: a launcher at (x, y) shooting along y to `end` once a cycle (ShotCount shots, ShotGap apart) from `phase`.
        static void Arrow(List<SoloRoomElement> e, string name, float x, float y, ArrowDirection dir, float end, int tell, int phase) =>
            e.Add(E(SoloRoomElementKind.Arrow, name, (x, y), (.5f, .4f), settings: new SoloRoomTrapSettings(
                new ArrowLane(dir, y, end, unitsPerTick: ArrowSpeed, tellTicks: tell),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: ShotCount * ShotGap, phaseTicks: phase, cooldownTicks: ArrowCooldown))));

        // D-236: shot k's launcher at the front ('F') or back ('B') end of corridor `name`.
        static string Shot(string name, int k, char end) => $"{name}_{k + 1}{end}";

        // D-235/D-236: a corridor's launchers' names (both ends).
        static IEnumerable<string> Shots(string name) => Enumerable.Range(0, ShotCount).SelectMany(k => new[] { Shot(name, k, 'F'), Shot(name, k, 'B') });

        /// <summary>D-236: for each variant, the launchers it doesn't fire (each shot's other end), for ArrowVariantSetup.</summary>
        internal static string[][] VariantOff() =>
            Variants.Select(v => Arrows.SelectMany((a, c) => Enumerable.Range(0, ShotCount).Select(k => Shot(a.name, k, v[c][k] == 'F' ? 'B' : 'F'))).ToArray()).ToArray();

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));
    }
}
