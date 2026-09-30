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
        [SerializeField] CatMotor2D motor;
        [SerializeField] ObserverContext observer;

        GravityReceiver gravity;
        RealityRoot reality;
        Transform motionRoot;
        Rigidbody2D body;
        CatAnimStateMachine stateMachine;

        bool initialized;
        // PAX-087 (D-089): the climb pose turns this transform; its authored pose is restored on leaving Climb.
        Vector3 restLocalPosition;
        Quaternion restLocalRotation;
        Collider2D bodyCollider;
        bool climbPosed;
        Vector2 lastWorldPosition;
        Vector2 smoothedVelocity;
        // The clip on screen and where it is: `phase` is in frames for a clip played by time, in units travelled for one
        // played by distance (CatClip.ByDistance). It is kept while the speed changes, so a loop never restarts.
        CatClip current;
        float phase;
        int shownFacing = 1;
        int missingClipWarnings;

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
            restLocalRotation = transform.localRotation;
            foreach (Collider2D c in motor.GetComponents<Collider2D>()) if (!c.isTrigger) { bodyCollider = c; break; }

            if (gravity == null)
            {
                Debug.LogError(
                    $"CatVisualPresenter on '{gameObject.name}' found no GravityReceiver on its assigned motor. Disabling.",
                    this);
                enabled = false;
                return;
            }

            CatClip turn = clips.ForState(CatAnimState.Turn);
            stateMachine = new CatAnimStateMachine(new CatAnimSettings(
                config.RiseExit,
                config.FallEnter,
                config.WalkEnter,
                config.WalkExit,
                config.LandDuration,
                config.RunEnterSpeed,
                config.RunExitSpeed,
                turn != null ? turn.Duration : 0f,
                config.FlipHysteresis,
                config.SnapAcceleration,
                config.MinStateFrames));
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
        /// <summary>The clip whose frame is on screen, by name (Climb shows "Walk" or "Idle" until its sheet is wired).</summary>
        public string ClipName { get; private set; } = "";
        /// <summary>The index of the frame on screen within <see cref="ClipName"/>.</summary>
        public int FrameIndex { get; private set; }
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

            CatAnimState next = teleported
                ? stateMachine.State
                : stateMachine.Step(new CatAnimInput(motor.IsGrounded, motor.IsClimbing, rawAlong, rawVelocityAlongGravity, dt,
                    motor.JumpedThisStep, down.y > 0f ? 1f : -1f));
            ApplyFacing(stateMachine.Facing);

            UpdateEchoAlpha();
            ApplyClimbPose(next == CatAnimState.Climb, down);
            // PAX-087 (D-089): until the climbing item wires the climb sheet, Climb shows the Walk frames while the cat moves
            // on the vine (at the vertical speed) and the Idle frame while it hangs still, turned by the climb pose.
            if (next == CatAnimState.Climb)
            {
                bool moving = Mathf.Abs(rawVelocityAlongGravity) > config.WalkEnter;
                CatAnimState shown = moving ? CatAnimState.Walk : CatAnimState.Idle;
                UpdateSprite(shown, clips.ForState(shown), moving ? Mathf.Abs(rawVelocityAlongGravity) : 0f, 0f, dt, byDistance: false);
            }
            else UpdateSprite(next, clips.ForState(next), Mathf.Abs(rawAlong), travelled, dt, byDistance: true);
            shownFacing = Facing;
        }

        /// <summary>PAX-087 (D-089): the climb pose. Turns the visual by the returned angle so the head points screen-up
        /// for either facing and either gravity, and places it so the sprite's own centre (`spriteCentre`, in the visual's
        /// unscaled local space, e.g. the current frame's bounds centre) lands on the collider's centre. `facing` is the
        /// sign of the visual's localScale.x; positions are in the cat's local space.</summary>
        public static float ClimbPose(float facing, bool gravityDown, Vector2 spriteCentre, Vector2 colliderCentre, out Vector2 position)
        {
            float sign = Mathf.Sign(facing);
            float angle = 90f * sign * (gravityDown ? 1f : -1f);
            Vector2 scaled = new(spriteCentre.x * sign, spriteCentre.y);
            position = colliderCentre - (Vector2)(Quaternion.Euler(0f, 0f, angle) * scaled);
            return angle;
        }

        void ApplyClimbPose(bool climbing, Vector2 down)
        {
            if (!climbing)
            {
                if (!climbPosed) return;
                transform.localPosition = restLocalPosition;
                transform.localRotation = restLocalRotation;
                climbPosed = false;
                return;
            }
            Vector2 centre = bodyCollider != null ? bodyCollider.offset : (Vector2)restLocalPosition;
            Vector2 spriteCentre = bodyRenderer.sprite != null ? (Vector2)bodyRenderer.sprite.bounds.center : Vector2.zero;
            if (bodyRenderer.transform != transform) spriteCentre += (Vector2)bodyRenderer.transform.localPosition;
            Vector3 scale = transform.localScale;
            spriteCentre = Vector2.Scale(spriteCentre, new Vector2(Mathf.Abs(scale.x), scale.y));
            float angle = ClimbPose(transform.localScale.x, down.y <= 0f, spriteCentre, centre, out Vector2 position);
            transform.localPosition = new Vector3(position.x, position.y, restLocalPosition.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            climbPosed = true;
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
            float alpha = observer != null && observer.Driver != null && observer.Driver.Kind == InputSourceKind.EchoReplay
                ? config.EchoAlpha
                : 1f;
            Color color = bodyRenderer.color;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                bodyRenderer.color = color;
            }
            if (outlineRenderer != null)
            {
                Color outlineColor = outlineRenderer.color;
                if (!Mathf.Approximately(outlineColor.a, alpha))
                {
                    outlineColor.a = alpha;
                    outlineRenderer.color = outlineColor;
                }
            }
        }

        // One frame of `clip`. Entering a clip picks its first frame: a loop starts on the frame whose pose is closest to the
        // one on screen (no pose pop, e.g. Walk from Idle's stance, Run from Walk); a one-shot starts at 0. A clip with
        // strides is played by distance (`travelled`, units along the surface): each frame lasts its own stride, so a
        // planted paw stays put at any speed. Otherwise by time: at the clip's fps, or for Walk without strides at the
        // speed-scaled fps (FlipbookMath.FpsForSpeed). The phase carries across speed changes.
        void UpdateSprite(CatAnimState state, CatClip clip, float speed, float travelled, float dt, bool byDistance)
        {
            if (clip == null || clip.Count == 0)
            {
                WarnMissingClipOnce(state);
                return;
            }

            bool distance = byDistance && clip.ByDistance;
            int index;
            if (clip != current)
            {
                index = EntryFrame(clip, distance);
                current = clip;
                phase = distance ? clip.DistanceAtFrame(index) : index;
            }
            else if (distance)
            {
                phase += travelled;
                index = clip.FrameAtDistance(phase);
            }
            else
            {
                float fps = clip.State == CatAnimState.Walk
                    ? FlipbookMath.FpsForSpeed(speed, config.ReferenceSpeed, clip.Fps, config.MinFps, config.MaxFps)
                    : clip.Fps;
                phase += (dt > 0f ? dt : 0f) * fps;
                index = FrameAt(phase, clip.Count, clip.Loop);
            }

            Sprite sprite = clip.Frames[index];
            if (sprite == null)
            {
                WarnMissingClipOnce(state);
                return;
            }

            bodyRenderer.sprite = sprite;
            if (outlineRenderer != null) outlineRenderer.sprite = sprite;
            ClipName = clip.Slot;
            FrameIndex = index;
        }

        // A one-shot starts at 0. A loop starts on its entry frame closest to the pose on screen; braking into a clip played by
        // distance (a digital stop from a run), on the frame from which the remaining travel ends on a stance frame, so the
        // cat stops with its paws where Idle puts them. The remaining travel on screen is how far the drawn root trails the
        // body (interpolation) plus the body's braking distance at the motor's deceleration (0 against a wall). Reads only.
        int EntryFrame(CatClip clip, bool distance)
        {
            if (!clip.Loop || current == null) return 0;
            Vector2 pose = current.PoseCentroid(FrameIndex);
            if (distance && stateMachine.Braking && body != null && config.BrakeDeceleration > 0f)
            {
                Vector2 down = gravity.Direction;
                float bodySpeed = Mathf.Abs(GravityFrame.Along(body.linearVelocity, down));
                float lag = Mathf.Abs(GravityFrame.Along(body.position - (Vector2)motionRoot.position, down));
                return clip.StanceEntry(lag + BrakingDistance(bodySpeed, config.BrakeDeceleration), pose, shownFacing, Facing);
            }
            return clip.ClosestPose(pose, shownFacing, Facing);
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

        static int FrameAt(float framePhase, int frameCount, bool loop)
        {
            if (frameCount <= 1) return 0;
            int frame = Mathf.FloorToInt(framePhase);
            if (!loop) return Mathf.Clamp(frame, 0, frameCount - 1);
            frame %= frameCount;
            return frame < 0 ? frame + frameCount : frame;
        }

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
