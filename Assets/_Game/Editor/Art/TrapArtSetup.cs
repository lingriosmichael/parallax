using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§7, §11 R5, §12 R10): builds each trap's art under the room's "Art" root, beside the traps and never
    /// under them, when the level is built (SoloRoomBuilder.BuildRoom), so Rebuild All Levels reproduces it: every element
    /// kind of the trap kit (this file: collapsing floors and fake platforms, arrows and spears, geysers; TrapArtSetup.Kit:
    /// spikes, falling blocks, moving and shrinking floors, flips, the inverter, the storm cloud, snap vines, the retreating
    /// door) and the room's death effects. With no TrapArtConfig the room keeps its grey-box and any old Art root is removed.
    /// Nothing here touches a trap, its colliders or its grey-box renderers.</summary>
    public static partial class TrapArtSetup
    {
        public const string ConfigPath = "Assets/_Game/Data/TrapArtConfig.asset";
        public const string ArtRootName = "Art";
        const int EffectOrder = 2;

        public static void BuildArt(Transform roomRoot, RealityRoot root, SoloRoomDefinition room, RoomManager rooms, List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(ConfigPath);
            Transform existing = roomRoot.Find(ArtRootName);
            if (config == null)
            {
                if (existing != null) { Object.DestroyImmediate(existing.gameObject); changes.Add("removed " + roomRoot.name + "/Art (no TrapArtConfig)"); }
                return;
            }
            int layer = root.gameObject.layer;
            Transform artRoot = ArtChild(roomRoot, ArtRootName, layer, changes);
            var built = new HashSet<string>();
            foreach (SoloRoomElement e in room.Elements)
            {
                Transform t = roomRoot.Find(e.Name);
                if (t == null) continue;
                if (t.TryGetComponent(out CollapsingFloorTrap floor)) built.Add(BuildCollapsingFloor(artRoot, roomRoot, room, e, floor, rooms, config, layer, changes));
                else if (t.TryGetComponent(out ArrowTrap arrow) && config.HasArrow) built.Add(BuildArrow(artRoot, roomRoot, room, e, arrow, rooms, config, layer, changes));
                else if (t.TryGetComponent(out GeyserTrap geyser) && config.HasGeyser) built.Add(BuildGeyser(artRoot, e, geyser, rooms, config, layer, changes));
                else if (BuildKitElement(artRoot, roomRoot, room, e, t, rooms, config, layer, changes) is string name) built.Add(name);
            }
            built.Add(BuildDeath(artRoot, room, roomRoot, rooms, config, layer, changes));
            // D-101: trap bodies draw BodyScale bigger. Disguises (collapsing floors, falling blocks, moving and shrinking floors)
            // must cover exactly their trap (P10) and stay at 1, and so does a disguised launcher's projectile (it rests behind
            // the host skin: 10% more would poke out past it before the reveal).
            foreach (TrapArt art in artRoot.GetComponentsInChildren<TrapArt>(true))
            {
                TrapKitSetup.Write(art, changes, ("bodyScale", art is CollapsingFloorArt or SolidArt || art is ArrowArt { Disguised: true } ? 1f : config.BodyScale));
                if (art is SpikeArt) TrapKitSetup.Write(art, changes, ("heightScale", config.SpikeHeightScale));
            }
            foreach (Transform stale in artRoot.Cast<Transform>().Where(c => !built.Contains(c.name)).ToList())
            {
                Object.DestroyImmediate(stale.gameObject);
                changes.Add("removed stale art " + stale.name);
            }
        }

        // ---------- collapsing floors and fake platforms ----------

        static string BuildCollapsingFloor(Transform artRoot, Transform roomRoot, SoloRoomDefinition room, SoloRoomElement e, CollapsingFloorTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            SpriteRenderer greybox = trap.GetComponent<SpriteRenderer>();
            SpriteRenderer host = HostRenderer(roomRoot, room, e, floorFirst: true);
            HostSkin skinLook = HostSkin.Of(host);
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            CollapsingFloorArt presenter = SetupUtility.Ensure<CollapsingFloorArt>(art.gameObject, changes);

            SpriteRenderer skin = Child(art, "Skin", layer, changes);
            skinLook.ApplyTo(skin, e.Size);
            skin.sortingOrder = greybox.sortingOrder;
            skin.transform.localScale = Vector3.one;

            // The shards: the floor cut (TrapArtMath.ShardRect, seeded by the name), each drawn with the host skin.
            uint cut = TrapArtMath.Seed(e.Name, 0);
            int columns = TrapArtMath.ShardColumns(e.Size.x), rows = Mathf.Max(1, config.ShardRows);
            var local = new Rect(-e.Size * 0.5f, e.Size);
            var shards = new List<SpriteRenderer>();
            var homes = new List<Vector2>();
            var sizes = new List<Vector2>();
            for (int i = 0; i < columns * rows; i++)
            {
                Rect r = TrapArtMath.ShardRect(local, cut, i, columns, rows);
                // D-103: cut lines snapped to the 1/128 u grid from the floor's corner, so no seam pixel is half covered on the
                // reveal tick (the background showed through mid-pixel seams). The floor's outer edges are unchanged.
                float Snap(float v, float lo) => lo + Mathf.Round((v - lo) * 128f) / 128f;
                r = Rect.MinMaxRect(Snap(r.xMin, local.xMin), Snap(r.yMin, local.yMin), Snap(r.xMax, local.xMin), Snap(r.yMax, local.yMin));
                SpriteRenderer shard = Child(art, $"Shard_{i:00}", layer, changes);
                skinLook.ApplyTo(shard, r.size);
                // PAX-A15 (approved 2026-10-01): at the floor's own order, so on the reveal tick its cap and edge trims stay on
                // top (the frame matches the one before) and falling shards pass behind the cat.
                shard.sortingOrder = greybox.sortingOrder;
                shard.transform.localScale = Vector3.one;
                shard.enabled = false;
                shards.Add(shard); homes.Add(r.center); sizes.Add(r.size);
            }
            RemoveExtra(art, "Shard_", columns * rows, changes);
            RemoveChild(art, "Backing", changes);
            SpriteRenderer[] dust = Effects(art, "Dust", config.DustPerFloor, config.Dust, config.TrapMaterial, greybox.sortingOrder + EffectOrder, layer, changes);

            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("skin", skin), ("greyboxVisual", greybox), ("size", e.Size));
            WriteSkin(presenter, "hostSkin", skinLook, changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            SetupUtility.SetArray(presenter, "shards", shards.Cast<Object>().ToArray(), changes);
            WriteVectors(presenter, "shardHomes", homes, changes);
            WriteVectors(presenter, "shardSizes", sizes, changes);
            SetupUtility.SetArray(presenter, "dust", dust.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- arrows ----------

        static string BuildArrow(Transform artRoot, Transform roomRoot, SoloRoomDefinition room, SoloRoomElement e, ArrowTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            ArrowLane lane = e.Settings.Arrow;
            SpriteRenderer greyLauncher = trap.GetComponent<SpriteRenderer>();
            bool spear = lane.Spear;
            SpriteRenderer greyArrow = (trap.transform.Find("Arrow") ?? trap.transform.Find(e.Name + "_Shaft"))?.GetComponent<SpriteRenderer>();
            HostSkin hostLook = lane.Disguised ? HostSkin.Of(HostRenderer(roomRoot, room, e, floorFirst: false)) : default;
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            ArrowArt presenter = SetupUtility.Ensure<ArrowArt>(art.gameObject, changes);

            SpriteRenderer launcher = Child(art, "Launcher", layer, changes);
            launcher.sortingOrder = greyLauncher.sortingOrder;
            if (lane.Disguised) { hostLook.ApplyTo(launcher, e.Size); launcher.transform.localScale = Vector3.one; launcher.flipX = false; }   // scale after the draw mode (Sliced rewrites it)
            else { launcher.sprite = config.ArrowLauncher; launcher.sharedMaterial = HazardMaterial(config); launcher.drawMode = SpriteDrawMode.Simple; launcher.flipX = lane.Direction == ArrowDirection.Left; }

            SpriteRenderer arrow = Child(art, "Arrow", layer, changes);   // a spear's head
            arrow.sprite = spear ? config.SpearHead : config.Arrow; arrow.sharedMaterial = HazardMaterial(config); arrow.drawMode = SpriteDrawMode.Simple;
            arrow.sortingOrder = greyArrow != null ? greyArrow.sortingOrder : 0;
            arrow.flipX = lane.Direction == ArrowDirection.Left;
            arrow.enabled = false;
            SpriteRenderer shaft = null;
            if (spear)
            {
                shaft = Child(art, "Shaft", layer, changes);
                shaft.sprite = config.SpearShaft; shaft.sharedMaterial = HazardMaterial(config); shaft.drawMode = SpriteDrawMode.Tiled;
                shaft.sortingOrder = arrow.sortingOrder; shaft.flipX = arrow.flipX; shaft.enabled = false;
            }
            else RemoveChild(art, "Shaft", changes);

            // A disguised launcher's reveal: the slot drawn over its host skin from the fire tick (§12 R8).
            SpriteRenderer slot = null;
            if (lane.Disguised)
            {
                slot = Child(art, "Slot", layer, changes);
                slot.sprite = config.ArrowLauncher; slot.sharedMaterial = HazardMaterial(config); slot.drawMode = SpriteDrawMode.Simple;
                slot.sortingOrder = launcher.sortingOrder + 1; slot.flipX = lane.Direction == ArrowDirection.Left; slot.enabled = false;
            }
            else RemoveChild(art, "Slot", changes);

            SpriteRenderer glint = Child(art, "Glint", layer, changes);
            glint.sprite = config.Glint; glint.sharedMaterial = config.TrapMaterial; glint.drawMode = SpriteDrawMode.Simple;
            glint.sortingOrder = arrow.sortingOrder + EffectOrder; glint.enabled = false;
            SpriteRenderer[] impact = Effects(art, "Impact", config.ImpactPuffs, config.Dust, config.TrapMaterial, arrow.sortingOrder + EffectOrder, layer, changes);

            int flight = ArrowMath.FlightTicks(LevelLayoutValidator.ArrowTravel(e), lane.UnitsPerTick);
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("launcherArt", launcher), ("greyboxLauncher", greyLauncher),
                ("arrowArt", arrow), ("greyboxArrow", greyArrow), ("disguised", lane.Disguised), ("launcherSprite", config.ArrowLauncher), ("arrowSprite", config.Arrow),
                ("trapMaterial", HazardMaterial(config)), ("launcherSize", e.Size), ("arrowSize", new Vector2(lane.Length, lane.Thickness)),
                ("direction", (int)lane.Direction), ("tellTicks", lane.TellTicks), ("flightTicks", flight), ("glint", glint), ("slot", slot),
                ("spear", spear), ("shaftArt", shaft), ("spearHeadSprite", config.SpearHead), ("spearShaftSprite", config.SpearShaft));
            WriteSkin(presenter, "hostSkin", hostLook, changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greyLauncher, greyArrow }, changes);
            SetupUtility.SetArray(presenter, "impact", impact.Cast<Object>().ToArray(), changes);
            // No Apply here: until its Awake, the trap's grey-box arrow is still enabled, which reads as fired.
            return art.name;
        }

        // ---------- geysers ----------

        static string BuildGeyser(Transform artRoot, SoloRoomElement e, GeyserTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            GeyserSettings g = e.Settings.Geyser.Resolved;
            SpriteRenderer greyVent = trap.Vent, greyColumn = trap.Column;
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            GeyserArt presenter = SetupUtility.Ensure<GeyserArt>(art.gameObject, changes);

            SpriteRenderer vent = Child(art, "Vent", layer, changes);
            vent.sprite = config.GeyserVent; vent.sharedMaterial = config.TrapMaterial; vent.drawMode = SpriteDrawMode.Simple;
            vent.sortingOrder = greyVent.sortingOrder;

            SpriteRenderer column = Child(art, "Column", layer, changes);
            column.sprite = config.GeyserColumnFrames[0]; column.sharedMaterial = config.TrapMaterial;
            column.drawMode = SpriteDrawMode.Tiled; column.tileMode = SpriteTileMode.Continuous;
            column.size = greyColumn != null ? greyColumn.size : new Vector2(g.ColumnWidth, g.ColumnHeight);
            column.transform.localScale = Vector3.one;
            column.sortingOrder = greyColumn != null ? greyColumn.sortingOrder : greyVent.sortingOrder;
            column.enabled = false;

            int effects = Mathf.Max(vent.sortingOrder, column.sortingOrder) + EffectOrder;
            SpriteRenderer tellGlow = Child(art, "TellGlow", layer, changes);
            tellGlow.sprite = config.Dust.Length > 0 ? config.Dust[0] : config.Glint; tellGlow.sharedMaterial = config.TrapMaterial; tellGlow.drawMode = SpriteDrawMode.Simple;
            tellGlow.sortingOrder = effects; tellGlow.enabled = false; tellGlow.color = new Color(1f, 0.62f, 0.3f, 1f);
            SpriteRenderer[] tell = Effects(art, "Tell", config.TellDrops, config.Spray, config.TrapMaterial, effects, layer, changes);
            SpriteRenderer[] steam = Effects(art, "Steam", config.SteamWisps, config.Steam, config.TrapMaterial, effects, layer, changes);
            SpriteRenderer[] burst = Effects(art, "Burst", config.BurstPuffs, config.Spray, config.TrapMaterial, effects, layer, changes);
            SpriteRenderer[] spray = Effects(art, "Spray", config.SprayPuffs, config.Spray, config.TrapMaterial, effects, layer, changes);

            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("ventArt", vent), ("greyboxVent", greyVent),
                ("columnArt", column), ("greyboxColumn", greyColumn), ("ventSprite", config.GeyserVent), ("trapMaterial", config.TrapMaterial),
                ("ventSize", e.Size), ("tellTicks", g.TellTicks), ("direction", (int)g.Direction), ("tellGlow", tellGlow));
            SetupUtility.SetArray(presenter, "columnFrames", config.GeyserColumnFrames.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greyVent, greyColumn }, changes);
            SetupUtility.SetArray(presenter, "tellDrops", tell.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "steam", steam.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "burst", burst.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "spray", spray.Cast<Object>().ToArray(), changes);
            presenter.Apply();
            return art.name;
        }

        // ---------- helpers ----------

        /// <summary>PAX-V08: the material lethal bodies draw with (spikes, an arrow or spear and its launcher or slot): unlit, so
        /// a hazard keeps its full colour, outline and rim under any level's lights (a night level's dim light made them melt
        /// into an unlit sky; HazardContrastTests). Effects (glint, dust) keep the trap material. Falls back to the trap material
        /// if URP's unlit sprite material can't be found.</summary>
        internal static Material HazardMaterial(TrapArtConfig config) => EnvironmentKit.UnlitMaterial != null ? EnvironmentKit.UnlitMaterial : config.TrapMaterial;

        /// <summary>The fixed geometry a disguised trap sits in (a launcher) or passes for (a floor): the first Floor,
        /// Ceiling, Wall or PitBottom overlapping it; a floor with no overlap passes for the room's first Floor.</summary>
        internal static SpriteRenderer HostRenderer(Transform roomRoot, SoloRoomDefinition room, SoloRoomElement trap, bool floorFirst)
        {
            static bool Fixed(SoloRoomElementKind k) => k is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.PitBottom;
            var box = new Rect(trap.Position - trap.Size * 0.5f, trap.Size);
            SoloRoomElement? pick = null;
            foreach (SoloRoomElement h in room.Elements)
                if (Fixed(h.Kind) && new Rect(h.Position - h.Size * 0.5f, h.Size).Overlaps(box)) { pick = h; break; }
            // PAX-102 (D-085 amendment): a block that is a section of a split slab takes the slab it touches.
            if (pick == null && trap.Kind == SoloRoomElementKind.FallingBlock) pick = room.Elements.Cast<SoloRoomElement?>().FirstOrDefault(h => Fixed(h.Value.Kind)
                && new Rect(h.Value.Position - h.Value.Size * 0.5f - Vector2.one * 0.01f, h.Value.Size + Vector2.one * 0.02f).Overlaps(box));
            if (pick == null && floorFirst) pick = room.Elements.Cast<SoloRoomElement?>().FirstOrDefault(h => h.Value.Kind == SoloRoomElementKind.Floor);
            if (pick == null) pick = room.Elements.Cast<SoloRoomElement?>().FirstOrDefault(h => Fixed(h.Value.Kind));
            Transform t = pick == null ? roomRoot.Find("Wall_Left") : roomRoot.Find(pick.Value.Name);
            if (t == null) t = roomRoot.Find("Wall_Left");
            SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
            if (r == null) Debug.LogError($"TrapArtSetup: no host geometry for disguised '{trap.Name}' in {roomRoot.name}.");
            return r;
        }

        // SetupUtility.EnsureChild without the undo record: a route session builds every room many times over, and with art
        // on every element the created-object undo records overflowed Unity's undo stack (Band2RouteResultsTests, L014).
        // The art is rebuilt idempotently by the level rebuild, so there's nothing to undo.
        static Transform ArtChild(Transform parent, string name, int layer, List<string> changes)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
                changes.Add("created " + parent.name + "/" + name);
            }
            if (child.gameObject.layer != layer) { child.gameObject.layer = layer; changes.Add("set " + parent.name + "/" + name + ".layer"); }
            return child;
        }

        static Transform ArtObject(Transform artRoot, string name, Vector3 at, int layer, List<string> changes)
        {
            Transform t = ArtChild(artRoot, name, layer, changes);
            if (t.position != at) { t.position = at; changes.Add("placed art " + name); }
            return t;
        }

        static SpriteRenderer Child(Transform parent, string name, int layer, List<string> changes)
        {
            Transform t = ArtChild(parent, name, layer, changes);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity;
            SpriteRenderer r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
            r.sortingLayerName = parent.GetComponentInParent<RealityRoot>() is RealityRoot root ? RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay) : r.sortingLayerName;
            return r;
        }

        static SpriteRenderer[] Effects(Transform art, string prefix, int count, Sprite[] sprites, Material material, int order, int layer, List<string> changes)
        {
            var list = new SpriteRenderer[sprites.Length == 0 ? 0 : count];
            for (int i = 0; i < list.Length; i++)
            {
                SpriteRenderer r = Child(art, $"{prefix}_{i:00}", layer, changes);
                r.sprite = sprites[i % sprites.Length]; r.sharedMaterial = material; r.drawMode = SpriteDrawMode.Simple;
                r.sortingOrder = order; r.enabled = false; r.color = Color.white;
                list[i] = r;
            }
            RemoveExtra(art, prefix + "_", list.Length, changes);
            return list;
        }

        static void RemoveChild(Transform art, string name, List<string> changes)
        {
            Transform c = art.Find(name);
            if (c == null) return;
            Object.DestroyImmediate(c.gameObject);
            changes.Add("removed " + art.name + "/" + name);
        }

        static void RemoveExtra(Transform art, string prefix, int keep, List<string> changes)
        {
            foreach (Transform c in art.Cast<Transform>().Where(c => c.name.StartsWith(prefix) && int.TryParse(c.name.Substring(prefix.Length), out int i) && i >= keep).ToList())
            {
                Object.DestroyImmediate(c.gameObject);
                changes.Add("removed " + c.name);
            }
        }

        static void WriteSkin(Object target, string field, HostSkin skin, List<string> changes)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.FindPropertyRelative("HostName").stringValue = skin.HostName ?? "";
            p.FindPropertyRelative("Sprite").objectReferenceValue = skin.Sprite;
            p.FindPropertyRelative("Material").objectReferenceValue = skin.Material;
            p.FindPropertyRelative("Color").colorValue = skin.Color;
            p.FindPropertyRelative("DrawMode").enumValueIndex = (int)skin.DrawMode;
            p.FindPropertyRelative("SortingLayer").stringValue = skin.SortingLayer ?? "";
            p.FindPropertyRelative("TileOrigin").vector2Value = skin.TileOrigin;
            // PAX-A15: a world-tiled host's tiling too, or the saved skin loses it and the presenter never resamples (a tell).
            p.FindPropertyRelative("WorldTiled").boolValue = skin.WorldTiled;
            p.FindPropertyRelative("Tile").vector2Value = skin.Tile;
            p.FindPropertyRelative("UVRect").vector4Value = skin.UVRect;
            if (so.ApplyModifiedPropertiesWithoutUndo()) changes.Add("wrote " + target.name + "." + field);
        }

        static void WriteVectors(Object target, string field, List<Vector2> values, List<string> changes)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).vector2Value = values[i];
            if (so.ApplyModifiedPropertiesWithoutUndo()) changes.Add("wrote " + target.name + "." + field);
        }
    }
}
