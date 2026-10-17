using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Core.Presentation;
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
            bool down = PointsDown(room, e);
            int side = PointsSide(room, e);
            return BuildSpikes(artRoot, e.Name, trap, greybox, rises, down, side, rooms, config, layer, changes, (a0, a1) => BladeReach(room, e, down, side, a0, a1));
        }

        /// <summary>The developer (2026-10-16): no blade may show past the solid that hides it, or pierce one in front of it.
        /// How far a strip's blades may reach from its base (world units) before they'd leave a solid covering the strip (a
        /// floor or a collapsing floor laid over hidden spikes) or enter one standing in front of it, less a small margin;
        /// 0 when nothing limits them. Solids at their authored poses (a level's start). `alongMin`..`alongMax`: the part of the
        /// strip asked about (one blade's span, from the strip's centre along its row); the whole strip by default.</summary>
        internal static float BladeReach(SoloRoomDefinition room, SoloRoomElement e, bool pointsDown, int pointsSide,
            float alongMin = float.NegativeInfinity, float alongMax = float.PositiveInfinity)
        {
            const float Margin = 0.03f, Touch = 0.01f;
            var strip = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
            Vector2 outward = pointsSide != 0 ? new Vector2(pointsSide, 0f) : new Vector2(0f, pointsDown ? -1f : 1f);
            bool sideways = pointsSide != 0;
            float Out(Vector2 p) => Vector2.Dot(p, outward);
            float baseU = Mathf.Min(Out(strip.min), Out(strip.max));
            float c = sideways ? strip.center.y : strip.center.x;
            float a0 = Mathf.Max(sideways ? strip.yMin : strip.xMin, c + alongMin), a1 = Mathf.Min(sideways ? strip.yMax : strip.xMax, c + alongMax);
            float reach = float.PositiveInfinity;
            foreach (SoloRoomSkin.Solid s in SoloRoomSkin.Solids(room))
            {
                if (s.Name == e.Name) continue;
                float b0 = sideways ? s.Rect.yMin : s.Rect.xMin, b1 = sideways ? s.Rect.yMax : s.Rect.xMax;
                if (b1 <= a0 + Touch || b0 >= a1 - Touch) continue;   // beside the strip, not over it
                float u0 = Mathf.Min(Out(s.Rect.min), Out(s.Rect.max)) - baseU, u1 = Mathf.Max(Out(s.Rect.min), Out(s.Rect.max)) - baseU;
                if (u1 <= Touch) continue;                            // behind the base: the strip's host
                reach = Mathf.Min(reach, u0 <= Touch ? u1 : u0);      // a cover: stay inside it; in front: stop at its face
            }
            return float.IsPositiveInfinity(reach) ? 0f : Mathf.Max(0.05f, reach - Margin);
        }

        /// <summary>PAX-107 (D-124): a spiked hinge floor's face, its own spikes art (pointing out of the floor's top face, turned
        /// with it), beside the floor's solid art.</summary>
        static string BuildSpikedFace(Transform artRoot, SoloRoomElement e, MovingTrap mover, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes) =>
            ReadObject(mover, "spikes") is SpriteRenderer face && face != null
                ? BuildSpikes(artRoot, e.Name + "_Spikes", mover, face, true, false, 0, rooms, config, layer, changes) : null;

        static string BuildSpikes(Transform artRoot, string name, RoomTrap trap, SpriteRenderer greybox, bool rises, bool pointsDown, int pointsSide, RoomManager rooms, TrapArtConfig config, int layer, List<string> changes, System.Func<float, float, float> reachOver = null)
        {
            if (greybox == null || config.SpikeStrip == null) return null;
            Transform art = ArtObject(artRoot, name, greybox.transform.position, layer, changes);
            SpikeArt presenter = SetupUtility.Ensure<SpikeArt>(art.gameObject, changes);
            SpriteRenderer body = Child(art, "Spikes", layer, changes);
            body.sprite = config.SpikeStrip; body.sharedMaterial = HazardMaterial(config); body.drawMode = SpriteDrawMode.Tiled;
            body.sortingOrder = greybox.sortingOrder; body.enabled = greybox.enabled;
            SpriteRenderer[] grit = rises ? Effects(art, "Grit", config.GritPuffs, config.Dust, config.TrapMaterial, greybox.sortingOrder + EffectOrder, layer, changes) : Effects(art, "Grit", 0, config.Dust, config.TrapMaterial, 0, layer, changes);
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", name), ("body", body), ("greyboxBody", greybox),
                ("strip", config.SpikeStrip), ("trapMaterial", HazardMaterial(config)), ("pointsDown", pointsDown), ("pointsSide", pointsSide), ("rises", rises));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            SetupUtility.SetArray(presenter, "grit", grit.Cast<Object>().ToArray(), changes);
            BuildBlades(art, presenter, name, greybox, pointsSide, config, layer, changes, reachOver);
            return art.name;
        }

        /// <summary>PAX-A18 (D-151): the strip's blades, one renderer each, laid out once (SpikeLayout, seeded by the element's
        /// name) at the kit's one blade size; SpikeArt poses them every frame. Without painted blades: none (the tiled strip).</summary>
        static void BuildBlades(Transform art, SpikeArt presenter, string name, SpriteRenderer greybox, int pointsSide, TrapArtConfig config, int layer, List<string> changes, System.Func<float, float, float> reachOver)
        {
            Sprite[] sprites = config.SpikeBlades.Where(s => s != null).ToArray();
            Vector2 box = pointsSide != 0 ? new Vector2(greybox.size.y, greybox.size.x) : greybox.size;
            float tallest = sprites.Length > 0 ? sprites.Max(s => s.bounds.size.y) : 0f;
            var blades = new List<SpriteRenderer>();
            var variants = new List<int>();
            var along = new List<float>();
            var reaches = new List<float>();
            var back = new List<bool>();
            if (sprites.Length > 0 && tallest > 0f)
            {
                (float k, _) = SpikeArt.BladeScale(box.y, config.BodyScale * (box.y > SpikeArt.MaxStripHeight ? 1f : config.SpikeHeightScale), tallest);
                float[] widths = sprites.Select(s => s.bounds.size.x * k).ToArray();
                List<SpikeLayout.Blade> layout = SpikeLayout.Lay(box.x, widths, TrapArtMath.Seed(name, 0));
                for (int i = 0; i < layout.Count; i++)
                {
                    SpriteRenderer b = Child(art, "Blade_" + i, layer, changes);
                    b.sprite = sprites[layout[i].Variant]; b.sharedMaterial = HazardMaterial(config); b.drawMode = SpriteDrawMode.Simple;
                    b.sortingOrder = greybox.sortingOrder; b.enabled = greybox.enabled;
                    blades.Add(b); variants.Add(layout[i].Variant); along.Add(layout[i].Along);
                    // Each blade's own reach, over its own span: only the blades under a low solid draw smaller.
                    reaches.Add(reachOver != null ? reachOver(layout[i].Along - layout[i].Width * 0.5f, layout[i].Along + layout[i].Width * 0.5f) : 0f);
                    back.Add(false);
                }
                // The developer (2026-10-16, "Can we overlap them somehow?"): a back row, one blade between each two front ones,
                // smaller and a little dimmer, drawn just behind them, so the strip reads as one dense bed of iron.
                for (int i = 0; i + 1 < layout.Count; i++)
                {
                    int vb = (int)(TrapArtMath.Hash01(TrapArtMath.Seed(name, 0), 2000 + i) * sprites.Length) % sprites.Length;
                    if (vb == layout[i].Variant || vb == layout[i + 1].Variant) vb = (vb + 1) % sprites.Length;
                    float at = (layout[i].Along + layout[i].Width * 0.5f + layout[i + 1].Along - layout[i + 1].Width * 0.5f) * 0.5f;
                    float w = sprites[vb].bounds.size.x * k * SpikeArt.BackScale;
                    SpriteRenderer b = Child(art, "Blade_" + blades.Count, layer, changes);
                    b.sprite = sprites[vb]; b.sharedMaterial = HazardMaterial(config); b.drawMode = SpriteDrawMode.Simple;
                    // Same order as the front row, a hair further back (an order lower, it sorted among the outside masonry and
                    // broke its batches).
                    b.sortingOrder = greybox.sortingOrder; b.enabled = greybox.enabled;
                    b.transform.localPosition = new Vector3(b.transform.localPosition.x, b.transform.localPosition.y, 0.001f);
                    blades.Add(b); variants.Add(vb); along.Add(at); back.Add(true);
                    reaches.Add(reachOver != null ? reachOver(at - w * 0.5f, at + w * 0.5f) : 0f);
                }
            }
            for (int i = blades.Count; art.Find("Blade_" + i) != null; i++) RemoveChild(art, "Blade_" + i, changes);
            SetupUtility.SetArray(presenter, "blades", blades.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "bladeSprites", sprites.Cast<Object>().ToArray(), changes);
            var so = new SerializedObject(presenter);
            SerializedProperty v = so.FindProperty("bladeVariant"), a = so.FindProperty("bladeAlong"), rr = so.FindProperty("bladeReach"), bk = so.FindProperty("bladeBack");
            v.arraySize = variants.Count; a.arraySize = along.Count; rr.arraySize = reaches.Count; bk.arraySize = back.Count;
            for (int i = 0; i < variants.Count; i++)
            {
                v.GetArrayElementAtIndex(i).intValue = variants[i]; a.GetArrayElementAtIndex(i).floatValue = along[i];
                rr.GetArrayElementAtIndex(i).floatValue = reaches[i]; bk.GetArrayElementAtIndex(i).boolValue = back[i];
            }
            so.FindProperty("bladeHeight").floatValue = tallest;
            so.FindProperty("bladeGrowth").floatValue = config.BodyScale * config.SpikeHeightScale;
            if (so.ApplyModifiedPropertiesWithoutUndo()) changes.Add("laid out " + name + "'s " + blades.Count + " blades");
        }

        /// <summary>Spikes hang from a ceiling (point down) when a fixed element's underside meets their top and none meets
        /// their bottom; otherwise they stand on their host and point up.</summary>
        internal static bool PointsDown(SoloRoomDefinition room, SoloRoomElement e)
        {
            static bool Fixed(SoloRoomElementKind k) => k is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.GripWall or SoloRoomElementKind.PitBottom;
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

        /// <summary>A strip taller than it's wide, on a fixed element's side (touching it or set into it), points away from it:
        /// +1 east off a wall to its west, -1 west off one to its east; 0 otherwise (the developer: Thorns_R in L019 drew as
        /// one stretched tooth).</summary>
        internal static int PointsSide(SoloRoomDefinition room, SoloRoomElement e)
        {
            if (e.Size.y <= e.Size.x) return 0;
            static bool Fixed(SoloRoomElementKind k) => k is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.GripWall or SoloRoomElementKind.PitBottom;
            var box = new Rect(e.Position - e.Size * 0.5f, e.Size);
            bool west = false, east = false;
            foreach (SoloRoomElement h in room.Elements)
            {
                if (!Fixed(h.Kind)) continue;
                var r = new Rect(h.Position - h.Size * 0.5f, h.Size);
                if (!(r.yMin < box.yMax - 1e-3f && r.yMax > box.yMin + 1e-3f)) continue;
                if (Mathf.Abs(r.xMax - box.xMin) < 0.06f || (r.xMax > box.xMin && r.xMax <= box.center.x + 1e-3f && r.xMin < box.xMin)) west = true;
                if (Mathf.Abs(r.xMin - box.xMax) < 0.06f || (r.xMin < box.xMax && r.xMin >= box.center.x - 1e-3f && r.xMax > box.xMax)) east = true;
            }
            return west == east ? 0 : west ? 1 : -1;
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
            // PAX-102 (the developer: "falling blocks should leave a hole"): a block in a ceiling left a dark socket once it had
            // gone. PAX-107 ("leaving it black looks weird. It should show the background"): the host's fill leaves the
            // block out instead (SoloRoomSkin.LeavesHole), so the air shows through; no socket.
            SpriteRenderer socket = null;
            RemoveChild(art, "Socket", changes);
            SpriteRenderer[] dust = Effects(art, "Dust", config.SolidDust, config.Dust, config.TrapMaterial, greybox.sortingOrder + EffectOrder, layer, changes);
            // 2026-10-17 (the developer: "Whatever falls needs to crash and look like a pile of rubble and not like a perfect
            // square, but this goes only for the ones that fall and never go back up"): a falling block that has landed shows
            // a heap of broken stone over its footprint instead of its skin (every falling block stays down until its room
            // resets). Not a grip wall's (D-114: its moss is its grip).
            SpriteRenderer rubble = null;
            Vector2 rubbleScale = Vector2.one;
            // (Only one that falls: a block that rises, TrapLab's Block_F, is climbed. And a slam that comes straight down and
            // stays, L016's Slam_1, Slam_3a and Slam_3b: the developer, "I think this will look different once we make it look
            // like rubble".)
            Vector2 drop = kind == SolidArtKind.Mover ? ReadVector(trap, "offset") : Vector2.zero;
            bool staysDown = kind == SolidArtKind.Mover && Mathf.Abs(drop.x) < 1e-4f && drop.y < 0f && ReadInt(trap, "returnTicks") == 0
                && ReadInt(trap, "repeatMode") == (int)TrapRepeatMode.Once && ReadInt(trap, "hingeTicks") == 0 && !(Prop(trap, "riseFirst")?.boolValue ?? false);
            bool falls = (kind == SolidArtKind.FallingBlock && (FallingBlockDirection)ReadInt(trap, "direction") == FallingBlockDirection.Down) || staysDown;
            var rubbleSlot = falls && !e.Settings.Grip.IsConfigured ? RubbleSlot(room, greybox.size, e.Name) : null;
            if (rubbleSlot.HasValue)
            {
                (Sprite heapSprite, Material heapMask, Vector2 footprint) = rubbleSlot.Value;
                rubble = Child(art, "Rubble", layer, changes);
                rubble.sprite = heapSprite; rubble.sharedMaterial = heapMask; rubble.drawMode = SpriteDrawMode.Simple;
                rubble.color = SoloRoomSkin.Body;
                rubble.sortingOrder = greybox.sortingOrder; rubble.enabled = false;
                rubbleScale = new Vector2(greybox.size.x / footprint.x, greybox.size.y / footprint.y);
            }
            else RemoveChild(art, "Rubble", changes);

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
                    if (rubble != null) landTick = moveTicks;   // a slam that stays: rubble once it's down
                    break;
                case SolidArtKind.Shrinker:
                    shrinkTicks = ReadInt(trap, "shrinkTicks");
                    break;
            }
            // PAX-A18 (D-151): a slam's iron striking plate and a hinge floor's knuckle, children of the skin (they move with it).
            SpriteRenderer plate = null, knuckle = null;
            float knuckleAlong = 0f;
            bool slams = kind == SolidArtKind.Mover && (Prop(trap, "slams")?.boolValue ?? false);
            bool hinges = kind == SolidArtKind.Mover && ReadInt(trap, "hingeTicks") > 0;
            if (slams && config.SlamPlate != null)
            {
                plate = Child(skin.transform, "Plate", layer, changes);
                // One order above the skin and its trims (drawn after them), in the skin's own plane (inside its grey-box's bounds).
                plate.sprite = config.SlamPlate; plate.sharedMaterial = config.TrapMaterial; plate.sortingOrder = greybox.sortingOrder + 1;
                plate.transform.localPosition = Vector3.zero;
            }
            else RemoveChild(skin.transform, "Plate", changes);
            if (hinges && config.HingeKnuckle != null)
            {
                knuckle = Child(skin.transform, "Knuckle", layer, changes);
                knuckle.sprite = config.HingeKnuckle; knuckle.sharedMaterial = config.TrapMaterial; knuckle.drawMode = SpriteDrawMode.Simple;
                knuckle.sortingOrder = greybox.sortingOrder + 1;
                knuckle.transform.localPosition = Vector3.zero;
                Vector2 pivot = ReadVector(trap, "hingePivot");
                knuckleAlong = Mathf.Sign(pivot.x == 0f ? -1f : pivot.x) * greybox.size.x * 0.5f;
            }
            else RemoveChild(skin.transform, "Knuckle", changes);
            TrapKitSetup.Write(presenter, changes, ("plate", plate), ("knuckle", knuckle), ("knuckleAlong", knuckleAlong));
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("kind", (int)kind), ("skin", skin), ("greyboxBody", greybox),
                ("restPivot", (Vector2)trap.transform.position), ("moveDirection", direction), ("landTick", landTick), ("moveTicks", moveTicks),
                ("holdTicks", holdTicks), ("returnTicks", returnTicks), ("shrinkTicks", shrinkTicks), ("crack", crack), ("socket", socket),
                ("rubble", rubble), ("rubbleScale", rubbleScale));
            WriteSkin(presenter, "hostSkin", skinLook, changes);
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            SetupUtility.SetArray(presenter, "dust", dust.Cast<Object>().ToArray(), changes);
            return art.name;
        }

        // A fallen block's heap for a footprint: the shape nearest its proportions (a square block, a wide one, a slab), a
        // variant by its name, on the room's stone; and the footprint the heap was drawn for (env_bumpy.RUBBLE_SHAPES).
        static (Sprite heap, Material mask, Vector2 footprint)? RubbleSlot(SoloRoomDefinition room, Vector2 size, string name)
        {
            float aspect = size.x / Mathf.Max(0.01f, size.y);
            (string shape, Vector2 footprint) = aspect >= 3f ? ("Slab", new Vector2(2f, 0.5f)) : aspect >= 1.45f ? ("Wide", new Vector2(2f, 1f)) : ("Sq", Vector2.one);
            Sprite heap = EnvironmentKit.Sprite($"ENV_RubbleHeap_{shape}_{(int)(SoloRoomSkin.Hash(name, 930) * 2f)}");
            Sprite fill = SoloRoomSkin.FillSprite(room);
            Material mask = fill != null && fill.name.StartsWith("ENV_Fill_")
                ? EnvironmentKit.TileMaterial(EnvironmentKit.EdgeKind(EnvironmentKit.RubbleKind, fill.name.Substring("ENV_Fill_".Length))) : null;
            if (heap == null || mask == null) return null;
            return (heap, mask, footprint);
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
