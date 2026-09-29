using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§9.1, §11 R5): shared helpers for the trap art tests. The level builder lives in the Editor assembly, which
    // this assembly doesn't reference, so it's reached by reflection like every other Editor type.
    static class TrapArtTestKit
    {
        static readonly Type Builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
        static readonly Type TrapLab = Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor");
        static readonly Type Layouts = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");

        /// <summary>Every trap kind has art (§12 R10): every RoomTrap except a vine that never snaps (the environment's).</summary>
        public static bool IsGated(RoomTrap trap) => trap != null && !(trap is ClimbVine vine && !vine.Snaps);

        /// <summary>Static spikes and pit floors are Hazards without a RoomTrap; they have art too (every spike, §11 R1).</summary>
        public static bool IsBareHazard(Component c) => c is Hazard && c.GetComponent<RoomTrap>() == null;

        public static bool IsSpear(ArrowTrap arrow) => (bool)Private(arrow, "spear");

        public static object Private(object target, string field)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(target);
            }
            Assert.Fail($"{target.GetType().Name}.{field} not found.");
            return null;
        }

        /// <summary>L001-L020 (every LevelLayouts entry named L + number).</summary>
        public static IEnumerable<string> LevelIds() =>
            ((IDictionary)Layouts.GetField("ById").GetValue(null)).Keys.Cast<string>().Where(k => k.Length == 4 && k[0] == 'L' && char.IsDigit(k[1])).OrderBy(k => k);

        public static object Level(string id) => ((IDictionary)Layouts.GetField("ById").GetValue(null))[id];

        public static IList TrapLabRooms() => (IList)TrapLab.GetField("Rooms").GetValue(null);

        /// <summary>Every level and every Trap Lab room, as (name, room definition).</summary>
        public static IEnumerable<TestCaseData> AllRooms()
        {
            foreach (string id in LevelIds()) yield return new TestCaseData(id).SetName("L:" + id);
            IList lab = TrapLabRooms();
            for (int i = 0; i < lab.Count; i++) yield return new TestCaseData("TrapLab" + i).SetName("L:TrapLab" + i);
        }

        /// <summary>The rooms with declared routes (§11 R5): L001-L020 and Trap Lab rooms 3-12 (rooms 0-2 predate routes; their
        /// elements are built and skin-checked by TrapArtBuildTests).</summary>
        public static IEnumerable<string> RoutedRoomIds()
        {
            foreach (string id in LevelIds()) yield return id;
            for (int i = 3; i < TrapLabRooms().Count; i++)
                if (TrapLabRoutes.GetMethod("Room" + i) != null) yield return "TrapLab" + i;
        }

        static readonly Type TrapLabRoutes = Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor");
        static readonly Type LevelRoutesType = Type.GetType("Parallax.Editor.Levels.LevelRoutes, Parallax.Editor");

        /// <summary>A routed room's declared routes (RoomRoutes).</summary>
        public static object Routes(string id) => id.StartsWith("TrapLab")
            ? TrapLabRoutes.GetMethod("Room" + id.Substring(7)).Invoke(null, null)
            : ((IDictionary)LevelRoutesType.GetField("ById").GetValue(null))[id];

        public static object Room(string id) => id.StartsWith("TrapLab") ? TrapLabRooms()[int.Parse(id.Substring(7))] : Level(id);

        /// <summary>Builds `room` under a new "RealityRoot_A" in the active scene: with the art (BuildRoom, as a level is built)
        /// or the grey-box only (BuildRoomWithoutArt). Returns the room's root.</summary>
        public static Transform Build(object room, bool art, string rootName = "RealityRoot_A")
        {
            var rootGo = new GameObject(rootName) { layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A)) };
            RealityRoot root = rootGo.AddComponent<RealityRoot>();
            CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            MethodInfo build = Builder.GetMethod(art ? "BuildRoom" : "BuildRoomWithoutArt", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(build, "SoloRoomBuilder." + (art ? "BuildRoom" : "BuildRoomWithoutArt") + " not found.");
            build.Invoke(null, new object[] { rootGo.transform, root, room, null, null, null, null, motor, new List<string>() });
            int id = (int)room.GetType().GetField("Id").GetValue(room);
            Transform roomRoot = rootGo.transform.Find($"Room_{id + 1}");
            Assert.NotNull(roomRoot, $"no Room_{id + 1} was built.");
            return roomRoot;
        }

        /// <summary>Runs `body` in a fresh empty scene, then puts back the Test Runner's untitled scene (PAX-075 R21/R22).</summary>
        public static void InScratchScene(Action body)
        {
            RouteTestApi.FreshScratchScene();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try { SceneManager.SetActiveScene(temp); body(); }
            finally { Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true }); }
        }

        /// <summary>The element names a room declares.</summary>
        public static IEnumerable<string> ElementNames(object room) =>
            ((IEnumerable)room.GetType().GetField("Elements").GetValue(room)).Cast<object>().Select(e => (string)e.GetType().GetField("Name").GetValue(e));

        public static bool Shown(SpriteRenderer r) => r != null && r.enabled && r.gameObject.activeInHierarchy;

        /// <summary>A drawn effect: shown, with a sprite and some opacity.</summary>
        public static bool Drawn(SpriteRenderer r) => Shown(r) && r.sprite != null && r.color.a > 0f;

        /// <summary>The route harness's visibility signature of one renderer (RouteReplay.Signature's fields).</summary>
        public static string Signature(SpriteRenderer s)
        {
            bool visible = Shown(s);
            if (!visible) return "hidden";
            Vector2 drawn = s.drawMode == SpriteDrawMode.Simple ? (Vector2)s.transform.lossyScale : s.size;
            return $"{s.color}|{(s.sprite != null ? s.sprite.name : "none")}|{s.transform.position}|{s.transform.eulerAngles.z:F2}|{drawn}|{s.bounds}";
        }

        public static string Path(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (Transform c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
