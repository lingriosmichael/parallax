using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 item 3: the climb measurements. A climb sprite's body centre is the centroid of its drawn pixels after
    /// an erosion that removes the thin tail, legs and ears (A08's `body_centre`: a square of radius 7 source px, 5 px at the
    /// 192 px base); A08 registered the climb sheets so that it lands on the collider's centre with the Visual at rest.</summary>
    static class CatClimbMeasure
    {
        static readonly Dictionary<SpriteAlphaCache.Entry, Vector2> centres = new();

        /// <summary>The body centre in the sprite's local space (units from the pivot, facing +1).</summary>
        public static Vector2 BodyCentreLocal(SpriteAlphaCache.Entry e)
        {
            if (centres.TryGetValue(e, out Vector2 c)) return c;
            int w = 0, h = 0;
            for (int i = 0; i < e.Columns.Length; i++) { w = Mathf.Max(w, e.ColumnX[i] + 1); foreach (int y in e.Columns[i]) h = Mathf.Max(h, y + 1); }
            var mask = new bool[w, h];
            for (int i = 0; i < e.Columns.Length; i++) foreach (int y in e.Columns[i]) mask[e.ColumnX[i], y] = true;
            int r = CaptureThresholds.BodyErosionSpritePx;
            double sx = 0, sy = 0; int n = 0;
            for (int x = r; x < w - r; x++)
                for (int y = r; y < h - r; y++)
                {
                    bool keep = true;
                    for (int dx = -r; dx <= r && keep; dx++) for (int dy = -r; dy <= r && keep; dy++) keep = mask[x + dx, y + dy];
                    if (!keep) continue;
                    sx += x + .5; sy += y + .5; n++;
                }
            c = n == 0 ? Vector2.zero : new Vector2(((float)(sx / n) - e.PivotPx.x) / e.Ppu, ((float)(sy / n) - e.PivotPx.y) / e.Ppu);
            centres[e] = c;
            return c;
        }

        /// <summary>How far (sprite px) the drawn sprite's body centre lies from where the collider's centre puts it: the
        /// collider centre plus the Visual's own offset from its rest pose (the lift that keeps a climbing cat's paws out of the
        /// ground at a vine's foot, reported apart). `lift` returns that offset (units, world).</summary>
        public static float BodyOffsetSpritePx(SpriteAlphaCache cache, SpriteRenderer body, Transform root, Vector3 restLocal, Vector2 colliderCentre, out Vector2 lift)
        {
            lift = (Vector2)(body.transform.position - root.TransformPoint(restLocal));
            if (body.sprite == null) return 0f;
            SpriteAlphaCache.Entry e = cache.Get(body.sprite);
            Vector2 centre = body.transform.localToWorldMatrix.MultiplyPoint3x4(BodyCentreLocal(e));
            return (centre - (colliderCentre + lift)).magnitude * e.Ppu;
        }

        public static bool VineClip(string clip) => clip == "Climb" || clip == "Hang";
    }

    public static partial class CatCapture
    {
        /// <summary>PAX-V07 item 3 (ClimbPoseTests): plays a climb scenario (no rendering) and measures, on every frame the
        /// presenter shows a Climb or Hang frame: the Visual's rotation from its rest pose (degrees), its offset from its rest
        /// position while the cat is clear of the ground (more than `ClearOfGroundUnits` below it, units), the body centre's
        /// distance from the collider centre (sprite px, the ground lift taken out), and how deep any drawn pixel goes into a
        /// solid along gravity (sprite px). `screenFacing` is the facing on the vine (+1 screen-right). Null when it ran.</summary>
        public static string ClimbPlacement(string scenarioName, out int vineFrames, out float maxRotationDeg, out float maxRestOffset,
            out float maxBodyOffsetSp, out float maxDepthSp, out int screenFacing)
        {
            vineFrames = 0; maxRotationDeg = maxRestOffset = maxBodyOffsetSp = 0f; maxDepthSp = float.NegativeInfinity; screenFacing = 0;
            CaptureScenario scenario = CatCaptureScenarios.All.FirstOrDefault(s => s.Name == scenarioName);
            if (scenario == null) return $"no scenario '{scenarioName}'";
            using var session = new RouteSession();
            using var alpha = new SpriteAlphaCache();
            List<CaptureStep> steps = scenario.Steps(session);
            session.Clear();
            CatCaptureRig rig = CatCaptureRig.Build(scenario.Room(session), scenario.StartX);
            try
            {
                Transform visual = rig.Presenter.transform, root = rig.Cat.transform;
                Vector3 restLocal = visual.localPosition; Quaternion restRotation = visual.localRotation;
                var runner = new Runner(steps);
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = rig.Root.PhysicsMask, useTriggers = false };
                var hits = new RaycastHit2D[8];
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
                        prevPos = curPos; prevRot = curRot;
                        curPos = rig.Body.position; curRot = rig.Body.rotation;
                        tickPosition = root.position; tickRotation = root.rotation;
                    }
                    if (finished) break;
                    float a = CatCaptureMath.InterpolationAlpha(loopTime, rig.Tick, tickLength);
                    CatCaptureMath.InterpolatedPose(prevPos, prevRot, curPos, curRot, a, InterpolationTeleportUnits, out Vector2 pos, out float rot);
                    root.SetPositionAndRotation(new Vector3(pos.x, pos.y, tickPosition.z), Quaternion.Euler(0f, 0f, rot));
                    rig.Presenter.Present(1f / FrameRate);
                    if (!rig.Cat.IsClimbing || !CatClimbMeasure.VineClip(rig.Presenter.ClipName)) continue;
                    Vector2 down = rig.Gravity.Direction;
                    Vector2 centre = root.TransformPoint(rig.CatCollider.offset);
                    vineFrames++;
                    if (screenFacing == 0) screenFacing = rig.Presenter.Facing * (down.y < 0f ? 1 : -1);
                    maxRotationDeg = Mathf.Max(maxRotationDeg, Quaternion.Angle(visual.localRotation, restRotation));
                    maxBodyOffsetSp = Mathf.Max(maxBodyOffsetSp, CatClimbMeasure.BodyOffsetSpritePx(alpha, rig.BodyRenderer, root, restLocal, centre, out _));
                    int count = Physics2D.Raycast(centre, down, filter, hits, ClearOfGroundUnits);
                    bool clear = true;
                    for (int i = 0; i < count; i++) if (hits[i].collider != rig.CatCollider && !hits[i].collider.isTrigger) clear = false;
                    if (clear) maxRestOffset = Mathf.Max(maxRestOffset, (visual.localPosition - restLocal).magnitude);
                    FrameMeasure m = CatCaptureMeasure.Measure(alpha, rig.BodyRenderer, down, centre, filter, rig.CatCollider);
                    if (m.SurfaceFound) maxDepthSp = Mathf.Max(maxDepthSp, m.PenetrationSpritePx);
                }
                root.SetPositionAndRotation(tickPosition, tickRotation);
            }
            finally
            {
                session.Clear();
            }
            return null;
        }

        /// <summary>A climbing cat whose collider centre is further than this from any solid along gravity is clear of the
        /// ground: its Visual must be at its rest pose (the ground lift only acts nearer than 2 x the pose's reach below).</summary>
        const float ClearOfGroundUnits = 1.3f;
    }
}
