using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>D-236 (an exception to D-040 for L028 only; the developer, 2026-10-28: "We predefined 5 variations, on render we pick
    /// 1"): picks one of the level's arrow patterns when the scene loads and switches off the launchers, and their art, that the
    /// pattern doesn't use. D-238 ("I died 4 times and it was exactly the same pattern"; a death re-rolls too): after each death's
    /// reset (RoomDeath.Died), it picks again; a launcher switched back on is reset to its initial state, so it fires on the room
    /// tick's beat like the rest. Never the same pattern twice in a row while the game runs. Baked by the level build
    /// (ArrowVariantSetup).</summary>
    public sealed class ArrowVariantPicker : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Variant
        {
            [Tooltip("The launchers (and their art) this pattern doesn't fire: switched off when it's picked.")]
            public GameObject[] off = new GameObject[0];
        }

        [SerializeField] Variant[] variants = new Variant[0];
        [SerializeField] RoomDeath roomDeath;

        static int lastPick = -1;
        readonly List<GameObject> all = new List<GameObject>();
        readonly HashSet<GameObject> offNow = new HashSet<GameObject>();

        public int Picked { get; private set; } = -1;

        public void SetVariants(Variant[] value, RoomDeath death) { variants = value ?? new Variant[0]; roomDeath = death; }

        void Awake()
        {
            if (variants.Length == 0) { Debug.LogWarning($"ArrowVariantPicker '{name}': no variants baked; every launcher stays on.", this); return; }
            foreach (Variant v in variants)
                foreach (GameObject o in v.off)
                {
                    if (o == null) { Debug.LogError($"ArrowVariantPicker '{name}': a variant lists a missing launcher; rebuild the level.", this); continue; }
                    if (!all.Contains(o)) all.Add(o);
                }
            Pick();
        }

        void OnEnable() { if (roomDeath != null) roomDeath.Died += OnDied; else Debug.LogError($"ArrowVariantPicker '{name}': no RoomDeath; deaths won't re-roll the pattern.", this); }
        void OnDisable() { if (roomDeath != null) roomDeath.Died -= OnDied; }

        void OnDied(DeathInfo info) { if (variants.Length > 1) Pick(); }

        void Pick()
        {
            int pick = Random.Range(0, variants.Length);
            if (variants.Length > 1 && pick == lastPick) pick = (pick + 1 + Random.Range(0, variants.Length - 1)) % variants.Length;
            lastPick = Picked = pick;
            offNow.Clear();
            foreach (GameObject o in variants[pick].off) if (o != null) offNow.Add(o);
            foreach (GameObject o in all)
            {
                bool on = !offNow.Contains(o);
                if (o.activeSelf == on) continue;
                o.SetActive(on);
                // Back on: from its initial state (it missed every step while off).
                if (on && o.TryGetComponent(out RoomTrap trap)) trap.ResetToInitial();
            }
        }
    }
}
