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
            public bool Passed => OnScreenLead >= RevealLeadTicks;
            public override string ToString() => Fit
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
            LevelCameraConfig camera, int framesPerSecond, float phase, float startDirection)
        {
            var p = new CameraMath.FollowParams(camera.MaxViewHeight, camera.LookAhead, camera.LookAheadFlipDistance, camera.DeadZoneHalfExtents, camera.SmoothTime, camera.MaxSpeed);
            List<TickRecord> records = replay.Records;
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
                if (!visible) runStart = -1;
                else if (runStart < 0) runStart = k;
            }
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
