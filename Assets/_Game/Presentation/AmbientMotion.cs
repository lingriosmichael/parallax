using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A16 §3.5: decor motion. Drift (wrapping within a tile so a tiled band never shows its end), bob, sway and
    /// an alpha breath, on render time accumulated from Time.deltaTime, so it freezes with the pause (RunningState sets the
    /// time scale). Presentation only: never on anything with a collider, never read by gameplay or ticks.</summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        [Tooltip("Sideways drift, units per second (local x).")]
        [SerializeField] float driftSpeed;
        [Tooltip("The drift wraps within this width (a band's tile width); 0 never wraps.")]
        [SerializeField] float wrapWidth;
        [SerializeField] float bobAmplitude;
        [SerializeField] float bobFrequency = 0.12f;
        [SerializeField] float swayDegrees;
        [SerializeField] float swayFrequency = 0.2f;
        [Tooltip("Alpha breath, as a fraction of the renderer's alpha (0.03 = ±3%).")]
        [SerializeField] float breathe;
        [SerializeField] float breatheFrequency = 0.125f;
        [Tooltip("Phase, 0..1, so neighbours don't move in step.")]
        [SerializeField] float phase;

        Vector3 basePosition;
        Quaternion baseRotation;
        SpriteRenderer sprite;
        float baseAlpha, time;
        bool started;

        public float DriftSpeed => driftSpeed;

        public void Configure(float drift, float wrap, float bob, float bobHz, float sway, float swayHz, float breath, float breathHz, float startPhase)
        {
            driftSpeed = drift; wrapWidth = wrap; bobAmplitude = bob; bobFrequency = bobHz;
            swayDegrees = sway; swayFrequency = swayHz; breathe = breath; breatheFrequency = breathHz; phase = startPhase;
        }

        void Awake()
        {
            started = true;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) baseAlpha = sprite.color.a;
        }

        void Update() => Step(Time.deltaTime);

        /// <summary>Advances the motion by `dt` seconds of render time (0 while paused) and applies it.</summary>
        public void Step(float dt)
        {
            if (!started) Awake();   // edit-mode callers (tests) step it without Awake
            time += dt;
            float tau = 2f * Mathf.PI;
            Vector3 p = basePosition;
            if (driftSpeed != 0f) p.x += wrapWidth > 0f ? Mathf.Repeat(driftSpeed * time, wrapWidth) - (driftSpeed > 0f ? wrapWidth : 0f) : driftSpeed * time;
            if (bobAmplitude != 0f) p.y += bobAmplitude * Mathf.Sin(tau * (bobFrequency * time + phase));
            transform.localPosition = p;
            if (swayDegrees != 0f) transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, swayDegrees * Mathf.Sin(tau * (swayFrequency * time + phase)));
            if (breathe != 0f && sprite != null)
            {
                Color c = sprite.color;
                c.a = baseAlpha * (1f + breathe * Mathf.Sin(tau * (breatheFrequency * time + phase)));
                sprite.color = c;
            }
        }
    }
}
