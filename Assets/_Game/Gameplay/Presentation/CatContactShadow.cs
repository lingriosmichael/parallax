using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>D-294: a soft shadow on the surface under the cat, full on the ground, thinning and fading as the cat rises off it, so
    /// it stands on the stone instead of floating. Hidden on a vine or a wall. Reads the motor and the physics scene, writes only its
    /// own renderer.</summary>
    public sealed class CatContactShadow : MonoBehaviour
    {
        [SerializeField] CatMotor2D motor;
        [SerializeField] SpriteRenderer shadow;
        [SerializeField, Min(0.1f)] float reach = 1.6f;
        [SerializeField, Min(0.01f)] float width = 1.2f, height = 0.28f;
        [SerializeField, Range(0f, 1f)] float strength = 0.75f;

        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        GravityReceiver gravity;
        Collider2D[] own;
        ContactFilter2D filter;

        void Awake()
        {
            if (motor == null || shadow == null) { enabled = false; return; }
            gravity = motor.GetComponent<GravityReceiver>();
            own = motor.GetComponentsInChildren<Collider2D>(true);
            gameObject.layer = motor.gameObject.layer;   // what its reality's camera draws (the scene setup sets the cat's, after the prefab)
            // Over the stone, which draws above anything behind the cat, on its own reality's layer (the prefab holds A's; the scene
            // setup moves B's cat to B's). Dark on a dark cat, it doesn't show on the paws it touches.
            CatVisualPresenter drawn = motor.GetComponentInChildren<CatVisualPresenter>(true);
            if (drawn != null && drawn.TryGetComponent(out SpriteRenderer body)) { shadow.sortingLayerID = body.sortingLayerID; shadow.sortingOrder = StoneTop; }
            RealityRoot reality = motor.GetComponentInParent<RealityRoot>();
            filter = new ContactFilter2D { useTriggers = false, useLayerMask = reality != null, layerMask = reality != null ? reality.PhysicsMask : default };
        }

        void LateUpdate()
        {
            if (gravity == null) return;
            Vector2 down = gravity.Direction, from = motor.transform.position;
            float distance = float.PositiveInfinity;
            int count = motor.IsClimbing || motor.IsClinging ? 0 : Physics2D.Raycast(from, down, filter, hits, reach + PawDrop);
            for (int i = 0; i < count; i++)
                if (hits[i].distance < distance && System.Array.IndexOf(own, hits[i].collider) < 0) distance = hits[i].distance;
            shadow.enabled = !float.IsInfinity(distance);
            if (!shadow.enabled) return;
            float near = 1f - Mathf.Clamp01((distance - PawDrop) / reach), size = Mathf.Lerp(0.6f, 1f, near);
            Vector2 at = from + down * (distance + Sink);
            transform.SetPositionAndRotation(new Vector3(at.x, at.y, transform.position.z), Quaternion.identity);
            Vector2 native = shadow.sprite.bounds.size;
            transform.localScale = new Vector3(width * size / native.x, height * size / native.y, 1f);
            Color c = shadow.color; c.a = strength * near * near; shadow.color = c;
        }

        // How far the paws hang under the cat's root (the bottom of its collider on Cat_Player), and how far into the surface the
        // shadow's middle lies, so little of it shows over the stone's edge.
        const float PawDrop = 0.4f, Sink = 0.05f;
        // Over the stone's top caps, which draw above the cat's own order.
        const int StoneTop = 6;
    }
}
