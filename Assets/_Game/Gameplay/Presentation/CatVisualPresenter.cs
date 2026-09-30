using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    // Purely cosmetic: reads its own transform's motion and the local GravityReceiver,
    // never writes to anything outside Visual, never networked, never read by gameplay.
    [DisallowMultipleComponent]
    public sealed class CatVisualPresenter : MonoBehaviour
    {
        [SerializeField] CatVisualConfig config;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteRenderer outlineRenderer;
        [Tooltip("PAX-V07: the clip table, one clip per wired A08 slot (written by PARALLAX/Setup/Cat Visual).")]
        [SerializeField] CatClipSet clips = new CatClipSet();
        [Tooltip("PAX-V07 §4: death kind -> death clip (written by PARALLAX/Setup/Cat Visual).")]
        [SerializeField] CatDeathClipTable deathClips;
        [Tooltip("PAX-V07: this cat's death hold, respawn and level complete (on this prefab; written by the setup menu).")]
        [SerializeField] CatPresentationSignals signals;
        [SerializeField] CatMotor2D motor;
        [SerializeField] ObserverContext observer;

        GravityReceiver gravity;
        RealityRoot reality;
        Transform motionRoot;
        Rigidbody2D body;
        CatAnimStateMachine stateMachine;

        bool initialized;
        // Visual's authored pose (the paw row at the collider's bottom). The presenter never moves it (ruled 2026-09-30: art
        // defects aren't hidden in code); item 3: the climb frames are drawn vertical and registered on the collider, so
        // Visual keeps this pose on the vine too (no interim 90° climb pose).
        Vector3 restLocalPosition;
        Collider2D bodyCollider;
        Vector2 lastWorldPosition;
        Vector2 smoothedVelocity;
        // The clip on screen and where it is: `phase` is in frames for a clip played by time, in units travelled for one
        // played by distance (CatClip.ByDistance). It is kept while the speed changes, so a loop never restarts.
        CatClip current;
        float phase;
        int shownFacing = 1;
        int missingClipWarnings;
        // Item 2: the air clips' velocity range (a normal jump's speed).
        float jumpSpeed;
        int pendingAirFrame = -1;
        float lastBodyAlong;
        bool stopping = true;
        bool airStateEnding;

        void Awake()
        {
            if (config == null || bodyRenderer == null || motor == null)
            {
                Debug.LogError(
                    $"CatVisualPresenter on '{gameObject.name}' is missing its config, body renderer, or motor. Disabling.",
                    this);
                enabled = false;
                return;
            }

            gravity = motor.GetComponent<GravityReceiver>();
            reality = motor.GetComponentInParent<RealityRoot>();
            motionRoot = motor.transform;
            body = motor.GetComponent<Rigidbody2D>();
            restLocalPosition = transform.localPosition;
            foreach (Collider2D c in motor.GetComponents<Collider2D>()) if (!c.isTrigger) { bodyCollider = c; break; }

            if (gravity == null)
            {
                Debug.LogError(
                    $"CatVisualPresenter on '{gameObject.name}' found no GravityReceiver on its assigned motor. Disabling.",
                    this);
                enabled = false;
                return;
            }

            CatClip land = clips.ForState(CatAnimState.Land);
            CatClip takeOff = clips.ForState(CatAnimState.TakeOff), hardLand = clips.ForState(CatAnimState.HardLand);
            stateMachine = new CatAnimStateMachine(new CatAnimSettings(
                config.RiseExit,
                config.FallEnter,
                config.WalkEnter,
                config.WalkExit,
                land != null ? land.DurationFromEntry : config.LandDuration,
                config.RunEnterSpeed,
                config.RunExitSpeed,
                config.TurnHoldTime,
                config.FlipHysteresis,
                config.SnapAcceleration,
                config.MinStateFrames,
                config.HardLandDistance,
                takeOff != null ? takeOff.DurationFromEntry : 0f,
                hardLand != null ? hardLand.DurationFromEntry : 0f,
                config.AirGraceDrop,
                config.SpeedLeadTolerance,
                config.ClimbStillSpeed,
                config.LeapSpeedMin(gravity.Strength),
                config.LeapSpeedMax(gravity.Strength),
                LeapDuration(),
                clips.ForState(CatAnimState.Flip)?.DurationFromEntry ?? 0f,
                config.FidgetDelay,
                clips.Durations(CatAnimState.IdleFidget),
                clips.ForState(CatAnimState.Respawn)?.DurationFromEntry ?? 0f));
            jumpSpeed = config.JumpSpeed(gravity.Strength);
            shownFacing = Facing;

            if (outlineRenderer != null)
            {
                Color outlineColor = reality != null && reality.Id == ObserverId.B
                    ? config.OutlineColorB
                    : config.OutlineColorA;

                var block = new MaterialPropertyBlock();
                outlineRenderer.GetPropertyBlock(block);
                block.SetColor(OutlineColorId, outlineColor);
                block.SetFloat(OutlineWidthId, config.OutlineWidth);
                outlineRenderer.SetPropertyBlock(block);
            }
        }

        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        /// <summary>PAX-V07 (§11 R9): what the presenter shows, read by the capture harness and tests. Read-only.</summary>
        public CatAnimState State => stateMachine != null ? stateMachine.State : CatAnimState.Idle;
        /// <summary>The clip whose frame is on screen, by name (a slot: "Climb", or "TakeOff" for the bridge at a grab).</summary>
        public string ClipName { get; private set; } = "";
        /// <summary>The index of the frame on screen within <see cref="ClipName"/>.</summary>
        public int FrameIndex { get; private set; }
        /// <summary>How many frames the clip on screen has (0 before any).</summary>
        public int ClipFrameCount => current != null ? current.Count : 0;
        /// <summary>+1 facing the cat's local right, -1 facing its left (the sign of Visual's localScale.x).</summary>
        public int Facing => transform.localScale.x >= 0f ? 1 : -1;

        void LateUpdate()
        {
            if (!bodyRenderer.isVisible)
            {
                initialized = false;
                smoothedVelocity = Vector2.zero;
                return;
            }
            Present(Time.deltaTime);
        }

        /// <summary>PAX-V07 (§11 R9): one presentation frame of `dt` seconds. LateUpdate calls it while the body renderer is
        /// visible; the capture harness and EditMode tests call it directly. Reads the motor and its transform, writes only
        /// Visual. Does nothing before Awake has run (or after Awake disabled the presenter).</summary>
        public void Present(float dt)
        {
            if (stateMachine == null) return;

            Vector2 currentPosition = motionRoot.position;
            if (!initialized)
            {
                lastWorldPosition = currentPosition;
                initialized = true;
            }

            Vector2 delta = currentPosition - lastWorldPosition;
            lastWorldPosition = currentPosition;

            bool teleported = delta.magnitude >= config.TeleportDistance;
            Vector2 measuredVelocity = !teleported && dt > 0f
                ? delta / dt
                : Vector2.zero;

            if (teleported)
            {
                smoothedVelocity = Vector2.zero;
                stateMachine.Reset();
                current = null;
            }
            else
            {
                float smoothing = config.VelocitySmoothingTime > 0f
                    ? 1f - Mathf.Exp(-dt / config.VelocitySmoothingTime)
                    : 1f;
                smoothedVelocity = Vector2.Lerp(smoothedVelocity, measuredVelocity, smoothing);
            }

            Vector2 down = gravity.Direction;
            float rawAlong = GravityFrame.Along(measuredVelocity, down);
            float rawVelocityAlongGravity = Vector2.Dot(measuredVelocity, down);
            float travelled = teleported ? 0f : Mathf.Abs(GravityFrame.Along(delta, down));

            float height = -Vector2.Dot(currentPosition, down);
            Vector2 paws = motionRoot.TransformPoint(restLocalPosition);
            Collider2D ground = motor.GroundCollider;
            bool rollingOff = ground != null && !motor.JumpedThisStep && CatPresentationSignals.SunkBelow(ground.bounds, paws, down, config.GroundSinkTolerance);
            // Ruled 2026-09-30: a landing enters Land / HardLand on the frame matching the pose on screen (the fall's last
            // pose), and Walk↔Run changes only on a frame whose paws match the other gait.
            Vector2 pose = current != null ? current.PoseCentroid(FrameIndex) : Vector2.zero;
            float landFrom = LandingDuration(CatAnimState.Land, pose), hardLandFrom = LandingDuration(CatAnimState.HardLand, pose);
            bool gaitReady = current == null || !current.HasSwitchFrames || current.IsSwitchFrame(FrameIndex);
            // Items 6 and 7: this cat's death hold, its respawn (the frame it teleports shows the Respawn clip there, never the
            // held death pose) and level complete, from the scene (CatPresentationSignals).
            bool holding = signals != null && signals.Holding;
            bool respawned = signals != null && signals.ConsumeRespawned();
            bool complete = signals != null && signals.LevelComplete;
            // A stop brakes at the motor's deceleration, a reversal at its acceleration: read from the body's last speed change.
            float bodyAlong = body != null ? GravityFrame.Along(body.linearVelocity, down) : rawAlong;
            if (!Mathf.Approximately(bodyAlong, lastBodyAlong))
            {
                stopping = Mathf.Abs(lastBodyAlong) - Mathf.Abs(bodyAlong) >= config.BrakeDeceleration * TickTime.SecondsPerTick * 0.9f || Mathf.Abs(bodyAlong) < 1e-3f;
                lastBodyAlong = bodyAlong;
            }
            CatAnimState before = stateMachine.State;
            CatAnimState next = teleported && !respawned
                ? stateMachine.State
                : stateMachine.Step(new CatAnimInput(motor.IsGrounded && !rollingOff, motor.IsClimbing, rawAlong, rawVelocityAlongGravity, dt,
                    motor.JumpedThisStep, down.y > 0f ? 1f : -1f, height, rollingOff: rollingOff,
                    bodySurfaceSpeed: body != null ? GravityFrame.Along(body.linearVelocity, down) : rawAlong,
                    bodyVelocityAlongGravity: body != null ? Vector2.Dot(body.linearVelocity, down) : rawVelocityAlongGravity,
                    landDuration: landFrom, hardLandDuration: hardLandFrom, gaitSwitchReady: gaitReady,
                    holding: holding, respawned: respawned, levelComplete: complete, stopping: stopping));

            UpdateEchoAlpha();
            if (next == CatAnimState.Turn) ShowFlipFrame(before != CatAnimState.Turn);
            else if (next == CatAnimState.Climb || next == CatAnimState.Hang) Vine(next, teleported ? 0f : -Vector2.Dot(delta, down), dt);
            else
            {
                float progress = CatClipSet.AirProgress(next, rawVelocityAlongGravity, config.RiseExit, config.FallEnter, jumpSpeed);
                airStateEnding = CatClip.AirFramesLeft(next, rawVelocityAlongGravity, config.RiseExit, config.FallEnter, gravity.Strength * dt) < config.MinStateFrames;
                CatClip clip = next == CatAnimState.IdleFidget ? clips.ForState(next, stateMachine.FidgetIndex)
                    : next == CatAnimState.Death && deathClips != null ? deathClips.For(signals != null ? signals.HoldKind : CatDeathKind.Default) ?? clips.ForState(next)
                    : clips.ForState(next);
                UpdateSprite(next, clip, Mathf.Abs(rawAlong), travelled, dt, byDistance: true, progress);
            }
            // The facing applies after the frame is chosen: Turn holds its flip frame facing the old way for one frame, and the
            // next frame shows the same pose mirrored, the gait continuing from it.
            ApplyFacing(stateMachine.Facing);
            shownFacing = Facing;
        }

        // Ruled 2026-09-30: the turn flips on the most symmetrical frame of the clip on screen (the one whose mirror moves the
        // silhouette least, counting the step to it from the frame shown), held for Turn's one frame. Clips without pose data
        // hold the frame on screen.
        void ShowFlipFrame(bool entering)
        {
            if (!entering || current == null) return;
            int frame = current.FlipFrame(FrameIndex);
            ShowFrame(current, frame);
            phase = current.ByDistance ? current.DistanceAtFrame(frame) : frame;   // the gait resumes from the flip frame
        }

        // How long `state`'s clip would show if it started now, from its frame matching `pose` (seconds; 0 = none).
        float LandingDuration(CatAnimState state, Vector2 pose)
        {
            CatClip clip = clips.ForState(state);
            if (clip == null || current == null) return 0f;
            return clip.DurationFrom(clip.ClosestPose(pose, shownFacing, Facing));
        }

        // Item 3: on the vine. Climb is played by the distance the drawn cat climbs (its strides from the art: a gripping paw
        // stays on its hold; down the vine the phase runs backwards, the clip reversed). Hang shows its own drawing (final
        // critic: not whichever climb frame was on screen, a frozen mid-stride).
        void Vine(CatAnimState next, float climbed, float dt)
        {
            if (next == CatAnimState.Hang) UpdateSprite(next, clips.ForState(CatAnimState.Hang), 0f, 0f, dt, byDistance: false);
            else UpdateSprite(CatAnimState.Climb, clips.ForState(CatAnimState.Climb), 0f, climbed, dt, byDistance: true);
        }

        // D-034: the presenter owns Visual's facing; the state machine decides it (a ground reversal flips at Turn's end).
        void ApplyFacing(int facing)
        {
            if (facing == Facing) return;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facing > 0 ? 1f : -1f);
            transform.localScale = scale;
        }

        void UpdateEchoAlpha()
        {
            float alpha = observer != null && observer.Driver != null && observer.Driver.Kind == InputSourceKind.EchoReplay ? config.EchoAlpha : 1f;
            SetAlpha(bodyRenderer, alpha);
            SetAlpha(outlineRenderer, alpha);
        }

        static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null || Mathf.Approximately(renderer.color.a, alpha)) return;
            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }

        // One frame of `clip`, entered on EntryFrame. Rise, Apex and Fall follow the arc (`progress`); a clip with strides is
        // played by distance (`travelled`): each frame lasts its own stride, so a planted paw stays put at any speed; others by
        // time, at the clip's fps (Walk without strides at FlipbookMath.FpsForSpeed). The phase carries across speed changes.
        void UpdateSprite(CatAnimState state, CatClip clip, float speed, float travelled, float dt, bool byDistance, float progress = -1f)
        {
            if (clip == null || clip.Count == 0)
            {
                WarnMissingClipOnce(state);
                return;
            }

            bool distance = byDistance && clip.ByDistance;
            int index;
            if (progress >= 0f)
            {
                int target = clip.FrameForProgress(progress);
                if (clip != current) pendingAirFrame = -1;
                if (clip == current && airStateEnding) target = Mathf.Min(target, FrameIndex);   // no frame shown only once before the next state
                index = clip == current ? CatClip.SteadyAdvance(FrameIndex, target, ref pendingAirFrame) : target;
                current = clip;
                phase = index;
            }
            else if (clip != current)
            {
                index = EntryFrame(clip, distance);
                current = clip;
                phase = distance ? clip.DistanceAtFrame(index) : index;
            }
            else if (distance)
            {
                phase += travelled;
                index = clip.FrameAtDistance(phase);
                // Final critic: a loop covering several frames per display frame (the climb at full speed) steps through a regular
                // subset (every k-th frame) instead of strobing through uneven skips.
                int step = clip.CycleLength > 0f ? Mathf.FloorToInt(Mathf.Abs(travelled) * clip.Count / clip.CycleLength) : 1;
                if (step > 1 && clip.Loop) index -= index % step;
            }
            else
            {
                float fps = clip.State == CatAnimState.Walk
                    ? FlipbookMath.FpsForSpeed(speed, config.ReferenceSpeed, clip.Fps, config.MinFps, config.MaxFps)
                    : clip.Fps;
                phase += (dt > 0f ? dt : 0f) * fps;
                index = FrameAt(phase, clip.Count, clip.Loop);
            }

            if (clip.Frames[index] == null) { WarnMissingClipOnce(state); return; }
            ShowFrame(clip, index);
        }

        void ShowFrame(CatClip clip, int index)
        {
            if (clip == null || index < 0 || index >= clip.Count || clip.Frames[index] == null) return;
            if (clip != current) { current = clip; phase = clip.ByDistance ? clip.DistanceAtFrame(index) : index; }
            bodyRenderer.sprite = clip.Frames[index];
            if (outlineRenderer != null) outlineRenderer.sprite = clip.Frames[index];
            ClipName = clip.Slot;
            FrameIndex = index;
        }

        // Item 3: Leap shows its clip from its entry frame.
        float LeapDuration()
        {
            CatClip leap = clips.ForState(CatAnimState.Leap);
            return leap != null ? leap.DurationFromEntry : 0f;
        }

        // Where a clip starts. A one-shot starts at its first entry frame, except a landing (from an air pose, or Land after
        // HardLand), which starts on its frame matching the pose on screen. A loop starts on its entry frame closest to the
        // pose on screen, with three exceptions: from the air, on a frame whose paws reach for the ground; braking into a clip
        // played by distance, on the frame from which the remaining travel (the drawn root's lag behind the body plus the
        // motor's braking distance) ends on a stance frame; and Walk↔Run, on the other gait's frame matching the switch frame.
        int EntryFrame(CatClip clip, bool distance)
        {
            if (current == null) return clip.Loop ? 0 : clip.FirstEntryFrame;
            Vector2 pose = current.PoseCentroid(FrameIndex);
            bool fromAir = IsAir(current.State);
            bool landing = clip.State == CatAnimState.Land || clip.State == CatAnimState.HardLand;
            if (!clip.Loop)
                return landing && (fromAir || current.State == CatAnimState.HardLand)
                    ? clip.ClosestPose(pose, shownFacing, Facing) : clip.FirstEntryFrame;
            if (fromAir) return clip.LandEntry(pose, shownFacing, Facing);
            if (distance && stateMachine.Braking && body != null && config.BrakeDeceleration > 0f)
            {
                Vector2 down = gravity.Direction;
                float bodySpeed = Mathf.Abs(GravityFrame.Along(body.linearVelocity, down));
                float lag = Mathf.Abs(GravityFrame.Along(body.position - (Vector2)motionRoot.position, down));
                return clip.StanceEntry(lag + CatClip.BrakingDistance(bodySpeed, config.BrakeDeceleration), pose, shownFacing, Facing);
            }
            if (current.IsSwitchFrame(FrameIndex) && IsGait(current.State) && IsGait(clip.State)) return current.SwitchTarget(FrameIndex);
            return clip.ClosestPose(pose, shownFacing, Facing);
        }

        static bool IsGait(CatAnimState s) => s == CatAnimState.Walk || s == CatAnimState.Run;

        static bool IsAir(CatAnimState s) => s == CatAnimState.Rise || s == CatAnimState.Apex || s == CatAnimState.Fall || s == CatAnimState.TakeOff
            || s == CatAnimState.Flip;

        static int FrameAt(float framePhase, int frameCount, bool loop) =>
            frameCount <= 1 ? 0 : loop ? (int)Mathf.Repeat(Mathf.FloorToInt(framePhase), frameCount) : Mathf.Clamp(Mathf.FloorToInt(framePhase), 0, frameCount - 1);

        void WarnMissingClipOnce(CatAnimState state)
        {
            int bit = 1 << (int)state;
            if ((missingClipWarnings & bit) != 0) return;

            missingClipWarnings |= bit;
            Debug.LogWarning(
                $"CatVisualPresenter on '{gameObject.name}' has a missing or empty {state} clip. Keeping the previous state's frame.",
                this);
        }
    }
}
