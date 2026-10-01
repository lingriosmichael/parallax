using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13 (§7, §11 R4/R5): the art for one trap. It lives beside the trap under the room's "Art" root, never
    /// under it, so the route harness (which reads every renderer under an element) never sees it. It reads the trap's
    /// state, the room clock and the trap's grey-box renderers, and never writes game state; the grey-box keeps every value
    /// the harness hashes and only stops drawing (forceRenderingOff, which is not saved and is set here at runtime).
    /// Render is a pure function of that state and the ticks since the trap's events, so a checkpoint rewind, the route
    /// harness and the game all draw the same thing. Bodies mirror a grey-box renderer (visible exactly when it's enabled,
    /// inside its bounds); effects (dust, shards, spray, glints) may extend past the bounds and start no earlier than the
    /// event they show.</summary>
    public abstract class TrapArt : MonoBehaviour
    {
        public readonly struct Body
        {
            public readonly SpriteRenderer Art, Greybox;
            public Body(SpriteRenderer art, SpriteRenderer greybox) { Art = art; Greybox = greybox; }
        }

        public readonly struct Effect
        {
            public readonly string Group;
            public readonly SpriteRenderer Renderer;
            public Effect(string group, SpriteRenderer renderer) { Group = group; Renderer = renderer; }
        }

        [SerializeField] protected RoomManager rooms;
        [Tooltip("The trap's grey-box renderers this art draws instead of.")]
        [SerializeField] SpriteRenderer[] greybox = new SpriteRenderer[0];
        [Tooltip("The name effect variants are seeded from (the element's name).")]
        [SerializeField] protected string seedName;
        [Tooltip("D-101: how much bigger than its grey-box a body draws (art only; colliders unchanged). 1 = exactly the grey-box. Disguised floors stay at 1.")]
        [SerializeField, Min(1f)] protected float bodyScale = 1f;

        readonly List<Body> bodies = new();
        readonly List<Effect> effects = new();

        public abstract RoomTrap Trap { get; }
        /// <summary>False for art that draws without a RoomTrap (static spikes and pits, which are Hazards; the room's death
        /// effects).</summary>
        protected virtual bool NeedsTrap => true;
        protected int RoomTick => rooms != null ? rooms.RoomLifeTick : 0;
        protected string SeedName => seedName;
        public float BodyScale => bodyScale;

        /// <summary>Ticks since the trap's latest fire, or -1 if it hasn't fired (or was reset).</summary>
        protected int TicksSinceFire => Trap == null || Trap.LatestFireTick < 0 ? -1 : RoomTick - Trap.LatestFireTick;

        public IReadOnlyList<Body> Bodies { get { bodies.Clear(); CollectBodies(bodies); return bodies; } }
        public IReadOnlyList<Effect> Effects { get { effects.Clear(); CollectEffects(effects); return effects; } }

        protected abstract void CollectBodies(List<Body> into);
        protected abstract void CollectEffects(List<Effect> into);
        protected abstract void Render();

        protected virtual void Awake() => HideGreybox();

        /// <summary>The grey-box stops drawing (not saved; the harness never reads it).</summary>
        public void HideGreybox()
        {
            foreach (SpriteRenderer r in greybox) if (r != null) r.forceRenderingOff = true;
        }

        void LateUpdate() => Apply();

        /// <summary>Draws this tick's state. The game calls it every frame; tests and the contact sheet call it directly.</summary>
        public void Apply()
        {
            if (NeedsTrap && Trap == null)
            {
                // Its grey-box is already hidden (Awake), so the trap would draw nothing: say so once. Rebuild the level.
                if (!reportedMissingTrap) Debug.LogError($"{GetType().Name} on '{name}': no trap assigned; nothing is drawn for it (rebuild the level).", this);
                reportedMissingTrap = true;
                return;
            }
            Render();
        }

        bool reportedMissingTrap;

        protected static bool Shows(SpriteRenderer r) => r != null && r.enabled && r.gameObject.activeInHierarchy;

        /// <summary>Shows a small effect sprite at a place, scale and opacity, or hides it.</summary>
        protected static void Place(SpriteRenderer r, bool visible, Vector2 at, float scale, float alpha)
        {
            if (r == null) return;
            r.enabled = visible;
            if (!visible) return;
            r.transform.position = new Vector3(at.x, at.y, r.transform.position.z);
            r.transform.localScale = Vector3.one * scale;
            Color c = r.color; c.a = alpha; r.color = c;
        }

        /// <summary>A sprite fitted uniformly inside `box` units (Simple draw mode, set before the scale).</summary>
        protected static void Fit(SpriteRenderer r, Sprite sprite, Vector2 box)
        {
            if (r == null) return;
            if (r.sprite != sprite) r.sprite = sprite;
            r.drawMode = SpriteDrawMode.Simple;
            if (sprite == null) return;
            Vector2 native = sprite.bounds.size;
            float k = Mathf.Min(box.x / Mathf.Max(native.x, 1e-4f), box.y / Mathf.Max(native.y, 1e-4f));
            r.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>Mirrors a body onto its grey-box renderer: shown exactly when the grey-box is, at its place.</summary>
        protected static bool Mirror(SpriteRenderer art, SpriteRenderer source)
        {
            if (art == null) return false;
            bool shown = source != null && source.enabled && source.gameObject.activeInHierarchy;
            art.enabled = shown;
            if (shown)
            {
                Transform a = art.transform, s = source.transform;
                a.position = new Vector3(s.position.x, s.position.y, a.position.z);
                a.rotation = s.rotation;
            }
            return shown;
        }
    }
}
