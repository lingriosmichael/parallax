using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-108 (D-157): the title's cat. Loops its idle frames and, every FidgetEvery seconds, plays the look-around
    /// once (the same sheets the player cat uses; no Animator, as the cat's art rules require).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TitleCatIdle : MonoBehaviour
    {
        [SerializeField] TitleScreenConfig config;
        [SerializeField] Sprite[] idle = new Sprite[0];
        [SerializeField] Sprite[] fidget = new Sprite[0];
        [Tooltip("The rim drawn behind the cat (the player cat's outline material); it shows the same frame.")]
        [SerializeField] SpriteRenderer outline;

        SpriteRenderer spriteRenderer;
        float clock;
        float fidgetClock = -1f;

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (config == null || idle == null || idle.Length == 0) return;
            float dt = Time.unscaledDeltaTime;
            clock += dt;

            if (fidgetClock >= 0f)
            {
                fidgetClock += dt;
                int f = Mathf.FloorToInt(fidgetClock * config.FidgetFps);
                if (f < fidget.Length) { Show(fidget[f]); return; }
                fidgetClock = -1f;
                clock = 0f;
            }
            else if (fidget != null && fidget.Length > 0 && config.FidgetEvery > 0f && clock >= config.FidgetEvery)
            {
                fidgetClock = 0f;
                Show(fidget[0]);
                return;
            }

            Show(idle[Mathf.FloorToInt(clock * config.IdleFps) % idle.Length]);
        }

        void Show(Sprite frame)
        {
            spriteRenderer.sprite = frame;
            if (outline != null) outline.sprite = frame;
        }
    }
}
