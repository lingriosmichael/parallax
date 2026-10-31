using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-270 (Docs/Design/L031-L035_concepts.md §7): level 39, "Barrage", built on hard-light bars (PAX-111): a bar flies level,
    // lethal in flight, and where it sticks it's solid, a step. The lie: "dodge the shots". The bars that almost hit you are the
    // ones you climb.
    //  - Section 0: Shaft A, glass-floored. Each bar flies across it just over a standing cat's head and sticks in the far wall,
    //    a jump up and across from the last: stand still as it flies (a cat that jumps is hit), then climb. Out at the top onto
    //    the corridor, where two bars come from behind, out of Shaft A's wall: Low (shin height, jump it) and High (a jumping
    //    cat's height: run under it, never jump). Both fly on under the Lintel into Shaft B; High is its first step.
    //  - Section 1: Shaft B, ten steps the same way over its glass floor (High, the first, is already there). From B10, up onto
    //    the Lintel's top and west to the door.
    static class L039Layout
    {
        internal const float Base = -2f, PitDepth = 1.5f, ShaftW = 4.8f, WallT = 1f, Rise = 1.25f, GlassW = 1.6f;
        // Bars: BarLen long, BarSpeed u/tick after their tell.
        internal const float BarLen = 1.4f, BarSpeed = .6f;
        internal const int StepTell = 30, BarTell = 24, GlassDelay = 75;
        // ---- Section 0 ----
        // Shaft A: inside x ShaftA0-ShaftA1, floor 0; A1-A5 alternate east, west, ... (tops Rise apart from AFirst); the corridor
        // (floor CorrY = the east wall's top, roof CorrRoof) runs east to Shaft B.
        internal const float ShaftA0 = WallT, ShaftA1 = ShaftA0 + ShaftW, AFirst = 1.1f, CorrY = AFirst + ACount * Rise, CorrRoof = CorrY + 3.5f;
        internal const int ACount = 5;
        internal static float ATop(int k) => AFirst + (k - 1) * Rise;     // k = 1..ACount
        internal static bool AEast(int k) => k % 2 == 1;
        // The barrage, out of Shaft A's west wall: Low fired as a cat passes LowX, High as it passes HighX.
        internal const float LowX = 11f, HighX = 18f, LowTop = .5f, HighTop = 1.5f;
        // ---- Section 1 ----
        internal const float ShaftB0 = 30f, ShaftB1 = ShaftB0 + ShaftW, Width = ShaftB1 + WallT, LintelFoot = CorrY + 1.6f;
        internal const float GateX = ShaftB0 - 3f, Checkpoint1X = ShaftB0 - 1.6f;
        // Shaft B's steps: B1 is High, stuck in its east wall; B2-B10 alternate west, east, ..., Rise apart. The top floor is a
        // step up from B10 (west), over the Lintel and west.
        internal const int BCount = 10;
        internal static float BTop(int k) => CorrY + HighTop + (k - 1) * Rise;  // k = 1..BCount
        internal static bool BEast(int k) => k % 2 == 1;
        internal static readonly float TopFloorY = BTop(BCount) + 1f, RoofY = TopFloorY + 3.5f, Top = RoofY + 1.5f;
        internal const float TopFloorX0 = ShaftB0 - 10f, DoorX = ShaftB0 - 6f;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(ShaftA0 + 2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,TopFloorY + .75f),(.6f,1.5f)),
            };

            // ---------- Section 0: Shaft A ----------
            Block(e, SoloRoomElementKind.Wall, "WallA_W", 0f, ShaftA0, Base, CorrRoof);
            Block(e, SoloRoomElementKind.Wall, "WallA_E", ShaftA1, ShaftA1 + WallT, Base, CorrY);
            ShaftFloor(e, "A", ShaftA0, ShaftA1, 0f);
            for (int k = 1; k <= ACount; k++) e.Add(k == 1 ? StepBar("A1", true, ATop(1), ShaftA0, ShaftA1, 0f, 0f) : StepBar($"A{k}", AEast(k), ATop(k), ShaftA0, ShaftA1, ATop(k - 1)));

            // ---------- The corridor ----------
            Block(e, SoloRoomElementKind.Floor, "Corridor", ShaftA1 + WallT, ShaftB0, Base, CorrY);
            Block(e, SoloRoomElementKind.Ceiling, "Mass", 0f, ShaftB0 - WallT, CorrRoof, TopFloorY);
            Block(e, SoloRoomElementKind.Ceiling, "Mass_Top", 0f, TopFloorX0, TopFloorY, Top);
            e.Add(Barrage("Low", LowX, LowTop));
            e.Add(Barrage("High", HighX, HighTop));

            // ---------- Section 1: Shaft B ----------
            Block(e, SoloRoomElementKind.Wall, "Lintel", ShaftB0 - WallT, ShaftB0, LintelFoot, TopFloorY);
            Block(e, SoloRoomElementKind.Wall, "WallB_E", ShaftB1, Width, Base, Top);
            Block(e, SoloRoomElementKind.Ceiling, "Roof", TopFloorX0, ShaftB1, RoofY, Top);
            ShaftFloor(e, "B", ShaftB0, ShaftB1, CorrY);
            for (int k = 2; k <= BCount; k++) e.Add(StepBar($"B{k}", BEast(k), BTop(k), ShaftB0, ShaftB1, BTop(k - 1)));

            var s0 = new List<string> { "GlassA_1", "GlassA_2", "GlassA_3", "Low", "High" };
            for (int k = 1; k <= ACount; k++) s0.Add($"A{k}");
            var s1 = new List<string> { "GlassB_1", "GlassB_2", "GlassB_3" };
            for (int k = 2; k <= BCount; k++) s1.Add($"B{k}");
            var sections = new[] {
                CheckpointSection.Start("ShaftA", new Vector2(ShaftA0 + 2f, 0f), s0.ToArray()),
                new CheckpointSection("ShaftB", new Vector2(Checkpoint1X, CorrY), new Rect(GateX, CorrY, .4f, 1.5f), s1.ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A shaft's floor at y: glass over a laser pit, giving way GlassDelay after a cat has stood on it.
        static void ShaftFloor(List<SoloRoomElement> e, string shaft, float x0, float x1, float y)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, $"Pit_{shaft}", ((x0 + x1) * .5f, (Base + y - PitDepth) * .5f), (x1 - x0, y - PitDepth - Base)));
            e.Add(E(SoloRoomElementKind.Hazard, $"Laser_{shaft}", ((x0 + x1) * .5f, y - PitDepth + .15f), (x1 - x0, .3f)));
            for (int i = 0; i < 3; i++)
            {
                float g0 = x0 + i * GlassW;
                e.Add(E(SoloRoomElementKind.CollapsingFloor, $"Glass{shaft}_{i + 1}", (g0 + GlassW * .5f, y - PitDepth * .5f), (GlassW, PitDepth), settings: new SoloRoomTrapSettings(delayTicks: GlassDelay)));
            }
        }

        // A step bar across a shaft (x0-x1): fired from the wall opposite the side it sticks on (east: out of the west wall), its top
        // at `top`, as a cat comes up over the step below (`below`; its trigger starts a little over that step's top, so a cat
        // standing there sets it off and a cat jumping onto the step before doesn't; one jumping onto `below` itself may as it
        // rises, and lands well within StepTell).
        static SoloRoomElement StepBar(string name, bool east, float top, float x0, float x1, float below, float over = .3f)
        {
            float lane = top - .2f, y0 = below + over, y1 = top + .2f;
            float launcherX = east ? x0 - .25f : x1 + .25f;
            return E(SoloRoomElementKind.Arrow, name, (launcherX, lane), (.5f, .4f), ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0),
                new SoloRoomTrapSettings(ArrowLane.HardLightBar(east ? ArrowDirection.Right : ArrowDirection.Left, lane, east ? x1 : x0, BarLen, unitsPerTick: BarSpeed, tellTicks: StepTell),
                    new SoloRoomTrapSettings(delayTicks: 0)));
        }

        // A barrage bar out of Shaft A's west wall along the corridor into Shaft B's east wall, its top `top` over the corridor's
        // floor, fired as a cat passes x.
        static SoloRoomElement Barrage(string name, float x, float top)
        {
            float lane = CorrY + top - .2f;
            return E(SoloRoomElementKind.Arrow, name, (ShaftA0 - .25f, lane), (.5f, .4f), (x + .2f, (CorrY + CorrRoof) * .5f), (.4f, CorrRoof - CorrY),
                new SoloRoomTrapSettings(ArrowLane.HardLightBar(ArrowDirection.Right, lane, ShaftB1, BarLen, unitsPerTick: BarSpeed, tellTicks: BarTell), new SoloRoomTrapSettings(delayTicks: 0)));
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
