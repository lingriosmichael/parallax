using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // D-292: the builders for the painted set: spinners and pendulums, pressure plates, spiked undersides, and Obsidian's phase, glitch
    // and flip platforms. Each reads its trap's settings (never writes them) and wires a presenter over its grey-box.
    public static partial class TrapArtSetup
    {
        static string BuildSpinner(Transform artRoot, SpinnerTrap trap, SoloRoomElement e, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes)
        {
            var rotor = (Transform)ReadObject(trap, "rotor");
            if (rotor == null) return null;
            SpriteRenderer[] arms = rotor.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.Contains("_Arm")).ToArray();
            SpriteRenderer hubBox = rotor.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name.EndsWith("_Hub"));
            bool pendulum = ReadInt(trap, "swingPeriod") > 0;
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            SpinnerArt presenter = SetupUtility.Ensure<SpinnerArt>(art.gameObject, changes);
            SpriteRenderer hub = Child(art, "Hub", layer, changes);
            hub.sprite = config.SpinnerHub; hub.sharedMaterial = config.TrapMaterial; hub.drawMode = SpriteDrawMode.Simple; hub.sortingOrder = 1;
            var drawn = new List<SpriteRenderer>();
            for (int i = 0; i < arms.Length; i++)
            {
                SpriteRenderer arm = Child(art, "Arm" + i, layer, changes);
                arm.sprite = pendulum ? config.PendulumShaft : config.SpinnerArm; arm.sharedMaterial = config.TrapMaterial; arm.drawMode = SpriteDrawMode.Tiled; arm.tileMode = SpriteTileMode.Continuous;
                arm.sortingOrder = 0; arm.enabled = false;
                drawn.Add(arm);
            }
            SpriteRenderer ring = null, head = null;
            if (pendulum && drawn.Count > 0)
            {
                ring = Child(drawn[0].transform, "Ring", layer, changes);
                ring.sprite = config.PendulumRing; ring.sharedMaterial = config.TrapMaterial; ring.drawMode = SpriteDrawMode.Simple; ring.sortingOrder = 1;
                head = Child(drawn[0].transform, "Head", layer, changes);
                head.sprite = config.PendulumHead; head.sharedMaterial = config.TrapMaterial; head.drawMode = SpriteDrawMode.Simple; head.sortingOrder = 1;
            }
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("hub", hub), ("greyboxHub", hubBox), ("ring", ring), ("head", head),
                ("armLength", e.Settings.Spinner.ArmLength), ("pendulum", pendulum));
            SetupUtility.SetArray(presenter, "arms", drawn.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "greyboxArms", arms.Cast<Object>().ToArray(), changes);
            SetupUtility.SetArray(presenter, "greybox", arms.Cast<Object>().Append(hubBox).Where(o => o != null).ToArray(), changes);
            return art.name;
        }

        static string BuildPlate(Transform artRoot, SoloRoomDefinition room, SoloRoomElement e, PlateTrap trap, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes)
        {
            var look = (Transform)ReadObject(trap, "look");
            SpriteRenderer greybox = look != null ? look.GetComponent<SpriteRenderer>() : null;
            if (greybox == null) return null;
            Transform art = ArtObject(artRoot, e.Name, trap.transform.position, layer, changes);
            PlateArt presenter = SetupUtility.Ensure<PlateArt>(art.gameObject, changes);
            SpriteRenderer body = Child(art, "Plate", layer, changes);
            body.sprite = config.PressurePlate; body.sharedMaterial = config.TrapMaterial; body.drawMode = SpriteDrawMode.Simple; body.sortingOrder = greybox.sortingOrder;
            body.flipY = PointsDown(room, e);
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", e.Name), ("body", body), ("greyboxBody", greybox), ("width", e.Size.x * 1.1f));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox }, changes);
            return art.name;
        }

        static string BuildFaceStrip(Transform artRoot, string name, MovingTrap trap, SpriteRenderer face, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes)
        {
            Transform art = ArtObject(artRoot, name, face.transform.position, layer, changes);
            FaceStripArt presenter = SetupUtility.Ensure<FaceStripArt>(art.gameObject, changes);
            SpriteRenderer body = Child(art, "Strip", layer, changes);
            body.sprite = config.SpikedStrip; body.sharedMaterial = HazardMaterial(config); body.drawMode = SpriteDrawMode.Tiled; body.tileMode = SpriteTileMode.Continuous;
            body.sortingOrder = face.sortingOrder; body.enabled = false;
            TrapKitSetup.Write(presenter, changes, ("trap", trap), ("rooms", rooms), ("seedName", name), ("body", body), ("greyboxBody", face));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { face }, changes);
            return art.name;
        }

        // GT-05 is one painting scaled to the block's width: wider than this it would stand far taller than the block (L030's Ceiling_F).
        const float CrusherMaxWidth = 4f;

        static bool PaintedCrusher(MovingTrap mover, TrapArtConfig config) =>
            config.SpikedCrusher != null && (Prop(mover, "slams")?.boolValue ?? false)
            && mover.TryGetComponent(out SpriteRenderer greybox) && greybox.size.x <= CrusherMaxWidth;

        /// <summary>A spiked slam in Golden Ruins: GT-05, block and spikes as one painting; null for any other mover.</summary>
        static string SpikedCrusher(Transform artRoot, string name, MovingTrap mover, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes)
        {
            SpriteRenderer greybox = mover.GetComponent<SpriteRenderer>();
            var spikes = ReadObject(mover, "spikes") as SpriteRenderer;
            if (greybox == null || spikes == null || !PaintedCrusher(mover, config)) return null;
            Transform art = ArtObject(artRoot, name, greybox.transform.position, layer, changes);
            CrusherArt presenter = SetupUtility.Ensure<CrusherArt>(art.gameObject, changes);
            SpriteRenderer body = Child(art, "Crusher", layer, changes);
            body.sprite = config.SpikedCrusher; body.sharedMaterial = config.TrapMaterial; body.drawMode = SpriteDrawMode.Simple; body.sortingOrder = greybox.sortingOrder + 1;
            TrapKitSetup.Write(presenter, changes, ("trap", mover), ("rooms", rooms), ("seedName", name), ("body", body), ("greyboxBody", greybox));
            SetupUtility.SetArray(presenter, "greybox", new Object[] { greybox, spikes }, changes);
            return art.name;
        }

        /// <summary>An Obsidian mover with painted states, or null for any other mover (it keeps its host's skin).</summary>
        static string PaintedPlatform(Transform artRoot, string name, MovingTrap mover, TrapArtConfig config, RoomManager rooms, int layer, List<string> changes)
        {
            SpriteRenderer greybox = mover.GetComponent<SpriteRenderer>();
            if (greybox == null) return null;
            if (ReadInt(mover, "flipTicks") > 0 && config.FlipPlate != null)
            {
                Transform plate = ArtObject(artRoot, name, greybox.transform.position, layer, changes);
                FlipPlateArt flip = SetupUtility.Ensure<FlipPlateArt>(plate.gameObject, changes);
                SpriteRenderer body = Child(plate, "Plate", layer, changes);
                body.sprite = config.FlipPlate; body.sharedMaterial = config.TrapMaterial; body.drawMode = SpriteDrawMode.Simple; body.sortingOrder = greybox.sortingOrder + 1;
                TrapKitSetup.Write(flip, changes, ("trap", mover), ("rooms", rooms), ("seedName", name), ("body", body), ("greyboxBody", greybox));
                var spikes = ReadObject(mover, "spikes") as SpriteRenderer;
                SetupUtility.SetArray(flip, "greybox", spikes != null ? new Object[] { greybox, spikes } : new Object[] { greybox }, changes);
                return plate.name;
            }
            bool phases = ReadInt(mover, "phaseOnTicks") > 0;
            bool glitches = (Prop(mover, "glitch")?.boolValue ?? false) && (Prop(mover, "path")?.arraySize ?? 0) > 0;
            if (!(phases && config.PhaseLit != null) && !(glitches && config.GlitchSolid != null)) return null;
            Transform art = ArtObject(artRoot, name, greybox.transform.position, layer, changes);
            PaintedPlatformArt presenter = SetupUtility.Ensure<PaintedPlatformArt>(art.gameObject, changes);
            SpriteRenderer main = Child(art, "Platform", layer, changes);
            main.sprite = phases ? config.PhaseLit : config.GlitchSolid; main.sharedMaterial = config.TrapMaterial; main.drawMode = SpriteDrawMode.Simple; main.sortingOrder = greybox.sortingOrder;
            SpriteRenderer caps = null, ghost = null;
            var ghostBox = ReadObject(mover, "ghost") as SpriteRenderer;
            if (phases)
            {
                caps = Child(art, "Caps", layer, changes);
                caps.sprite = config.PhaseOff; caps.sharedMaterial = config.TrapMaterial; caps.drawMode = SpriteDrawMode.Simple; caps.sortingOrder = greybox.sortingOrder; caps.enabled = false;
            }
            else if (ghostBox != null)
            {
                ghost = Child(art, "Ghost", layer, changes);
                ghost.sprite = config.GlitchGhost; ghost.sharedMaterial = config.TrapMaterial; ghost.drawMode = SpriteDrawMode.Simple; ghost.sortingOrder = greybox.sortingOrder; ghost.enabled = false;
            }
            TrapKitSetup.Write(presenter, changes, ("trap", mover), ("rooms", rooms), ("seedName", name), ("body", main), ("greyboxBody", greybox), ("caps", caps), ("ghost", ghost), ("greyboxGhost", ghost != null ? ghostBox : null),
                ("steady", phases ? config.PhaseLit : config.GlitchSolid), ("warning", phases ? config.PhaseFade : config.GlitchWarn), ("hangs", !phases));
            SetupUtility.SetArray(presenter, "greybox", ghostBox != null ? new Object[] { greybox, ghostBox } : new Object[] { greybox }, changes);
            return art.name;
        }
    }
}
