using System.Collections.Generic;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>D-295: a fixed hazard drawn as growth: thorn clusters in Golden Ruins, crystal in Obsidian Void. Laid out once by the setup
    /// over a soft shadow at their roots and the shade of the stone they grow from, a dim row behind a lit one, smaller toward the ends,
    /// so a hazard belongs to what it stands on. They never move: this only shows them with their grey-box.</summary>
    public sealed class ThornArt : TrapArt
    {
        [SerializeField] RoomTrap trap;                 // a pit's hidden spikes; null for a Hazard
        [SerializeField] SpriteRenderer greyboxBody, bed, ground;
        [Tooltip("D-295's rule: a pit's growth is always there, even over hidden spikes: nothing in a pit ever appears.")]
        [SerializeField] bool always;
        [SerializeField] SpriteRenderer[] clusters = new SpriteRenderer[0];
        [Tooltip("Premium plan gate 6: the soft dark rim behind each lit cluster, so its pale bark reads on a bright sky. Shade, like the bed.")]
        [SerializeField] SpriteRenderer[] rims = new SpriteRenderer[0];
        [Tooltip("D-298: stone over a pit whose floor gives way: it passes for solid ground while every lid is whole and at home.")]
        [SerializeField] SpriteRenderer[] curtains = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] lids = new SpriteRenderer[0];
        [Tooltip("How far past its grey-box this hazard draws, as a fraction of the grey-box's size (written by the setup from its layout).")]
        [SerializeField] Vector2 growth;

        public override RoomTrap Trap => trap;
        /// <summary>Drawn whatever its grey-box does (a pit's hidden spikes).</summary>
        public bool Always => always;
        protected override bool NeedsTrap => false;
        public override Vector2 BodyGrowth => growth;

        public IReadOnlyList<SpriteRenderer> Clusters => clusters;
        public IReadOnlyList<SpriteRenderer> Curtains => curtains;
        /// <summary>The lit row, which is what must read (the row behind it is shade): the second half of the clusters.</summary>
        public IEnumerable<SpriteRenderer> LitClusters { get { for (int i = clusters.Length / 2; i < clusters.Length; i++) yield return clusters[i]; } }

        protected override void CollectBodies(List<Body> into)
        {
            foreach (SpriteRenderer c in clusters) into.Add(new Body(c, greyboxBody));
            foreach (SpriteRenderer r in rims) into.Add(new Body(r, greyboxBody));
            if (bed != null) into.Add(new Body(bed, greyboxBody));
            if (ground != null) into.Add(new Body(ground, greyboxBody));
        }

        protected override void CollectEffects(List<Effect> into) { }

        Vector2[] lidHomes;

        protected override void Render()
        {
            bool shown = always || Shows(greyboxBody);
            foreach (SpriteRenderer c in clusters) if (c != null) c.enabled = shown;
            foreach (SpriteRenderer r in rims) if (r != null) r.enabled = shown;
            if (bed != null) bed.enabled = shown;
            if (ground != null) ground.enabled = shown;
            if (curtains.Length == 0) return;
            if (lidHomes == null)
            {
                lidHomes = new Vector2[lids.Length];
                for (int i = 0; i < lids.Length; i++) if (lids[i] != null) lidHomes[i] = lids[i].transform.position;
            }
            // D-300: each lid holds up its own strip, so the stone goes where the floor went, not all at once.
            for (int i = 0; i < lids.Length && i < curtains.Length; i++)
                if (curtains[i] != null) curtains[i].enabled = lids[i] != null && Shows(lids[i]) && ((Vector2)lids[i].transform.position - lidHomes[i]).sqrMagnitude < 1e-4f;
        }
    }
}
