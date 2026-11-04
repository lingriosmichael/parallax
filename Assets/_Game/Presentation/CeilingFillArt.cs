using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-248 (the developer, of L029's falling ceilings: "the ceiling needs to come down but filled. Why can I see the
    /// background?"): draws the room's stone from a descending ceiling plate's top up to where its top was at home, so the ceiling
    /// reads as one mass pushing down instead of a plate leaving the sky behind it. Presentation only: it reads the plate's transform
    /// every frame (so a room reset or rewind is followed), never the trap's state. Baked by CeilingFillSetup.
    /// D-249 ("Clean up the art"): it draws the whole piece, from its bottom up to its home top, at its x (a plug slides across
    /// first); the piece's own skin and trims are hidden, so the ceiling is one plain block of the room's stone.
    /// D-308 (the developer: "yes ceilings", a redraw with real stone): the block's open ends take broken-stone strips that grow with it.</summary>
    public sealed class CeilingFillArt : MonoBehaviour
    {
        [SerializeField] Transform plate;
        [SerializeField] SpriteRenderer fill;
        [SerializeField] float width;
        [SerializeField] float halfHeight;
        [Tooltip("D-300: its broken underside, the edge every slab has; it follows the block's bottom.")]
        [SerializeField] SpriteRenderer lip;
        [SerializeField] float lipOffset;
        [Tooltip("D-308: its broken sides, the strips every face has; they grow with the block, down to its lip.")]
        [SerializeField] SpriteRenderer[] sides = new SpriteRenderer[0];
        [SerializeField] float[] sideOffsets = new float[0];
        [SerializeField] float sideBelow;
        [Tooltip("D-308: the block's and lip's centre, off the plate's (its ends pulled in under the strips).")]
        [SerializeField] float shift;
        [Tooltip("D-308: a plug grows this far at each end as it slides out of its home, under the broken ends of the pieces it closes.")]
        [SerializeField] float slideGrow;

        float homeX, homeY, lipWidth;
        bool ready;

        public void Configure(Transform plateTransform, SpriteRenderer fillRenderer, float plateWidth, float plateHalfHeight)
        {
            plate = plateTransform; fill = fillRenderer; width = plateWidth; halfHeight = plateHalfHeight;
        }

        public void ConfigureLip(SpriteRenderer lipRenderer, float offset) { lip = lipRenderer; lipOffset = offset; }

        /// <summary>D-308: side strips at x offsets from the plate's centre, running from the home top to `below` under the block; the
        /// block (and its lip) `blockWidth` wide, `blockShift` off the plate's centre, so its straight ends sit under the strips.</summary>
        public void ConfigureSides(SpriteRenderer[] renderers, float[] offsets, float below, float blockWidth, float blockShift)
        {
            sides = renderers; sideOffsets = offsets; sideBelow = below; width = blockWidth; shift = blockShift;
        }

        public void ConfigureSlideGrow(float grow) { slideGrow = grow; }

        void Start()
        {
            if (plate == null || fill == null)
            {
                Debug.LogError($"CeilingFillArt '{name}': no plate or fill renderer; rebuild the level.", this);
                enabled = false;
                return;
            }
            homeX = plate.position.x; homeY = plate.position.y;
            if (lip != null) lipWidth = lip.size.x;
            ready = true;
        }

        void LateUpdate()
        {
            if (!ready) return;
            Vector3 p = plate.position;
            float top = homeY + halfHeight, bottom = Mathf.Min(p.y, homeY) - halfHeight;
            fill.enabled = true;
            float grow = Mathf.Min(slideGrow, Mathf.Abs(p.x - homeX)) * 2f;
            fill.size = new Vector2(width + grow, top - bottom);
            if (lip != null && slideGrow > 0f) lip.size = new Vector2(lipWidth + grow, lip.size.y);
            fill.transform.position = new Vector3(p.x + shift, (top + bottom) * .5f, fill.transform.position.z);
            if (lip != null) lip.transform.position = new Vector3(p.x + shift, bottom + lipOffset, lip.transform.position.z);
            for (int i = 0; i < sides.Length && i < sideOffsets.Length; i++)
            {
                SpriteRenderer side = sides[i];
                if (side == null) continue;
                float foot = bottom - sideBelow;
                side.size = new Vector2(side.size.x, top - foot);
                side.transform.position = new Vector3(p.x + sideOffsets[i], (top + foot) * .5f, side.transform.position.z);
            }
        }
    }
}
