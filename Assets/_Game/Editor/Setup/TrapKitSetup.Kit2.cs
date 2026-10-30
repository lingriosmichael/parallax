using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-119/120/121 (D-197): the builders for pressure plates, portals and aiming turrets (the conveyor was removed, D-199).
    // Placeholder art: a plate slab, two coloured portal boxes (with the environment kit, D-199: two stone arches with coloured veils), a
    // grey turret with a red line.
    public static partial class TrapKitSetup
    {
        static readonly Color PlateColor = new(.95f, .8f, .2f, 1f);
        static readonly Color PortalIn = new(.3f, .6f, 1f, .55f), PortalOut = new(1f, .45f, .9f, .55f);
        static readonly Color TurretGrey = new(.25f, .25f, .3f, 1f), BeamRed = new(1f, .2f, .2f, .6f);

        // A pressure plate: a trigger box over the plate and the cat's height, its look (a slab child) flush on the floor.
        internal static PlateTrap BuildPlateCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, List<string> changes)
        {
            GameObject go = CreateTrap<PlateTrap>(parent, root, name, position + new Vector2(0f, .3f), new Vector2(size.x, size.y + .6f), Color.clear, roomId, rooms, death, observers, true, null, changes);
            Transform slab = SetupUtility.EnsureChild(go.transform, name + "_Slab", root.gameObject.layer, changes);
            slab.localPosition = new Vector3(0f, -.3f, 0f); slab.localRotation = Quaternion.identity; slab.localScale = Vector3.one;
            SpriteRenderer look = SetupUtility.SetVisual(slab.gameObject, root, size, PlateColor, changes);
            if (look.sortingOrder != 1) { look.sortingOrder = 1; changes.Add("set " + slab.name + ".sortingOrder"); }
            PlateTrap trap = go.GetComponent<PlateTrap>();
            Write(trap, changes, ("look", slab));
            return trap;
        }

        // A portal: its entry box (a trigger on the trap's object, drawn in PortalIn) and its "Exit" child (drawn in PortalOut), with
        // the exit's path from the timing's PathSettings.
        internal static PortalTrap BuildPortalCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Vector2 exitLocal, Vector2 exitSize, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, List<string> changes)
        {
            GameObject go = CreateTrap<PortalTrap>(parent, root, name, position, size, PortalIn, roomId, rooms, death, observers, true, 1, changes);
            Transform exit = SetupUtility.EnsureChild(go.transform, "Exit", root.gameObject.layer, changes);
            exit.localPosition = exitLocal; exit.localRotation = Quaternion.identity; exit.localScale = Vector3.one;
            SpriteRenderer look = SetupUtility.SetVisual(exit.gameObject, root, exitSize, PortalOut, changes);
            if (look.sortingOrder != 1) { look.sortingOrder = 1; changes.Add("set " + name + ".Exit.sortingOrder"); }
            // D-199 (L021 "the art is not clean"): with the environment kit on, each end is the door's stone arch with a coloured veil
            // in its opening (blue in, magenta out) in place of the flat box; the exit's veil blinks its warning.
            if (PortalArch(go.transform, size, PortalIn, root, changes) is SpriteRenderer && PortalArch(exit, exitSize, PortalOut, root, changes) is SpriteRenderer veil)
            {
                foreach (SpriteRenderer box in new[] { go.GetComponent<SpriteRenderer>(), look })
                    if (box != null && box.enabled) { box.enabled = false; changes.Add("disabled " + box.name + " greybox"); }
                look = veil;
            }
            PathSettings path = settings.Path;
            PortalTrap trap = go.GetComponent<PortalTrap>();
            Write(trap, changes, ("exit", exit), ("exitLook", look), ("exitHome", exitLocal), ("exitSize", exitSize), ("twoWay", settings.Portal.TwoWay),
                ("exitPath", path.HasPoints ? path.Points : new Vector2[0]), ("exitGlitch", path.HasPoints && path.Glitch), ("warnTicks", path.HasPoints ? path.WarnTicks : 0),
                ("moveTicks", Mathf.Max(1, settings.MoveTicks)), ("holdTicks", settings.HoldTicks), ("returnTicks", settings.ReturnTicks));
            return trap;
        }

        // D-199: a portal end's look: the door's arch (ENV_Door, standing on the box's foot), a soft glow behind it and a veil over its
        // opening, both in `tint`. The veil, or null without the environment kit.
        static SpriteRenderer PortalArch(Transform end, Vector2 size, Color tint, RealityRoot root, List<string> changes)
        {
            Sprite arch = SoloRoomSkin.Enabled ? Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Door") : null;
            Sprite glow = Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Glow");
            if (arch == null || glow == null) return null;
            string band = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            SpriteRenderer Part(string part, Sprite sprite, Vector2 at, Vector2 scale, Color color, int order, Material material)
            {
                Transform t = SetupUtility.EnsureChild(end, part, end.gameObject.layer, changes);
                t.localPosition = at; t.localRotation = Quaternion.identity; t.localScale = new Vector3(scale.x, scale.y, 1f);
                var r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                r.sprite = sprite; r.color = color; r.sortingLayerName = band; r.sortingOrder = order;
                if (material != null) r.sharedMaterial = material;
                return r;
            }
            Vector2 foot = new(0f, -size.y * .5f - arch.bounds.min.y);
            Vector2 opening = new(arch.bounds.size.x * .42f, arch.bounds.size.y * .7f), mid = foot + new Vector2(0f, arch.bounds.center.y - .05f);
            float g = 2.4f / glow.bounds.size.x;
            Part("ArchGlow", glow, mid, new Vector2(g, g), new Color(tint.r, tint.g, tint.b, .45f), -4, EnvironmentStackSetup.GlowMaterial);
            Part("Arch", arch, foot, Vector2.one, Color.white, -2, null);
            return Part("Veil", glow, mid, new Vector2(opening.x / glow.bounds.size.x * 1.6f, opening.y / glow.bounds.size.y * 1.4f),
                new Color(tint.r, tint.g, tint.b, .9f), -1, Parallax.Editor.Art.EnvironmentKit.UnlitMaterial);
        }

        // An aiming turret: its body (grey, no collider; it sits in a solid) and its "Line" child, drawn and sized by the trap.
        internal static TurretTrap BuildTurretCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, TurretSettings k, List<string> changes)
        {
            GameObject go = CreateTrap<TurretTrap>(parent, root, name, position, size, TurretGrey, roomId, rooms, death, observers, false, 0, changes, box: false);
            Transform lineT = SetupUtility.EnsureChild(go.transform, "Line", root.gameObject.layer, changes);
            SpriteRenderer line = SetupUtility.SetVisual(lineT.gameObject, root, new Vector2(1f, k.BeamWidth), BeamRed, changes);
            if (line.sortingOrder != 2) { line.sortingOrder = 2; changes.Add("set " + name + ".Line.sortingOrder"); }
            line.enabled = false;
            TurretTrap trap = go.GetComponent<TurretTrap>();
            Write(trap, changes, ("line", line), ("aimTicks", k.AimTicks), ("lockTicks", k.LockTicks), ("fireTicks", k.FireTicks), ("beamWidth", k.BeamWidth), ("range", k.Range),
                ("ceilingMount", k.CeilingMount), ("aimZoneMin", k.AimZone.min), ("aimZoneSize", k.AimZone.size));
            return trap;
        }
    }
}
