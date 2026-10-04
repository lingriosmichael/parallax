using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // PAX-A13 (§12 R10): the rest of the kit's builders. Each reads its trap's serialized settings (never writes them) and
    // wires a presenter that mirrors the trap's grey-box renderers.
    public static partial class TrapArtSetup
    {
        /// <summary>Builds the art for one element of a kind this file owns; returns its art object's name, or null.</summary>
        static string BuildKitElement(Transform artRoot, Transform roomRoot, SoloRoomDefinition room, SoloRoomElement e, Transform t, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            if (t.TryGetComponent(out HiddenSpikesTrap hidden)) return BuildSpikes(artRoot, room, e, hidden, hidden.GetComponent<SpriteRenderer>(), true, rooms, config, layer, changes);
            if (t.TryGetComponent(out MovingTrap mover))
                return mover.GetComponent<Hazard>() != null || (MovingTrapKind)ReadInt(mover, "kind") == MovingTrapKind.Hazard
                    ? BuildSpikes(artRoot, room, e, mover, mover.GetComponent<SpriteRenderer>(), false, rooms, config, layer, changes)
                    : BuildSolid(artRoot, roomRoot, room, e, mover, mover.GetComponent<SpriteRenderer>(), SolidArtKind.Mover, rooms, config, layer, changes);
            if (t.TryGetComponent(out FallingBlockTrap block)) return BuildSolid(artRoot, roomRoot, room, e, block, block.GetComponent<SpriteRenderer>(), SolidArtKind.FallingBlock, rooms, config, layer, changes);
            if (t.TryGetComponent(out ShrinkingFloorTrap shrink)) return BuildSolid(artRoot, roomRoot, room, e, shrink, (SpriteRenderer)ReadObject(shrink, "visual"), SolidArtKind.Shrinker, rooms, config, layer, changes);
            if (t.TryGetComponent(out GravityFlipTrap flip)) return BuildFlip(artRoot, e, flip, rooms, config, layer, changes);
            if (t.TryGetComponent(out InverterTrap inverter)) return BuildInverter(artRoot, e, inverter, rooms, config, layer, changes);
            if (t.TryGetComponent(out StormCloudTrap cloud)) return BuildCloud(artRoot, e, cloud, rooms, config, layer, changes);
            if (t.TryGetComponent(out ClimbVine vine) && vine.Snaps) return BuildVine(artRoot, e, vine, rooms, config, layer, changes);
            if (t.TryGetComponent(out DoorRetreatTrap retreat)) return BuildDoorRetreat(artRoot, room, e, retreat, rooms, config, layer, changes);
            if (t.TryGetComponent(out Hazard hazard) && t.GetComponent<RoomTrap>() == null) return BuildSpikes(artRoot, room, e, null, hazard.GetComponent<SpriteRenderer>(), false, rooms, config, layer, changes);
            return null;
        }

        // ---------- spikes: static, pit floors, hidden, periodic, sliding ----------

        static string BuildSpikes(Transform artRoot, SoloRoomDefinition room, SoloRoomElement e, RoomTrap trap, SpriteRenderer greybox, bool rises, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            if (greybox == null || config.SpikeStrip == null) return null;
            Transform art = ArtObject(artRoot, e.Name, greybox.transform.position, layer, changes);
            SpikeArt presenter = SetupUtility.Ensure<SpikeArt>(art.gameObject, changes);
            SpriteRenderer body = Child(art, "Spikes", layer, changes);
            body.sprite = config.SpikeStrip; body.sharedMaterial = HazardMaterial(config); body.drawMode = SpriteDrawMode.Tiled;
            body.sortingOrder = greybox.sortingOrder; body.enabled = greybox.enabled;
            SpriteRenderer[] grit = rises ? Effects(art, "Grit", config.GritPuffs, config.Dust, config.TrapMaterial, greybox.sortingOrder + EffectOrder, layer, changes) : Effects(art, "Grit", 0, config.Dust, config.TrapMaterial, 0, layer, changes);
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("body", body), ("greyboxBody", greybox),
                ("strip", config.SpikeStrip), ("trapMaterial", HazardMaterial(config)), ("pointsDown", PointsDown(room, e)), ("rises", rises));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            SetupUtility.SetArray(presenter, "grit", grit.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        /// <summary>Spikes hang from a ceiling (point down) when a fixed element's underside meets their top and none meets
        /// their bottom; otherwise they stand on their host and point up.</summary>
        internal static bool PointsDown(SoloRoomDefinition room, SoloRoomElement e)
        {
            static bool Fixed(SoloRoomElementKind k) => k is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.PitBottom;
            var box = new Rect(e.Position - e.Size * 0.5f, e.Size);
            bool above = false, below = false;
            foreach (SoloRoomElement h in room.Elements)
            {
                if (!Fixed(h.Kind)) continue;
                var r = new Rect(h.Position - h.Size * 0.5f, h.Size);
                bool xOverlap = r.xMin < box.xMax - 1e-3f && r.xMax > box.xMin + 1e-3f;
                if (!xOverlap) continue;
                if (Mathf.Abs(r.yMin - box.yMax) < 0.06f || (r.yMin < box.yMax && r.yMin > box.center.y && r.yMax > box.yMax)) above = true;
                if (Mathf.Abs(r.yMax - box.yMin) < 0.06f || (r.yMax > box.yMin && r.yMax < box.center.y && r.yMin < box.yMin)) below = true;
            }
            return above && !below;
        }

        // PAX-102: a falling block inside the room's roof (the highest ceiling), in a room with no gravity flip.
        public static bool IsRoofBlock(SoloRoomDefinition room, SoloRoomElement block)
        {
            if (block.Kind != SoloRoomElementKind.FallingBlock || room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip)) return false;
            var ceilings = room.Elements.Where(x => x.Kind == SoloRoomElementKind.Ceiling).Select(x => new Rect(x.Position - x.Size * 0.5f, x.Size)).ToList();
            if (ceilings.Count == 0) return false;
            float roof = ceilings.Max(c => c.yMax);
            var box = new Rect(block.Position - block.Size * 0.5f, block.Size);
            return ceilings.Any(c => c.yMax >= roof - 0.05f && c.Overlaps(box));
        }

        // ---------- solids: falling blocks, moving floors, shrinking floors ----------

        static string BuildSolid(Transform artRoot, Transform roomRoot, SoloRoomDefinition room, SoloRoomElement e, RoomTrap trap, SpriteRenderer greybox, SolidArtKind kind, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            if (greybox == null) return null;
            HostSkin skinLook = HostSkin.Of(HostRenderer(roomRoot, room, e, floorFirst: kind != SolidArtKind.FallingBlock));
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            SolidArt presenter = SetupUtility.Ensure<SolidArt>(art.gameObject, changes);
            SpriteRenderer skin = Child(art, "Skin", layer, changes);
            skinLook.ApplyTo(skin, greybox.size);
            skin.transform.localScale = Vector3.one;
            skin.sortingOrder = greybox.sortingOrder; skin.enabled = greybox.enabled;

            SpriteRenderer crack = null;
            if (kind == SolidArtKind.FallingBlock && config.BlockCrack != null)
            {
                crack = Child(art, "Crack", layer, changes);
                crack.sprite = config.BlockCrack; crack.sharedMaterial = config.TrapMaterial; crack.drawMode = SpriteDrawMode.Simple;
                crack.sortingOrder = greybox.sortingOrder + 1; crack.enabled = false;
            }
            else RemoveChild(art, "Crack", changes);
            // PAX-102 (the developer: "falling blocks should leave a hole"): a block in the room's roof, in a room with no gravity
            // flip (no cat stands on a roof's underside), gets its socket: hidden at its authored pose, shown once it has left.
            SpriteRenderer socket = null;
            Sprite hole = kind == SolidArtKind.FallingBlock && IsRoofBlock(room, e) ? EnvironmentKit.Sprite("ENV_Socket") : null;
            if (hole != null)
            {
                socket = Child(art, "Socket", layer, changes);
                socket.sprite = hole; socket.sharedMaterial = config.TrapMaterial; socket.drawMode = SpriteDrawMode.Simple;
                socket.transform.localPosition = Vector3.zero;
                socket.transform.localScale = new Vector3(greybox.size.x / hole.bounds.size.x, greybox.size.y / hole.bounds.size.y, 1f);
                socket.sortingOrder = greybox.sortingOrder + 1; socket.enabled = false;
            }
            else RemoveChild(art, "Socket", changes);
            SpriteRenderer[] dust = Effects(art, "Dust", config.SolidDust, config.Dust, config.TrapMaterial, greybox.sortingOrder + EffectOrder, layer, changes);

            Vector2 direction = Vector2.zero;
            int landTick = 0, moveTicks = 0, holdTicks = 0, returnTicks = 0, shrinkTicks = 0;
            switch (kind)
            {
                case SolidArtKind.FallingBlock:
                    direction = (FallingBlockDirection)ReadInt(trap, "direction") == FallingBlockDirection.Up ? Vector2.up : Vector2.down;
                    landTick = Mathf.CeilToInt(ReadFloat(trap, "travelDistance") / Mathf.Max(1e-4f, ReadFloat(trap, "unitsPerTick")) - 1e-4f);
                    break;
                case SolidArtKind.Mover:
                    Vector2 offset = ReadVector(trap, "offset");
                    direction = offset.sqrMagnitude > 0f ? offset.normalized : Vector2.right;
                    moveTicks = ReadInt(trap, "moveTicks"); holdTicks = ReadInt(trap, "holdTicks"); returnTicks = ReadInt(trap, "returnTicks");
                    break;
                case SolidArtKind.Shrinker:
                    shrinkTicks = ReadInt(trap, "shrinkTicks");
                    break;
            }
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("kind", (int)kind), ("skin", skin), ("greyboxBody", greybox),
                ("restPivot", (Vector2)trap.transform.position), ("moveDirection", direction), ("landTick", landTick), ("moveTicks", moveTicks),
                ("holdTicks", holdTicks), ("returnTicks", returnTicks), ("shrinkTicks", shrinkTicks), ("crack", crack), ("socket", socket));
            WriteSkin(presenter, "hostSkin", skinLook, changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            SetupUtility.SetArray(presenter, "dust", dust.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- gravity flips ----------

        static string BuildFlip(Transform artRoot, SoloRoomElement e, GravityFlipTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            SpriteRenderer zone = trap.GetComponent<SpriteRenderer>();
            bool visible = zone != null && zone.color.a > 0f;
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            FlipArt presenter = SetupUtility.Ensure<FlipArt>(art.gameObject, changes);
            SpriteRenderer ring = null;
            SpriteRenderer[] motes = new SpriteRenderer[0];
            if (visible)
            {
                ring = Child(art, "Glyph", layer, changes);
                ring.sprite = config.GlyphRing; ring.sharedMaterial = config.TrapMaterial; ring.drawMode = SpriteDrawMode.Simple; ring.sortingOrder = zone.sortingOrder;
                motes = Effects(art, "Mote", config.FlipMotes, new[] { config.Mote }, config.TrapMaterial, zone.sortingOrder + 1, layer, changes);
            }
            else { RemoveChild(art, "Glyph", changes); Effects(art, "Mote", 0, new[] { config.Mote }, config.TrapMaterial, 0, layer, changes); }
            SpriteRenderer pulse = Child(art, "Pulse", layer, changes);
            pulse.sprite = config.Ring; pulse.sharedMaterial = config.TrapMaterial; pulse.drawMode = SpriteDrawMode.Simple;
            pulse.sortingOrder = (zone != null ? zone.sortingOrder : 0) + EffectOrder; pulse.enabled = false;
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("ring", ring), ("greyboxZone", visible ? zone : null),
                ("glyphRing", config.GlyphRing), ("pulse", pulse), ("zoneSize", e.Size));
            SetupUtility.SetArray(presenter, "greybox", visible ? new Object[] { zone } : new Object[0], changes);
            SetupUtility.SetArray(presenter, "motes", motes.Cast<Object>().ToArray(), changes);
            BuildFlipHalo(ring, layer, changes);
            return art.name;
        }

        /// <summary>PAX-A15 (A3): a dark soft backing behind a visible flip's glyph ring, so the gold ring reads against a gold
        /// sky. A child of the ring (it takes the ring's fitted scale), shown exactly when the ring is (ShownWith). Only with
        /// the environment kit on; the ring's own art and the trap are unchanged.</summary>
        static void BuildFlipHalo(SpriteRenderer ring, int layer, List<string> changes)
        {
            if (ring == null) return;
            var look = AssetDatabase.LoadAssetAtPath<Parallax.Editor.Levels.LevelLookConfig>(Parallax.Editor.Levels.LevelLookConfig.AssetPath);
            Sprite halo = SoloRoomSkin.Enabled ? EnvironmentKit.Sprite("ENV_Halo") : null;
            if (halo == null || look == null || ring.sprite == null) { RemoveChild(ring.transform, "Halo", changes); return; }
            SpriteRenderer backing = Child(ring.transform, "Halo", layer, changes);
            backing.sprite = halo; backing.sharedMaterial = EnvironmentKit.UnlitMaterial; backing.drawMode = SpriteDrawMode.Simple;
            backing.sortingOrder = ring.sortingOrder - 1;
            backing.color = new Color(1f, 1f, 1f, look.FlipHaloAlpha);
            float k = ring.sprite.bounds.size.x * look.FlipHaloScale / halo.bounds.size.x;
            backing.transform.localScale = new Vector3(k, k, 1f);
            ShownWith follow = SetupUtility.Ensure<ShownWith>(backing.gameObject, changes);
            TrapKitSetup.Write(follow, changes, ("owner", ring));
            SetupUtility.SetArray(follow, "renderers", new Object[] { backing }, changes);
            follow.Apply();
        }

        // ---------- the inverter ----------

        static string BuildInverter(Transform artRoot, SoloRoomElement e, InverterTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            var greyOrb = (SpriteRenderer)ReadObject(trap, "orb");
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            InverterArt presenter = SetupUtility.Ensure<InverterArt>(art.gameObject, changes);
            SpriteRenderer orb = null;
            if (greyOrb != null)
            {
                orb = Child(art, "Orb", layer, changes);
                orb.sprite = config.InverterOrb; orb.sharedMaterial = config.TrapMaterial; orb.drawMode = SpriteDrawMode.Simple; orb.sortingOrder = greyOrb.sortingOrder;
            }
            else RemoveChild(art, "Orb", changes);
            SpriteRenderer ring = Child(art, "CueRing", layer, changes), mark = Child(art, "CueMark", layer, changes);
            ring.sprite = config.CueRing; ring.sharedMaterial = config.TrapMaterial; ring.sortingOrder = trap.Ring != null ? trap.Ring.sortingOrder : -1; ring.enabled = false;
            mark.sprite = config.CueMark; mark.sharedMaterial = config.TrapMaterial; mark.sortingOrder = trap.Mark != null ? trap.Mark.sortingOrder : 1; mark.enabled = false;
            SpriteRenderer flare = Child(art, "Flare", layer, changes);
            flare.sprite = config.Ring; flare.sharedMaterial = config.TrapMaterial; flare.drawMode = SpriteDrawMode.Simple;
            flare.sortingOrder = (greyOrb != null ? greyOrb.sortingOrder : 0) + EffectOrder; flare.enabled = false;
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("orb", orb), ("greyboxOrb", greyOrb),
                ("ring", ring), ("greyboxRing", trap.Ring), ("mark", mark), ("greyboxMark", trap.Mark), ("orbSprite", config.InverterOrb),
                ("ringSprite", config.CueRing), ("markSprite", config.CueMark), ("zoneSize", e.Size), ("ringSize", TrapKitSetup.CueRingSize),
                ("markSize", TrapKitSetup.CueMarkSize), ("flare", flare));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greyOrb, trap.Ring, trap.Mark }.Where(o => o != null).ToArray(), changes);
            return art.name;
        }

        // ---------- the storm cloud ----------

        static string BuildCloud(Transform artRoot, SoloRoomElement e, StormCloudTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            CloudArt presenter = SetupUtility.Ensure<CloudArt>(art.gameObject, changes);
            int order = trap.Cloud != null ? trap.Cloud.sortingOrder : 1;
            SpriteRenderer cloud = Child(art, "Cloud", layer, changes);
            cloud.sprite = config.StormCloud; cloud.sharedMaterial = config.TrapMaterial; cloud.drawMode = SpriteDrawMode.Simple; cloud.sortingOrder = order;
            SpriteRenderer target = Child(art, "Target", layer, changes);
            target.sprite = config.BoltSegment; target.sharedMaterial = config.TrapMaterial; target.drawMode = SpriteDrawMode.Sliced; target.sortingOrder = order; target.enabled = false;
            SpriteRenderer[] bolt = Effects(art, "Bolt", config.BoltSegments, new[] { config.BoltSegment }, config.TrapMaterial, order + 1, layer, changes);
            SpriteRenderer glow = Child(art, "ChargeGlow", layer, changes), flash = Child(art, "Flash", layer, changes), scorch = Child(art, "Scorch", layer, changes);
            foreach ((SpriteRenderer r, Sprite s, Color c) in new[] { (glow, config.Flash, new Color(1f, 0.8f, 0.45f, 1f)), (flash, config.Flash, new Color(1f, 0.97f, 0.85f, 1f)), (scorch, config.Scorch, Color.white) })
            {
                r.sprite = s; r.sharedMaterial = config.TrapMaterial; r.drawMode = SpriteDrawMode.Simple; r.color = c; r.sortingOrder = order + EffectOrder; r.enabled = false;
            }
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("cloud", cloud), ("greyboxCloud", trap.Cloud),
                ("target", target), ("greyboxTarget", trap.Target), ("greyboxBolt", trap.Bolt), ("cloudSprite", config.StormCloud), ("targetSprite", config.BoltSegment),
                ("cloudSize", ReadVector(trap, "cloudSize")), ("firstStrikeDelay", ReadInt(trap, "firstStrikeDelay")), ("strikePeriod", ReadInt(trap, "strikePeriod")),
                ("tellTicks", ReadInt(trap, "tellTicks")), ("chargeGlow", glow), ("flash", flash), ("scorch", scorch));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { trap.Cloud, trap.Target, trap.Bolt }.Where(o => o != null).ToArray(), changes);
            SetupUtility.SetArray(presenter, "bolt", bolt.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- snap vines ----------

        static string BuildVine(Transform artRoot, SoloRoomElement e, ClimbVine vine, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            Object[] segments = ReadObjects(vine, "segments");
            Transform art = ArtObject(artRoot, e.Name, vine.transform.position, layer, changes);
            VineArt presenter = SetupUtility.Ensure<VineArt>(art.gameObject, changes);
            int order = segments.Length > 0 && segments[0] is SpriteRenderer s0 ? s0.sortingOrder : 0;
            SpriteRenderer[] pieces = Effects(art, "Piece", segments.Length, new[] { config.Mote }, config.TrapMaterial, order, layer, changes);
            SpriteRenderer[] leaves = Effects(art, "Leaf", config.VineLeaves, config.Leaves, config.TrapMaterial, order + 1, layer, changes);
            TrapKitSetup.Write(presenter, changes, ("trap", vine), ("rooms", rooms), ("seedName", e.Name));
            SetupUtility.SetArray(presenter, "segments", segments, changes);
            SetupUtility.SetArray(presenter, "pieces", pieces.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "leaves", leaves.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- the retreating door ----------

        static string BuildDoorRetreat(Transform artRoot, SoloRoomDefinition room, SoloRoomElement e, DoorRetreatTrap trap, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            var door = (Transform)ReadObject(trap, "doorRoot");
            SoloRoomElement? doorElement = room.Elements.Cast<SoloRoomElement?>().FirstOrDefault(x => x.Value.Kind == SoloRoomElementKind.Door);
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            DoorRetreatArt presenter = SetupUtility.Ensure<DoorRetreatArt>(art.gameObject, changes);
            SpriteRenderer[] dust = Effects(art, "Dust", config.DoorDust, config.Dust, config.TrapMaterial, 3, layer, changes);
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("door", door),
                ("doorSize", doorElement.HasValue ? doorElement.Value.Size : new Vector2(0.6f, 1.5f)), ("moveTicks", ReadInt(trap, "moveTicks")));
            SetupUtility.SetArray(presenter, "dust", dust.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- the room's death effects ----------

        static string BuildDeath(Transform artRoot, SoloRoomDefinition room, Transform roomRoot, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes)
        {
            Transform art = ArtObject(artRoot, "Death", roomRoot.position, layer, changes);
            DeathArt presenter = SetupUtility.Ensure<DeathArt>(art.gameObject, changes);
            SpriteRenderer[] puffs = Effects(art, "Puff", config.DeathPuffs, config.Dust, config.TrapMaterial, 5, layer, changes);
            Object death = rooms != null ? ReadObject(rooms, "roomDeath") : null, observers = rooms != null ? ReadObject(rooms, "observers") : null;
            var pits = room.Elements.Where(x => x.Kind == SoloRoomElementKind.Hazard && x.HazardRole == SoloRoomHazardRole.OpeningBottom)
                .Select(x => roomRoot.Find(x.Name)).Where(x => x != null).Select(x => (Object)x.GetComponent<Hazard>()).Where(h => h != null).ToArray();
            TrapKitSetup.Write(presenter, changes, ("rooms", rooms), ("seedName", "Death"), ("death", death), ("observers", observers),
                ("spark", config.Glint), ("grit", config.Dust.FirstOrDefault()), ("dust", config.Dust.LastOrDefault()), ("splash", config.Spray.FirstOrDefault()),
                ("flashSprite", config.Flash), ("scorchSprite", config.Scorch));
            SetupUtility.SetArray(presenter, "pits", pits, changes);
            SetupUtility.SetArray(presenter, "puffs", puffs.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // ---------- reading a trap's settings (never writing them) ----------

        static SerializedProperty Prop(Object target, string field)
        {
            SerializedProperty p = new SerializedObject(target).FindProperty(field);
            if (p == null) Debug.LogError($"TrapArtSetup: {target.GetType().Name} has no serialized '{field}'.");
            return p;
        }

        static int ReadInt(Object target, string field) => Prop(target, field)?.intValue ?? 0;
        static float ReadFloat(Object target, string field) => Prop(target, field)?.floatValue ?? 0f;
        static Vector2 ReadVector(Object target, string field) => Prop(target, field)?.vector2Value ?? Vector2.zero;
        static Object ReadObject(Object target, string field) => Prop(target, field)?.objectReferenceValue;

        static Object[] ReadObjects(Object target, string field)
        {
            SerializedProperty p = Prop(target, field);
            if (p == null || !p.isArray) return new Object[0];
            var list = new Object[p.arraySize];
            for (int i = 0; i < list.Length; i++) list[i] = p.GetArrayElementAtIndex(i).objectReferenceValue;
            return list;
        }
    }
}
