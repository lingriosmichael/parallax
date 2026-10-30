using System.Collections.Generic;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-116 (D-187): the spinner builder. The trap's object sits on the pivot and never turns; its child "Rotor" turns and holds
    // the hub (grey, harmless) and one red "<name>_Arm<i>" per arm, from the pivot outward. A triggered spinner's trigger box is
    // a child of the trap, so it stays put. Placeholder art.
    public static partial class TrapKitSetup
    {
        static readonly Color HubGrey = new(.32f, .32f, .36f, 1f);
        static readonly Color ArmRed = new(.85f, .15f, .15f, 1f);

        internal static SpinnerTrap BuildSpinnerCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 hubSize, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            SpinnerSettings k = settings.Spinner;
            GameObject go = CreateTrap<SpinnerTrap>(parent, root, name, position, hubSize, Color.clear, roomId, rooms, death, observers, false, null, changes, box: false);
            Transform rotor = SetupUtility.EnsureChild(go.transform, "Rotor", root.gameObject.layer, changes);
            rotor.localPosition = Vector3.zero; rotor.localRotation = Quaternion.Euler(0f, 0f, k.StartAngle); rotor.localScale = Vector3.one;
            for (int i = 0; i < 4; i++)
            {
                string armName = name + "_Arm" + i;
                Transform existing = rotor.Find(armName);
                if (i >= k.Arms) { if (existing != null) { Object.DestroyImmediate(existing.gameObject); changes.Add("removed " + armName); } continue; }
                Transform arm = SetupUtility.EnsureChild(rotor, armName, root.gameObject.layer, changes);
                float a = i * 360f / Mathf.Max(1, k.Arms);
                arm.localPosition = Quaternion.Euler(0f, 0f, a) * new Vector3(k.ArmLength * .5f, 0f, 0f);
                arm.localRotation = Quaternion.Euler(0f, 0f, a); arm.localScale = Vector3.one;
                SpriteRenderer look = SetupUtility.SetVisual(arm.gameObject, root, new Vector2(k.ArmLength, k.ArmWidth), ArmRed, changes);
                if (look.sortingOrder != -1) { look.sortingOrder = -1; changes.Add("set " + armName + ".sortingOrder"); }
            }
            Transform hub = SetupUtility.EnsureChild(rotor, name + "_Hub", root.gameObject.layer, changes);
            hub.localPosition = Vector3.zero; hub.localRotation = Quaternion.identity; hub.localScale = Vector3.one;
            SpriteRenderer hubLook = SetupUtility.SetVisual(hub.gameObject, root, hubSize, HubGrey, changes);
            if (hubLook.sortingOrder != 0) { hubLook.sortingOrder = 0; changes.Add("set " + name + "_Hub.sortingOrder"); }
            BoxCollider2D trigger = !k.AlwaysOn && settings.TriggerSource == Parallax.Core.TrapTriggerSource.Overlap && triggerSize != Vector2.zero
                ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            SpinnerTrap trap = go.GetComponent<SpinnerTrap>();
            Write(trap, changes, ("trigger", trigger), ("rotor", rotor), ("arms", k.Arms), ("armLength", k.ArmLength), ("armWidth", k.ArmWidth),
                ("degreesPerTick", k.DegreesPerTick), ("startAngle", k.StartAngle), ("alwaysOn", k.AlwaysOn), ("delayTicks", settings.DelayTicks),
                ("swingAmplitude", k.SwingAmplitude), ("swingPeriod", k.SwingPeriod));   // PAX-118 (D-197)
            return trap;
        }
    }
}
