using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>D-293: moments with no state of their own, drawn over the state that's playing: standing up out of the sit (the
    /// cat is already moving), stepping off a ledge, a long fall, pushing against a wall, and the bridges on and off a vine.
    /// Presentation only: the table never sees them, and a clip that isn't wired is skipped, so the state's own clip shows.</summary>
    public sealed class CatTransitionClips
    {
        // IdleSit's frames: from here the cat reads as sitting, and from SatFrame it's all the way down.
        const int SittingFrame = 2, SatFrame = 5;
        // A press this long with the cat standing still is a push: starting to walk stands still for a frame or two too.
        const float PushDelay = .1f;
        // Land's frame after its contact: a dismount lands on the crouch.
        const int DismountFrame = 1;

        readonly CatClip fall, fallLoop, stepDown, sit, standUp, wallPush, reach, land;
        CatClip playing;
        float phase, pressTime;

        public CatTransitionClips(CatClipSet clips)
        {
            fall = clips.ForState(CatAnimState.Fall);
            fallLoop = clips.ForSlot("FallLoop");
            stepDown = clips.ForSlot("StepDown");
            sit = clips.ForSlot("IdleSit");
            standUp = clips.ForSlot("StandUp");
            wallPush = clips.ForSlot("WallPush");
            reach = clips.ForSlot("Reach");
            land = clips.ForState(CatAnimState.Land);
        }

        public CatClip Clip => playing;
        public int Frame => playing == null ? 0 : playing.Loop ? (int)Mathf.Repeat(Mathf.FloorToInt(phase), playing.Count) : Mathf.FloorToInt(phase);

        /// <summary>How wide a ground turn draws the cat, `time` seconds after it mirrored: from `narrow` to 1 over `open`.</summary>
        public static float TurnWidth(float time, float narrow, float open) => open > 0f ? Mathf.Lerp(narrow, 1f, time / open) : 1f;

        /// <summary>One presentation frame: true while a transition shows instead of `next`'s clip (`Clip`, `Frame`). `pressing`:
        /// the player holds the way the cat faces.</summary>
        public bool Step(CatAnimState before, CatAnimState next, CatClip shown, int shownFrame, float dt, bool pressing = false)
        {
            bool still = next == CatAnimState.Idle || next == CatAnimState.IdleFidget;
            pressTime = pressing && still ? pressTime + dt : 0f;
            if (playing != null && (playing != shown || !Continues(next))) playing = null;
            if (playing == null) return Start(before, next, shown, shownFrame);
            phase += dt * playing.Fps;
            if (!playing.Loop && phase >= playing.Count) playing = null;
            return playing != null;
        }

        bool Continues(CatAnimState next)
        {
            if (playing == wallPush) return pressTime > 0f;
            if (playing == reach) return OnVine(next) || next == CatAnimState.Leap;
            if (playing == standUp || playing == land) return IsGait(next) || next == CatAnimState.Idle;
            return next == CatAnimState.Fall;
        }

        bool Start(CatAnimState before, CatAnimState next, CatClip shown, int shownFrame)
        {
            if (next == CatAnimState.Fall && shown != null && shown == fall && shownFrame == fall.Count - 1) return Play(fallLoop, 0);
            if (next == CatAnimState.Fall && (IsGait(before) || before == CatAnimState.Idle)) return Play(stepDown, 0);
            if (IsGait(next) && before == CatAnimState.IdleFidget && shown != null && shown == sit && shownFrame >= SittingFrame)
                return Play(standUp, shownFrame >= SatFrame ? 0 : 1);
            if (pressTime >= PushDelay) return Play(wallPush, 0);
            if (OnVine(next) && !OnVine(before)) return Play(reach, 0);
            if (next == CatAnimState.Leap && OnVine(before)) return Play(reach, 0);
            if ((IsGait(next) || next == CatAnimState.Idle) && OnVine(before)) return Play(land, DismountFrame);
            return false;
        }

        bool Play(CatClip clip, int frame)
        {
            if (clip == null || clip.Count == 0) return false;
            playing = clip;
            phase = frame;
            return true;
        }

        static bool OnVine(CatAnimState s) => s == CatAnimState.Climb || s == CatAnimState.Hang;

        static bool IsGait(CatAnimState s) => s == CatAnimState.Walk || s == CatAnimState.Run;
    }
}
