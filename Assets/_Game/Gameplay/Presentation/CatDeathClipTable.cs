using System;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>PAX-V07 §4: death kind → death clip. A missing kind falls back to the Default clip (the frightened pose).
    /// Created and filled by PARALLAX/Setup/Cat Visual at Assets/_Game/Data/CatA_DeathClips.asset.</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Cat Death Clips")]
    public sealed class CatDeathClipTable : ScriptableObject
    {
        [Serializable]
        sealed class Entry
        {
            public CatDeathKind kind;
            public CatClip clip;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        public bool Has(CatDeathKind kind) => Find(kind) != null;

        /// <summary>The clip for `kind`, or the Default clip when `kind` has none (null only without a Default).</summary>
        public CatClip For(CatDeathKind kind) => Find(kind) ?? Find(CatDeathKind.Default);

        /// <summary>Sets `kind`'s clip (the setup menu writes the asset through SerializedObject; tests use this).</summary>
        public void Set(CatDeathKind kind, CatClip clip)
        {
            foreach (Entry e in entries) if (e.kind == kind) { e.clip = clip; return; }
            Array.Resize(ref entries, entries.Length + 1);
            entries[^1] = new Entry { kind = kind, clip = clip };
        }

        /// <summary>`kind`'s own clip, without the Default fallback (null when it has none).</summary>
        public CatClip Exact(CatDeathKind kind) => Find(kind);

        CatClip Find(CatDeathKind kind)
        {
            if (entries == null) return null;
            foreach (Entry e in entries) if (e != null && e.kind == kind && e.clip != null && e.clip.Count > 0) return e.clip;
            return null;
        }
    }
}
