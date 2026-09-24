using System.Collections.Generic;
using System.Text;
using Parallax.Core;

namespace Parallax.Editor.Routes
{
    // PAX-075 (D-079): what the harness saw at the end of one tick (after Physics2D.Simulate).
    // Positions are the cat collider's centre, local to the room's origin.
    public sealed class TickRecord
    {
        public int Tick, RoomLifeTick;
        public float X, Y, Vx, Vy;
        public bool Grounded, GravityUp, Dead, Holding, Complete;
        public string Ground;
        public bool JumpPressed; public int Move;
        // Per element, in ReplayResult.Elements order.
        public int[] FireTick;   // the element's LatestFireTick (room ticks), -1 before its first fire
        public int[] Signature;  // hash of every SpriteRenderer in the element's subtree (Q5)
        // PAX-076 (D-083) R3, for the camera tell rule only (SameAs ignores them): the cat's Transform position, and
        // per element the union of its enabled, active SpriteRenderers' bounds (Rendered false: nothing drawn).
        // Both local to the room's origin.
        public float CatX, CatY;
        public UnityEngine.Rect[] RenderBounds;
        public bool[] Rendered;

        public bool SameAs(TickRecord o)
        {
            if (Tick != o.Tick || RoomLifeTick != o.RoomLifeTick || X != o.X || Y != o.Y || Vx != o.Vx || Vy != o.Vy
                || Grounded != o.Grounded || GravityUp != o.GravityUp || Dead != o.Dead || Holding != o.Holding || Complete != o.Complete
                || Ground != o.Ground || JumpPressed != o.JumpPressed || Move != o.Move) return false;
            for (int i = 0; i < FireTick.Length; i++) if (FireTick[i] != o.FireTick[i] || Signature[i] != o.Signature[i]) return false;
            return true;
        }

        public override string ToString() =>
            $"t{Tick} r{RoomLifeTick} x{X:F4} y{Y:F4} vx{Vx:F3} vy{Vy:F3} g{(Grounded ? 1 : 0)}:{Ground} up{(GravityUp ? 1 : 0)} m{Move}{(JumpPressed ? " J" : "")}{(Dead ? " DEAD" : "")}{(Complete ? " DONE" : "")}";
    }

    // What a route condition sees: the previous tick's record plus element lookups by name.
    public sealed class RouteView
    {
        internal ReplayResult Result;
        public TickRecord Last => Result.Records[Result.Records.Count - 1];
        TickRecord Previous => Result.Records.Count > 1 ? Result.Records[Result.Records.Count - 2] : Result.Records[0];
        int Index(string element)
        {
            int i = Result.Elements.IndexOf(element);
            if (i < 0) throw new KeyNotFoundException($"Route condition names '{element}', which is not an element of this room.");
            return i;
        }
        public bool Fired(string element) => Last.FireTick[Index(element)] >= 0;
        public bool Stopped(string element) { int i = Index(element); return Fired(element) && Last.Signature[i] != Result.Records[0].Signature[i] && Last.Signature[i] == Previous.Signature[i]; }
        public bool Moving(string element) { int i = Index(element); return Last.Signature[i] != Previous.Signature[i]; }
        public bool Home(string element) { int i = Index(element); return Fired(element) && Last.Signature[i] == Result.Records[0].Signature[i]; }
    }

    public sealed class KillInfo
    {
        public int Tick;                 // the tick whose room step killed (ObserverSet.Tick)
        public bool CauseKnown; public DeathCause Cause;
        public List<string> Candidates = new();   // R6: names only; never decides whether a kill happened
        public string Killer => Candidates.Count == 1 ? Candidates[0] : null;
    }

    public sealed class ReplayResult
    {
        public string Route;
        public List<string> Elements = new();          // element names; index matches TickRecord arrays; last is R.CatGravity
        public List<TickRecord> Records = new();       // Records[0] = the built room before tick 1
        public Dictionary<int, int> StepStartTick = new();
        public bool Completed;                          // the route's goal was reached with no death
        public KillInfo Kill;
        public string Failure;                          // harness-level reason (tick cap, forced start before tick 1)
        public bool AliveAtCap;                         // PAX-080: the cat was alive and the goal unreached at the 1500-tick or 600-tick step cap
        public Dictionary<string, int> ArrowFirstLethalTick = new();   // R5: arrows expose fire + tell (harness ticks)

