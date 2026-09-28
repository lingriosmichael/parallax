using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Core.Cameras;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Cameras;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-076 (D-083, KIT-4): the camera tell rule (D-071 (6), D-078). For every betrayal that dies, its reveal
    // (RevealedBy's first visible change) must be on screen for at least RevealLeadTicks before it can first kill
    // (RouteValidator.Lead's end: the kill, or an arrow's first lethal tick). The level camera is CameraMath.Step,
    // the game's own step, run over the replay's recorded cat positions (no extra replays per camera case):
    // - frames at 30 and 60 fps, each at 4 phases of a frame, and a starting look direction of -1, 0 and +1 (a
    //   respawn snap keeps the last attempt's direction; Start begins at 0);
    // - the drawn cat is Rigidbody2D interpolation's: between the previous tick's pose and this tick's, so up to
    //   one tick behind the physics (R4);
    // - the frames only drive the smoothing and the interpolated target (R10): at tick k the element is visible when
    //   its rendered bounds at tick k (once it has vanished, the bounds it was last drawn at) overlap the view of the
    //   latest frame rendered at or before tick k. No frame delay is added: the lead counts simulation ticks (D-057);
    // - the on-screen lead is the end tick minus the first tick from which the element is visible at every tick up to
    //   the end. The worst of the 24 cases counts. A room in fit mode at an aspect passes trivially at that aspect.
    // PAX-060 (D-097, opt-in): a betrayal that declares an escape (Betrayal.Escape) is checked against its last escape tick
    // instead of the kill (EscapeTell); every other betrayal keeps the rule above unchanged.
    public static partial class LevelLayoutValidator
    {
        public const string LevelCameraConfigPath = "Assets/_Game/Data/LevelCameraConfig.asset";
        public static readonly string[] CameraTellAspectNames = { "4:3", "16:9", "20:9" };
        public static readonly float[] CameraTellAspects = { 4f / 3f, 16f / 9f, 20f / 9f };
        public static readonly int[] CameraTellFramesPerSecond = { 30, 60 };
        public static readonly float[] CameraTellPhases = { 0f, .25f, .5f, .75f };
        public static readonly float[] CameraTellStartDirections = { -1f, 0f, 1f };

        public sealed class CameraTellResult
        {
            public string Betrayal, RevealedBy, Aspect;
            public bool Fit;
            public int Reveal = -1, End = -1, Lead = -1;      // RouteValidator.Lead's ticks
            public int OnScreenLead = -1;                     // worst case; Lead when Fit
            public int WorstFps; public float WorstPhase, WorstStartDirection;
            // D-097: an escape-backed row. Reveal and LastEscape come from the escape's own replay; Required is the span
            // reveal..last escape (inclusive), which must be on screen throughout and at least RouteValidator.WindowTicks.
            public bool Escape;
            public int LastEscape = -1, Required = RevealLeadTicks;
            public bool Passed => Escape ? OnScreenLead >= Required && Required >= RouteValidator.WindowTicks : OnScreenLead >= RevealLeadTicks;
            public override string ToString() => Escape
                ? Fit
                    ? $"{RevealedBy} @ {Aspect}: fit, escape (reveal t{Reveal}, last escape t{LastEscape}, {Required} ticks)"
                    : $"{RevealedBy} @ {Aspect}: escape, on screen {OnScreenLead} of {Required} (reveal t{Reveal}, last escape t{LastEscape}; worst {WorstFps} fps, phase {WorstPhase}, start direction {WorstStartDirection})"
                : Fit
                    ? $"{RevealedBy} @ {Aspect}: fit (lead {Lead})"
                    : $"{RevealedBy} @ {Aspect}: on screen {OnScreenLead} of lead {Lead} (reveal t{Reveal}, end t{End}; worst {WorstFps} fps, phase {WorstPhase}, start direction {WorstStartDirection})";
        }

        public static List<string> ValidateCameraTell(string levelId, SoloRoomDefinition room, RoomRoutes routes)
        {
            var errors = new List<string>();
            using var session = new RouteSession();
            CameraTell(session, levelId, room, routes, AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(LevelCameraConfigPath), errors);
            return errors;
        }

        public static List<CameraTellResult> CameraTell(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes, LevelCameraConfig camera, List<string> errors)
        {
            var results = new List<CameraTellResult>();
            if (camera == null) { errors.Add($"{levelId}: no LevelCameraConfig ({LevelCameraConfigPath}); the camera tell rule is undefined."); return results; }
            Bounds frameBounds = SoloRoomBuilder.ComputeRoomBounds(room, camera.ViewMargin);
            Vector2 frameCentre = (Vector2)frameBounds.center - room.Origin, frameSize = frameBounds.size;

            foreach (Betrayal betrayal in routes.Betrayals)
            {
                if (betrayal.Outcome != BetrayalOutcome.Dies) continue;
                ReplayResult replay = RouteHarness.Replay(session, room, betrayal.Route, new ReplayOptions { ResolveCause = false });
                LeadResult lead = RouteValidator.Lead(replay, betrayal);
                // A betrayal that doesn't die or never reveals is the route rule's error (D-079); nothing to see here.
                if (replay.Kill == null || lead.FirstVisibleTick < 0) continue;
                int end = lead.FirstVisibleTick + lead.Lead;
                if (betrayal.Escape != null) { EscapeTell(session, levelId, room, betrayal, lead.Lead, frameCentre, frameSize, camera, results, errors); continue; }
                int element = replay.Elements.IndexOf(betrayal.RevealedBy);
                for (int a = 0; a < CameraTellAspects.Length; a++)
                {
                    var result = new CameraTellResult { Betrayal = betrayal.Name, RevealedBy = betrayal.RevealedBy, Aspect = CameraTellAspectNames[a], Reveal = lead.FirstVisibleTick, End = end, Lead = lead.Lead };
                    result.Fit = CameraMath.IsFitMode(frameSize, camera.MaxViewHeight, CameraTellAspects[a]);
                    if (result.Fit) result.OnScreenLead = lead.Lead;
                    else WorstOnScreenLead(replay, element, lead.FirstVisibleTick, end, frameCentre, frameSize, CameraTellAspects[a], camera, result);
                    results.Add(result);
                    if (!result.Passed) errors.Add($"{levelId}: betrayal '{betrayal.Name}': {result} is below {RevealLeadTicks} ticks on screen (D-083 camera tell rule).");
                }
            }
            return results;
        }

        // PAX-060 (D-097): the escape-backed reveal. Escape(d) presses its way out d ticks after the reveal; d runs 0, 1, 2, ...
        // (up to the betrayal's own lead) while Escape(d) completes the level, and the last escape tick is the reveal plus the
        // last such d. In that escape's replay (the same path as a cat that hasn't pressed yet, up to the press), the reveal
        // must be on screen at every tick from the reveal through the last escape tick, a span of at least WindowTicks; the
        // worst of the 24 camera cases counts, at every aspect.
        static void EscapeTell(RouteSession session, string levelId, SoloRoomDefinition room, Betrayal betrayal, int maxDelay, Vector2 frameCentre, Vector2 frameSize,
            LevelCameraConfig camera, List<CameraTellResult> results, List<string> errors)
        {
            ReplayResult last = null;
            int reveal = -1, lastD = -1;
            for (int d = 0; d <= maxDelay; d++)
            {
                ReplayResult replay = RouteHarness.Replay(session, room, betrayal.Escape(d), new ReplayOptions { ResolveCause = false });
                int seen = replay.FirstVisibleChange(betrayal.RevealedBy);
                if (!replay.Completed || seen < 0) break;
                last = replay; reveal = seen; lastD = d;
            }
            if (last == null) { errors.Add($"{levelId}: betrayal '{betrayal.Name}': its declared escape doesn't complete the level even pressed at the reveal (D-097)."); return; }
            int lastEscape = reveal + lastD, element = last.Elements.IndexOf(betrayal.RevealedBy);
            for (int a = 0; a < CameraTellAspects.Length; a++)
            {
                var result = new CameraTellResult { Betrayal = betrayal.Name, RevealedBy = betrayal.RevealedBy, Aspect = CameraTellAspectNames[a], Escape = true,
                    Reveal = reveal, LastEscape = lastEscape, End = lastEscape + 1, Required = lastD + 1 };
                result.Fit = CameraMath.IsFitMode(frameSize, camera.MaxViewHeight, CameraTellAspects[a]);
                if (result.Fit) result.OnScreenLead = result.Required;
                else
                {
                    result.OnScreenLead = int.MaxValue;
                    foreach (int fps in CameraTellFramesPerSecond)
                    foreach (float phase in CameraTellPhases)
                    foreach (float direction in CameraTellStartDirections)
                    {
                        int onScreen = OnScreen(last, element, reveal, result.End, frameCentre, frameSize, CameraTellAspects[a], camera, fps, phase, direction, true);
                        if (onScreen >= result.OnScreenLead) continue;
                        result.OnScreenLead = onScreen; result.WorstFps = fps; result.WorstPhase = phase; result.WorstStartDirection = direction;
                    }
                }
                results.Add(result);
                if (!result.Passed)
                    errors.Add($"{levelId}: betrayal '{betrayal.Name}': {result} is not on screen from the reveal through the last escape tick, for at least {RouteValidator.WindowTicks} ticks (D-097 escape-backed reveal).");
            }
        }

        static void WorstOnScreenLead(ReplayResult replay, int element, int reveal, int end, Vector2 frameCentre, Vector2 frameSize, float aspect, LevelCameraConfig camera, CameraTellResult result)
        {
            result.OnScreenLead = int.MaxValue;
            foreach (int fps in CameraTellFramesPerSecond)
            foreach (float phase in CameraTellPhases)
            foreach (float direction in CameraTellStartDirections)
            {
                int onScreen = OnScreenLead(replay, element, reveal, end, frameCentre, frameSize, aspect, camera, fps, phase, direction);
                if (onScreen >= result.OnScreenLead) continue;
                result.OnScreenLead = onScreen; result.WorstFps = fps; result.WorstPhase = phase; result.WorstStartDirection = direction;
            }
        }

        // One camera case: the level camera's Start at tick 0, then one CameraMath.Step per rendered frame. R10: the
        // frames only drive the smoothing and the interpolated target; visibility is judged per simulation tick, against
        // the view of the latest frame rendered at or before that tick. No frame delay is added (display latency is a
        // Phase H device check).
        public static int OnScreenLead(ReplayResult replay, int element, int reveal, int end, Vector2 frameCentre, Vector2 frameSize, float aspect,
            LevelCameraConfig camera, int framesPerSecond, float phase, float startDirection) =>
            OnScreen(replay, element, reveal, end, frameCentre, frameSize, aspect, camera, framesPerSecond, phase, startDirection, false);

        // fromReveal false: D-083's on-screen lead (the run that reaches the end). True (D-097): the ticks the element stays
        // on screen from the reveal, up to the end, stopping at the first tick it's off screen.
        static int OnScreen(ReplayResult replay, int element, int reveal, int end, Vector2 frameCentre, Vector2 frameSize, float aspect,
            LevelCameraConfig camera, int framesPerSecond, float phase, float startDirection, bool fromReveal)
        {
            var p = new CameraMath.FollowParams(camera.MaxViewHeight, camera.LookAhead, camera.LookAheadFlipDistance, camera.DeadZoneHalfExtents, camera.SmoothTime, camera.MaxSpeed);
            List<TickRecord> records = replay.Records;
            // PAX-083: the loop below stops at records.Count, so a replay shorter than the lead's end would read as on
            // screen up to the end and overstate the lead. Fail instead.
            if (records.Count == 0) throw new InvalidOperationException($"OnScreenLead: the replay recorded no ticks; the camera tell rule can't measure the reveal (end t{end}).");
            if (records.Count < end) throw new InvalidOperationException($"OnScreenLead: the replay has {records.Count} ticks, short of the lead's end t{end}; the camera tell rule can't measure the reveal.");
            Vector2 start = Cat(records[0]);
            // LevelCameraFollow.Start/SnapToTarget: anchor at the cat, an immediate step, velocity zeroed.
            var state = new CameraMath.FollowState { AnchorX = start.x, LastDirection = startDirection };
            float viewHeight = CameraMath.Step(ref state, start, frameCentre, frameSize, aspect, p, true, 0f);
            state.Velocity = Vector2.zero; state.AnchorX = start.x;

            double tick = TickTime.SecondsPerTick, frame = 1.0 / framesPerSecond;
            int f = 0, runStart = -1;
            for (int k = 0; k < end && k < records.Count; k++)
            {
                // Render every frame whose latest completed tick is k or earlier.
                while (true)
                {
                    double time = (f + phase) * frame;
                    int frameTick = (int)Math.Floor(time / tick + 1e-9);
                    if (frameTick > k) break;
                    float alpha = (float)(time / tick - frameTick);
                    Vector2 drawn = frameTick == 0 ? start : Vector2.Lerp(Cat(records[frameTick - 1]), Cat(records[frameTick]), alpha);
                    viewHeight = CameraMath.Step(ref state, drawn, frameCentre, frameSize, aspect, p, false, (float)frame);
                    f++;
                }
                if (k < reveal) continue;
                var half = new Vector2(viewHeight * .5f * aspect, viewHeight * .5f);
                Rect view = Rect.MinMaxRect(state.Centre.x - half.x, state.Centre.y - half.y, state.Centre.x + half.x, state.Centre.y + half.y);
                bool visible = ChangeBounds(records, element, k, out Rect changed) && changed.Overlaps(view);
                if (fromReveal && !visible) return k - reveal;
                if (!visible) runStart = -1;
                else if (runStart < 0) runStart = k;
            }
            if (fromReveal) return end - reveal;
            return runStart < 0 ? 0 : end - runStart;
        }

        // Where the change is seen at tick k: the element's rendered bounds, or, once it has vanished (a collapse,
        // a fake platform), the bounds it was last drawn at.
        static bool ChangeBounds(List<TickRecord> records, int element, int k, out Rect bounds)
        {
            for (int t = k; t >= 0; t--)
                if (records[t].Rendered[element]) { bounds = records[t].RenderBounds[element]; return true; }
            bounds = default;
            return false;
        }

        static Vector2 Cat(TickRecord r) => new(r.CatX, r.CatY);
    }
}
