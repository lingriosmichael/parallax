using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>Premium plan Phase 6 (D-314): the results beat's cat, sitting on the rim of the level-complete plate. It loops its idle
    /// frames and now and then looks around (the player cat's own sheets, as a flipbook: no Animator, as the cat's art rules
    /// require), on unscaled time, so it lives while the game behind it is stopped. Presentation only. Written by
    /// PARALLAX/Setup/Level Complete UI.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ResultsCat : MonoBehaviour
    {
        [SerializeField] Sprite[] idle = new Sprite[0];
        [SerializeField] Sprite[] look = new Sprite[0];
        [SerializeField] float fps = 10f;
        [Tooltip("Seconds between two look-arounds.")]
        [SerializeField] float lookEvery = 3.5f;

        Image image;
        float clock, lookClock = -1f;

        void Awake() => image = GetComponent<Image>();

        void OnEnable()
        {
            clock = 0f;
            lookClock = -1f;
            if (image == null) image = GetComponent<Image>();
            if (idle != null && idle.Length > 0) image.sprite = idle[0];
        }

        void Update()
        {
            if (idle == null || idle.Length == 0) return;
            float dt = Time.unscaledDeltaTime;
            clock += dt;
            if (lookClock >= 0f)
            {
                lookClock += dt;
                int f = Mathf.FloorToInt(lookClock * fps);
                if (f < look.Length) { image.sprite = look[f]; return; }
                lookClock = -1f;
                clock = 0f;
            }
            else if (look != null && look.Length > 0 && lookEvery > 0f && clock >= lookEvery)
            {
                lookClock = 0f;
                image.sprite = look[0];
                return;
            }
            image.sprite = idle[Mathf.FloorToInt(clock * fps) % idle.Length];
        }
    }
}
