using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V06 / P5.2: the living air. A few leaves (Golden Ruins, more in The Overgrown Quarter) or cold motes (Obsidian Void,
    /// rising) drift through the camera's view, swaying. They draw at the top of the background band, so they are always behind the
    /// play layer: never over a hazard, a lane or the cat's path. Each keeps to the view (it wraps a little outside it), so the air is
    /// never bunched at one edge. Presentation only, on render time (it freezes with the pause); Reduce motion hides it. The level
    /// build bakes the world and the count (PARALLAX/Setup/Game Feel, LevelSetup).</summary>
    [DisallowMultipleComponent]
    public sealed class LevelAir : MonoBehaviour
    {
        const float Margin = 1f;   // world units past the view's edges where a piece wraps, out of sight
        const int Order = 100;     // the top of the background band, under every gameplay sprite

        [SerializeField] FeelConfig config;
        [SerializeField] Camera view;
        [SerializeField] bool obsidian;
        [SerializeField] int count;

        struct Bit
        {
            public SpriteRenderer Renderer;
            public Vector2 At;
            public float Phase, Speed;
        }

        Bit[] bits;
        float time;

        void OnEnable()
        {
            if (config == null || view == null)
            {
                Debug.LogError("LevelAir: FeelConfig or the camera is missing; the level's air stays still. Run PARALLAX/Setup/Game Feel (PAX-V06).", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (bits == null) return;
            foreach (Bit b in bits) if (b.Renderer != null) b.Renderer.enabled = false;
        }

        static float Spread(int i, float k) => Mathf.Repeat(i * 0.6180339f + k, 1f);   // evenly spread, no randomness

        Rect ViewRect()
        {
            float h = view.orthographicSize * 2f, w = h * view.aspect;
            Vector2 c = view.transform.position;
            return new Rect(c.x - w * 0.5f - Margin, c.y - h * 0.5f - Margin, w + Margin * 2f, h + Margin * 2f);
        }

        void Build()
        {
            Sprite[] leaves = config.Leaves;
            bool usable = obsidian ? config.Mote != null : leaves != null && leaves.Length > 0;
            bits = new Bit[usable ? Mathf.Max(0, count) : 0];
            Rect r = ViewRect();
            string layer = RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Middle);
            for (int i = 0; i < bits.Length; i++)
            {
                var go = new GameObject("Air_" + i);
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = obsidian ? config.Mote : leaves[i % leaves.Length];
                renderer.sortingLayerName = layer;
                renderer.sortingOrder = Order;
                renderer.color = obsidian ? config.MoteTint : config.LeafTint;
                float size = (obsidian ? config.MoteSize : config.LeafSize) * (0.7f + 0.6f * Spread(i, 0.3f));
                float scale = size / Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                bits[i] = new Bit { Renderer = renderer, At = new Vector2(r.xMin + r.width * Spread(i, 0.1f), r.yMin + r.height * Spread(i * 3, 0.7f)), Phase = Spread(i, 0.5f), Speed = 0.6f + 0.8f * Spread(i * 7, 0.2f) };
            }
        }

        void Update()
        {
            if (bits == null) Build();
            bool calm = LevelProgressStore.ReduceMotion;
            float dt = Time.deltaTime;
            time += dt;
            Rect r = ViewRect();
            float tau = 2f * Mathf.PI;
            for (int i = 0; i < bits.Length; i++)
            {
                ref Bit b = ref bits[i];
                if (b.Renderer == null) continue;
                b.Renderer.enabled = !calm;
                if (calm) continue;
                // Leaves fall; motes rise, at half the pace.
                b.At.y -= (obsidian ? -0.5f : 1f) * config.AirFall * b.Speed * dt;
                b.At.x = r.xMin + Mathf.Repeat(b.At.x - r.xMin, r.width);
                b.At.y = r.yMin + Mathf.Repeat(b.At.y - r.yMin, r.height);
                float swing = Mathf.Sin((time * config.AirSwayFrequency * b.Speed + b.Phase) * tau);
                Transform t = b.Renderer.transform;
                t.position = new Vector3(b.At.x + swing * config.AirSway * (obsidian ? 0.4f : 1f), b.At.y, transform.position.z);
                if (!obsidian) t.rotation = Quaternion.Euler(0f, 0f, swing * 50f + b.Phase * 360f);
                else
                {
                    Color c = config.MoteTint;
                    c.a *= 0.6f + 0.4f * Mathf.Sin((time * 0.7f + b.Phase) * tau);   // a slow twinkle
                    b.Renderer.color = c;
                }
            }
        }
    }
}
