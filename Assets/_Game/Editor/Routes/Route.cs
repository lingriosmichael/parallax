using System;
using System.Collections.Generic;
using System.Globalization;
using Parallax.Core;

namespace Parallax.Editor.Routes
{
    // PAX-075 (D-079): a room's anatomy as data. A route is a list of scripted steps the harness
    // turns into one CatCommand per tick. Conditions read the previous tick's post-physics record
    // (the frame the player last saw), before this tick's motor step.
    // PAX-087 (D-089): HoldClimb and ReleaseClimb are appended; Climb is held beside Move, and Release() clears Move only.
    // PAX-090 (D-091): Rewind is appended: the harness kills the cat (a forced death, not the route's) and steps the death
    // hold until the rewind to the current checkpoint section; the next step starts on the tick after it.
    public enum RouteStepKind { Hold, Release, Jump, Until, For, Margin, HoldClimb, ReleaseClimb, Rewind }
    public enum TimedMode { None, Shift, Hesitate }
    // PAX-087 (D-089): a vertical hold, screen-relative like Hold(Right/Left) (Climb is never gravity-projected).
    public enum Vertical { Down = -1, Up = 1 }

    public sealed class RouteCondition
    {
        public readonly string Label;
        internal readonly Func<RouteView, bool> Test;
        internal RouteCondition(string label, Func<RouteView, bool> test) { Label = label; Test = test; }
        public RouteCondition And(RouteCondition other) => new(Label + " && " + other.Label, v => Test(v) && other.Test(v));
    }

    public sealed class RouteStep
    {
        public readonly RouteStepKind Kind;
        public readonly int Direction;
        public readonly RouteCondition Condition;
        public readonly int Ticks;
        public readonly TimedMode Timing;
        // Margin only.
        public readonly string MarginName;
        public readonly RouteCondition MarginFrom, MarginTo;
        public readonly int MarginAtLeast;

        internal RouteStep(RouteStepKind kind, int direction = 0, RouteCondition condition = null, int ticks = 0, TimedMode timing = TimedMode.None,
            string marginName = null, RouteCondition marginFrom = null, RouteCondition marginTo = null, int marginAtLeast = 0)
        {
            Kind = kind; Direction = direction; Condition = condition; Ticks = ticks; Timing = timing;
            MarginName = marginName; MarginFrom = marginFrom; MarginTo = marginTo; MarginAtLeast = marginAtLeast;
        }

        public RouteStep Timed(TimedMode mode) => new(Kind, Direction, Condition, Ticks, mode, MarginName, MarginFrom, MarginTo, MarginAtLeast);

        public string Label => Kind switch
        {
            RouteStepKind.Hold => Direction > 0 ? "Hold(Right)" : "Hold(Left)",
            RouteStepKind.Release => "Release()",
            RouteStepKind.HoldClimb => Direction > 0 ? "Hold(Up)" : "Hold(Down)",
            RouteStepKind.ReleaseClimb => "ReleaseClimb()",
            RouteStepKind.Jump => "Jump()",
            RouteStepKind.Until => "Until(" + Condition.Label + ")",
            RouteStepKind.For => "For(" + Ticks + ")",
            RouteStepKind.Rewind => "Rewind()",
            _ => "Margin(" + MarginName + ")",
        };
    }

    public sealed class Route
    {
        public readonly string Name;
        public readonly IReadOnlyList<RouteStep> Steps;
        // What "completes" a replay of this route: the room's door by default.
        public readonly RouteCondition Goal;
        // PAX-090 (D-091), FromSection only: the checkpoint section whose gate the prefix replays to, how many more ticks
        // of the prefix run before the Rewind step, and that step's index. Null section: an ordinary route.
        public readonly string RewindSection;
        public readonly int RewindAfterTicks;
        public readonly int RewindIndex = -1;

        public Route(string name, params RouteStep[] steps) : this(name, null, steps) { }
        public Route(string name, RouteCondition goal, params RouteStep[] steps)
        {
            Name = name; Steps = steps; Goal = goal ?? R.RoomComplete();
        }

        Route(string name, RouteCondition goal, RouteStep[] steps, string rewindSection, int rewindAfterTicks, int rewindIndex) : this(name, goal, steps)
        {
            RewindSection = rewindSection; RewindAfterTicks = rewindAfterTicks; RewindIndex = rewindIndex;
        }

