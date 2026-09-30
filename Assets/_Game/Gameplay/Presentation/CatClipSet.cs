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

        public CatClip() { }

        public CatClip(string slot, CatAnimState state, Sprite[] frames, float fps, bool loop, float[] strideUnits, Vector2[] poseCentroids,
            int[] entryFrames = null, int[] stanceFrames = null)
        {
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

        public CatClip ForSlot(string slot)
        {
            if (clips == null) return null;
            foreach (CatClip clip in clips) if (clip != null && clip.Slot == slot) return clip;
            return null;
        }
    }
}