        public int FirstTick(System.Func<TickRecord, bool> test)
        {
            for (int i = 1; i < Records.Count; i++) if (test(Records[i])) return Records[i].Tick;
            return -1;
        }

        public int FirstVisibleChange(string element)
        {
            int e = Elements.IndexOf(element);
            if (e < 0) return -1;
            for (int i = 1; i < Records.Count; i++) if (Records[i].Signature[e] != Records[0].Signature[e]) return Records[i].Tick;
            return -1;
        }

        public string Dump(int from = 0, int to = int.MaxValue)
        {
            var sb = new StringBuilder();
            for (int i = System.Math.Max(0, from); i < Records.Count && i <= to; i++) sb.AppendLine(Records[i].ToString());
            return sb.ToString();
        }
    }

    public sealed class WindowResult
    {
        public string Route, Step; public TimedMode Mode;
        public int AuthoredTick, Count, Low, High; public bool OpenLow, OpenHigh;
        public override string ToString() => $"{Route} · {Step} [{Mode}] window {Count} (d {Low}..{High}{(OpenLow ? ", open low" : "")}{(OpenHigh ? ", open high" : "")})";
    }

    public sealed class MarginResult
    {
        public string Route, Name; public int From = -1, To = -1, Value = int.MinValue, AtLeast;
        public bool Passed => From >= 0 && To >= 0 && Value >= AtLeast;
        public override string ToString() => $"{Route} · margin {Name} = {(From < 0 || To < 0 ? "never" : Value.ToString())} (from t{From} to t{To}, needs >= {AtLeast})";
    }

    public sealed class LeadResult
    {
        public string Betrayal, ExpectedKiller, Killer, RevealedBy; public DeathCause ExpectedCause; public bool CauseKnown; public DeathCause Cause;
        public int KillTick = -1, FirstLethalTick = -1, FirstVisibleTick = -1, Lead = int.MinValue;
        public override string ToString() => $"{Betrayal}: killer {Killer ?? "?"} ({(CauseKnown ? Cause.ToString() : "?")}), kill t{KillTick}, first lethal t{FirstLethalTick}, {RevealedBy} visible t{FirstVisibleTick}, lead {Lead}";
    }

    // PAX-080 (D-080): a Recovers betrayal. It passes with no death, the room complete, and RevealedBy
    // visibly changed before the completion tick.
    public sealed class RecoveryResult
    {
        public string Betrayal, RevealedBy, Failure;
        public bool Died, Completed;
        public int FirstVisibleTick = -1, CompletionTick = -1;
        public bool Passed => !Died && Completed && FirstVisibleTick >= 0 && FirstVisibleTick < CompletionTick;
        public override string ToString() => $"{Betrayal}: recovers {(Passed ? "yes" : "NO")} ({RevealedBy} visible t{FirstVisibleTick}, complete t{CompletionTick}{(Died ? ", died" : "")}{(Failure != null ? ", " + Failure : "")})";
    }

    public sealed class RouteReport
    {
        public string LevelId;
        public List<string> Errors = new();
        public bool SolutionCompleted, Deterministic;
        public List<WindowResult> Windows = new();
        public List<MarginResult> Margins = new();
        public List<LeadResult> Leads = new();
        public List<RecoveryResult> Recoveries = new();
        public int Replays; public double Seconds;
        public string Summary()
        {
            var sb = new StringBuilder($"{LevelId}: solution {(SolutionCompleted ? "completes" : "FAILS")}, deterministic {Deterministic}, {Replays} replays, {Seconds:F1} s\n");
            foreach (WindowResult w in Windows) sb.AppendLine("  " + w);
            foreach (MarginResult m in Margins) sb.AppendLine("  " + m);
            foreach (LeadResult l in Leads) sb.AppendLine("  " + l);
            foreach (RecoveryResult r in Recoveries) sb.AppendLine("  " + r);
            foreach (string e in Errors) sb.AppendLine("  ERROR " + e);
            return sb.ToString();
        }
    }
}
