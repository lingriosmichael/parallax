using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0: one contact sheet's cut, around the frame where something changes. `Kind` is
    /// "From-to-To" for a presenter state change, "takeoff" (grounded to airborne) or "landing" (airborne to grounded);
    /// `Strip` marks the kinds that also get a zoomed paw strip. Start/End are frame indices, inclusive.</summary>
    public sealed class CaptureTransition
    {
        public int Frame, Start, End;
        public string Kind, From, To;
        public bool Strip;
    }

    /// <summary>PAX-V07 gauntlet item 0 (§11 R8, R14): the capture harness's pure math. No Unity objects, no state.</summary>
    public static class CatCaptureMath
    {
        // A frame at exactly a tick's end runs that tick first (the player loop's FixedUpdate catch-up); the epsilon only
        // absorbs the rounding of n/60 against k/50.
        const double TickEdgeEpsilon = 1e-9;

        /// <summary>How many ticks the player loop has run by the frame at `frameTime` seconds: every tick whose end is at
        /// or before it.</summary>
        public static int TicksDueBy(double frameTime, double tickLength) =>
            tickLength <= 0.0 ? 0 : (int)System.Math.Floor(frameTime / tickLength + TickEdgeEpsilon);

        /// <summary>Rigidbody2D interpolation's blend for a frame at `frameTime`, after tick `lastTick` (which ended at
        /// lastTick × tickLength): the fraction of a tick since then, clamped to 0..1.</summary>
        public static float InterpolationAlpha(double frameTime, int lastTick, double tickLength)
        {
            if (tickLength <= 0.0) return 1f;
            double a = (frameTime - lastTick * tickLength) / tickLength;
            return (float)(a < 0.0 ? 0.0 : a > 1.0 ? 1.0 : a);
        }

        /// <summary>The pose interpolation shows between the previous and the current tick's body pose (position lerped,
        /// rotation by the shorter way round). A move of `teleportDistance` or more between the two ticks is a teleport
        /// (a respawn): the current pose is shown at once.</summary>
        public static void InterpolatedPose(Vector2 prevPos, float prevRot, Vector2 curPos, float curRot, float alpha,
            float teleportDistance, out Vector2 pos, out float rot)
        {
            if ((curPos - prevPos).magnitude >= teleportDistance) { pos = curPos; rot = curRot; return; }
            pos = Vector2.LerpUnclamped(prevPos, curPos, alpha);
            rot = Mathf.LerpAngle(prevRot, curRot, alpha);
        }

        /// <summary>The world position of the centre of pixel (px, py) of a sprite, in pixels from the sprite rect's
        /// bottom-left, given its pivot in the same pixels, its pixels per unit, and the renderer's localToWorldMatrix.</summary>
        public static Vector2 PixelToWorld(int px, int py, Vector2 pivotPx, float ppu, Matrix4x4 visualToWorld)
        {
            var local = new Vector3((px + 0.5f - pivotPx.x) / ppu, (py + 0.5f - pivotPx.y) / ppu, 0f);
            return visualToWorld.MultiplyPoint3x4(local);
        }

        /// <summary>How far `point` is past `surface` along gravity (`down`, unit): positive inside the solid, negative
        /// above it (a gap).</summary>
        public static float DepthAlongGravity(Vector2 point, Vector2 surface, Vector2 down) => Vector2.Dot(point - surface, down);

        /// <summary>Groups sorted positions along the surface into clusters wherever consecutive values are more than
        /// `gap` apart. Each cluster is (mean position, count).</summary>
        public static Vector2[] Clusters(float[] sortedAlong, float gap)
        {
            var result = new List<Vector2>();
            if (sortedAlong == null || sortedAlong.Length == 0) return result.ToArray();
            float sum = sortedAlong[0];
            int count = 1;
            for (int i = 1; i < sortedAlong.Length; i++)
            {
                if (sortedAlong[i] - sortedAlong[i - 1] > gap)
                {
                    result.Add(new Vector2(sum / count, count));
                    sum = 0f; count = 0;
                }
                sum += sortedAlong[i];
                count++;
            }
            result.Add(new Vector2(sum / count, count));
            return result.ToArray();
        }

        /// <summary>Like <see cref="Clusters"/>, with each cluster's extent: (first position, last position, mean).</summary>
        public static Vector3[] ClusterRanges(float[] sortedAlong, float gap)
        {
            var result = new List<Vector3>();
            if (sortedAlong == null || sortedAlong.Length == 0) return result.ToArray();
            int start = 0;
            float sum = sortedAlong[0];
            for (int i = 1; i <= sortedAlong.Length; i++)
            {
                if (i < sortedAlong.Length && sortedAlong[i] - sortedAlong[i - 1] <= gap) { sum += sortedAlong[i]; continue; }
                result.Add(new Vector3(sortedAlong[start], sortedAlong[i - 1], sum / (i - start)));
                if (i < sortedAlong.Length) { start = i; sum = sortedAlong[i]; }
            }
            return result.ToArray();
        }

        /// <summary>PAX-V07 item 1: which new contact cluster continues each planted paw. A planted paw is the same paw only
        /// where its contact pixels still overlap the ground it covered at the previous sample (ranges are (min, max, mean)
        /// along the surface); among overlapping clusters the nearest centre wins, one to one. -1 = the paw lifted. A paw
        /// planting just ahead of one lifting (a cat's hind paw landing by its fore paw) is a new plant, not a slide.</summary>
        public static int[] MatchPlanted(Vector3[] previous, Vector3[] next)
        {
            var match = new int[previous.Length];
            var used = new bool[next.Length];
            for (int p = 0; p < previous.Length; p++)
            {
                match[p] = -1;
                float best = float.PositiveInfinity;
                for (int k = 0; k < next.Length; k++)
                {
                    if (used[k] || next[k].x > previous[p].y || next[k].y < previous[p].x) continue;
                    float d = Mathf.Abs(next[k].z - previous[p].z);
                    if (d < best) { best = d; match[p] = k; }
                }
                if (match[p] >= 0) used[match[p]] = true;
            }
            return match;
        }

        /// <summary>The contact sheets' cuts: one per presenter state change and per takeoff / landing (by the motor's
        /// grounded flag), each `before` frames before to `after` frames after, clamped to the capture, in frame order
        /// (at one frame, the takeoff or landing first). At most `maxPerKind` of each kind, the earliest.</summary>
        public static CaptureTransition[] SelectTransitions(string[] states, bool[] grounded, int before, int after, int maxPerKind)
        {
            var result = new List<CaptureTransition>();
            var perKind = new Dictionary<string, int>();
            int last = states.Length - 1;
            void Add(int frame, string kind, string from, string to, bool strip)
            {
                perKind.TryGetValue(kind, out int n);
                if (n >= maxPerKind) return;
                perKind[kind] = n + 1;
                result.Add(new CaptureTransition
                {
                    Frame = frame, Kind = kind, From = from, To = to, Strip = strip,
                    Start = Mathf.Max(0, frame - before), End = Mathf.Min(last, frame + after),
                });
            }
            for (int i = 1; i < states.Length; i++)
            {
                if (grounded != null && grounded[i - 1] != grounded[i])
                    Add(i, grounded[i] ? "landing" : "takeoff", grounded[i - 1] ? "grounded" : "air", grounded[i] ? "grounded" : "air", true);
                if (states[i] != states[i - 1]) Add(i, states[i - 1] + "-to-" + states[i], states[i - 1], states[i], false);
            }
            return result.ToArray();
        }

        /// <summary>Cuts where the scenario's input changes on a step marked for it (e.g. walk to run, run to stop): the first
        /// frame whose step label is one of `cutLabels`, each time that step starts. Kind "step: label". No strip.</summary>
        public static CaptureTransition[] SelectStepCuts(string[] stepPerFrame, string[] cutLabels, int before, int after)
        {
            var result = new List<CaptureTransition>();
            var cuts = new HashSet<string>(cutLabels);
            int last = stepPerFrame.Length - 1;
            for (int i = 1; i < stepPerFrame.Length; i++)
            {
                if (stepPerFrame[i] == stepPerFrame[i - 1] || !cuts.Contains(stepPerFrame[i])) continue;
                result.Add(new CaptureTransition
                {
                    Frame = i, Kind = "step: " + stepPerFrame[i], From = stepPerFrame[i - 1], To = stepPerFrame[i],
                    Start = Mathf.Max(0, i - before), End = Mathf.Min(last, i + after),
                });
            }
            return result.ToArray();
        }

        /// <summary>Cuts for moments the presenter state doesn't name: "turn" (the facing flips while grounded), "walk-off"
        /// (grounded to airborne with no jump in the last `jumpLookback` frames), "death" (a death hold starts; the cut runs
        /// `deathAfter` frames so the whole hold is on it) and "respawn" (the hold ends). At most `maxPerKind` of each.
        /// Walk-offs get a paw strip.</summary>
        public static CaptureTransition[] SelectEventCuts(int[] facing, bool[] grounded, bool[] jumped, bool[] holding,
            int before, int after, int deathAfter, int jumpLookback, int maxPerKind)
        {
            var result = new List<CaptureTransition>();
            var perKind = new Dictionary<string, int>();
            int last = facing.Length - 1;
            void Add(int frame, string kind, int span, bool strip)
            {
                perKind.TryGetValue(kind, out int n);
                if (n >= maxPerKind) return;
                perKind[kind] = n + 1;
                result.Add(new CaptureTransition { Frame = frame, Kind = kind, From = kind, To = kind, Strip = strip,
                    Start = Mathf.Max(0, frame - before), End = Mathf.Min(last, frame + span) });
            }
            for (int i = 1; i < facing.Length; i++)
            {
                if (holding[i] && !holding[i - 1]) { Add(i, "death", deathAfter, false); continue; }
                if (!holding[i] && holding[i - 1]) { Add(i, "respawn", after, false); continue; }
                if (holding[i]) continue;
                if (facing[i] != facing[i - 1] && grounded[i]) Add(i, "turn", after, false);
                if (grounded[i - 1] && !grounded[i])
                {
                    bool jump = false;
                    for (int k = Mathf.Max(0, i - jumpLookback); k <= i; k++) jump |= jumped[k];
                    if (!jump) Add(i, "walk-off", after, true);
                }
            }
            return result.ToArray();
        }
    }
}