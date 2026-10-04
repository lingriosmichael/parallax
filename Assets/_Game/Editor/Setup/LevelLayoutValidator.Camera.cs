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
    // PAX-076 (D-083, KIT-4), amended 2026-10-03: the camera rule. Surprise is allowed: a trap no longer has to be on
    // screen before it fires. Every dying betrayal's killer, in its lethal pose, must be on screen at some point from the
    // kill tick (ReplayResult.Kill: the tick whose room step killed) until the death hold ends (RoomSafetyConfig.HoldTicks),
    // so every death is readable. During the hold the room is frozen as it killed (D-058) and the camera keeps easing
    // toward the frozen cat (D-058 amendment). The killer is the trap the replay attributes the kill to. The level camera
    // is CameraMath.Step, the game's own step, run over the replay's recorded cat positions (no extra replays per camera
    // case):
    // - frames at 30 and 60 fps, each at 4 phases of a frame, and a starting look direction of -1, 0 and +1 (a
    //   respawn snap keeps the last attempt's direction; Start begins at 0);
    // - the drawn cat is Rigidbody2D interpolation's: between the previous tick's pose and this tick's, so up to
    //   one tick behind the physics (R4);
    // - the frames only drive the smoothing and the interpolated target (R10): the killer is on screen at a tick when its
    //   rendered bounds at the kill tick (an angled arrow: its turned corners; a trap that has vanished: the bounds it was
    //   last drawn at) overlap the view of the latest frame rendered at or before that tick.
    // The worst of the 24 cases counts, at 4:3, 16:9 and 20:9. A room in fit mode at an aspect passes at that aspect.
    // PAX-060 (D-097, opt-in, unchanged): a betrayal that declares an escape (Betrayal.Escape) is also checked against its
    // last escape tick (EscapeTell).
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
            public string Betrayal, RevealedBy, Killer, Aspect;
            public bool Fit;
            public int Reveal = -1, End = -1, Lead = -1;      // RouteValidator.Lead's ticks (for the table)
            public int KillTick = -1;
            public bool KillerOnScreen;                       // worst case: on screen by the hold's end in all 24 cases
            public int WorstFps; public float WorstPhase, WorstStartDirection;
            // D-097: an escape-backed row. Reveal and LastEscape come from the escape's own replay; Required is the span
            // reveal..last escape (inclusive), which must be on screen throughout and at least RouteValidator.WindowTicks.
            public bool Escape;
            public int LastEscape = -1, Required = RevealLeadTicks, OnScreenLead = -1;
            public bool Passed => Escape ? OnScreenLead >= Required && Required >= RouteValidator.WindowTicks : KillerOnScreen;
            public override string ToString() => Escape
                ? Fit
                    ? $"{RevealedBy} @ {Aspect}: fit, escape (reveal t{Reveal}, last escape t{LastEscape}, {Required} ticks)"
                    : $"{RevealedBy} @ {Aspect}: escape, on screen {OnScreenLead} of {Required} (reveal t{Reveal}, last escape t{LastEscape}; worst {WorstFps} fps, phase {WorstPhase}, start direction {WorstStartDirection})"
                : Fit
                    ? $"{Killer} @ {Aspect}: fit (kill t{KillTick})"
                    : KillerOnScreen
                        ? $"{Killer} @ {Aspect}: on screen by the hold's end (kill t{KillTick})"
                        : $"{Killer} @ {Aspect}: off screen from the kill (t{KillTick}) to the hold's end (worst {WorstFps} fps, phase {WorstPhase}, start direction {WorstStartDirection})";
        }

        public static List<string> ValidateCameraTell(string levelId, SoloRoomDefinition room, RoomRoutes routes)
        {
            var errors = new List<string>();
            using var session = new RouteSession();
            CameraTell(session, levelId, room, routes, AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(LevelCameraConfigPath), errors);
            return errors;
        }

        /// <summary>D-104: the level's own camera (LevelCameras: its view height and lift), as the level build bakes it.</summary>
        public static List<CameraTellResult> CameraTell(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes, LevelCameraConfig camera, List<string> errors) =>
            CameraTellWith(session, levelId, room, routes, camera, Parallax.Editor.Levels.LevelCameras.ViewHeight(levelId, camera), Parallax.Editor.Levels.LevelCameras.Bias(levelId), errors);

        static CameraMath.FollowParams Follow(LevelCameraConfig camera, float viewHeight, float bias) =>
            new(viewHeight, camera.LookAhead, camera.LookAheadFlipDistance, camera.DeadZoneHalfExtents, camera.SmoothTime, camera.MaxSpeed, bias);

        /// <summary>D-104: with an explicit view height and lift (the measurement of a candidate camera).</summary>
        public static List<CameraTellResult> CameraTellWith(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes, LevelCameraConfig camera, float viewHeight, float bias, List<string> errors)
        {
            var results = new List<CameraTellResult>();
            if (camera == null) { errors.Add($"{levelId}: no LevelCameraConfig ({LevelCameraConfigPath}); the camera tell rule is undefined."); return results; }
            CameraMath.FollowParams p = Follow(camera, viewHeight, bias);
            int hold = HoldTicks();
            Bounds frameBounds = SoloRoomBuilder.ComputeRoomBounds(room, camera.ViewMargin);
            Vector2 frameCentre = (Vector2)frameBounds.center - room.Origin, frameSize = frameBounds.size;

            foreach (Betrayal betrayal in routes.Betrayals)
            {
                if (betrayal.Outcome != BetrayalOutcome.Dies) continue;
                ReplayResult replay = RouteHarness.Replay(session, room, betrayal.Route, new ReplayOptions { ResolveCause = false });
                // A betrayal that doesn't die is the route rule's error (D-079); nothing to see here.
                if (replay.Kill == null) continue;
                LeadResult lead = RouteValidator.Lead(replay, betrayal);
                string killer = replay.Kill.Killer ?? betrayal.Killer;
                int element = replay.Elements.IndexOf(killer), kill = replay.Kill.Tick;
                if (element < 0) { errors.Add($"{levelId}: betrayal '{betrayal.Name}': its killer {killer ?? "(none)"} is not a drawn element, so the camera rule can't see it (D-083 amendment)."); continue; }
                for (int a = 0; a < CameraTellAspects.Length; a++)
                {
                    var result = new CameraTellResult
                    {
                        Betrayal = betrayal.Name, RevealedBy = betrayal.RevealedBy, Killer = killer, Aspect = CameraTellAspectNames[a],
                        Reveal = lead.FirstVisibleTick, Lead = lead.Lead, End = lead.FirstVisibleTick >= 0 ? lead.FirstVisibleTick + lead.Lead : -1, KillTick = kill,
                    };
                    result.Fit = CameraMath.IsFitMode(frameSize, p.MaxViewHeight, CameraTellAspects[a]);
                    result.KillerOnScreen = result.Fit || WorstKillerOnScreen(replay, element, kill, hold, frameCentre, frameSize, CameraTellAspects[a], p, result);
                    results.Add(result);
                    if (!result.Passed) errors.Add($"{levelId}: betrayal '{betrayal.Name}': {result}: the killer must be on screen before the death hold ends (D-083 amendment).");
                }
                if (betrayal.Escape != null && lead.FirstVisibleTick >= 0) EscapeTell(session, levelId, room, betrayal, lead.Lead, frameCentre, frameSize, p, results, errors);
            }
            return results;
        }

        // PAX-060 (D-097): the escape-backed reveal. Escape(d) presses its way out d ticks after the reveal; d runs 0, 1, 2, ...
        // (up to the betrayal's own lead) while Escape(d) completes the level, and the last escape tick is the reveal plus the
        // last such d. In that escape's replay (the same path as a cat that hasn't pressed yet, up to the press), the reveal
        // must be on screen at every tick from the reveal through the last escape tick, a span of at least WindowTicks; the
        // worst of the 24 camera cases counts, at every aspect.
        static void EscapeTell(RouteSession session, string levelId, SoloRoomDefinition room, Betrayal betrayal, int maxDelay, Vector2 frameCentre, Vector2 frameSize,
            CameraMath.FollowParams camera, List<CameraTellResult> results, List<string> errors)
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
                        int onScreen = OnScreen(last, element, reveal, result.End, frameCentre, frameSize, CameraTellAspects[a], camera, fps, phase, direction);
                        if (onScreen >= result.OnScreenLead) continue;
                        result.OnScreenLead = onScreen; result.WorstFps = fps; result.WorstPhase = phase; result.WorstStartDirection = direction;
                    }
                }
                results.Add(result);
                if (!result.Passed)
                    errors.Add($"{levelId}: betrayal '{betrayal.Name}': {result} is not on screen from the reveal through the last escape tick, for at least {RouteValidator.WindowTicks} ticks (D-097 escape-backed reveal).");
            }
        }

        // The death hold's length (RoomSafetyConfig.HoldTicks; RoomDeath's default 30 when the asset is missing).
        public const string RoomSafetyConfigPath = "Assets/_Game/Data/RoomSafetyConfig.asset";
        static int HoldTicks() => AssetDatabase.LoadAssetAtPath<Parallax.Gameplay.Rooms.RoomSafetyConfig>(RoomSafetyConfigPath) is { } c ? c.HoldTicks : 30;

        // True when the killer is on screen before the hold ends in every camera case; otherwise the first case that never
        // shows it.
        static bool WorstKillerOnScreen(ReplayResult replay, int element, int kill, int hold, Vector2 frameCentre, Vector2 frameSize, float aspect, CameraMath.FollowParams camera, CameraTellResult result)
        {
            foreach (int fps in CameraTellFramesPerSecond)
            foreach (float phase in CameraTellPhases)
            foreach (float direction in CameraTellStartDirections)
            {
                if (KillerOnScreen(replay, element, kill, hold, frameCentre, frameSize, aspect, camera, fps, phase, direction)) continue;
                result.WorstFps = fps; result.WorstPhase = phase; result.WorstStartDirection = direction;
                return false;
            }
            return true;
        }

        // One camera case: the level camera's Start at tick 0, then one CameraMath.Step per rendered frame, through the
        // kill and on through the hold with the cat frozen where it died. R10: the frames only drive the smoothing and the
        // interpolated target; the killer, frozen in its pose at the kill tick, is judged at each tick from the kill to the
        // hold's last, against the view of the latest frame rendered at or before that tick. No frame delay is added.
        public static bool KillerOnScreen(ReplayResult replay, int element, int kill, int holdTicks, Vector2 frameCentre, Vector2 frameSize, float aspect,
            LevelCameraConfig camera, int framesPerSecond, float phase, float startDirection) =>
            KillerOnScreen(replay, element, kill, holdTicks, frameCentre, frameSize, aspect, Follow(camera, camera.MaxViewHeight, 0f), framesPerSecond, phase, startDirection);

        static bool KillerOnScreen(ReplayResult replay, int element, int kill, int holdTicks, Vector2 frameCentre, Vector2 frameSize, float aspect,
            CameraMath.FollowParams p, int framesPerSecond, float phase, float startDirection)
        {
            List<TickRecord> records = replay.Records;
            // PAX-083: a replay that stops before the kill can't show where the killer was; fail instead of guessing.
            if (records.Count == 0) throw new InvalidOperationException($"KillerOnScreen: the replay recorded no ticks; the camera rule can't see the kill (t{kill}).");
            if (records.Count <= kill) throw new InvalidOperationException($"KillerOnScreen: the replay has {records.Count} ticks, short of the kill at t{kill}; the camera rule can't see it.");
            int last = kill + Math.Max(0, holdTicks - 1);
            Rect[] views = ViewsThroughHold(records, kill, last, frameCentre, frameSize, aspect, p, framesPerSecond, phase, startDirection);
            for (int t = kill; t <= last; t++)
                if (ChangeVisible(records, element, kill, views[t])) return true;
            return false;
        }

        // The camera rule's loop (as ViewPerRecord), on past the kill to tick `last` with the cat frozen at its kill pose.
        static Rect[] ViewsThroughHold(List<TickRecord> records, int kill, int last, Vector2 frameCentre, Vector2 frameSize, float aspect,
            CameraMath.FollowParams p, int framesPerSecond, float phase, float startDirection)
        {
            Vector2 CatAt(int t) { TickRecord r = records[Math.Min(t, kill)]; return new Vector2(r.CatX, r.CatY); }
            var views = new Rect[last + 1];
            Vector2 start = CatAt(0);
            var state = new CameraMath.FollowState { AnchorX = start.x, LastDirection = startDirection };
            float viewHeight = CameraMath.Step(ref state, start, frameCentre, frameSize, aspect, p, true, 0f);
            state.Velocity = Vector2.zero; state.AnchorX = start.x;
            double tick = TickTime.SecondsPerTick, frame = 1.0 / framesPerSecond;
            int f = 0;
            for (int k = 0; k <= last; k++)
            {
                while (true)
                {
                    double time = (f + phase) * frame;
                    int frameTick = (int)Math.Floor(time / tick + 1e-9);
                    if (frameTick > k) break;
                    float alpha = (float)(time / tick - frameTick);
                    Vector2 drawn = frameTick == 0 ? start : Vector2.Lerp(CatAt(frameTick - 1), CatAt(frameTick), alpha);
                    viewHeight = CameraMath.Step(ref state, drawn, frameCentre, frameSize, aspect, p, false, (float)frame);
                    f++;
                }
                var half = new Vector2(viewHeight * .5f * aspect, viewHeight * .5f);
                views[k] = Rect.MinMaxRect(state.Centre.x - half.x, state.Centre.y - half.y, state.Centre.x + half.x, state.Centre.y + half.y);
            }
            return views;
        }

        // D-097: the ticks the element stays on screen from the reveal, up to the end, stopping at the first tick it's off
        // screen.
        static int OnScreen(ReplayResult replay, int element, int reveal, int end, Vector2 frameCentre, Vector2 frameSize, float aspect,
            CameraMath.FollowParams p, int framesPerSecond, float phase, float startDirection)
        {
            List<TickRecord> records = replay.Records;
            if (records.Count == 0) throw new InvalidOperationException($"OnScreen: the replay recorded no ticks; the escape-backed reveal can't be measured (end t{end}).");
            if (records.Count < end) throw new InvalidOperationException($"OnScreen: the replay has {records.Count} ticks, short of the end t{end}; the escape-backed reveal can't be measured.");
            Rect[] views = ViewPerRecord(replay, frameCentre, frameSize, aspect, p, framesPerSecond, phase, startDirection);
            for (int k = reveal; k < end; k++)
                if (!ChangeVisible(records, element, k, views[k])) return k - reveal;
            return end - reveal;
        }

        // Where the change is seen at tick k: the element's rendered bounds, or, once it has vanished (a collapse,
        // a fake platform), the bounds it was last drawn at.
        // PAX-099 (D-106): an angled arrow is seen when a corner of its launcher or its turned arrow is in view (the ruling:
        // the turned box's corners, not the sprite's axis-aligned box); every other element by its rendered bounds.
        static bool ChangeVisible(List<TickRecord> records, int element, int k, Rect view)
        {
            for (int t = k; t >= 0; t--)
            {
                if (!records[t].Rendered[element]) continue;
                Vector2[] corners = records[t].TurnedCorners?[element];
                if (corners == null) return records[t].RenderBounds[element].Overlaps(view);
                foreach (Vector2 c in corners) if (view.Contains(c)) return true;
                return false;
            }
            return false;
        }

        static bool ChangeBounds(List<TickRecord> records, int element, int k, out Rect bounds)
        {
            for (int t = k; t >= 0; t--)
                if (records[t].Rendered[element]) { bounds = records[t].RenderBounds[element]; return true; }
            bounds = default;
            return false;
        }
    }
}
