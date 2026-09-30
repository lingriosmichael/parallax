using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // PAX-V07 item 1: the capture harness's visual-only parity run (CatVisualOnlyParityTests) and the presenter thresholds
    // the state rules read. Split from CatCapture.cs to keep it under 400 lines.
    public static partial class CatCapture
    {
        /// <summary>PAX-V07 §3, §6 (CatVisualOnlyParityTests): the scenario's ticks alone (the presenter never stepped)
        /// against the same ticks with 60 fps presentation frames between them (the interpolated pose, Present(1/60), the
        /// pose restored before the next tick), no rendering. Returns null when the body's position and velocity match on
        /// every tick, else the first difference. `statesSeen` lists the presenter states shown, comma-separated.</summary>
        public static string VisualOnlyParity(string scenarioName, out int ticks, out string statesSeen)
        {
            CaptureScenario scenario = CatCaptureScenarios.All.FirstOrDefault(s => s.Name == scenarioName)
                ?? throw new ArgumentException($"CatCapture: no scenario '{scenarioName}'.");
            var seen = new HashSet<string>();
            var log = new List<string>();
            List<string> reference;
            using (var session = new RouteSession())
            {
                List<CaptureStep> steps = scenario.Steps(session);
                reference = Headless(session, scenario, steps);
                session.Clear();
                CatCaptureRig rig = CatCaptureRig.Build(scenario.Room(session), scenario.StartX);
                try
                {
                    var runner = new Runner(scenario.Steps(session));
                    Transform root = rig.Cat.transform;
                    float tickLength = TickTime.SecondsPerTick;
                    Vector2 prevPos = rig.Body.position, curPos = prevPos; float prevRot = rig.Body.rotation, curRot = prevRot;
                    Vector3 tickPosition = root.position; Quaternion tickRotation = root.rotation;
                    for (int n = 0; ; n++)
                    {
                        double loopTime = n / (double)FrameRate + tickLength;
                        int due = CatCaptureMath.TicksDueBy(loopTime, tickLength);
                        bool finished = false;
                        while (rig.Tick < due)
                        {
                            root.SetPositionAndRotation(tickPosition, tickRotation);
                            Physics2D.SyncTransforms();
                            if (!runner.Next(rig, out CatCommand command)) { finished = true; break; }
                            rig.Step(command);
                            log.Add(TickState(rig));
                            prevPos = curPos; prevRot = curRot;
                            curPos = rig.Body.position; curRot = rig.Body.rotation;
                            tickPosition = root.position; tickRotation = root.rotation;
                        }
                        if (finished) break;
                        float a = CatCaptureMath.InterpolationAlpha(loopTime, rig.Tick, tickLength);
                        CatCaptureMath.InterpolatedPose(prevPos, prevRot, curPos, curRot, a, InterpolationTeleportUnits, out Vector2 pos, out float rot);
                        root.SetPositionAndRotation(new Vector3(pos.x, pos.y, tickPosition.z), Quaternion.Euler(0f, 0f, rot));
                        rig.Presenter.Present(1f / FrameRate);
                        seen.Add(rig.Presenter.State.ToString());
                    }
                    root.SetPositionAndRotation(tickPosition, tickRotation);
                }
                finally
                {
                    session.Clear();
                }
            }
            ticks = log.Count;
            statesSeen = string.Join(",", seen.OrderBy(x => x));
            for (int i = 0; i < Mathf.Max(log.Count, reference.Count); i++)
            {
                string got = i < log.Count ? log[i] : "(none)", want = i < reference.Count ? reference[i] : "(none)";
                if (got != want) return $"tick {i + 1}: presenter stepped {got} vs never stepped {want}";
            }
            return null;
        }

        static bool GroundState(string state) => state == "Idle" || state == "Walk" || state == "Run" || state == "Turn";

        // The presenter's thresholds, from its config and its Turn clip (serialized data; the presenter's fields are private).
        static CaptureSpeeds Speeds(CatCaptureRig rig)
        {
            var so = new SerializedObject(rig.Presenter);
            var config = so.FindProperty("config").objectReferenceValue as CatVisualConfig;
            var speeds = new CaptureSpeeds
            {
                WalkEnter = config != null ? config.WalkEnter : 0f,
                RunEnter = config != null ? config.RunEnterSpeed : float.PositiveInfinity,
                RunExit = config != null ? config.RunExitSpeed : float.PositiveInfinity,
            };
            SerializedProperty clips = so.FindProperty("clips.clips");
            for (int i = 0; clips != null && i < clips.arraySize; i++)
            {
                SerializedProperty c = clips.GetArrayElementAtIndex(i);
                float fps = c.FindPropertyRelative("fps").floatValue;
                if (c.FindPropertyRelative("slot").stringValue == "Turn" && fps > 0f) speeds.TurnSeconds = c.FindPropertyRelative("frames").arraySize / fps;
            }
            return speeds;
        }

    }
}
