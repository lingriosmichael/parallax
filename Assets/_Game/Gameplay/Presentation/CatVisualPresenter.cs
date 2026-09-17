using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    // Purely cosmetic: reads its own transform's motion and the local GravityReceiver,
    // never writes to anything outside Visual, never networked, never read by gameplay.
    [DisallowMultipleComponent]
    public sealed class CatVisualPresenter : MonoBehaviour
    {
        enum State { Idle, Walk, Air }

        [SerializeField] CatVisualConfig config;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteRenderer outlineRenderer;
        [SerializeField] Sprite[] frames;

        GravityReceiver gravity;
        RealityRoot reality;

        bool initialized;
        Vector2 lastWorldPosition;
        float walkClock;
        float walkFps;
        State state = State.Idle;

        void Awake()
        {
            gravity = GetComponentInParent<GravityReceiver>();
            reality = GetComponentInParent<RealityRoot>();

            if (config == null || bodyRenderer == null || frames == null || frames.Length == 0)
            {
                Debug.LogError($"CatVisualPresenter on '{gameObject.name}' is missing its config, body renderer, or frames. Disabling.", this);
                enabled = false;
                return;
            }

            if (gravity == null)
            {
                Debug.LogError($"CatVisualPresenter on '{gameObject.name}' found no GravityReceiver in its parents. Disabling.", this);
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
                return;
            }

            Vector2 currentPosition = transform.position;
            if (!initialized)
            {
                lastWorldPosition = currentPosition;
                initialized = true;
            }

            float dt = Time.deltaTime;
            Vector2 velocity = dt > 0f ? (currentPosition - lastWorldPosition) / dt : Vector2.zero;
            lastWorldPosition = currentPosition;

            Vector2 down = gravity.Direction;
            float along = GravityFrame.Along(velocity, down);
            float upSpeed = GravityFrame.UpSpeed(velocity, down);

            UpdateFacing(along);
            UpdateState(along, upSpeed, dt);
            UpdateSprite();
        }

        void UpdateFacing(float along)
        {
            if (Mathf.Abs(along) <= config.FlipHysteresis) return;

            bool currentFacingRight = transform.localScale.x >= 0f;
            bool desiredFacingRight = along > 0f;
            if (desiredFacingRight == currentFacingRight) return;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (desiredFacingRight ? 1f : -1f);
            transform.localScale = scale;
        }

        void UpdateState(float along, float upSpeed, float dt)
        {
            if (Mathf.Abs(upSpeed) > config.AirThreshold)
            {
                state = State.Air;
                return;
            }

            if (Mathf.Abs(along) > config.IdleSpeedThreshold)
            {
                if (state != State.Walk) walkClock = 0f;
                state = State.Walk;
                walkFps = FlipbookMath.FpsForSpeed(along, config.ReferenceSpeed, config.WalkFps, config.MinFps, config.MaxFps);
                walkClock += dt;
                return;
            }

            state = State.Idle;
        }

        void UpdateSprite()
        {
            int index;
            switch (state)
            {
                case State.Air:
                    index = config.AirFrame;
                    break;
                case State.Walk:
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
