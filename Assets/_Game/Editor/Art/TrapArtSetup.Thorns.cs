using System.Collections.Generic;
using System.Linq;
using Parallax.Core.Presentation;
using Parallax.Editor.Levels;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // D-295: fixed hazards as growth (ThornArt): thorn clusters in Golden Ruins, crystal in Obsidian Void.
    public static partial class TrapArtSetup
    {
        /// <summary>How tall the lit row draws (world units), and a tall hazard's height over its grey-box's.</summary>
        const float ThornHeight = 0.72f, ThornTallGrowth = 1.3f;
        /// <summary>The share of a cluster's width each next one starts at: they overlap into one growth with no gap.</summary>
        const float ThornStep = 0.46f, ThornJitter = 0.1f;
        /// <summary>A hazard shorter than one cluster draws it this much wider than itself: smaller, it reads as a stray twig.</summary>
        const float ThornShortFit = 1.6f;
        /// <summary>The first and last clusters' size, so a hazard tapers off into the stone instead of stopping square.</summary>
        const float ThornEnd = 0.72f;
        /// <summary>The dim row behind: a little larger, in the shade.</summary>
        const float ThornBack = 1.12f;
        // D-318: the back row near-black (was 0.34, a mid tone): the lit branches over it missed 3:1 against exactly that tone.
        static readonly Color ThornBackTint = new(0.08f, 0.065f, 0.06f, 1f), CrystalBackTint = new(0.3f, 0.32f, 0.42f, 1f);
        /// <summary>How far the lit row leans into the level's light (0 = as painted).</summary>
        const float ThornLevelLight = 0.5f;
        /// <summary>The soft shadow at the roots: its strength, and how tall it is.</summary>
        const float ThornBedStrength = 0.92f, ThornBedHeight = 0.75f, ThornBedCover = 1.15f, ThornBedSpread = 1.4f;   // D-303: deeper, so the lit branches clear 3:1 on a mid-tone background without being brightened further
        /// <summary>The stone it grows from falls into shade: how deep, how strong, and the order that draws it over the stone's caps.</summary>
        const float ThornGroundDepth = 0.75f, ThornGroundStrength = 0.82f;
        const int ThornGroundOrder = 6;
        /// <summary>How much of a pit its breaking floors must span to hide it.</summary>
        const float PitLidShare = 0.8f;

        /// <summary>D-295's rule, what the player sees: lethal growth is there when you arrive and stays; metal comes out at you. So every
        /// fixed hazard is growth, and so is everything in a pit, hidden spikes too: two pits side by side never differ. Everything that
        /// pops up, cycles or slides on open ground is metal.</summary>
        internal static bool Grows(SoloRoomDefinition room, SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.Hazard || (e.Kind == SoloRoomElementKind.HiddenSpikes && InPit(room, e));

        /// <summary>A strip on a pit's floor: declared as one, or with stone rising on both sides of it.</summary>
        internal static bool InPit(SoloRoomDefinition room, SoloRoomElement e)
        {
            if (PointsDown(room, e) || PointsSide(room, e) != 0) return false;
            if (e.HazardRole == SoloRoomHazardRole.OpeningBottom
                || (room.Openings ?? System.Array.Empty<SoloRoomOpening>()).Any(o => o.Kind == SoloRoomOpeningKind.Pit && o.HazardName == e.Name)) return true;
            return Walled(room, e);
        }

        /// <summary>Stone rises on both sides of a strip.</summary>
        static bool Walled(SoloRoomDefinition room, SoloRoomElement e)
        {
            var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            bool Wall(bool left) => solids.Any(o => !o.Trap && Mathf.Abs((left ? o.Rect.xMax : o.Rect.xMin) - (left ? r.xMin : r.xMax)) < 0.3f && o.Rect.yMax >= r.yMax + 0.3f && o.Rect.yMin <= r.yMin + 0.05f);
            return Wall(true) && Wall(false);
        }

        /// <summary>D-298 (the developer: "You shouldnt be able to see the trap below, should be a surprise"): the floors over a pit that give
        /// way, when they span most of it and it has walls: the pit passes for solid ground until one of them goes. Golden Ruins only: an
        /// Obsidian pit is open to the void, so there's no stone to pass for.</summary>
        internal static List<SoloRoomElement> PitLids(SoloRoomDefinition room, SoloRoomElement e)
        {
            var lids = new List<SoloRoomElement>();
            if (Biomes.Of(room) == Biome.ObsidianVoid || !Walled(room, e)) return lids;
            var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
            float spanned = 0f;
            foreach (SoloRoomElement o in room.Elements)
            {
                if (o.Kind is not (SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.FallingBlock)) continue;
                var lid = new Rect(room.Origin + o.Position - o.Size * 0.5f, o.Size);
                float over = Mathf.Min(lid.xMax, r.xMax) - Mathf.Max(lid.xMin, r.xMin);
                if (over <= 0f || lid.yMin < r.yMax) continue;
                spanned += over;
                lids.Add(o);
            }
            if (spanned < PitLidShare * r.width) lids.Clear();
            // D-300: a pit's walls stand from its hazard up to its lid. A hazard far under a lid with air around it (L016's) is no pit: stone
            // or a back wall over that whole drop stood up in the open as a pillar.
            if (lids.Count > 0)
            {
                float lidY = lids.Min(l => room.Origin.y + l.Position.y - l.Size.y * 0.5f);
                List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
                bool Stands(bool left) => solids.Any(o => !o.Trap && Mathf.Abs((left ? o.Rect.xMax : o.Rect.xMin) - (left ? r.xMin : r.xMax)) < 0.3f && o.Rect.yMin <= r.yMax + 0.5f && o.Rect.yMax >= lidY - 0.5f);
                if (!Stands(true) || !Stands(false)) lids.Clear();
            }
            return lids;
        }

        /// <summary>A fixed hazard's growth, or null without its sprites in the kit. Every piece stays within its hazard's length, and
        /// under anything over it (BladeReach, the rule that art never shows past what covers it). `trap`: a pit's hidden spikes, always drawn.</summary>
        static string BuildThorns(Transform artRoot, Transform roomRoot, SoloRoomDefinition room, SoloRoomElement e, SpriteRenderer greybox, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes, RoomTrap trap = null)
        {
            Sprite[] sprites = config.ThornClusters.Where(s => s != null).ToArray();
            Sprite[] rimSprites = config.ThornRims.Where(s => s != null).ToArray();
            if (greybox == null || sprites.Length == 0) return null;
            bool crystal = Biomes.Of(room) == Biome.ObsidianVoid;
            bool down = PointsDown(room, e);
            int side = PointsSide(room, e);
            Transform art = ArtObject(artRoot, e.Name, greybox.transform.position, layer, changes);
            if (art.TryGetComponent(out SpikeArt spikes)) { Object.DestroyImmediate(spikes); changes.Add("removed " + e.Name + "'s spikes"); }
            if (art.TryGetComponent(out LaserArt laser)) { Object.DestroyImmediate(laser); changes.Add("removed " + e.Name + "'s laser"); }
            foreach (Transform old in art.Cast<Transform>().ToList()) RemoveChild(art, old.name, changes);
            ThornArt presenter = SetupUtility.Ensure<ThornArt>(art.gameObject, changes);

            Vector2 box = side != 0 ? new Vector2(greybox.size.y, greybox.size.x) : greybox.size;
            Vector2 outward = side != 0 ? new Vector2(side, 0f) : new Vector2(0f, down ? -1f : 1f), across = side != 0 ? Vector2.up : Vector2.right;
            Vector2 foot = (Vector2)greybox.transform.position - outward * (box.y * 0.5f);
            Quaternion turn = Quaternion.Euler(0f, 0f, down ? 180f : -90f * side);
            float height = box.y > SpikeArt.MaxStripHeight ? box.y * ThornTallGrowth : ThornHeight;
            float tallest = sprites.Max(s => s.bounds.size.y), widest = sprites.Max(s => s.bounds.size.x);
            float k = Mathf.Min(height / tallest, ThornShortFit * box.x / widest);
            float width = widest * k, step = width * ThornStep;
            int count = Mathf.Max(1, Mathf.CeilToInt((box.x - width) / step) + 1);
            uint seed = TrapArtMath.Seed(e.Name, 0);
            Color light = Color.Lerp(Color.white, LevelPalettes.EnvGrade(LevelPalettes.For(LevelLooks.LevelOf(room))), ThornLevelLight);
            Color shade = crystal ? CrystalBackTint : ThornBackTint;
            var clusters = new List<Object>();
            float overAlong = 0f, overTips = 0f;

            // `pin` -1 or +1 puts a cluster's outer tip on that end of the hazard: a gap at the end of a lethal row reads as a safe spot.
            SpriteRenderer Cluster(string name, int variant, float along, float scale, Color tint, int order, bool flip, int pin = 0)
            {
                Sprite sprite = sprites[variant % sprites.Length];
                float half = sprite.bounds.size.x * scale * 0.5f, cap = BladeReach(room, e, down, side, along - half, along + half);
                float tall = sprite.bounds.size.y * scale;
                if (cap > 0f && tall > cap) { scale *= cap / tall; half *= cap / tall; tall = cap; }
                along = pin != 0 ? pin * Mathf.Max(0f, box.x * 0.5f - half) : Mathf.Clamp(along, Mathf.Min(0f, half - box.x * 0.5f), Mathf.Max(0f, box.x * 0.5f - half));
                SpriteRenderer r = Child(art, name, layer, changes);
                r.sprite = sprite; r.sharedMaterial = HazardMaterial(config); r.drawMode = SpriteDrawMode.Simple; r.color = tint;
                r.sortingOrder = order; r.flipX = flip; r.enabled = greybox.enabled || trap != null;
                Vector2 at = foot + across * along;
                r.transform.SetPositionAndRotation(new Vector3(at.x, at.y, art.position.z), turn);
                r.transform.localScale = new Vector3(scale, scale, 1f);
                overAlong = Mathf.Max(overAlong, Mathf.Abs(along) + half - box.x * 0.5f);
                overTips = Mathf.Max(overTips, tall - box.y);
                clusters.Add(r);
                return r;
            }

            float Along(int i, int n) => n == 1 ? 0f : -(box.x - width) * 0.5f + (box.x - width) * i / (n - 1f);
            // The shade first: drawn behind, offset half a step from the lit row, so its branches show through the gaps.
            for (int i = 0; i < count; i++)
                Cluster("Back_" + i, i + 2, Along(i, count) + step * 0.5f * (i % 2 == 0 ? 1f : -1f), k * ThornBack, shade, greybox.sortingOrder - 1, TrapArtMath.Hash01(seed, 2100 + i) < 0.5f);
            var rims = new List<Object>();
            float rimOver = 0f;
            for (int i = 0; i < count; i++)
            {
                bool end = count > 2 && (i == 0 || i == count - 1);
                float size = k * (end ? ThornEnd : 0.92f + 0.16f * TrapArtMath.Hash01(seed, 2200 + i));
                SpriteRenderer lit = Cluster("Cluster_" + i, i, Along(i, count) + TrapArtMath.HashSigned(seed, 2300 + i) * step * ThornJitter, size, light, greybox.sortingOrder,
                    TrapArtMath.Hash01(seed, 2400 + i) < 0.5f, end ? (i == 0 ? -1 : 1) : 0);
                // Gate 6: its dark rim, placed exactly as the cluster (the rim's canvas is padded round the cluster's, its pivot moved to
                // match), with the back row: shade behind the pale bark on any sky, the branch itself unchanged.
                if (rimSprites.Length != sprites.Length) continue;
                SpriteRenderer rim = Child(art, "Rim_" + i, layer, changes);
                rim.sprite = rimSprites[i % rimSprites.Length]; rim.sharedMaterial = lit.sharedMaterial; rim.drawMode = SpriteDrawMode.Simple; rim.color = Color.white;
                rim.sortingOrder = greybox.sortingOrder - 1; rim.flipX = lit.flipX; rim.enabled = lit.enabled;
                rim.transform.SetPositionAndRotation(lit.transform.position, lit.transform.rotation);
                rim.transform.localScale = lit.transform.localScale;
                rimOver = Mathf.Max(rimOver, (rim.sprite.bounds.size.x - lit.sprite.bounds.size.x) * 0.5f * lit.transform.localScale.x);
                rims.Add(rim);
            }
            RemoveExtra(art, "Rim_", rims.Count, changes);
            overAlong += rimOver;

            // The roots' shadow, on the stone under the growth.
            SpriteRenderer bed = null;
            bool night = Parallax.Editor.Levels.LevelPalettes.For(Parallax.Editor.Levels.LevelLooks.LevelOf(room)).Moon;   // D-307: L010's pit fell to flat black under its thorns
            Sprite soft = EnvironmentKit.Sprite("ENV_Halo");
            if (soft != null)
            {
                bed = Child(art, "Bed", layer, changes);
                bed.sprite = soft; bed.sharedMaterial = HazardMaterial(config); bed.drawMode = SpriteDrawMode.Simple;
                bed.color = new Color(0f, 0f, 0f, night ? ThornBedStrength * 0.55f : ThornBedStrength); bed.sortingOrder = greybox.sortingOrder - 2; bed.enabled = greybox.enabled || trap != null;
                // D-318: as tall as the growth, so its upper branches stand on shade too (at 0.75 u only the roots did, and over
                // mid-tone stone the branches above missed 3:1). The test counts the lit clusters alone, so the shade behind only helps.
                float bedHeight = Mathf.Max(ThornBedHeight, height * ThornBedCover);
                Vector2 at = foot + outward * (bedHeight * 0.36f);
                bed.transform.SetPositionAndRotation(new Vector3(at.x, at.y, art.position.z), side != 0 ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity);
                float bedWidth = (box.x + 2f * overAlong) * ThornBedSpread;
                bed.transform.localScale = new Vector3(bedWidth / soft.bounds.size.x, bedHeight / soft.bounds.size.y, 1f);   // D-318: past the clusters' outer branches (its edges are soft)
                overAlong = Mathf.Max(overAlong, (bedWidth - box.x) * 0.5f);   // the frame the parity test allows covers the shade too
                overTips = Mathf.Max(overTips, bedHeight);
            }

            // The stone under it, in shade: dark at the roots, gone within ThornGroundDepth, so what it grows from belongs to it.
            SpriteRenderer ground = null;
            Sprite ramp = EnvironmentKit.Sprite("ENV_FaceShade");
            if (ramp != null)
            {
                ground = Child(art, "Ground", layer, changes);
                ground.sprite = ramp; ground.sharedMaterial = HazardMaterial(config); ground.drawMode = SpriteDrawMode.Simple;
                ground.color = new Color(0.06f, 0.045f, 0.04f, night ? ThornGroundStrength * 0.5f : ThornGroundStrength); ground.sortingOrder = ThornGroundOrder; ground.enabled = greybox.enabled || trap != null;
                Vector2 at = foot - outward * (ThornGroundDepth * 0.5f);
                // The ramp is opaque at its bottom: its dark end turned to the roots.
                ground.transform.SetPositionAndRotation(new Vector3(at.x, at.y, art.position.z), Quaternion.Euler(0f, 0f, down ? 0f : side != 0 ? 90f * side : 180f));
                ground.transform.localScale = new Vector3(box.x / ramp.bounds.size.x, ThornGroundDepth / ramp.bounds.size.y, 1f);
                overTips = Mathf.Max(overTips, ThornGroundDepth);
            }

            // A pit under a floor that gives way passes for solid stone until the floor goes: the room's own stone over all of it,
            // one strip under each lid (D-300), so what opens is the hole the lid left.
            var curtains = new List<SpriteRenderer>();
            List<SoloRoomElement> lids = PitLids(room, e).OrderBy(l => l.Position.x).ToList();
            Sprite fill = SoloRoomSkin.FillSprite(room);
            Material litFill = EnvironmentKit.TileMaterial("Fill");
            if (lids.Count > 0 && fill != null && litFill != null)
            {
                var pit = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                float top = lids.Min(l => room.Origin.y + l.Position.y - l.Size.y * 0.5f), bottom = pit.yMin - 0.1f;
                for (int i = 0; i < lids.Count; i++)
                {
                    // Each strip runs to the next lid's edge, and the end ones into the pit's walls.
                    float x0 = i == 0 ? pit.xMin - SoloRoomSkin.DeepReach : room.Origin.x + lids[i].Position.x - lids[i].Size.x * 0.5f;
                    float x1 = i == lids.Count - 1 ? pit.xMax + SoloRoomSkin.DeepReach : room.Origin.x + lids[i + 1].Position.x - lids[i + 1].Size.x * 0.5f;
                    SpriteRenderer strip = Child(art, $"Curtain_{i:00}", layer, changes);
                    strip.sprite = fill; strip.sharedMaterial = litFill; strip.drawMode = SpriteDrawMode.Sliced; strip.size = new Vector2(x1 - x0, top - bottom);
                    strip.color = SoloRoomSkin.Body; strip.color = new Color(strip.color.r, strip.color.g, strip.color.b, 1f); strip.sortingOrder = ThornGroundOrder + 1;
                    strip.transform.position = new Vector3((x0 + x1) * 0.5f, (top + bottom) * 0.5f, art.position.z);
                    strip.transform.localRotation = Quaternion.identity;
                    curtains.Add(strip);
                }
            }
            RemoveExtra(art, "Curtain_", curtains.Count, changes);
            RemoveChild(art, "Curtain", changes);
            var lidBoxes = lids.Select(l => roomRoot.Find(l.Name)?.GetComponent<SpriteRenderer>()).Where(r => r != null).Cast<Object>().ToArray();
            SetupUtility.SetArray(presenter, "curtains", curtains.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "lids", lidBoxes, changes);

            Vector2 frame = new(Mathf.Max(0f, overAlong + 0.02f) / box.x, (overTips + 0.02f) / box.y);
            TrapKitSetup.Write(presenter, changes, ("rooms", rooms), ("seedName", e.Name), ("greyboxBody", greybox), ("trap", trap), ("always", trap != null), ("bed", bed), ("ground", ground), ("growth", side != 0 ? new Vector2(frame.y, frame.x) : frame));
            SetupUtility.SetArray(presenter, "clusters", clusters.ToArray(), changes);
            SetupUtility.SetArray(presenter, "rims", rims.ToArray(), changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            return art.name;
        }
    }
}
