using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static class TrapLayoutValidator
    {
        public static bool TryValidate(SoloRoomDefinition room, out string error)
        {
            var byName = new Dictionary<string, SoloRoomElement>();
            foreach (SoloRoomElement e in room.Elements) byName[e.Name] = e;
            foreach (SoloRoomElement e in room.Elements)
            {
                SoloRoomTrapSettings s = e.Settings;
                if (!s.IsConfigured) continue;
                if (s.TriggerSource == TrapTriggerSource.Chain && s.RepeatMode == TrapRepeatMode.Periodic) { error = $"{e.Name}: a Periodic trap cannot use a chain source."; return false; }
                if (s.TriggerSource == TrapTriggerSource.Chain)
                {
                    // PAX-083: one message per case. A name that isn't in this room (chains never resolve across rooms)...
                    if (!byName.TryGetValue(s.ChainSource ?? string.Empty, out SoloRoomElement source)) { error = $"{e.Name}: chain source '{s.ChainSource}' is not an element of this room; chain names never resolve across rooms."; return false; }
                    // ...and an element of this room that isn't a trap.
                    if (!IsTrap(source.Kind)) { error = $"{e.Name}: chain source must be a trap; '{source.Name}' is a {source.Kind}."; return false; }
                    if (ChainDelay(e) < 1) { error = $"{e.Name}: chain delay is below one tick."; return false; }
                    if (e.Kind == SoloRoomElementKind.GravityFlip) { error = $"{e.Name}: GravityFlip cannot be a chain target."; return false; }
                    if (source.Kind == SoloRoomElementKind.GravityFlip && source.Settings.RearmOnExit) { error = $"{e.Name}: rearm-on-exit GravityFlip cannot be a chain source."; return false; }
                    if (HasCycle(e.Name, byName)) { error = $"{e.Name}: chain cycle."; return false; }
                }
                if (e.Kind == SoloRoomElementKind.DoorRetreat && s.RepeatMode != TrapRepeatMode.Once) { error = $"{e.Name}: DoorRetreat is Once only."; return false; }
                if ((e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.GravityFlip) && s.RepeatMode == TrapRepeatMode.Periodic) { error = $"{e.Name}: this trap cannot be Periodic."; return false; }
                if (s.RepeatMode == TrapRepeatMode.Periodic && s.CooldownTicks >= s.PeriodTicks) { error = $"{e.Name}: periodic cooldown must be below period."; return false; }
                if (e.Kind == SoloRoomElementKind.MovingTrap && s.RepeatMode != TrapRepeatMode.Once && s.CooldownTicks < s.MoveTicks + s.HoldTicks + s.ReturnTicks) { error = $"{e.Name}: moving cooldown ends before motion."; return false; }
            }
            error = null; return true;
        }
        static int ChainDelay(SoloRoomElement e) => e.Kind == SoloRoomElementKind.HiddenSpikes ? e.Settings.RevealDelayTicks : e.Settings.DelayTicks;
        static bool IsTrap(SoloRoomElementKind kind) => kind == SoloRoomElementKind.CollapsingFloor || kind == SoloRoomElementKind.HiddenSpikes || kind == SoloRoomElementKind.FallingBlock || kind == SoloRoomElementKind.GravityFlip || kind == SoloRoomElementKind.DoorRetreat || kind == SoloRoomElementKind.MovingTrap || kind == SoloRoomElementKind.Arrow;
        static bool HasCycle(string start, Dictionary<string, SoloRoomElement> byName)
        {
            var seen = new HashSet<string>(); string current = start;
            while (byName.TryGetValue(current, out SoloRoomElement element) && element.Settings.TriggerSource == TrapTriggerSource.Chain)
            {
                if (!seen.Add(current)) return true;
                current = element.Settings.ChainSource;
            }
            return false;
        }
    }
}