        // PAX-090 (D-091) Q4: the solution's steps (untimed) until it passes checkpointSection's gate, then a real death and
        // the rewind to that gate, then `then`. The harness jumps from wherever the prefix is to the Rewind step on the tick
        // the gate is passed (plus rewindAfterTicks); a prefix that never passes it fails the replay.
        public static Route FromSection(Route solution, string checkpointSection, string name, params RouteStep[] then) =>
            FromSection(solution, checkpointSection, 0, name, then);

        public static Route FromSection(Route solution, string checkpointSection, int rewindAfterTicks, string name, params RouteStep[] then)
        {
            if (string.IsNullOrEmpty(checkpointSection)) throw new ArgumentException($"Route.FromSection '{name}': checkpointSection is required.");
            var steps = new List<RouteStep>();
            foreach (RouteStep step in solution.Steps) steps.Add(step.Timing == TimedMode.None ? step : step.Timed(TimedMode.None));
            int rewind = steps.Count;
            steps.Add(R.Rewind());
            steps.AddRange(then);
            return new Route(name, solution.Goal, steps.ToArray(), checkpointSection, rewindAfterTicks < 0 ? 0 : rewindAfterTicks, rewind);
        }

        // R8: a betrayal reuses the solution's steps up to and including the first step with this label.
        public static Route PrefixOf(Route source, string throughStep, string name, params RouteStep[] then)
        {
            var steps = new List<RouteStep>();
            foreach (RouteStep step in source.Steps)
            {
                steps.Add(step.Timing == TimedMode.None ? step : step.Timed(TimedMode.None));
                if (step.Label == throughStep) { steps.AddRange(then); return new Route(name, source.Goal, steps.ToArray()); }
            }
            throw new ArgumentException($"Route.PrefixOf: '{source.Name}' has no step '{throughStep}'.");
        }
    }

    // PAX-080 (D-080): how a betrayal route ends. Dies: at its killer, with a lead from RevealedBy. Recovers: the
    // room completes after RevealedBy has visibly changed (a dead end that isn't a soft-lock on its authored path).
    public enum BetrayalOutcome { Dies, Recovers }

    public sealed class Betrayal
    {
        public readonly string Name, Killer, RevealedBy;
        public readonly DeathCause Cause;
        public readonly Route Route;
        public readonly BetrayalOutcome Outcome;
        // PAX-060 (D-097): an optional declared escape. Escape(d) is a route that presses its way out d ticks after
        // RevealedBy's first visible change (R.Revealed) and completes the level. With one declared, the camera tell rule
        // checks the reveal against the last escape tick instead of the kill (LevelLayoutValidator.EscapeTell).
        public readonly Func<int, Route> Escape;
        public Betrayal(string name, string killer, DeathCause cause, Route route, string revealedBy = null, Func<int, Route> escape = null)
        {
            Name = name; Killer = killer; Cause = cause; Route = route; RevealedBy = revealedBy ?? killer; Outcome = BetrayalOutcome.Dies; Escape = escape;
        }

        Betrayal(string name, string revealedBy, Route route)
        {
            Name = name; RevealedBy = revealedBy; Route = route; Outcome = BetrayalOutcome.Recovers;
        }

        // The route's goal is RoomComplete(), whatever the route it's built from declares.
        public static Betrayal Recovers(string name, string revealedBy, Route route)
        {
            if (string.IsNullOrEmpty(revealedBy)) throw new ArgumentException($"Betrayal.Recovers '{name}': revealedBy is required.");
            var steps = new RouteStep[route.Steps.Count];
            for (int i = 0; i < steps.Length; i++) steps[i] = route.Steps[i];
            return new Betrayal(name, revealedBy, new Route(route.Name, R.RoomComplete(), steps));
        }
    }

    public sealed class RoomRoutes
    {
        public readonly Route Solution;
        public readonly IReadOnlyList<Betrayal> Betrayals;
        public RoomRoutes(Route solution, params Betrayal[] betrayals) { Solution = solution; Betrayals = betrayals; }
    }

    // The authoring vocabulary: using static Parallax.Editor.Routes.R.
    public static class R
    {
        public const int Right = 1, Left = -1;
        public const Vertical Up = Vertical.Up, Down = Vertical.Down;
        public const string CatGravity = "Cat.Gravity";
        // PAX-085 (D-087): the second extra element. Its first visible change is the inverter cue switching on (the fire
        // tick); its render box for the camera tell rule is the cat's collider.
        public const string CatInverted = "Cat.Inverted";

