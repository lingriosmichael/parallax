using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A13: the trap kit's sprites, material and counts. The level builder reads it (TrapArtSetup) and copies
    /// what each trap's art needs onto that art; with no config, levels build with the grey-box only. Written by
    /// PARALLAX/Art/Import Trap Kit from the post-processed sprites (Tools/Art/trap_process.py).</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Trap Art Config")]
    public sealed class TrapArtConfig : ScriptableObject
    {
        [Tooltip("Parallax/2D/Sprite-Lit-Flip: lit like URP's sprites, with normal maps that turn with a flipped sprite.")]
        public Material TrapMaterial;
        [Tooltip("Parallax/2D/Sprite-Lit-WorldTile: a host skin sampled in world space (§12 R8).")]
        public Material WorldTileMaterial;
        [Header("Arrow (TRAP-01, TRAP-02)")]
        public Sprite ArrowLauncher;
        public Sprite Arrow;
        public Sprite Glint;
        [Header("Geyser (TRAP-03, T04)")]
        public Sprite GeyserVent;
        public Sprite[] GeyserColumnFrames = new Sprite[0];
        [Header("Effects (T06)")]
        public Sprite[] Dust = new Sprite[0];
        public Sprite[] Spray = new Sprite[0];
        [Header("Bodies, TRAP-04 onward (painted in ChatGPT; code-drawn placeholders until saved)")]
        public Sprite SpikeStrip;
        public Sprite SpearHead, SpearShaft;
        public Sprite InverterOrb, CueRing, CueMark;
        public Sprite GlyphRing;
        public Sprite StormCloud;
        public Sprite Scorch, BlockCrack;
        public Sprite[] Steam = new Sprite[0];
        public Sprite[] Leaves = new Sprite[0];
        [Header("Code effects")]
        public Sprite BoltSegment, Flash, Ring, Mote;
        [Tooltip("Sprites that are still code-drawn placeholders for a ChatGPT body (the contact sheet marks them PLACEHOLDER).")]
        public string[] Placeholders = new string[0];
        [Tooltip("The kit's defaults version; PARALLAX/Art/Import Trap Kit applies new count defaults once when it rises.")]
        public int KitVersion;
        [Header("Size (D-101)")]
        [Tooltip("Trap bodies draw this much bigger than their grey-box, around it (art only; hitboxes unchanged). Disguised floors stay exact.")]
        [Min(1f)] public float BodyScale = 1.1f;
        [Tooltip("Gauntlet (the developer: \"spikes need to be longer\"): spikes draw this many times their grey-box's height, from their base outward (art only; the hitbox is unchanged).")]
        [Min(1f)] public float SpikeHeightScale = 1.8f;
        [Tooltip("The teeth in one tile of the spike strip (TRAP-04: equal cells, Tools/Art/trap_bodies.py). Spikes draw a whole number of them, so no end tooth is cut.")]
        [Min(1)] public int SpikeStripTeeth = 11;
        [Header("Counts")]
        [Min(0)] public int ShardRows = 2;
        [Min(0)] public int DustPerFloor = 4;
        [Min(0)] public int ImpactPuffs = 2;
        [Min(0)] public int TellDrops = 3;
        [Min(0)] public int BurstPuffs = 4;
        [Min(0)] public int SprayPuffs = 8;
        [Min(0)] public int SteamWisps = 3;
        [Min(0)] public int GritPuffs = 4;
        [Min(0)] public int SolidDust = 4;
        [Min(0)] public int FlipMotes = 4;
        [Min(0)] public int BoltSegments = 9;
        [Min(0)] public int VineLeaves = 6;
        [Min(0)] public int DoorDust = 4;
        [Min(0)] public int DeathPuffs = 8;

        public bool HasArrow => ArrowLauncher != null && Arrow != null;
        public bool HasGeyser => GeyserVent != null && GeyserColumnFrames.Length > 0;
        public bool IsPlaceholder(Sprite s) => s != null && System.Array.IndexOf(Placeholders, s.name) >= 0;
    }
}
