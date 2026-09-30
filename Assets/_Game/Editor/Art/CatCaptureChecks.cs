using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Parallax.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0: the thresholds of the automated checks (HARNESS.md lists them). Sprite px are at the
    /// sprite's own PPU; phone px are at <see cref="PhonePixelsPerUnit"/>.</summary>
    static class CaptureThresholds
    {
        public const float PhonePixelsPerUnit = 80f;
        public const float AlphaOpaque = 0.5f;          // a pixel counts as drawn at alpha >= this
        public const float PawsInFloorSpritePx = 2f;    // no drawn pixel deeper than this inside a solid, along gravity
        public const float HeadInCeilingSpritePx = 2f;  // item 2: nor inside a solid above it, against gravity
        public const int NearGroundTicks = 3;           // air frames this close (in ticks) to a grounded tick are checked too
        public const float FloatGapSpritePx = 2f;       // grounded frames: the lowest drawn pixel within this of the surface
        public const float ContactBandSpritePx = 2f;    // contact pixels: within this of the surface
        public const float ClusterGapSpritePx = 3f;     // contact pixels further apart than this are separate paws
        public const float PawMatchUnits = 0.25f;       // (item 0; unused since item 1: a planted paw is matched by overlapping contact pixels, CatCaptureMath.MatchPlanted)
        public const float FootSlidePhonePx = 3f;       // a planted paw drifts at most this from plant to lift
        public const float PosePopPhonePx = 3f;         // centroid jump at a sprite change, beyond the root's motion
        public const float PoseBoxPhonePx = 6f;         // bounding-box edge jump (reported, not pass/fail)
        public const float CarriedJitterPhonePx = 0.5f; // PAX-A14: a still cat on a moving floor: its and the floor's judder, and its move against the floor, per frame
        public const float RayMarginUnits = 0.5f;       // the surface search reaches this far past the column's lowest pixel
        public const int BodyErosionSpritePx = 5;       // item 3: a body centre is the drawn pixels' centroid after this erosion (A08's radius 7 at source scale)
        public const float ClimbBodyOffsetSpritePx = 6f; // item 3: on the vine, the body centre lies within this of the collider centre (the lift taken out)
    }

    /// <summary>One frame's measurements of the drawn cat, computed analytically from the sprite's alpha, pivot and PPU
    /// and the body renderer's world transform (never from the rendered image).</summary>
    sealed class FrameMeasure
    {
        public bool HasSprite, SurfaceFound;
        public string Surface = "";   // the solid under the deepest (or least-gapped) column
        public float PenetrationSpritePx = float.NegativeInfinity;   // max depth of a drawn pixel past the surface; < 0 = gap
        public float CeilingPenetrationSpritePx = float.NegativeInfinity;   // item 2: the same against gravity, into a solid above
        public bool CeilingFound;
        public float Ppu;
        public Vector2 Centroid, BoxMin, BoxMax;
        public float[] ContactAlong = Array.Empty<float>();          // along-surface coordinate of each contact pixel, sorted
    }

    /// <summary>Opaque pixels of each sprite, read from the sheet PNG on disk (what the importer was given).</summary>
    sealed class SpriteAlphaCache : IDisposable
    {
        public sealed class Entry { public Vector2 PivotPx; public float Ppu; public int[][] Columns; public int[] ColumnX; }
        readonly Dictionary<Sprite, Entry> sprites = new();
        readonly Dictionary<Texture2D, (Color32[] pixels, int width, int height)> sheets = new();

        public Entry Get(Sprite sprite)
        {
            if (sprites.TryGetValue(sprite, out Entry e)) return e;
            if (!sheets.TryGetValue(sprite.texture, out var sheet))
            {
                string path = AssetDatabase.GetAssetPath(sprite.texture);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                if (!tex.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException($"CatCapture: can't read '{path}'.");
                sheet = (tex.GetPixels32(), tex.width, tex.height);
                Object.DestroyImmediate(tex);
                if (sheet.width != sprite.texture.width || sheet.height != sprite.texture.height)
                    Debug.LogWarning($"CatCapture: '{path}' is {sheet.width}x{sheet.height} on disk but {sprite.texture.width}x{sprite.texture.height} imported; pixel positions are scaled.");
                sheets[sprite.texture] = sheet;
            }
            float s = sheet.width / (float)sprite.texture.width;
            Rect r = sprite.rect;
            int x0 = Mathf.RoundToInt(r.x * s), y0 = Mathf.RoundToInt(r.y * s), w = Mathf.RoundToInt(r.width * s), h = Mathf.RoundToInt(r.height * s);
            byte cut = (byte)Mathf.CeilToInt(CaptureThresholds.AlphaOpaque * 255f);
            var cols = new List<int[]>(); var xs = new List<int>();
            for (int x = 0; x < w; x++)
            {
                var ys = new List<int>();
                for (int y = 0; y < h; y++) if (sheet.pixels[(y0 + y) * sheet.width + x0 + x].a >= cut) ys.Add(y);
                if (ys.Count == 0) continue;
                cols.Add(ys.ToArray()); xs.Add(x);
            }
            e = new Entry { PivotPx = sprite.pivot * s, Ppu = sprite.pixelsPerUnit * s, Columns = cols.ToArray(), ColumnX = xs.ToArray() };
            sprites[sprite] = e;
            return e;
        }

        public void Dispose() { sprites.Clear(); sheets.Clear(); }
    }

    /// <summary>The per-frame measurement: every drawn pixel projected to the world; per pixel column, a ray along gravity
    /// from the collider centre's height finds the surface under it (the reality's mask, no triggers, not the cat).</summary>
    static class CatCaptureMeasure
    {
        static readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        public static FrameMeasure Measure(SpriteAlphaCache cache, SpriteRenderer body, Vector2 down, Vector2 colliderCentre, ContactFilter2D filter, Collider2D self)
        {
            var m = new FrameMeasure();
            if (body.sprite == null) return m;
            SpriteAlphaCache.Entry e = cache.Get(body.sprite);
            m.HasSprite = true;
            m.Ppu = e.Ppu;
            Matrix4x4 toWorld = body.transform.localToWorldMatrix;
            Vector2 right = GravityFrame.Right(down);
            float band = CaptureThresholds.ContactBandSpritePx / e.Ppu;
            var contact = new List<float>();
            Vector2 sum = Vector2.zero; int n = 0;
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity), max = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int c = 0; c < e.Columns.Length; c++)
            {
                int[] ys = e.Columns[c];
                var points = new Vector2[ys.Length];
                float lowest = float.NegativeInfinity; Vector2 lowestPoint = default;
                float highest = float.PositiveInfinity; Vector2 highestPoint = default;
                for (int i = 0; i < ys.Length; i++)
                {
                    Vector2 w = CatCaptureMath.PixelToWorld(e.ColumnX[c], ys[i], e.PivotPx, e.Ppu, toWorld);
                    points[i] = w;
                    sum += w; n++;
                    min = Vector2.Min(min, w); max = Vector2.Max(max, w);
                    float d = Vector2.Dot(w, down);
                    if (d > lowest) { lowest = d; lowestPoint = w; }
                    if (d < highest) { highest = d; highestPoint = w; }
                }
                // Item 2: the solid above this column (a low ceiling), searched from the collider centre's height against
                // gravity up to RayMarginUnits past the column's highest pixel; how far that pixel is inside it.
                Vector2 up = -down;
                Vector2 top0 = highestPoint + down * Vector2.Dot(colliderCentre - highestPoint, down);
                float upReach = Vector2.Dot(highestPoint - top0, up) + CaptureThresholds.RayMarginUnits;
                if (upReach > 0f && Surface(top0, up, upReach, filter, self, out Vector2 ceiling, out _))
                {
                    m.CeilingFound = true;
                    m.CeilingPenetrationSpritePx = Mathf.Max(m.CeilingPenetrationSpritePx, Vector2.Dot(highestPoint - ceiling, up) * e.Ppu);
                }
                // The ray starts at this column's along-surface position, at the collider centre's height (inside the cat).
                Vector2 origin = lowestPoint + down * Vector2.Dot(colliderCentre - lowestPoint, down);
                float reach = Vector2.Dot(lowestPoint - origin, down) + CaptureThresholds.RayMarginUnits;
                if (reach <= 0f || !Surface(origin, down, reach, filter, self, out Vector2 surface, out Collider2D solid)) continue;
                m.SurfaceFound = true;
                foreach (Vector2 w in points)
                {
                    float depth = CatCaptureMath.DepthAlongGravity(w, surface, down);
                    if (depth * e.Ppu > m.PenetrationSpritePx) { m.PenetrationSpritePx = depth * e.Ppu; m.Surface = solid.name; }
                    if (depth >= -band) contact.Add(Vector2.Dot(w, right));
                }
            }
            if (n > 0) { m.Centroid = sum / n; m.BoxMin = min; m.BoxMax = max; }
            contact.Sort();
            m.ContactAlong = contact.ToArray();
            return m;
        }

        static bool Surface(Vector2 origin, Vector2 down, float reach, ContactFilter2D filter, Collider2D self, out Vector2 surface, out Collider2D solid)
        {
            surface = default; solid = null;
            int count = Physics2D.Raycast(origin, down, filter, hits, reach);
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == self || hits[i].collider.isTrigger) continue;
                if (hits[i].distance <= 0f) continue;   // the ray started inside a solid (e.g. a head in a ceiling): not a floor
                if (hits[i].distance < best) { best = hits[i].distance; surface = hits[i].point; solid = hits[i].collider; }
            }
            return best < float.PositiveInfinity;
        }
    }

    /// <summary>PAX-V07 item 1: the presenter's thresholds the state rules compare the motor against (u/s, s), read from
    /// CatA_VisualConfig and the Turn clip.</summary>
    sealed class CaptureSpeeds
    {
        public float WalkEnter, RunEnter, RunExit, TurnSeconds;
        public float AirThreshold = float.PositiveInfinity, AirGraceDrop;   // item 2
        public float ClimbStill, LeapMin = float.PositiveInfinity, LeapMax = float.PositiveInfinity; public int MinStateFrames = 1;   // item 3
        public int SwitchWaitFrames;   // the longest wait for a Walk→Run switch frame at the run threshold (ruled 2026-09-30)
        public int RunSwitchWaitFrames;   // PAX-A14: the mirror, the longest wait for a Run→Walk switch frame at the run-exit threshold
        public const float DropTolerance = 0.005f;   // units: the drawn root's interpolation against the presenter's own reading
        public const int LagFrames = 2;   // interpolation shows the body up to one tick (1.2 frames) late: this many frames of lag are allowed
    }

    /// <summary>What a frame looked like, for the checks and the per-frame record.</summary>
    sealed class FrameRow
    {
        public int Frame, Tick; public double Time; public string Step; public bool Setup;
        public CatAnimState State; public string Clip, Sprite; public int ClipFrame, ClipFrames, Facing;
        public bool Grounded, Climbing, Jumped, Frozen, Holding, GravityDown;
        public float VAlong, VGravity; public Vector2 Root, ColliderCentre, Cam, Paw, PawImg;
        public FrameMeasure M;
        public float PopPhonePx = -1f, BoxPopPhonePx = -1f;   // -1: no sprite change this frame
        public bool Sunk;                        // item 2: grounded, but below its ground's top past airGraceDrop (shown off the ground)
        public float CarrierOffsetPhonePx;       // item 2: this frame's move of the Visual's pivot beyond the root's (the TakeOff anchor)
        public float ClimbBodyOffsetSpritePx = -1f;   // item 3: on the vine (a Climb or Hang frame), the body centre's distance from the collider centre; -1 otherwise
        public float ClimbLiftUnits;             // item 3: how far the Visual is drawn off its rest pose on that frame (the lift at a vine's foot)
        public bool Carried;                     // PAX-A14: a moving Carry floor carried the cat on the tick shown (CatMotor2D.CarrierVelocity)
        public Vector2 CarrierVelocity;          // PAX-A14: that floor's velocity on the tick shown
        public Vector2 FloorDrawn;               // PAX-A14: where the floor under the cat is drawn on this frame (its interpolated body pose)
        public string Image;
    }

    /// <summary>A state-vs-motor rule: a named test that is true when the frame breaks it. Later items add rows.</summary>
    sealed class StateRule
    {
        public readonly string Name, Description; public readonly Func<StateContext, bool> Broken;
        public StateRule(string name, string description, Func<StateContext, bool> broken) { Name = name; Description = description; Broken = broken; }
    }

    sealed class StateContext
    {
        public FrameRow Row, Prev, Next;
        public int GroundedFrames, AirborneFrames, LastAirRun;   // consecutive frames so far (this one included); the air run before the last touchdown
        // PAX-V07 item 1: consecutive frames (this one included) where Run shows below the run exit speed, Walk above the
        // run speed, or the cat moves against its facing above walkEnter.
        public int RunSlowFrames, WalkFastFrames, AgainstFacingFrames;
        // Item 2: how far the cat is below where it last stood (units), Apex frames outside the band, the frame before last's jump flag.
        public float GroundHeight, DropSinceGround;
        public int ApexOutsideFrames;
        // Item 3: consecutive frames on the vine without a vine state, Hang while moving, Climb while still.
        public int OffVineStateFrames, HangMovingFrames, ClimbStillFrames;
        // PAX-A14: consecutive frames a still cat on a moving floor shows Walk or Run.
        public int CarriedWalkFrames;
        public bool PrevPrevJumped;
        public CaptureSpeeds Speeds;
        public bool Air(CatAnimState s) => s == CatAnimState.Rise || s == CatAnimState.Apex || s == CatAnimState.Fall;
        public bool GroundLocomotion(CatAnimState s) => s == CatAnimState.Idle || s == CatAnimState.Walk || s == CatAnimState.Run || s == CatAnimState.Turn;
        public bool Excluded => Row.Holding || Row.Frozen;
        /// <summary>Item 2: on the ground as the presenter reads it (the motor's flag, less a capsule rolling off a corner).</summary>
        public bool OnGround => Row.Grounded && !Row.Sunk;
    }
}
