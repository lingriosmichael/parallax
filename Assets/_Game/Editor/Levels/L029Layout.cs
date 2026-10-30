using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-244 (replaces D-182/D-190/D-241/D-243; the developer, 2026-10-28: "FUlly redesign it. I think the idea is no longer fun. The
    // problem I see is that you fail once and go down you can immediately see what is below the other open gaps. So one death solves
    // a full floor. I think the spikes need to be hidden", then the concept "Trapdoor tower"): level 29, "Trapdoor Tower". Ten floors
    // in a tower 32 wide, the ground and the door under them. Every floor has three or four ways down that look the same: one is the
    // way on, the others dead ends, a certain death (D-224). Under each dead end the floor below hides spikes that spring only for a
    // cat falling from it (their trigger is the air under that slot, out of a jump's reach from the floor below), so the floor below
    // looks the same everywhere and a death shows only the one slot that killed. The camera shows ~2.9 below the cat: a floor's ways
    // down can't be seen into from above.
    //  - A slot is an open gap, or (on floors 2, 5 and 8) a stretch of floor that looks solid and crumbles under the cat.
    //  - The way on is sometimes a far wall's slot, sometimes a middle one (D-246). A slot's hidden spikes reach a running fall's drift past it (Drift), and no other slot of its floor is within a
    //    drift of the way on (a cat falling through the way on never springs them).
    //  - The ground: walk west to the door (D-246; it was at the east end). D-245 (the developer: "no checkpoints making it incredibly hard. And a trap right before
    //    the end where the door is"): no checkpoints (a death starts the tower again from the top), and Door_Trap, the ground just
    //    before the door, looks solid and crumbles into a spike pit under a running cat: jump it.
    static class L029Layout
    {
        internal const float Top = 55f, Step = 5.5f, Ground = 0f, Base = -3f, Width = 32f, SlotW = 1.5f, Drift = 3.6f;
        internal static float FloorY(int i) => Top - Step * i;
        internal static float Roof => Top + 4.5f;
        // Each floor's slots (x0, SlotW wide), the way on, and the crumbling one (-1: none). Laid out by a search for: slots 2.5 apart
        // or more, none under the previous way on's landing (its x ± Drift), the way on outside the other slots' landings above and
        // a drift (+0.5) from every other slot of its floor.
        // D-246 (the developer: "you just go through the middle options ... There is no safe path using the far left or far right
        // opening"): re-laid so the way on is a far wall's slot on five floors (0, 2 and 8 west; 7 and 9 east) and a middle one on
        // the rest, the nearest slot to the landing on three of them only.
        internal static readonly (float[] slots, int way, int crumble)[] Floors = {
            (new[] { 2f, 10f, 25.5f }, 0, -1),
            (new[] { 11f, 19.5f, 29f }, 1, -1),
            (new[] { .5f, 13f, 25.5f, 30f }, 0, 2),
            (new[] { 8f, 19f, 30f }, 1, -1),
            (new[] { .5f, 10f, 24.5f }, 2, -1),
            (new[] { 1f, 5f, 10.5f, 16.5f }, 3, 0),
            (new[] { 3f, 11f, 23.5f }, 2, -1),
            (new[] { 9f, 13.5f, 30f }, 2, -1),
            (new[] { .5f, 8f, 15.5f, 19.5f }, 0, 3),
            (new[] { 13.5f, 22f, 28.5f }, 2, -1),
        };
        internal const float SpawnX = 18.5f, DoorX = 1.5f;   // D-246: the door at the west end (floor 9's way on lands at the east)
        internal const int CrumbleDelay = 6;
        // D-247 (the developer: "is there anything keeps the pressure on? Maybe we can the ceiling slowly shifting down? ... The
        // ceiling falling should wait until you land"): each floor's ceiling, a plate CeilingThick thick flush under the floor above
        // (the roof over floor 0), cut at that floor's slots, comes down once a cat lands (CeilingGrace after the start on floor 0,
        // CeilingDelay on the rest) to CeilingGap over the floor, crushing a cat under it; floor 0's over CeilingTicksTop, floor 9's
        // over CeilingTicksBottom, the floors between in step. Jumping stops halfway (under 2.2 of headroom).
        internal const float CeilingThick = .5f, CeilingGap = .4f;
        internal const int CeilingGrace = 50, CeilingDelay = 10, CeilingTicksTop = 310, CeilingTicksBottom = 200;   // D-250: a little faster (was 350/225)
        // D-250 (the developer: "Make this part go faster"): a plug slides shut over PlugTicks, then comes down at the pieces' speed.
        internal const int PlugTicks = 18;
        // D-245: Door_Trap, over a spike pit (PitDepth deep) just before the door; it gives way DoorTrapDelay after a cat steps on.
        internal const float PitX0 = 3.5f, PitX1 = 6f, PitDepth = 1.5f;   // D-246: was x 26-28.5
        internal const int DoorTrapDelay = 3;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(SpawnX,Top),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,Ground + .75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, Roof, Roof + 1f);
            // The ground, with Door_Trap's pit before the door (D-245).
            Block(elements, SoloRoomElementKind.Floor, "Ground_W", 0f, PitX0, Base, Ground);
            Block(elements, SoloRoomElementKind.Floor, "Pit_Floor", PitX0, PitX1, Base, Ground - PitDepth);
            elements.Add(E(SoloRoomElementKind.Hazard, "Pit_Spikes", ((PitX0 + PitX1) * .5f, Ground - PitDepth + .15f), (PitX1 - PitX0, .3f)));
            Block(elements, SoloRoomElementKind.Floor, "Ground_E", PitX1, Width, Base, Ground);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor, "Door_Trap", ((PitX0 + PitX1) * .5f, Ground - .5f), (PitX1 - PitX0, 1f), ((PitX0 + PitX1) * .5f, Ground + .3f), (PitX1 - PitX0, .6f),
                new SoloRoomTrapSettings(delayTicks: DoorTrapDelay)));

            for (int i = 0; i < Floors.Length; i++)
            {
                var (slots, way, crumble) = Floors[i];
                float y = FloorY(i), below = i + 1 < Floors.Length ? FloorY(i + 1) : Ground;

                // The floor: stone between the slots; an open gap at each slot but the crumbling one.
                float cursor = 0f; int n = 0;
                foreach (float x0 in slots)
                {
                    if (x0 > cursor + .01f) Block(elements, SoloRoomElementKind.Floor, $"Floor_{i}_{n++}", cursor, x0, y - 1f, y);
                    cursor = x0 + SlotW;
                }
                if (cursor < Width - .01f) Block(elements, SoloRoomElementKind.Floor, $"Floor_{i}_{n}", cursor, Width, y - 1f, y);
                if (crumble >= 0)
                {
                    float cx = slots[crumble] + SlotW * .5f;
                    elements.Add(E(SoloRoomElementKind.CollapsingFloor, $"Crumble_{i}", (cx, y - .5f), (SlotW, 1f), (cx, y + .3f), (SlotW, .6f),
                        new SoloRoomTrapSettings(delayTicks: CrumbleDelay)));
                }

                // D-247: this floor's ceiling, in pieces between the slots of the floor above (none cut under the roof), sprung by a
                // cat on this floor (its trigger: the floor's whole width, a unit over it).
                // D-249 (the developer: "if i stand here in the gap it doesnt kill me"): each hole in it (under a slot of the floor
                // above) is closed first by a Plug sliding out of the wider piece beside it (hidden inside it till then), over
                // PlugTicks; then the pieces and the plugs come down together, one closed ceiling (the plug's path: across, then down,
                // at the pieces' speed).
                float ceiling = i == 0 ? Roof : FloorY(i - 1) - 1f;
                float travel = ceiling - CeilingThick - (y + CeilingGap);
                int ceilingTicks = Mathf.RoundToInt(Mathf.Lerp(CeilingTicksTop, CeilingTicksBottom, i / (Floors.Length - 1f)));
                float[] over = i == 0 ? new float[0] : Floors[i - 1].slots;
                int plugTicks = over.Length == 0 ? 0 : PlugTicks;
                int sprung = i == 0 ? CeilingGrace : CeilingDelay;
                var trigger = (pos: new Vector2(Width * .5f, y + .5f), size: new Vector2(Width, 1f));
                float start = 0f; int c = 0;
                foreach (float x1 in over.Append(Width))
                {
                    if (x1 > start + .01f)
                        elements.Add(E(SoloRoomElementKind.MovingTrap, $"Ceiling_{i}_{c++}", ((start + x1) * .5f, ceiling - CeilingThick * .5f), (x1 - start, CeilingThick),
                            (trigger.pos.x, trigger.pos.y), (trigger.size.x, trigger.size.y), new SoloRoomTrapSettings(
                                new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                                new SoloRoomTrapSettings(delayTicks: sprung + plugTicks, offset: new Vector2(0f, -travel), moveTicks: ceilingTicks,
                                    holdTicks: 9999, movingKind: MovingTrapKind.Solid))));
                    start = x1 + SlotW;
                }
                for (int h = 0; h < over.Length; h++)
                {
                    float x0 = over[h], left = x0 - (h > 0 ? over[h - 1] + SlotW : 0f), right = (h + 1 < over.Length ? over[h + 1] : Width) - (x0 + SlotW);
                    float dir = right >= left ? -1f : 1f;   // out of the wider piece: from the right it slides west (-1)
                    float home = dir < 0f ? x0 + SlotW * 1.5f : x0 - SlotW * .5f;
                    elements.Add(E(SoloRoomElementKind.MovingTrap, $"Plug_{i}_{h}", (home, ceiling - CeilingThick * .5f), (SlotW, CeilingThick),
                        (trigger.pos.x, trigger.pos.y), (trigger.size.x, trigger.size.y), new SoloRoomTrapSettings(
                            new PathSettings(new[] { new Vector2(dir * SlotW, 0f), new Vector2(dir * SlotW, -travel) }, firstLegTicks: plugTicks),
                            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                                new SoloRoomTrapSettings(delayTicks: sprung, moveTicks: plugTicks + ceilingTicks, holdTicks: 9999, movingKind: MovingTrapKind.Solid)))));
                }

                // Each dead end: spikes hidden in the floor below over the slot's landing (its x ± Drift, less that floor's own slots),
                // sprung by a cat in the air under the slot (from this floor's underside down to 2.4 over the floor below).
                (float x0, float x1)[] belowSlots = i + 1 < Floors.Length ? Floors[i + 1].slots.Select(sx => (sx, sx + SlotW)).ToArray() : new[] { (PitX0, PitX1) };
                for (int s = 0; s < slots.Length; s++)
                {
                    if (s == way) continue;
                    float x0 = slots[s], top = y - 1f, bottom = below + 2.4f;
                    Vector2 triggerPos = new(x0 + SlotW * .5f, (top + bottom) * .5f), triggerSize = new(SlotW, top - bottom);
                    int k = 0;
                    foreach (var (a, b) in Uncovered(Mathf.Max(.2f, x0 - Drift), Mathf.Min(Width - .2f, x0 + SlotW + Drift), belowSlots))
                    {
                        string name = $"Hidden_{i}_{s}_{k++}";
                        elements.Add(E(SoloRoomElementKind.HiddenSpikes, name, ((a + b) * .5f, below + .15f), (b - a, .3f), (triggerPos.x, triggerPos.y), (triggerSize.x, triggerSize.y),
                            new SoloRoomTrapSettings(revealDelayTicks: 0)));
                    }
                }
            }

            // D-245: no checkpoint sections.
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, null);
        }

        // The parts of a to b not over any of `gaps`.
        static IEnumerable<(float a, float b)> Uncovered(float a, float b, (float x0, float x1)[] gaps)
        {
            float cursor = a;
            foreach (var (x0, x1) in gaps.OrderBy(g => g.x0))
            {
                if (x1 <= cursor || x0 >= b) continue;
                if (x0 > cursor + .05f) yield return (cursor, x0);
                cursor = Mathf.Max(cursor, x1);
            }
            if (b > cursor + .05f) yield return (cursor, b);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
