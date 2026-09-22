using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-045 §8 coverage not in TrapKitTests: chain order (2), validator (3, 4),
    /// crush boundary (6), Trap Lab layout data (8). This assembly does not reference
    /// Parallax.Editor, so layout and validator types are reached by reflection, the same
    /// way SoloRoomsLayoutTests does it.</summary>
    public sealed class TrapKitV2Tests
    {
        // ---------- §8.2 Chain order independence (Parallax.Core, direct) ----------

        static (List<int> source, List<int> target) RunPair(TrapTiming source, TrapTiming target, int ticks, bool sourceFirst, Func<int, bool> sourceOverlap)
        {
            var s = new List<int>(); var t = new List<int>();
            for (int tick = 0; tick < ticks; tick++)
            {
                if (sourceFirst)
                {
                    if (source.Step(tick, sourceOverlap(tick))) s.Add(tick);
                    if (target.Step(tick, false, source.LatestFireTick)) t.Add(tick);
                }
                else
                {
                    if (target.Step(tick, false, source.LatestFireTick)) t.Add(tick);
                    if (source.Step(tick, sourceOverlap(tick))) s.Add(tick);
                }
            }
            return (s, t);
        }

        [Test] public void Chain_OnceFireTicksAreIndependentOfTickOrder()
        {
            foreach (bool sourceFirst in new[] { true, false })
            {
                var r = RunPair(new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 0, 0, 1, 0),
                                new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Once, 1, 0, 1, 0), 6, sourceFirst, tick => tick >= 2);
                CollectionAssert.AreEqual(new[] { 2 }, r.source, $"sourceFirst={sourceFirst}");
                CollectionAssert.AreEqual(new[] { 3 }, r.target, $"sourceFirst={sourceFirst}");
            }
        }

        [Test] public void Chain_RearmedSourceChainsAgainInBothOrders()
        {
            foreach (bool sourceFirst in new[] { true, false })
            {
                var r = RunPair(new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Rearm, 0, 3, 1, 0),
                                new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Rearm, 1, 2, 1, 0), 9, sourceFirst, _ => true);
                CollectionAssert.AreEqual(new[] { 0, 3, 6 }, r.source, $"sourceFirst={sourceFirst}");
                CollectionAssert.AreEqual(new[] { 1, 4, 7 }, r.target, $"sourceFirst={sourceFirst}");
            }
        }

        // D-055: a Rearm target that is not armed when its source fires ignores that fire.
        // Source fires every tick; target (delay 1, cooldown 10) fires at 1 and rearms at 11.
        // The source fire at 10 happened while the target was unarmed, so the next target
        // fire comes from the source fire at 11, i.e. tick 12, in either order.
        [Test] public void Chain_UnarmedTargetIgnoresFireInBothOrders()
        {
            foreach (bool sourceFirst in new[] { true, false })
            {
                var r = RunPair(new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Rearm, 0, 1, 1, 0),
                                new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Rearm, 1, 10, 1, 0), 15, sourceFirst, _ => true);
                CollectionAssert.AreEqual(new[] { 1, 12 }, r.target, $"sourceFirst={sourceFirst}");
            }
        }

        // ---------- §8.6 Crush boundary (Parallax.Core, direct) ----------
        // Solid 2x2 at the origin (right edge x = 1.0), depth 0.15, cat 1.0 x 0.5.

        [Test] public void Crush_BoundaryCases()
        {
            Bounds solid = new(Vector3.zero, new Vector3(2f, 2f));
            Vector3 cat = new(1f, .5f);
            Assert.IsFalse(TrapMotion.Crushes(new Bounds(new Vector3(1.5f, 0f), cat), solid, .15f), "touching the edge survives");
            Assert.IsFalse(TrapMotion.Crushes(new Bounds(new Vector3(1.4f, 0f), cat), solid, .15f), "0.10 u penetration survives");
            Assert.IsTrue(TrapMotion.Crushes(new Bounds(new Vector3(1.3f, 0f), cat), solid, .15f), "0.20 u penetration dies");
        }

        // ---------- Reflection helpers into Parallax.Editor.Setup ----------

        static Type EditorType(string name)
        {
            Type type = Type.GetType($"Parallax.Editor.Setup.{name}, Parallax.Editor");
            Assert.IsNotNull(type, $"Parallax.Editor.Setup.{name} not found");
            return type;
        }

        static object Coerce(object value, Type type)
        {
            if (value == null || value is DBNull) return type.IsValueType ? Activator.CreateInstance(type) : null;
            if (type.IsEnum && value.GetType() != type) return Enum.ToObject(type, value);
            return value;
        }

        static object Get(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(target);
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{type.Name}.{name} not found");
            return field.GetValue(target);
        }

        static object GetStatic(Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field != null) return field.GetValue(null);
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(property, $"{type.Name}.{name} not found");
            return property.GetValue(null);
        }

        // Builds SoloRoomTrapSettings by parameter name; every other parameter keeps its default.
        static object Settings(params (string name, object value)[] args)
        {
            ConstructorInfo ctor = EditorType("SoloRoomTrapSettings").GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            ParameterInfo[] parameters = ctor.GetParameters();
            foreach (var arg in args) Assert.IsTrue(parameters.Any(p => p.Name == arg.name), $"SoloRoomTrapSettings has no parameter '{arg.name}'");
            var values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                int match = Array.FindIndex(args, a => a.name == parameters[i].Name);
                values[i] = Coerce(match >= 0 ? args[match].value : parameters[i].DefaultValue, parameters[i].ParameterType);
            }
            return ctor.Invoke(values);
        }

        static object OverlapSource() => Settings(("delayTicks", 0));
        static object ChainFrom(string source, int delay = 1) => Settings(("delayTicks", delay), ("triggerSource", TrapTriggerSource.Chain), ("chainSource", source));

        // SoloRoomElement(kind, name, position, size, secondaryPosition, secondarySize, settings)
        static object Element(string kind, string name, object settings = null)
        {
            ConstructorInfo ctor = EditorType("SoloRoomElement").GetConstructors().First(c => c.GetParameters().Length == 7);
            ParameterInfo[] parameters = ctor.GetParameters();
            object[] raw = { Enum.Parse(EditorType("SoloRoomElementKind"), kind), name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, settings };
            return ctor.Invoke(raw.Select((v, i) => Coerce(v, parameters[i].ParameterType)).ToArray());
        }

        static Array Collection(Type parameterType, object[] items)
        {
            Type elementType = parameterType.IsArray ? parameterType.GetElementType() : parameterType.GetGenericArguments()[0];
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            return array;
        }

        // SoloRoomDefinition(id, origin, width, elements, openings, requiredJumps)
        static object Room(params object[] elements)
        {
            ConstructorInfo ctor = EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            ParameterInfo[] parameters = ctor.GetParameters();
            object[] raw = { 0, null, 32f, Collection(parameters[3].ParameterType, elements), Collection(parameters[4].ParameterType, new object[0]), Collection(parameters[5].ParameterType, new object[0]) };
            return ctor.Invoke(raw.Select((v, i) => Coerce(v, parameters[i].ParameterType)).ToArray());
        }

        static bool TryValidate(object room, out string error)
        {
            MethodInfo method = EditorType("TrapLayoutValidator").GetMethod("TryValidate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "TrapLayoutValidator.TryValidate not found");
            object[] args = { room, null };
            bool ok = (bool)method.Invoke(null, args);
            error = (string)args[1];
            return ok;
        }

        static bool Valid(params object[] elements) => TryValidate(Room(elements), out _);

        static IEnumerable<object> LabRooms() => ((IEnumerable)GetStatic(EditorType("TrapLabLayout"), "Rooms")).Cast<object>();

        // ---------- §8.3 / §8.4 Validator ----------

        [Test] public void Validator_AcceptsWellFormedChain() =>
            Assert.IsTrue(Valid(Element("FallingBlock", "A", OverlapSource()), Element("FallingBlock", "B", ChainFrom("A"))));

        [Test] public void Validator_RejectsChainDelayBelowOne() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "A", OverlapSource()), Element("FallingBlock", "B", ChainFrom("A", 0))));

        [Test] public void Validator_RejectsHiddenSpikesChainWithRevealDelayZero() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "A", OverlapSource()),
                Element("HiddenSpikes", "B", Settings(("delayTicks", 1), ("revealDelayTicks", 0), ("triggerSource", TrapTriggerSource.Chain), ("chainSource", "A")))));

        // Cross-room links: names resolve only inside the room, so a source that exists only
        // in another room is indistinguishable from a missing one.
        [Test] public void Validator_RejectsMissingOrCrossRoomSource() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "B", ChainFrom("NotInThisRoom"))));

        [Test] public void Validator_RejectsNonTrapSource() =>
            Assert.IsFalse(Valid(Element("Floor", "Floor"), Element("FallingBlock", "B", ChainFrom("Floor"))));

        [Test] public void Validator_RejectsTwoTrapCycle() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "A", ChainFrom("B")), Element("FallingBlock", "B", ChainFrom("A"))));

        [Test] public void Validator_RejectsSelfCycle() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "A", ChainFrom("A"))));

        [Test] public void Validator_RejectsGravityFlipAsChainTarget() =>
            Assert.IsFalse(Valid(Element("FallingBlock", "A", OverlapSource()), Element("GravityFlip", "B", ChainFrom("A"))));

        [Test] public void Validator_RejectsRearmOnExitGravityFlipAsChainSource() =>
            Assert.IsFalse(Valid(Element("GravityFlip", "A", Settings(("rearmOnExit", true))), Element("FallingBlock", "B", ChainFrom("A"))));

        [Test] public void Validator_RejectsRepeatingDoorRetreat() =>
            Assert.IsFalse(Valid(Element("DoorRetreat", "D", Settings(("repeatMode", TrapRepeatMode.Rearm), ("cooldownTicks", 10)))));

        [Test] public void Validator_RejectsPeriodicCollapse() =>
            Assert.IsFalse(Valid(Element("CollapsingFloor", "C", Settings(("repeatMode", TrapRepeatMode.Periodic), ("periodTicks", 10), ("cooldownTicks", 5)))));

        [Test] public void Validator_RejectsPeriodicCooldownNotBelowPeriod() =>
            Assert.IsFalse(Valid(Element("HiddenSpikes", "S", Settings(("repeatMode", TrapRepeatMode.Periodic), ("periodTicks", 10), ("cooldownTicks", 10)))));

        [Test] public void Validator_MovingCooldownMustCoverMotion()
        {
            object Moving(int cooldown) => Settings(("moveTicks", 10), ("holdTicks", 5), ("returnTicks", 10), ("repeatMode", TrapRepeatMode.Rearm), ("cooldownTicks", cooldown));
            Assert.IsFalse(Valid(Element("MovingTrap", "M", Moving(24))));
            Assert.IsTrue(Valid(Element("MovingTrap", "M", Moving(25))));
        }

        // ---------- §8.8 Trap Lab layout data ----------

        [Test] public void TrapLab_EveryRoomPassesValidator()
        {
            foreach (object room in LabRooms())
                Assert.IsTrue(TryValidate(room, out string error), $"Room {Get(room, "Id")}: {error}");
        }

        [Test] public void TrapLab_ChainedAndPeriodicTrapsDeclareNoTrigger()
        {
            foreach (object room in LabRooms())
                foreach (object element in (IEnumerable)Get(room, "Elements"))
                {
                    object settings = Get(element, "Settings");
                    if (!(bool)Get(settings, "IsConfigured")) continue;
                    var source = (TrapTriggerSource)Get(settings, "TriggerSource");
                    var repeat = (TrapRepeatMode)Get(settings, "RepeatMode");
                    if (source == TrapTriggerSource.Chain || repeat == TrapRepeatMode.Periodic)
                        Assert.AreEqual(Vector2.zero, (Vector2)Get(element, "SecondarySize"), $"Room {Get(room, "Id")} {Get(element, "Name")} declares a trigger box");
                }
        }

        [Test] public void TrapLab_HasEveryPax045Feature()
        {
            bool chain = false, periodic = false, rearm = false, movingHazard = false, movingSolid = false;
            foreach (object room in LabRooms())
                foreach (object element in (IEnumerable)Get(room, "Elements"))
                {
                    object settings = Get(element, "Settings");
                    if (!(bool)Get(settings, "IsConfigured")) continue;
                    bool isMoving = Get(element, "Kind").ToString() == "MovingTrap";
                    var kind = (MovingTrapKind)Get(settings, "MovingKind");
                    chain |= (TrapTriggerSource)Get(settings, "TriggerSource") == TrapTriggerSource.Chain;
                    periodic |= (TrapRepeatMode)Get(settings, "RepeatMode") == TrapRepeatMode.Periodic;
                    rearm |= (TrapRepeatMode)Get(settings, "RepeatMode") == TrapRepeatMode.Rearm;
                    movingHazard |= isMoving && kind == MovingTrapKind.Hazard;
                    movingSolid |= isMoving && kind == MovingTrapKind.Solid;
                }
            Assert.IsTrue(chain, "no chain"); Assert.IsTrue(periodic, "no periodic"); Assert.IsTrue(rearm, "no rearm");
            Assert.IsTrue(movingHazard, "no moving hazard"); Assert.IsTrue(movingSolid, "no moving solid");
        }
    }
}
