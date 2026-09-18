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
        [SerializeField] Sprite[] frames;
        [SerializeField] ObserverContext observer;

        GravityReceiver gravity;
        RealityRoot reality;
        Transform motionRoot;

        bool initialized;
        Vector2 lastWorldPosition;
        Vector2 smoothedVelocity;
        float walkClock;
        float walkFps;
        float belowIdleTime;
        CatVisualState state = CatVisualState.Idle;

        void Awake()
        {
            gravity = GetComponentInParent<GravityReceiver>();
            reality = GetComponentInParent<RealityRoot>();
            var body = GetComponentInParent<Rigidbody2D>();
            motionRoot = body != null ? body.transform : null;

            if (config == null || bodyRenderer == null || frames == null || frames.Length == 0)
            {
                Debug.LogError($"CatVisualPresenter on '{gameObject.name}' is missing its config, body renderer, or frames. Disabling.", this);
                enabled = false;
                return;
            }

            if (gravity == null || motionRoot == null)
            {
                Debug.LogError($"CatVisualPresenter on '{gameObject.name}' found no GravityReceiver or Rigidbody2D in its parents. Disabling.", this);
                enabled = false;
                return;
            }

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
            if (teleported)
            {
                smoothedVelocity = Vector2.zero;
            }
            else
            {
                Vector2 measuredVelocity = dt > 0f ? delta / dt : Vector2.zero;
                float smoothing = config.VelocitySmoothingTime > 0f
                    ? 1f - Mathf.Exp(-dt / config.VelocitySmoothingTime)
                    : 1f;
                smoothedVelocity = Vector2.Lerp(smoothedVelocity, measuredVelocity, smoothing);
            }

            Vector2 down = gravity.Direction;
            float along = GravityFrame.Along(smoothedVelocity, down);
            float upSpeed = GravityFrame.UpSpeed(smoothedVelocity, down);

            if (!teleported) UpdateFacing(along);
            UpdateState(along, upSpeed, dt, teleported);
            UpdateEchoAlpha();
            UpdateSprite();
        }

        void UpdateFacing(float along)
        {
            bool currentFacingRight = transform.localScale.x >= 0f;
            if (!CatVisualStateLogic.ShouldFlip(along, config.FlipHysteresis, currentFacingRight)) return;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (along > 0f ? 1f : -1f);
            transform.localScale = scale;
        }

        void UpdateState(float along, float upSpeed, float dt, bool teleported)
        {
            CatVisualState next = CatVisualStateLogic.Select(
                state, along, upSpeed, dt, config.IdleSpeedThreshold, config.AirThreshold,
                config.IdleDwell, teleported, ref belowIdleTime);
            if (next == CatVisualState.Walk)
            {
                if (state != CatVisualState.Walk) walkClock = 0f;
                walkFps = FlipbookMath.FpsForSpeed(along, config.ReferenceSpeed, config.WalkFps, config.MinFps, config.MaxFps);
                walkClock += dt;
            }
            state = next;
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

        void UpdateSprite()
        {
            int index;
            switch (state)
            {
                case CatVisualState.Air:
                    index = config.AirFrame;
                    break;
                case CatVisualState.Walk:
                    index = FlipbookMath.FrameIndex(walkClock, walkFps, config.WalkLoopStart, config.WalkLoopEnd);
                    break;
                default:
                    index = config.IdleFrame;
                    break;
            }

            index = Mathf.Clamp(index, 0, frames.Length - 1);
            Sprite sprite = frames[index];

            bodyRenderer.sprite = sprite;
            if (outlineRenderer != null) outlineRenderer.sprite = sprite;
        }
    }
}
