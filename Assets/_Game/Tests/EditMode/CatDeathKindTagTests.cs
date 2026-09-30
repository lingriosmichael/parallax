using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-V07 §4: where a death kind is declared. On the trap component (RoomTrap's DeclaredDeathKind, with a per-object
    // override), and on Hazard, whose kind the builders set (spikes -> Spiked, pit floors -> Pit; Phase 1 question 6).
    public sealed class CatDeathKindTagTests
    {
        readonly List<Object> made = new();

        [TearDown]
        public void DestroyMade()
        {
            foreach (Object o in made) if (o != null) Object.DestroyImmediate(o);
            made.Clear();
            RecreateUntitledScene();
        }

        // CLAUDE.md (PAX-075): leave the Test Runner's scene as it was: clean, so a route session after this fixture can run.
        internal static void RecreateUntitledScene() =>
            Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });

        T Make<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            made.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(Object target, string field, Action<SerializedProperty> write)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            Assert.NotNull(p, $"{target.GetType().Name}.{field}");
            write(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void EachTaggedTrap_ReportsItsDeclaredKind()
        {
            Assert.AreEqual(CatDeathKind.Spiked, Make<HiddenSpikesTrap>().DeathKind);
            Assert.AreEqual(CatDeathKind.Crushed, Make<FallingBlockTrap>().DeathKind);
            Assert.AreEqual(CatDeathKind.Zapped, Make<StormCloudTrap>().DeathKind);
            Assert.AreEqual(CatDeathKind.Arrow, Make<ArrowTrap>().DeathKind, "arrows and the spear variant");
        }

        [TestCase(MovingTrapKind.Solid, CatDeathKind.Crushed)]
        [TestCase(MovingTrapKind.Hazard, CatDeathKind.Spiked)]
        public void MovingTrap_ReportsByItsKind(MovingTrapKind kind, CatDeathKind expected)
        {
            MovingTrap trap = Make<MovingTrap>();
            Set(trap, "kind", p => p.enumValueIndex = (int)kind);
            Assert.AreEqual(expected, trap.DeathKind);
        }

        [Test]
        public void APerObjectOverride_WinsOverTheDeclaredKind()
        {
            FallingBlockTrap trap = Make<FallingBlockTrap>();
            Set(trap, "deathKindOverride", p => p.enumValueIndex = (int)CatDeathKind.Pit);
            Assert.AreEqual(CatDeathKind.Crushed, trap.DeathKind, "the override is off by default");
            Set(trap, "overrideDeathKind", p => p.boolValue = true);
            Assert.AreEqual(CatDeathKind.Pit, trap.DeathKind);
        }

        [Test]
        public void TheGeyser_AndAnUntaggedHazard_ReportDefault()
        {
            Assert.AreEqual(CatDeathKind.Default, Make<GeyserTrap>().DeathKind, "the geyser is air, not a death kind");
            Assert.AreEqual(CatDeathKind.Default, Make<Hazard>().DeathKind);
        }

        // The builders: a spike hazard is built Spiked, a pit floor (the hazard a Pit opening names) Pit. Trap Lab room 2 has a
        // pit (PitHazard under RearmCollapse) and room 1 a ceiling of spikes (CeilingHazard).
        [TestCase(2, "PitHazard", CatDeathKind.Pit)]
        [TestCase(1, "CeilingHazard", CatDeathKind.Spiked)]
        public void TheRoomBuilder_DeclaresEachHazardsKind(int room, string element, CatDeathKind expected)
        {
            Type builder = EditorType("Parallax.Editor.Setup.SoloRoomBuilder");
            Type layout = EditorType("Parallax.Editor.Setup.TrapLabLayout");
            object rooms = layout.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object definition = ((System.Collections.IList)rooms)[room];
            MethodInfo kindOf = builder.GetMethod("HazardKind", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(kindOf, "SoloRoomBuilder.HazardKind");
            Assert.AreEqual(expected, kindOf.Invoke(null, new[] { definition, element }));
        }

        [TestCase(CatDeathKind.Spiked)]
        [TestCase(CatDeathKind.Pit)]
        public void BuildHazardCore_SetsTheKindItIsGiven(CatDeathKind kind)
        {
            var root = new GameObject("RealityRoot_A");
            made.Add(root);
            var reality = root.AddComponent<Parallax.Gameplay.Reality.RealityRoot>();
            MethodInfo build = EditorType("Parallax.Editor.Setup.HazardSetup").GetMethod("BuildHazardCore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(build, "HazardSetup.BuildHazardCore");
            var hazard = (Hazard)build.Invoke(null, new object[] { root.transform, reality, "Spikes", null, null, null, Vector2.zero, Vector2.one, Color.red, null, new List<string>(), kind });
            Assert.AreEqual(kind, hazard.DeathKind);
        }

        static Type EditorType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null)
            ?? throw new AssertionException(name + " not found");
    }
}
