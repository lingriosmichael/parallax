using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-078 (D-076, R4/R10): a narrow per-tick stepper for one level layout at the real tick rate
    // (TickTime) with the real CatMotorConfig. Model, recorded in D-076:
    //  - tick k: motor step (CatMotor2D.Step: fall += g dt, a jump sets it to JumpMath speed, run
    //    speed toward the command), then the room step on poses from the end of tick k-1 (traps fire,
    //    kill checks, gravity flips on first overlap), then physics (x then y, against static solids);
    //  - the cat is a 1 x 0.56 box (the real capsule's rounded ends are ignored);
    //  - Floor/Wall/PitBottom/Ceiling, MovingTrap Solids at their current pose, and FallingBlocks that
    //    are not moving (hanging or landed) are solid; a moving FallingBlock kills on overlap of its
    //    box with its size shrunk by 0.04, 0.02 per side (FallingBlockTrap), with no push-out;
    //    moving Solids neither carry nor crush the cat; revealed HiddenSpikes and Hazards
    //    kill on overlap;
    //  - an Overlap trap fires at (first overlap tick + its delay), a Chain trap at (source fire +
    //    delay). Only Once timing is modelled: no Periodic or Rearm trap may sit in the stretch.
    // Layout types live in the Editor assembly, so elements are read through reflection.
    sealed class RoomStepper
    {
        public enum End { Running, Killed, PastX, OnCeiling, TimedOut }

        sealed class Item
        {
            public string Name, Kind, Chain, Movement; public Rect Box, Trigger; public bool HasTrigger;
            public int Delay, Fire = -1, MoveTicks; public float UnitsPerTick, Travel; public Vector2 Offset;
        }

        // FallingBlockTrap.cs:33 uses bounds.Expand(-.04f): the size shrinks by 0.04, i.e. 0.02 per side.
        const float Shrink = .02f, W = 1f, H = .56f;
        readonly List<Item> items = new List<Item>();
        readonly float dt, gravity, maxSpeed, acceleration, maxFall, jumpSpeed;
        float x, y, vx, vy; int g = -1; bool grounded = true; bool inFlip; int k;
        Vector2 prev;

        // True when the named FallingBlock has fired and its pose is wholly below the cat's box.
        public bool IsFiredAndBelowCat(string name)
        {
            Item t = items.FirstOrDefault(i => i.Name == name);
            return t != null && t.Fire >= 0 && t.Fire < k && Pose(t, k - 1).yMax <= prev.y;
        }

        public string Killer { get; private set; }
        public bool Flipped { get; private set; }
        public int Tick => k;

        public RoomStepper(object room, CatMotorConfig motor, float gravityStrength, float startCentreX, float startPaw)
        {
            dt = TickTime.SecondsPerTick; gravity = gravityStrength; maxSpeed = motor.MaxSpeed;
            acceleration = motor.Acceleration; maxFall = motor.MaxFallSpeed; jumpSpeed = JumpMath.SpeedForHeight(motor.JumpHeight, gravityStrength);
            Assert(Mathf.Approximately(motor.ColliderSize.x, W) && Mathf.Approximately(motor.ColliderSize.y, H), "stepper assumes a 1 x 0.56 cat");
            foreach (object e in (IEnumerable)Field(room, "Elements")) items.Add(Read(e));
            x = startCentreX; y = startPaw; vx = maxSpeed; prev = new Vector2(x, y);
        }

        // Holds right the whole time and jumps once, the first grounded tick with centre >= takeoffX.
        public End RunRightHolding(float takeoffX, float stopAtX, int maxTicks)
        {
            bool jumped = false;
            while (k < maxTicks)
            {
                bool jump = !jumped && g < 0 && grounded && x >= takeoffX;
                if (jump) jumped = true;
                Step(1f, jump);
                if (Killer != null) return End.Killed;
                if (prev.x >= stopAtX) return End.PastX;
                if (g > 0 && grounded) return End.OnCeiling;
            }
            return End.TimedOut;
        }

        void Step(float move, bool jump)
        {
            // Motor.
            float target = move * maxSpeed;
            vx = Mathf.MoveTowards(vx, target, acceleration * dt);
            vy += g * gravity * dt;
            vy = g < 0 ? Mathf.Max(vy, -maxFall) : Mathf.Min(vy, maxFall);
            if (jump && grounded) { vy = -g * jumpSpeed; grounded = false; }

            // Room step on the pose from the end of the previous tick.
            Rect cat = new Rect(prev.x - W * .5f, prev.y, W, H);
            foreach (Item t in items.Where(t => t.Fire < 0 && t.HasTrigger && t.Chain == null))
                if (cat.Overlaps(t.Trigger)) t.Fire = k + t.Delay;
            foreach (Item t in items.Where(t => t.Fire < 0 && t.Chain != null))
            {
                Item source = items.First(s => s.Name == t.Chain);
                if (source.Fire >= 0 && source.Fire <= k) t.Fire = source.Fire + t.Delay;
            }
            foreach (Item t in items)
            {
                if (t.Kind == "FallingBlock" && Moving(t, k) && cat.Overlaps(Inset(Pose(t, k - 1)))) Killer = t.Name;
                if ((t.Kind == "HiddenSpikes" && t.Fire >= 0 && k >= t.Fire) || t.Kind == "Hazard")
                    if (cat.Overlaps(t.Box)) Killer ??= t.Name;
            }
            bool overlappingFlip = items.Any(t => t.Kind == "GravityFlip" && cat.Overlaps(t.Box));
            if (overlappingFlip && !inFlip) { g = -g; Flipped = true; }
            inFlip = overlappingFlip;

            // Physics: x, then y, against this tick's static solids.
            List<Rect> solids = items.Where(t => IsSolid(t, k)).Select(t => Pose(t, k)).ToList();
            float nx = x + vx * dt;
            foreach (Rect s in solids)
                if (y < s.yMax - 1e-5f && y + H > s.yMin + 1e-5f && nx + W * .5f > s.xMin && nx - W * .5f < s.xMax)
                { nx = vx > 0f ? s.xMin - W * .5f : s.xMax + W * .5f; vx = 0f; }
            x = nx;
            float ny = y + vy * dt; grounded = false;
            foreach (Rect s in solids)
                if (x + W * .5f > s.xMin + 1e-5f && x - W * .5f < s.xMax - 1e-5f && ny < s.yMax && ny + H > s.yMin)
                {
                    ny = vy <= 0f ? s.yMax : s.yMin - H;
                    if ((g < 0 && vy <= 0f) || (g > 0 && vy >= 0f)) grounded = true;
                    vy = 0f;
                }
            y = ny; prev = new Vector2(x, y); k++;
        }

        bool IsSolid(Item t, int tick) =>
            t.Kind == "Floor" || t.Kind == "Wall" || t.Kind == "PitBottom" || t.Kind == "Ceiling"
            || (t.Kind == "MovingTrap" && t.Movement == "Solid") || (t.Kind == "FallingBlock" && !Moving(t, tick));

        static bool Moving(Item t, int tick) => t.Fire >= 0 && tick >= t.Fire && Travel(t, tick) < t.Travel;
        static float Travel(Item t, int tick) => t.Fire < 0 || tick < t.Fire ? 0f : Mathf.Min((tick - t.Fire) * t.UnitsPerTick, t.Travel);

        static Rect Pose(Item t, int tick)
        {
            Rect r = t.Box;
            if (t.Kind == "FallingBlock") r.y -= Travel(t, tick);
            if (t.Kind == "MovingTrap" && t.Fire >= 0 && tick > t.Fire) r.position += t.Offset * Mathf.Clamp01((float)(tick - t.Fire) / Mathf.Max(1, t.MoveTicks));
            return r;
        }

        static Rect Inset(Rect r) => Rect.MinMaxRect(r.xMin + Shrink, r.yMin + Shrink, r.xMax - Shrink, r.yMax - Shrink);

        static Item Read(object e)
        {
            object s = Field(e, "Settings");
            var item = new Item
            {
                Name = (string)Field(e, "Name"), Kind = Field(e, "Kind").ToString(),
                Box = Box((Vector2)Field(e, "Position"), (Vector2)Field(e, "Size")),
            };
            if (!(bool)Field(s, "IsConfigured")) return item;
            string repeat = Field(s, "RepeatMode").ToString();
            Assert(repeat == "Once" || item.Kind == "GravityFlip" || item.Kind == "CollapsingFloor", item.Name + ": the stepper models Once timing only (repeat " + repeat + ")");
            item.Delay = item.Kind == "HiddenSpikes" ? (int)Field(s, "RevealDelayTicks") : (int)Field(s, "DelayTicks");
            item.UnitsPerTick = (float)Field(s, "UnitsPerTick"); item.Travel = (float)Field(s, "TravelDistance");
            item.Offset = (Vector2)Field(s, "Offset"); item.MoveTicks = (int)Field(s, "MoveTicks");
            item.Movement = Field(s, "MovingKind").ToString();
            Assert(item.Kind != "FallingBlock" || Field(s, "Direction").ToString() == "Down", item.Name + ": the stepper models falling-down blocks only");
            if (Field(s, "TriggerSource").ToString() == "Chain") item.Chain = (string)Field(s, "ChainSource");
            else if (item.Kind != "GravityFlip" && item.Kind != "CollapsingFloor")
            {
                Vector2 size = (Vector2)Field(e, "SecondarySize");
                item.HasTrigger = true;
                item.Trigger = size != Vector2.zero ? Box((Vector2)Field(e, "SecondaryPosition"), size) : item.Box;
            }
            return item;
        }

        static Rect Box(Vector2 centre, Vector2 size) => new Rect(centre - size * .5f, size);
        static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException("RoomStepper: " + message); }
        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
