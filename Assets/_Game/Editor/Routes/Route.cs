using System;
using System.Collections.Generic;
using System.Globalization;
using Parallax.Core;

namespace Parallax.Editor.Routes
{
    // PAX-075 (D-079): a room's anatomy as data. A route is a list of scripted steps the harness
    // turns into one CatCommand per tick. Conditions read the previous tick's post-physics record
    // (the frame the player last saw), before this tick's motor step.
    public enum RouteStepKind { Hold, Release, Jump, Until, For, Margin }
    public enum TimedMode { None, Shift, Hesitate }

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
            RouteStepKind.Jump => "Jump()",
            RouteStepKind.Until => "Until(" + Condition.Label + ")",
            RouteStepKind.For => "For(" + Ticks + ")",
            _ => "Margin(" + MarginName + ")",
        };
    }

    public sealed class Route
    {
        public readonly string Name;
        public readonly IReadOnlyList<RouteStep> Steps;
        // What "completes" a replay of this route: the room's door by default.
        public readonly RouteCondition Goal;

        public Route(string name, params RouteStep[] steps) : this(name, null, steps) { }
        public Route(string name, RouteCondition goal, params RouteStep[] steps)
        {
            Name = name; Steps = steps; Goal = goal ?? R.RoomComplete();
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
        public Betrayal(string name, string killer, DeathCause cause, Route route, string revealedBy = null)
        {
            Name = name; Killer = killer; Cause = cause; Route = route; RevealedBy = revealedBy ?? killer; Outcome = BetrayalOutcome.Dies;
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
        public const string CatGravity = "Cat.Gravity";
        // PAX-085 (D-087): the second extra element. Its first visible change is the inverter cue switching on (the fire
        // tick); its render box for the camera tell rule is the cat's collider.
        public const string CatInverted = "Cat.Inverted";

        public static RouteStep Hold(int direction) => new(RouteStepKind.Hold, direction: direction);
        public static RouteStep Release() => new(RouteStepKind.Release);
        public static RouteStep Jump() => new(RouteStepKind.Jump);
        public static RouteStep Until(RouteCondition condition) => new(RouteStepKind.Until, condition: condition);
        public static RouteStep For(int ticks) => new(RouteStepKind.For, ticks: ticks);
        public static RouteStep Margin(string name, RouteCondition from, RouteCondition to, int atLeast) =>
            new(RouteStepKind.Margin, marginName: name, marginFrom: from, marginTo: to, marginAtLeast: atLeast);

        public static RouteCondition XAtLeast(float x) => new("X>=" + x.ToString(CultureInfo.InvariantCulture), v => v.Last.X >= x);
        public static RouteCondition XAtMost(float x) => new("X<=" + x.ToString(CultureInfo.InvariantCulture), v => v.Last.X <= x);
        public static RouteCondition Grounded() => new("Grounded", v => v.Last.Grounded);
        public static RouteCondition Airborne() => new("Airborne", v => !v.Last.Grounded);
        public static RouteCondition GroundedOn(string element) => new($"GroundedOn({element})", v => v.Last.Grounded && v.Last.Ground == element);
        public static RouteCondition Still() => new("Still", v => v.Last.Grounded && Math.Abs(v.Last.Vx) < 1e-3f);
        public static RouteCondition GravityUp() => new("GravityUp", v => v.Last.GravityUp);
        public static RouteCondition GravityDown() => new("GravityDown", v => !v.Last.GravityUp);
        public static RouteCondition Fired(string element) => new($"Fired({element})", v => v.Fired(element));
        // Fired, away from its authored pose, and not moving since the tick before (a landed block, a stopped arrow).
        public static RouteCondition Stopped(string element) => new($"Stopped({element})", v => v.Stopped(element));
        // Its visible state changed since the tick before (a trap in motion, a renderer switching).
        public static RouteCondition Moving(string element) => new($"Moving({element})", v => v.Moving(element));
        // Fired and back at its authored pose (a Rearm hazard that has returned).
        public static RouteCondition Home(string element) => new($"Home({element})", v => v.Home(element));
        public static RouteCondition RoomComplete() => new("RoomComplete", v => v.Last.Complete);
        public static RouteCondition Dead() => new("Dead", v => v.Last.Dead);
    }
}
