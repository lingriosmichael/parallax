using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-168 + D-196 (the developer, 2026-10-24: "Nothing improved in 24 ... The layouts are completely the same no new desigin no
    // new deaths"): level 24, "Ceiling Town", rebuilt as a braid: one long hall walked west to east on its floor and its ceiling
    // by turns. Purple orbs flip the cat between them; honest spikes on one face make the other the way.
    // D-209 (the developer, 2026-10-28): seven lanes, no checkpoint, no glitch blocks (pistons instead), no hidden flip; each lane's
    // spikes end 2.5 or more short of the orb that leaves it.
    // D-210 (the developer, 2026-10-28: "the level was way too easy absolutely no challenge … more random spontaneous direction
    // inverters … the actual floor that the cat stands on to also move either up or down to try and squish it, It should feel
    // claustrophobic", then "Add all the recommendations"): the hall closes in.
    //  - It shrinks lane by lane: 6 tall, then 5, then 4.2, then 3.6 in the last stretch.
    //  - Squeezes: a section of the face the cat walks on carries it toward the spiked face if it stops there (Drop_2, Lift_3,
    //    Drop_6); jaws close floor and ceiling together on their beat (Jaw_4, Jaw_5a, Jaw_5b, Jaw_6).
    //  - Five short hidden inverters among them (Inv_3, Inv_4, Inv_5a, whose end box swaps the controls back just before Inv_5b swaps
    //    them again, and Inv_6), besides Inv_1 and Inv_F.
    //  - The closing hall: from the last orb to the door the ceiling slams down behind the cat, section by section, a little
    //    slower than a run (Cave_1-9); Inv_F, hidden before the door, turns a cat that keeps holding right back into it.
    //  Lanes: 1 floor (Flip_U1); 2 ceiling (Inv_1, Spin_1, Drop_2, Piston_1, Flip_D1); 3 floor (Piston_2, Lift_3, Spin_2, Inv_3,
    //  Piston_3, Flip_U2); 4 ceiling (Piston_4, Spin_3, Inv_4, Jaw_4, Flip_D3); 5 floor (Inv_5a, Spin_5, Jaw_5a, Inv_5b, Piston_5,
    //  Jaw_5b, Flip_U5); 6 ceiling (Drop_6, Piston_6, Inv_6, Spin_6, Jaw_6, Flip_D6); 7 floor (the closing hall, Inv_F, the door).
    static class L024Layout
    {
        internal const float Floor = 0f, Base = -3f, RoofTop = 8f, Width = 108f;
        // The hall's height by x: its ceiling steps down at each x in CeilSteps (D-210).
        internal static readonly (float start, float ceil)[] CeilSteps = { (0f, 6f), (38f, 5f), (55f, 4.2f), (87.5f, 3.6f) };
        internal static float Ceil(float x) => CeilSteps.Last(s => x >= s.start).ceil;
        internal static float Mid(float x) => Ceil(x) * .5f;

        // A piston: out PistonMove, held PistonHold in the other half, back PistonMove, home the rest of PistonPeriod.
        internal const int PistonPeriod = 160, PistonMove = 25, PistonHold = 50;
        // Each piston's x, its home half (true: the ceiling's), and its phase.
        internal static readonly (string name, float x, bool ceilingHome, int phase)[] Pistons = {
            ("Piston_1", 18.5f, true, 158), ("Piston_2", 25.5f, false, 131), ("Piston_3", 34.5f, false, 129),
            ("Piston_4", 43.5f, true, 91), ("Piston_5", 64.5f, false, 54), ("Piston_6", 75.5f, true, 11),
        };
        // Each spinner's x, turn (degrees a tick) and start angle; its arm reaches 0.4 short of the floor and the ceiling.
        internal static readonly (string name, float x, float rate, float start)[] Spinners = {
            ("Spin_1", 13.5f, 3f, 156f), ("Spin_2", 30f, -3f, 357f), ("Spin_3", 48f, 4f, 316f), ("Spin_5", 59f, -4f, 96f), ("Spin_6", 80.5f, 3.6f, 212.4f),
        };
        // A squeeze section (D-210): a piece of the face the cat walks on (`onCeiling` for a ceiling lane), from x0 to x1;
        // SqueezeDelay after a cat stands on it, it moves at SqueezeSpeed toward the far face until a rider's head is in that face's
        // spikes, holds, and goes back.
        internal const int SqueezeDelay = 12, SqueezeHold = 30;
        internal const float SqueezeSpeed = .12f, SqueezeThick = .5f;
        internal static readonly (string name, float x0, float x1, bool onCeiling)[] Squeezes = {
            ("Drop_2", 16.3f, 17.8f, true), ("Lift_3", 26.3f, 27.9f, false), ("Drop_6", 72.2f, 74.4f, true),
        };
        // A jaw (D-210): a block rising from the floor and one dropping from the ceiling at x, JawWidth wide, each half the hall's
        // height less JawGap / 2, closing over JawMove, held JawHold, opening over JawMove, every JawPeriod from its phase.
        internal const int JawPeriod = 110, JawMove = 12, JawHold = 20;
        internal const float JawWidth = 1.2f, JawGap = .4f;
        internal static readonly (string name, float x, int phase)[] Jaws = { ("Jaw_4", 52f, 0), ("Jaw_5a", 61.8f, 35), ("Jaw_5b", 67.8f, 80), ("Jaw_6", 84f, 55) };
        // The closing hall (D-210): CaveCount sections, CaveWidth wide, from CaveFrom; the first slams CaveDelay after a cat passes
        // CaveTrigger, each next CaveStep after the one before (CaveWidth over CaveStep is a little slower than a run).
        internal const int CaveCount = 9, CaveDelay = 25, CaveStep = 20, CaveMove = 10;
        internal const float CaveFrom = 88f, CaveWidth = 2f, CaveTrigger = 88.2f, CaveThick = 1.2f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop + 1f) * .5f),(1f,RoofTop + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + RoofTop + 1f) * .5f),(1f,RoofTop + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,Floor),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(106.5f,Floor + .75f),(.6f,1.5f)),
            };

            // ---- The ground and the roof, each cut where a moving piece of it sits (a seat under or over each slot) ----
            var floorSlots = new List<(string name, float x0, float x1, float depth)>();
            var roofSlots = new List<(string name, float x0, float x1, float depth)>();
            foreach (var (name, x0, x1, onCeiling) in Squeezes) (onCeiling ? roofSlots : floorSlots).Add((name, x0, x1, SqueezeThick));
            foreach (var (name, x, _) in Jaws)
            {
                float reach = JawReach(x);
                floorSlots.Add((name + "_F", x - JawWidth * .5f, x + JawWidth * .5f, reach));
                roofSlots.Add((name + "_C", x - JawWidth * .5f, x + JawWidth * .5f, reach));
            }
            for (int i = 0; i < CaveCount; i++) roofSlots.Add(("Cave_" + (i + 1), CaveFrom + i * CaveWidth, CaveFrom + (i + 1) * CaveWidth, CaveThick));
            Ground(elements, floorSlots);
            Roof(elements, roofSlots);

            // ---- The honest spikes: each lane's other face, ending 2.5 or more short of the orb that leaves the lane, and clear of
            // the jaws ----
            Spikes(elements, "Spikes_C0", 2.5f, 5.5f, Ceil(4f), down: true);
            Spikes(elements, "Spikes_F1", 8f, 19f, Floor);
            Spikes(elements, "Spikes_C2", 22.5f, 36f, Ceil(30f), down: true);
            Spikes(elements, "Spikes_F3", 40f, 51f, Floor);
            Spikes(elements, "Spikes_C5a", 55.5f, 61f, Ceil(58f), down: true);
            Spikes(elements, "Spikes_C5b", 62.6f, 66.8f, Ceil(64f), down: true);
            Spikes(elements, "Spikes_F6", 72f, 83.2f, Floor);

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);
            Orb(elements, "Flip_U1", 6.5f, up, true);
            Orb(elements, "Flip_D1", 21.5f, down, false);
            Orb(elements, "Flip_U2", 38.5f, up, true);
            Orb(elements, "Flip_D3", 53.5f, down, false);
            Orb(elements, "Flip_U5", 70f, up, true);
            Orb(elements, "Flip_D6", 86f, down, false);

            // ---- The inverters, all hidden ----
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_1",(9f,Ceil(9f) - 1f),(.4f,2f),settings:new SoloRoomTrapSettings(
                new InverterSettings(300,disguised:true,endPosition:new Vector2(20.2f,Mid(20.2f)),endSize:new Vector2(.4f,Ceil(20.2f))),new SoloRoomTrapSettings(delayTicks:0))));
            Inverter(elements, "Inv_3", 33.3f, false, 70);
            Inverter(elements, "Inv_4", 50.7f, true, 60);
            // Inv_5a's end box, just past Jaw_5a, swaps the controls back; Inv_5b, just after it, swaps them again.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_5a",(56.5f,Floor + 1f),(.4f,2f),settings:new SoloRoomTrapSettings(
                new InverterSettings(200,disguised:true,endPosition:new Vector2(62.6f,Mid(62.6f)),endSize:new Vector2(.4f,Ceil(62.6f))),new SoloRoomTrapSettings(delayTicks:0))));
            Inverter(elements, "Inv_5b", 63.4f, false, 70);
            Inverter(elements, "Inv_6", 77.3f, true, 60);
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_F",(100f,Floor + 1f),(.4f,2f),settings:new SoloRoomTrapSettings(
                new InverterSettings(120,disguised:true),new SoloRoomTrapSettings(delayTicks:0))));

            // ---- Spinners and pistons, sized to their lane ----
            foreach (var (name, x, rate, start) in Spinners)
                elements.Add(E(SoloRoomElementKind.Spinner,name,(x,Mid(x)),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,Mid(x) - .4f,rate,start),new SoloRoomTrapSettings())));
            foreach (var (name, x, ceilingHome, phase) in Pistons)
            {
                float half = Mid(x);
                elements.Add(E(SoloRoomElementKind.MovingTrap,name,(x,ceilingHome ? half * 1.5f : half * .5f),(1f,half),settings:Piston(ceilingHome ? -half : half, phase)));
            }

            // ---- Squeezes ----
            foreach (var (name, x0, x1, onCeiling) in Squeezes)
            {
                float x = (x0 + x1) * .5f, w = x1 - x0, ceil = Ceil(x);
                // Until a rider's head is in the far face's spikes (0.3 tall): the hall's height less the cat's and the spikes', and a little.
                float travel = ceil - .56f - .3f + .05f;
                float face = onCeiling ? ceil : Floor;
                float centreY = onCeiling ? face + SqueezeThick * .5f : face - SqueezeThick * .5f, triggerY = onCeiling ? face - .3f : face + .3f;
                int move = Mathf.RoundToInt(travel / SqueezeSpeed);
                elements.Add(E(SoloRoomElementKind.MovingTrap, name, (x, centreY), (w, SqueezeThick), (x, triggerY), (w, .6f), new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(delayTicks: SqueezeDelay, offset: new Vector2(0f, onCeiling ? -travel : travel), moveTicks: move, holdTicks: SqueezeHold,
                        returnTicks: move, repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 2 * move + SqueezeHold, movingKind: MovingTrapKind.Solid))));
            }

            // ---- Jaws ----
            foreach (var (name, x, phase) in Jaws)
            {
                float reach = JawReach(x), ceil = Ceil(x);
                elements.Add(E(SoloRoomElementKind.MovingTrap, name + "_F", (x, Floor - reach * .5f), (JawWidth, reach), settings: Jaw(new Vector2(0f, reach), phase)));
                elements.Add(E(SoloRoomElementKind.MovingTrap, name + "_C", (x, ceil + reach * .5f), (JawWidth, reach), settings: Jaw(new Vector2(0f, -reach), phase)));
            }

            // ---- The closing hall ----
            float caveCeil = Ceil(CaveFrom), drop = caveCeil - JawGap;
            for (int i = 0; i < CaveCount; i++)
            {
                string name = "Cave_" + (i + 1);
                float x = CaveFrom + (i + .5f) * CaveWidth;
                var timing = i == 0
                    ? new SoloRoomTrapSettings(delayTicks: CaveDelay, offset: new Vector2(0f, -drop), moveTicks: CaveMove, holdTicks: 9999, movingKind: MovingTrapKind.Solid)
                    : new SoloRoomTrapSettings(delayTicks: CaveStep, offset: new Vector2(0f, -drop), moveTicks: CaveMove, holdTicks: 9999, triggerSource: TrapTriggerSource.Chain,
                        chainSource: "Cave_" + i, movingKind: MovingTrapKind.Solid);
                var settings = new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry, slams: true), timing);
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.MovingTrap, name, (x, caveCeil + CaveThick * .5f), (CaveWidth, CaveThick), (CaveTrigger, caveCeil * .5f), (.4f, caveCeil), settings)
                    : E(SoloRoomElementKind.MovingTrap, name, (x, caveCeil + CaveThick * .5f), (CaveWidth, CaveThick), settings: settings));
            }

            // D-209: no checkpoints (the developer: "no need for checkpoints in this one"); a death restarts the hall.
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, null);
        }

        // How far each half of a jaw moves: half the hall's height there, less half the gap left between them.
        static float JawReach(float x) => Mid(x) - JawGap * .5f;

        // The ground from Base to the floor, cut at each slot: a slot keeps a seat `depth` under the floor (its piece fills it).
        static void Ground(List<SoloRoomElement> e, List<(string name, float x0, float x1, float depth)> slots)
        {
            float cursor = 0f;
            foreach (var (name, x0, x1, depth) in slots.OrderBy(s => s.x0))
            {
                if (x0 > cursor) Block(e, SoloRoomElementKind.Floor, "Ground_" + name, cursor, x0, Base, Floor);
                Block(e, SoloRoomElementKind.Floor, name + "_Seat", x0, x1, Base, Floor - depth);
                cursor = x1;
            }
            Block(e, SoloRoomElementKind.Floor, "Ground_E", cursor, Width, Base, Floor);
        }

        // The roof from each stretch's ceiling (CeilSteps) up to RoofTop, cut at each slot: a slot keeps a cap `depth` over the
        // ceiling there (its piece fills the slot under the cap).
        static void Roof(List<SoloRoomElement> e, List<(string name, float x0, float x1, float depth)> slots)
        {
            var cuts = new List<float> { 0f, Width };
            foreach (var (start, _) in CeilSteps) cuts.Add(start);
            foreach (var (_, x0, x1, _) in slots) { cuts.Add(x0); cuts.Add(x1); }
            cuts = cuts.Distinct().OrderBy(c => c).ToList();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float x0 = cuts[i], x1 = cuts[i + 1], ceil = Ceil(x0);
                var slot = slots.FirstOrDefault(s => s.x0 <= x0 + 1e-3f && s.x1 >= x1 - 1e-3f);
                string name = slot.name != null ? slot.name + "_Cap" : $"Roof_{i}";
                Block(e, SoloRoomElementKind.Ceiling, name, x0, x1, slot.name != null ? ceil + slot.depth : ceil, RoofTop);
            }
        }

        // A gravity orb, 1 wide and 2 tall, standing on the floor (or hanging from the ceiling) at x.
        static void Orb(List<SoloRoomElement> e, string name, float x, SoloRoomTrapSettings settings, bool onFloor) =>
            e.Add(E(SoloRoomElementKind.GravityFlip, name, (x, onFloor ? Floor + 1.5f : Ceil(x) - 1.5f), (1f, 2f), settings: settings));

        // A short hidden inverter (D-210), standing on the floor (or hanging from the ceiling) at x, for `ticks`.
        static void Inverter(List<SoloRoomElement> e, string name, float x, bool onCeiling, int ticks) =>
            e.Add(E(SoloRoomElementKind.Inverter, name, (x, onCeiling ? Ceil(x) - 1f : Floor + 1f), (.4f, 2f), settings: new SoloRoomTrapSettings(
                new InverterSettings(ticks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));

        // A piston: half the hall's height, home in one half; it slides `dy` into the other half over PistonMove, holds, and slides
        // back, on PistonPeriod. A slam: coming down it crushes a cat under it, going up it crushes one against the ceiling.
        static SoloRoomTrapSettings Piston(float dy, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, dy), moveTicks: PistonMove, holdTicks: PistonHold, returnTicks: PistonMove, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: PistonPeriod, phaseTicks: phase, cooldownTicks: 2 * PistonMove + PistonHold, movingKind: MovingTrapKind.Solid));

        // One half of a jaw: out `offset` over JawMove, held JawHold, back over JawMove, every JawPeriod. A slam.
        static SoloRoomTrapSettings Jaw(Vector2 offset, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(offset: offset, moveTicks: JawMove, holdTicks: JawHold, returnTicks: JawMove, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: JawPeriod, phaseTicks: phase, cooldownTicks: 2 * JawMove + JawHold, movingKind: MovingTrapKind.Solid));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));
    }
}
