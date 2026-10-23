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
        [Tooltip("D-162: the thermal-vent steam for an upward geyser, one sprite per tick (Tools/Art/geyser_steam.py).")]
        public GeyserSteamFrames GeyserSteamUp = new();
        [Tooltip("D-162: the same for a vent in a ceiling: its jet fires down and its steam rises back.")]
        public GeyserSteamFrames GeyserSteamDown = new();
        [Header("Effects (T06)")]
        public Sprite[] Dust = new Sprite[0];
        public Sprite[] Spray = new Sprite[0];
        [Header("Bodies, TRAP-04 onward (painted in ChatGPT; code-drawn placeholders until saved)")]
        public Sprite SpikeStrip;
        [Tooltip("PAX-A18 (D-108 amendment): the launcher's mouth (its lethal part), drawn over its frame on the same canvas.")]
        public Sprite LauncherMouth;
        [Tooltip("PAX-A18 (D-151): the painted spike blades (SPIKE_blade_*), laid out one by one at one fixed size (SpikeLayout); empty = the tiled strip.")]
        public Sprite[] SpikeBlades = new Sprite[0];
        [Tooltip("D-161: the stout steel blades (SPIKE_steel_*, Tools/Art/spike_steel.py), for levels whose look asks for them (LevelLooks.Look.StoutSpikes).")]
        public Sprite[] SpikeBladesStout = new Sprite[0];
        [Tooltip("D-161: how much wider (x) and taller (y) the stout blades draw than the standard ones (art only; hitboxes unchanged).")]
        public Vector2 StoutBladeScale = new(1.6f, 1.25f);
        [Tooltip("PAX-A18: a slam's iron striking plate, repeated along its bottom edge; and a hinge floor's knuckle at its pivot.")]
        public Sprite SlamPlate, HingeKnuckle;
        [Tooltip("PAX-A18: cracks growing on a crumbling floor (small to shattering) and its broken edge.")]
        public Sprite[] Cracks = new Sprite[0];
        public Sprite CrumbleEdge;
        public Sprite SpearHead, SpearShaft;
        [Tooltip("2026-10-17: the whole painted spear at its one size (TRAP-05_spear).")]
        public Sprite SpearWhole;
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
        public bool HasGeyser => GeyserVent != null && GeyserSteamUp.Complete && GeyserSteamDown.Complete;
        public bool IsPlaceholder(Sprite s) => s != null && System.Array.IndexOf(Placeholders, s.name) >= 0;
        public GeyserSteamFrames GeyserSteam(Parallax.Core.GeyserDirection direction) => direction == Parallax.Core.GeyserDirection.Down ? GeyserSteamDown : GeyserSteamUp;
    }

    /// <summary>D-162: one direction's geyser steam, a sprite per tick, each pivoted on the vent's mouth: the tell's wisps
    /// (from the fire), the jet inside the launch column (the column's body, 1.1 x 1.5 u) and the plume round it (both from the
    /// eruption's start), and the steam dying away (from the eruption's end).</summary>
    [System.Serializable]
    public sealed class GeyserSteamFrames
    {
        public Sprite[] Tell = new Sprite[0];
        public Sprite[] Core = new Sprite[0];
        public Sprite[] Plume = new Sprite[0];
        public Sprite[] Fade = new Sprite[0];

        public bool Complete => Tell.Length > 0 && Core.Length > 0 && Plume.Length == Core.Length && Fade.Length > 0;
    }
}
