using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-V08: the danger colour, crimson, on every lethal thing (arrow heads and fletching, spike tips, the spear's
    /// blade, the launcher's mouth and its tell glow) and on nothing else: background and environment art never use it, and
    /// warm light (braziers, gold) stays amber/yellow. The sprites carry it baked (Tools/Art/hazard_readable.py, DANGER, the
    /// same value); code tints use these.</summary>
    public static class HazardPalette
    {
        /// <summary>The danger colour (sRGB), hue about 352°.</summary>
        public static readonly Color Danger = new(0.80f, 0.06f, 0.14f, 1f);

        /// <summary>A launcher's tint at the height of its tell: the stone flushes crimson (a multiply, so it stays light enough
        /// to read the stone; the mouth itself is crimson in the sprite).</summary>
        public static readonly Color TellTint = new(1f, 0.42f, 0.46f, 1f);

        /// <summary>The tell's glint at the arrow's head: the danger colour, brightened (it's drawn additive-looking over
        /// any background).</summary>
        public static readonly Color TellGlint = new(1f, 0.22f, 0.3f, 1f);
    }
}
