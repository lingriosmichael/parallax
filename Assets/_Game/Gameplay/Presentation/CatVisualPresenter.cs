using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    [System.Serializable]
    public sealed class CatAnimationClip
    {
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField] float fps;
        [SerializeField] bool loop;

        public CatAnimationClip(float fps, bool loop)
        {
            this.fps = fps;
            this.loop = loop;
        }

        public Sprite[] Frames => frames;
        public float Fps => fps;
        public bool Loop => loop;
    }

    // Purely cosmetic: reads its own transform's motion and the local GravityReceiver,
    // never writes to anything outside Visual, never networked, never read by gameplay.
    [DisallowMultipleComponent]
    public sealed class CatVisualPresenter : MonoBehaviour
    {
        [SerializeField] CatVisualConfig config;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteRenderer outlineRenderer;
        [SerializeField] CatAnimationClip idleClip = new CatAnimationClip(7f, true);
        [SerializeField] CatAnimationClip walkClip = new CatAnimationClip(10f, true);
        [SerializeField] CatAnimationClip riseClip = new CatAnimationClip(12f, false);
        [SerializeField] CatAnimationClip fallClip = new CatAnimationClip(12f, false);
        [SerializeField] CatAnimationClip landClip = new CatAnimationClip(12f, false);
        [SerializeField] CatMotor2D motor;
        [SerializeField] ObserverContext observer;

        GravityReceiver gravity;
        RealityRoot reality;
        Transform motionRoot;
        CatAnimStateMachine stateMachine;

        bool initialized;
        // PAX-087 (D-089): the climb pose turns this transform; its authored pose is restored on leaving Climb.
        Vector3 restLocalPosition;
        Quaternion restLocalRotation;
        Collider2D bodyCollider;
        bool climbPosed;
        Vector2 lastWorldPosition;
        Vector2 smoothedVelocity;
        float clipClock;
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

            stateMachine = new CatAnimStateMachine(
                config.RiseExit,
                config.FallEnter,
                config.WalkEnter,
                config.WalkExit,
                config.LandDuration);

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

        void LateUpdate()
        {
            if (!bodyRenderer.isVisible)
            {
                initialized = false;
                smoothedVelocity = Vector2.zero;
                return;
            }

            Vector2 currentPosition = motionRoot.position;
            if (!initialized)
            {
                lastWorldPosition = currentPosition;
                initialized = true;
            }

            float dt = Time.deltaTime;
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
                clipClock = 0f;
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
            float smoothedAlong = GravityFrame.Along(smoothedVelocity, down);

            if (!teleported) UpdateFacing(smoothedAlong);

            CatAnimState previous = stateMachine.State;
            CatAnimState next = teleported
                ? stateMachine.State
                : stateMachine.Step(motor.IsClimbing, motor.IsGrounded, rawAlong, rawVelocityAlongGravity, dt);
            if (next != previous) clipClock = 0f;

            UpdateEchoAlpha();
            ApplyClimbPose(next == CatAnimState.Climb, down);
            // PAX-087 (D-089): until the art ticket's climb sheet, Climb shows the Walk frames while the cat moves on the
            // vine (at the vertical speed) and the Idle frame while it hangs still, turned by the climb pose.
            if (next == CatAnimState.Climb)
            {
                bool moving = Mathf.Abs(rawVelocityAlongGravity) > config.WalkEnter;
                UpdateSprite(moving ? CatAnimState.Walk : CatAnimState.Idle, rawVelocityAlongGravity, dt);
            }
            else UpdateSprite(next, rawAlong, dt);
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

        void UpdateFacing(float along)
        {
            bool currentFacingRight = transform.localScale.x >= 0f;
            bool shouldFlip = Mathf.Abs(along) > config.FlipHysteresis
                && (along > 0f) != currentFacingRight;
            if (!shouldFlip) return;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (along > 0f ? 1f : -1f);
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

        void UpdateSprite(CatAnimState state, float rawAlong, float dt)
        {
            CatAnimationClip clip = ClipFor(state);
            if (clip == null || clip.Frames == null || clip.Frames.Length == 0)
            {
                WarnMissingClipOnce(state);
                return;
            }

            float fps = state == CatAnimState.Walk
                ? FlipbookMath.FpsForSpeed(
                    rawAlong,
                    config.ReferenceSpeed,
                    clip.Fps,
                    config.MinFps,
                    config.MaxFps)
                : clip.Fps;
            int index = FrameIndex(clipClock, fps, clip.Frames.Length, clip.Loop);
            Sprite sprite = clip.Frames[index];
            if (sprite == null)
            {
                WarnMissingClipOnce(state);
                return;
            }

            bodyRenderer.sprite = sprite;
            if (outlineRenderer != null) outlineRenderer.sprite = sprite;
            clipClock += dt > 0f ? dt : 0f;
        }

        CatAnimationClip ClipFor(CatAnimState state)
        {
            switch (state)
            {
                case CatAnimState.Walk: return walkClip;
                case CatAnimState.Rise: return riseClip;
                case CatAnimState.Fall: return fallClip;
                case CatAnimState.Land: return landClip;
                // PAX-087 (D-089): Climb shows the Idle frames until the art ticket delivers a climb sheet.
                default: return idleClip;
            }
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

        static int FrameIndex(float elapsed, float fps, int frameCount, bool loop)
        {
            if (frameCount <= 1 || fps <= 0f) return 0;

            int frame = Mathf.FloorToInt(elapsed * fps);
            return loop ? frame % frameCount : Mathf.Min(frame, frameCount - 1);
        }
    }
}