        public static RouteStep Hold(int direction) => new(RouteStepKind.Hold, direction: direction);
        public static RouteStep Release() => new(RouteStepKind.Release);
        // PAX-087 (D-089): hold the stick up or down (Climb ±1, full magnitude like every route move, D-081 (4)).
        public static RouteStep Hold(Vertical direction) => new(RouteStepKind.HoldClimb, direction: (int)direction);
        public static RouteStep ReleaseClimb() => new(RouteStepKind.ReleaseClimb);
        // PAX-090 (D-091): see RouteStepKind.Rewind. Only Route.FromSection places it.
        public static RouteStep Rewind() => new(RouteStepKind.Rewind);
        public static RouteStep Jump() => new(RouteStepKind.Jump);
        public static RouteStep Until(RouteCondition condition) => new(RouteStepKind.Until, condition: condition);
        public static RouteStep For(int ticks) => new(RouteStepKind.For, ticks: ticks);
        public static RouteStep Margin(string name, RouteCondition from, RouteCondition to, int atLeast) =>
            new(RouteStepKind.Margin, marginName: name, marginFrom: from, marginTo: to, marginAtLeast: atLeast);

        public static RouteCondition XAtLeast(float x) => new("X>=" + x.ToString(CultureInfo.InvariantCulture), v => v.Last.X >= x);
        public static RouteCondition XAtMost(float x) => new("X<=" + x.ToString(CultureInfo.InvariantCulture), v => v.Last.X <= x);
        // PAX-087 (D-089): how high the cat has climbed (its collider centre, room-local), like XAtLeast.
        public static RouteCondition YAtLeast(float y) => new("Y>=" + y.ToString(CultureInfo.InvariantCulture), v => v.Last.Y >= y);
        public static RouteCondition Grounded() => new("Grounded", v => v.Last.Grounded);
        public static RouteCondition Airborne() => new("Airborne", v => !v.Last.Grounded);
        public static RouteCondition GroundedOn(string element) => new($"GroundedOn({element})", v => v.Last.Grounded && v.Last.Ground == element);
        public static RouteCondition Still() => new("Still", v => v.Last.Grounded && Math.Abs(v.Last.Vx) < 1e-3f);
        public static RouteCondition GravityUp() => new("GravityUp", v => v.Last.GravityUp);
        public static RouteCondition GravityDown() => new("GravityDown", v => !v.Last.GravityUp);
        public static RouteCondition Fired(string element) => new($"Fired({element})", v => v.Fired(element));
        // PAX-060 (D-097): the element has visibly changed (its signature differs from the first tick's).
        public static RouteCondition Revealed(string element) => new($"Revealed({element})", v => v.Result.FirstVisibleChange(element) >= 0);
        // Fired, away from its authored pose, and not moving since the tick before (a landed block, a stopped arrow).
        public static RouteCondition Stopped(string element) => new($"Stopped({element})", v => v.Stopped(element));
        // Its visible state changed since the tick before (a trap in motion, a renderer switching).
        public static RouteCondition Moving(string element) => new($"Moving({element})", v => v.Moving(element));
        // Fired and back at its authored pose (a Rearm hazard that has returned).
        public static RouteCondition Home(string element) => new($"Home({element})", v => v.Home(element));
        public static RouteCondition RoomComplete() => new("RoomComplete", v => v.Last.Complete);
        public static RouteCondition Dead() => new("Dead", v => v.Last.Dead);
        // PAX-087 (D-089): the cat was on a vine at the end of the last tick.
        public static RouteCondition Climbing() => new("Climbing", v => v.Last.IsClimbing);
        // PAX-105 (D-110): on a wall (either side, the left one, the right one).
        public static RouteCondition Clinging() => new("Clinging", v => v.Last.IsClinging);
        public static RouteCondition ClingingLeft() => new("ClingingLeft", v => v.Last.IsClinging && v.Last.ClingSide < 0);
        public static RouteCondition ClingingRight() => new("ClingingRight", v => v.Last.IsClinging && v.Last.ClingSide > 0);
        // PAX-105: moving along gravity (past the top of a rise), the moment a grip face latches (PAX-106: by itself).
        public static RouteCondition Falling() => new("Falling", v => !v.Last.Grounded && (v.Last.GravityUp ? v.Last.Vy > 0f : v.Last.Vy < 0f));
    }
}
