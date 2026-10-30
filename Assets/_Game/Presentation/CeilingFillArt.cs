using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-248 (the developer, of L029's falling ceilings: "the ceiling needs to come down but filled. Why can I see the
    /// background?"): draws the room's stone from a descending ceiling plate's top up to where its top was at home, so the ceiling
    /// reads as one mass pushing down instead of a plate leaving the sky behind it. Presentation only: it reads the plate's transform
    /// every frame (so a room reset or rewind is followed), never the trap's state. Baked by CeilingFillSetup.
    /// D-249 ("Clean up the art"): it draws the whole piece, from its bottom up to its home top, at its x (a plug slides across
    /// first); the piece's own skin and trims are hidden, so the ceiling is one plain block of the room's stone.</summary>
    public sealed class CeilingFillArt : MonoBehaviour
    {
        [SerializeField] Transform plate;
        [SerializeField] SpriteRenderer fill;
        [SerializeField] float width;
        [SerializeField] float halfHeight;

        float homeY;
        bool ready;

        public void Configure(Transform plateTransform, SpriteRenderer fillRenderer, float plateWidth, float plateHalfHeight)
        {
            plate = plateTransform; fill = fillRenderer; width = plateWidth; halfHeight = plateHalfHeight;
        }

        void Start()
        {
            if (plate == null || fill == null)
            {
                Debug.LogError($"CeilingFillArt '{name}': no plate or fill renderer; rebuild the level.", this);
                enabled = false;
                return;
            }
            homeY = plate.position.y;
            ready = true;
        }

        void LateUpdate()
        {
            if (!ready) return;
            Vector3 p = plate.position;
            float top = homeY + halfHeight, bottom = Mathf.Min(p.y, homeY) - halfHeight;
            fill.enabled = true;
            fill.size = new Vector2(width, top - bottom);
            fill.transform.position = new Vector3(p.x, (top + bottom) * .5f, fill.transform.position.z);
        }
    }
}
