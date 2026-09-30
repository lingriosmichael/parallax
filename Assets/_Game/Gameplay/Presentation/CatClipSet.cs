using System;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V07 §5: one clip of the cat's flipbook, one A08 slot. Written by PARALLAX/Setup/Cat Visual from the A08
    /// manifest; never edited by hand.</summary>
    [Serializable]
    public sealed class CatClip
    {
        [SerializeField] string slot = "";
        [SerializeField] CatAnimState state;
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField] float fps;
        [SerializeField] bool loop;
        [Tooltip("Locomotion clips: how far the body travels over each frame, in world units (a planted paw's advance to " +
                 "the next frame, measured from the art). Played by distance, so the paws stay planted at any speed.")]
        [SerializeField] float[] strideUnits = new float[0];
        [Tooltip("Each frame's silhouette centroid relative to the pivot, in world units (facing +1). Entering a looping " +
                 "clip starts on the frame closest to the pose on screen.")]
        [SerializeField] Vector2[] poseCentroids = new Vector2[0];
        [Tooltip("Loops: the frames the clip may start on from another clip (empty = any), e.g. Run's push-off frames.")]
        [SerializeField] int[] entryFrames = new int[0];
        [Tooltip("Locomotion clips: the stance frames a stop settles on (paws where the Idle stance puts them).")]
        [SerializeField] int[] stanceFrames = new int[0];
        [Tooltip("Locomotion clips: the frames a landing from the air enters on (empty = the entry frames), e.g. Run's fore paws " +
                 "reaching to touch down.")]
        [SerializeField] int[] landEntryFrames = new int[0];
        [Tooltip("Walk / Run (ruled 2026-09-30): the frames on which this gait may hand over to the other one, where the paws of " +
                 "both clips match (measured from the art), and the other gait's frame each one enters on (same order).")]
        [SerializeField] int[] switchFrames = new int[0];
        [SerializeField] int[] switchTargets = new int[0];

        public CatClip() { }

        public CatClip(string slot, CatAnimState state, Sprite[] frames, float fps, bool loop, float[] strideUnits, Vector2[] poseCentroids,
            int[] entryFrames = null, int[] stanceFrames = null, int[] landEntryFrames = null, int[] switchFrames = null, int[] switchTargets = null)
        {
            this.switchFrames = switchFrames ?? new int[0];
            this.switchTargets = switchTargets ?? new int[0];
            this.landEntryFrames = landEntryFrames ?? new int[0];
            this.entryFrames = entryFrames ?? new int[0];
            this.stanceFrames = stanceFrames ?? new int[0];
            this.slot = slot;
            this.state = state;
            this.frames = frames ?? new Sprite[0];
            this.fps = fps;
            this.loop = loop;
            this.strideUnits = strideUnits ?? new float[0];
            this.poseCentroids = poseCentroids ?? new Vector2[0];
        }

        public string Slot => slot;
        public CatAnimState State => state;
        public Sprite[] Frames => frames;
        public float Fps => fps;
        public bool Loop => loop;
        public float[] StrideUnits => strideUnits;
        public Vector2[] PoseCentroids => poseCentroids;
        public int[] EntryFrames => entryFrames;
        public int[] StanceFrames => stanceFrames;
        public int[] LandEntryFrames => landEntryFrames;
        /// <summary>A one-shot starts here: its first entry frame (0 without entry frames).</summary>
        public int FirstEntryFrame => FirstEntry;
        /// <summary>How long the clip shows from frame `index` to its end (seconds at its fps).</summary>
        public float DurationFrom(int index) => fps > 0f ? Mathf.Max(0, Count - Mathf.Clamp(index, 0, Count)) / fps : 0f;
        /// <summary>How long a one-shot shows from its first entry frame to its end.</summary>
        public float DurationFromEntry => fps > 0f ? Mathf.Max(0, Count - FirstEntry) / fps : 0f;
        public int Count => frames != null ? frames.Length : 0;
        /// <summary>Played by distance travelled (one stride per frame) rather than by time.</summary>
        public bool ByDistance => strideUnits != null && strideUnits.Length == Count && Count > 0 && CycleLength > 0f;
        public float Duration => fps > 0f ? Count / fps : 0f;

        [NonSerialized] float cycle;
        [NonSerialized] bool cycleKnown;
        public float CycleLength
        {
            get
            {
                if (cycleKnown) return cycle;
                cycleKnown = true;
                cycle = 0f;
                if (strideUnits != null) foreach (float s in strideUnits) cycle += Mathf.Max(0f, s);
                return cycle;
            }
        }

        /// <summary>The frame at `distance` along the stride cycle (wrapped for a loop): frame i from DistanceAtFrame(i) for
        /// its own stride. So each frame appears where its planted paws sit exactly on the ground they covered in the frame
        /// before, and a held frame rides at most one stride ahead before the next one puts them back.</summary>
        public int FrameAtDistance(float distance)
        {
            if (!ByDistance) return 0;
            float d = loop ? Mathf.Repeat(distance, CycleLength) : Mathf.Clamp(distance, 0f, CycleLength);
            for (int i = 0; i < strideUnits.Length; i++)
            {
                d -= strideUnits[i];
                if (d < 0f) return i;
            }
            return loop ? 0 : Count - 1;
        }

        /// <summary>Where frame `index` starts along the stride cycle (its planted paws exactly on their prints).</summary>
        public float DistanceAtFrame(int index)
        {
            float d = 0f;
            for (int i = 0; i < index && i < strideUnits.Length; i++) d += strideUnits[i];
            return d;
        }

        /// <summary>The entry frame (among EntryFrames, or any frame when there are none) whose silhouette centroid, at
        /// `facing`, lies closest to `centroid` (the pose on screen, at `currentFacing`): the entry that pops least.</summary>
        public int ClosestPose(Vector2 centroid, int currentFacing, int facing)
        {
            if (poseCentroids == null || poseCentroids.Length != Count || Count == 0) return FirstEntry;
            int best = FirstEntry; float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < poseCentroids.Length; i++)
            {
                if (!IsEntry(i)) continue;
                float d = PoseDistance(i, centroid, currentFacing, facing);
                if (d < bestDistance - 1e-9f) { bestDistance = d; best = i; }
            }
            return best;
        }

        /// <summary>Braking into this clip (played by distance): a frame from which the body's remaining travel,
        /// `stopDistance`, ends on a stance frame (within half that stance frame's stride of its middle); among those, the
        /// one whose pose is closest to the one on screen. With none, the frame that ends nearest a stance's middle. Any
        /// frame may be the entry here: the brake picks it.</summary>
        public int StanceEntry(float stopDistance, Vector2 centroid, int currentFacing, int facing)
        {
            if (!ByDistance || stanceFrames == null || stanceFrames.Length == 0) return ClosestPose(centroid, currentFacing, facing);
            int best = -1, nearest = 0;
            float bestPose = float.PositiveInfinity, nearestMiss = float.PositiveInfinity;
            for (int i = 0; i < Count; i++)
            {
                float miss = Mathf.Abs(DistanceToStance(i, out int stance) - stopDistance);
                if (miss < nearestMiss) { nearestMiss = miss; nearest = i; }
                if (miss > strideUnits[stance] * 0.5f) continue;
                float pose = PoseDistance(i, centroid, currentFacing, facing);
                if (pose < bestPose) { bestPose = pose; best = i; }
            }
            return best >= 0 ? best : nearest;
        }

        /// <summary>A landing from the air into this loop: the land entry frame (or, without any, the entry frame) whose
        /// silhouette is closest to the pose on screen.</summary>
        public int LandEntry(Vector2 centroid, int currentFacing, int facing)
        {
            if (landEntryFrames == null || landEntryFrames.Length == 0 || poseCentroids == null || poseCentroids.Length != Count)
                return ClosestPose(centroid, currentFacing, facing);
            int best = landEntryFrames[0]; float bestDistance = float.PositiveInfinity;
            foreach (int i in landEntryFrames)
            {
                if (i < 0 || i >= Count) continue;
                float d = PoseDistance(i, centroid, currentFacing, facing);
                if (d < bestDistance - 1e-9f) { bestDistance = d; best = i; }
            }
            return best;
        }

        /// <summary>Item 2: an air clip's frame for `progress` (0 at the start of its velocity range, 1 at its end), clamped:
        /// Rise, Apex and Fall follow the cat's velocity along gravity, so each pose shows where the arc has it.</summary>
        public int FrameForProgress(float progress) => Count <= 1 ? 0 : Mathf.Clamp(Mathf.FloorToInt(progress * Count), 0, Count - 1);

        /// <summary>Item 2: an air clip's next frame. It never goes back (the drawn speed dips on the frame the body meets the
        /// floor), and it moves on only once the arc has asked for a later frame on two frames running, so an abrupt change
        /// (a head bump) never shows a frame for a single display frame. `pending` carries the request between frames.</summary>
        /// <summary>Round 2: about how many more display frames Rise or Apex has before the velocity along gravity leaves its
        /// band (`dv` = the change of that velocity in one frame); infinity for any other state.</summary>
        public static float AirFramesLeft(CatAnimState state, float velocityAlongGravity, float riseExit, float fallEnter, float dv)
        {
            if (dv <= 0f) return float.PositiveInfinity;
            if (state == CatAnimState.Rise) return (-velocityAlongGravity - riseExit) / dv;
            if (state == CatAnimState.Apex) return (fallEnter - velocityAlongGravity) / dv;
            return float.PositiveInfinity;
        }

        public static int SteadyAdvance(int shown, int target, ref int pending)
        {
            if (target <= shown) { pending = -1; return shown; }
            if (pending != target) { pending = target; return shown; }
            pending = -1;
            return target;
        }

        public float DistanceToStance(int index) => DistanceToStance(index, out _);

        /// <summary>How far the body travels from the start of frame `index` to the middle of the next stance frame (half
        /// its stride on a stance frame), so a stop that lands a little short or long still shows the stance.</summary>
        public float DistanceToStance(int index, out int stance)
        {
            float best = float.PositiveInfinity, start = DistanceAtFrame(index);
            stance = index;
            foreach (int s in stanceFrames)
            {
                if (s < 0 || s >= Count) continue;
                float d = Mathf.Repeat(DistanceAtFrame(s) - start, CycleLength) + strideUnits[s] * 0.5f;
                if (d < best) { best = d; stance = s; }
            }
            return best < float.PositiveInfinity ? best : 0f;
        }

        public bool IsStance(int index) => stanceFrames != null && System.Array.IndexOf(stanceFrames, index) >= 0;

        bool IsEntry(int index) => entryFrames == null || entryFrames.Length == 0 || System.Array.IndexOf(entryFrames, index) >= 0;
        int FirstEntry => entryFrames != null && entryFrames.Length > 0 ? entryFrames[0] : 0;

        float PoseDistance(int index, Vector2 centroid, int currentFacing, int facing)
        {
            if (poseCentroids == null || poseCentroids.Length != Count) return 0f;
            Vector2 target = new(centroid.x * currentFacing, centroid.y);
            Vector2 c = new(poseCentroids[index].x * facing, poseCentroids[index].y);
            return (c - target).magnitude;
        }

        /// <summary>How far a body moving at `speed` still travels while the motor brakes it at `deceleration`, tick by tick
        /// (each tick's velocity is the previous one less deceleration × tick, as CatMotor2D's MoveTowards does).</summary>
        public static float BrakingDistance(float speed, float deceleration)
        {
            float tick = TickTime.SecondsPerTick, step = deceleration * tick, d = 0f;
            if (step <= 0f) return 0f;
            for (float v = speed - step; v > 0f; v -= step) d += v * tick;
            return d;
        }

        public bool HasSwitchFrames => switchFrames != null && switchFrames.Length > 0;
        public bool IsSwitchFrame(int index) => switchFrames != null && Array.IndexOf(switchFrames, index) >= 0;

        /// <summary>The other gait's frame that switch frame `index` enters on (0 when `index` isn't a switch frame).</summary>
        public int SwitchTarget(int index)
        {
            int k = switchFrames != null ? Array.IndexOf(switchFrames, index) : -1;
            return k >= 0 && switchTargets != null && k < switchTargets.Length ? switchTargets[k] : 0;
        }

        /// <summary>How far the silhouette's centroid moves when frame `index` is mirrored about the pivot (units): twice its
        /// distance from the pivot across the facing.</summary>
        public float MirrorShift(int index) => 2f * Mathf.Abs(PoseCentroid(index).x);

        /// <summary>The frame a turn flips on (ruled 2026-09-30): the one whose mirror moves the silhouette least, counting the
        /// step to it from frame `shown` (for a loop; a one-shot, or a clip without pose data, keeps `shown`).</summary>
        public int FlipFrame(int shown)
        {
            if (!loop || poseCentroids == null || poseCentroids.Length != Count || shown < 0 || shown >= Count) return shown;
            int best = shown; float bestCost = MirrorShift(shown);
            for (int i = 0; i < Count; i++)
            {
                float cost = (poseCentroids[i] - poseCentroids[shown]).magnitude + MirrorShift(i);
                if (cost < bestCost - 1e-6f) { bestCost = cost; best = i; }
            }
            return best;
        }

        public Vector2 PoseCentroid(int index) =>
            poseCentroids != null && index >= 0 && index < poseCentroids.Length ? poseCentroids[index] : Vector2.zero;
    }

    /// <summary>PAX-V07 §5: the serializable clip table, one clip per wired A08 slot. Looked up by state (the first clip
    /// declared for it; Run falls back to Walk until it's wired) or by slot name (fidgets and deaths, later items).</summary>
    [Serializable]
    public sealed class CatClipSet
    {
        [SerializeField] CatClip[] clips = new CatClip[0];

        public CatClip[] Clips => clips;

        public CatClip ForState(CatAnimState state)
        {
            if (clips == null) return null;
            foreach (CatClip clip in clips) if (clip != null && clip.State == state && clip.Count > 0) return clip;
            return state == CatAnimState.Run ? ForState(CatAnimState.Walk) : null;
        }

        /// <summary>Item 2: where the arc has the cat, for Rise, Apex and Fall: 0 to 1 over each state's velocity band along
        /// gravity (positive = falling): Rise from the jump speed up to riseExit, Apex across the band, Fall from fallEnter to
        /// a normal jump's landing speed (held beyond it, a long fall). Negative for every other state.</summary>
        public static float AirProgress(CatAnimState state, float velocityAlongGravity, float riseExit, float fallEnter, float jumpSpeed)
        {
            float top = Mathf.Max(jumpSpeed, Mathf.Max(riseExit, fallEnter) + 0.01f);
            switch (state)
            {
                case CatAnimState.Rise: return Mathf.Clamp01((velocityAlongGravity + top) / (top - riseExit));
                case CatAnimState.Apex: return Mathf.Clamp01((velocityAlongGravity + riseExit) / Mathf.Max(0.01f, riseExit + fallEnter));
                case CatAnimState.Fall: return Mathf.Clamp01((velocityAlongGravity - fallEnter) / (top - fallEnter));
                default: return -1f;
            }
        }

        /// <summary>The `index`-th clip declared for `state` (item 5: the fidgets, in the cycle's order), or null.</summary>
        public CatClip ForState(CatAnimState state, int index)
        {
            if (clips == null) return null;
            foreach (CatClip clip in clips)
                if (clip != null && clip.State == state && clip.Count > 0 && index-- == 0) return clip;
            return null;
        }

        /// <summary>Each clip declared for `state`, in order: its length from its first entry frame (seconds).</summary>
        public float[] Durations(CatAnimState state)
        {
            var list = new System.Collections.Generic.List<float>();
            if (clips != null) foreach (CatClip clip in clips) if (clip != null && clip.State == state && clip.Count > 0) list.Add(clip.DurationFromEntry);
            return list.ToArray();
        }

        public CatClip ForSlot(string slot)
        {
            if (clips == null) return null;
            foreach (CatClip clip in clips) if (clip != null && clip.Slot == slot) return clip;
            return null;
        }
    }
}
