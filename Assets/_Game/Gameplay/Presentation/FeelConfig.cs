using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V06: how the game's feel beats look and how strong they are, all presentation (no gameplay reads it). One asset
    /// at AssetPath, made by PARALLAX/Setup/Game Feel (PAX-V06), its sprites from the trap kit's effects (Pack I, D-312). Tuned by
    /// the developer's hands: "too much / right / too little" per beat.</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Feel Config")]
    public sealed class FeelConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/FeelConfig.asset";

        [Header("Squash and stretch (the drawing only, never the collider)")]
        [Tooltip("Stretch on take-off, as a share of the cat's drawn height.")]
        [SerializeField] float takeOffStretch = 0.12f;
        [SerializeField] float landSquash = 0.12f;
        [SerializeField] float hardLandSquash = 0.22f;
        [Tooltip("Seconds a squash or stretch takes to settle.")]
        [SerializeField] float squashTime = 0.16f;

        [Header("Dust")]
        [SerializeField] Sprite[] runPuffs = new Sprite[0];
        [SerializeField] Sprite[] landPuffs = new Sprite[0];
        [SerializeField] Sprite[] skidPuffs = new Sprite[0];
        [SerializeField] Sprite[] impactRings = new Sprite[0];
        [SerializeField] Color dustTint = new Color(1f, 0.94f, 0.84f, 0.8f);
        [Tooltip("World units run between two run puffs.")]
        [SerializeField] float runPuffEvery = 1.1f;
        [Tooltip("Seconds between two puffs while sliding down a wall.")]
        [SerializeField] float wallPuffEvery = 0.18f;
        [SerializeField] float puffLife = 0.45f;
        [SerializeField] float impactLife = 0.5f;
        [Tooltip("A puff's drawn size, against the sprite's own.")]
        [SerializeField] float puffScale = 0.6f;
        [SerializeField] int poolSize = 32;

        [Header("Camera shake (translation only, D-020)")]
        [SerializeField] float deathShake = 0.16f;
        [SerializeField] float deathShakeTime = 0.28f;
        [SerializeField] float impactShake = 0.1f;
        [SerializeField] float impactShakeTime = 0.22f;
        [SerializeField] float hardLandShake = 0.05f;
        [SerializeField] float shakeFrequency = 22f;
        [Tooltip("An impact further than this from the cat (world units) shakes nothing and shows no dust.")]
        [SerializeField] float shakeWithin = 9f;
        [Tooltip("A mover that was going at least this fast (u/s) and stops at once is an impact: a falling block, a slam, a crusher.")]
        [SerializeField] float impactSpeed = 6f;

        [Header("Trap weight (P5.3): what a heavy mover throws when it stops at once")]
        [Tooltip("Golden Ruins stone chips, falling with gravity.")]
        [SerializeField] Sprite[] chips = new Sprite[0];
        [Tooltip("Sparks where metal stops (a slam, a piston, a crusher, a press).")]
        [SerializeField] Sprite[] sparks = new Sprite[0];
        [Tooltip("Obsidian Void: black glass shards, and cold sparks in place of metal's.")]
        [SerializeField] Sprite[] shards = new Sprite[0];
        [SerializeField] Sprite[] coldSparks = new Sprite[0];
        [SerializeField] int chipsPerImpact = 4;
        [SerializeField] float chipLife = 0.7f;
        [SerializeField] float chipSpeed = 3f;
        [SerializeField] float chipGravity = 18f;

        [Header("Air (P5.2): leaves in Golden Ruins, motes in Obsidian Void, behind the play layer")]
        [SerializeField] Sprite[] leaves = new Sprite[0];
        [SerializeField] Sprite mote;
        [SerializeField] Color leafTint = new Color(0.9f, 0.82f, 0.6f, 0.75f);
        [SerializeField] Color moteTint = new Color(0.55f, 0.95f, 1f, 0.55f);
        [Tooltip("World units a leaf falls per second (a mote rises at half of it).")]
        [SerializeField] float airFall = 0.35f;
        [Tooltip("How far a leaf swings sideways (world units), and how often (Hz).")]
        [SerializeField] float airSway = 0.5f;
        [SerializeField] float airSwayFrequency = 0.25f;
        [SerializeField] float leafSize = 0.22f;
        [SerializeField] float moteSize = 0.12f;

        [Header("Beats")]
        [SerializeField] Sprite flash;
        [Tooltip("The death flash on the cat: seconds (2-3 frames). It stays on with Reduce motion, as the one minimal cue.")]
        [SerializeField] float flashTime = 0.06f;
        [SerializeField] Color flashColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] float flashSize = 1.4f;
        [SerializeField] Sprite burst;
        [SerializeField] float burstTime = 0.55f;
        [Tooltip("The level-complete burst's size over the door (world units across).")]
        [SerializeField] float doorBurstSize = 3f;
        [Tooltip("The checkpoint gate's burst, smaller.")]
        [SerializeField] float gateBurstSize = 1.4f;

        public float TakeOffStretch => takeOffStretch;
        public float LandSquash => landSquash;
        public float HardLandSquash => hardLandSquash;
        public float SquashTime => squashTime;
        public Sprite[] RunPuffs => runPuffs;
        public Sprite[] LandPuffs => landPuffs;
        public Sprite[] SkidPuffs => skidPuffs;
        public Sprite[] ImpactRings => impactRings;
        public Color DustTint => dustTint;
        public float RunPuffEvery => runPuffEvery;
        public float WallPuffEvery => wallPuffEvery;
        public float PuffLife => puffLife;
        public float ImpactLife => impactLife;
        public float PuffScale => puffScale;
        public int PoolSize => poolSize;
        public float DeathShake => deathShake;
        public float DeathShakeTime => deathShakeTime;
        public float ImpactShake => impactShake;
        public float ImpactShakeTime => impactShakeTime;
        public float HardLandShake => hardLandShake;
        public float ShakeFrequency => shakeFrequency;
        public float ShakeWithin => shakeWithin;
        public float ImpactSpeed => impactSpeed;
        public Sprite[] Chips => chips;
        public Sprite[] Sparks => sparks;
        public Sprite[] Shards => shards;
        public Sprite[] ColdSparks => coldSparks;
        public int ChipsPerImpact => chipsPerImpact;
        public float ChipLife => chipLife;
        public float ChipSpeed => chipSpeed;
        public float ChipGravity => chipGravity;
        public Sprite[] Leaves => leaves;
        public Sprite Mote => mote;
        public Color LeafTint => leafTint;
        public Color MoteTint => moteTint;
        public float AirFall => airFall;
        public float AirSway => airSway;
        public float AirSwayFrequency => airSwayFrequency;
        public float LeafSize => leafSize;
        public float MoteSize => moteSize;
        public Sprite Flash => flash;
        public float FlashTime => flashTime;
        public Color FlashColor => flashColor;
        public float FlashSize => flashSize;
        public Sprite Burst => burst;
        public float BurstTime => burstTime;
        public float DoorBurstSize => doorBurstSize;
        public float GateBurstSize => gateBurstSize;
    }
}
