using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-076: PARALLAX/Setup/Trap Lab brings existing rooms up to date (TrapLabSetup.SyncRooms). Before, it built only
    // missing rooms, so rooms 3-4 kept their pre-PAX-082 geometry in Sandbox_TrapLab. Runs in the route session's empty
    // scene with the same wiring the menu finds in the sandbox.
    public sealed class TrapLabSyncTests
    {
        static readonly Type Setup = Type.GetType("Parallax.Editor.Setup.TrapLabSetup, Parallax.Editor");
        IDisposable session;
        Transform parent;
        RealityRoot root;
        object[] wiring;

        [SetUp]
        public void Open()
        {
            session = OpenSession();
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A));
            var rootGo = new GameObject("RealityRoot_A") { layer = layer };
            root = rootGo.AddComponent<RealityRoot>();
            typeof(RealityRoot).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(root, ObserverId.A);
            parent = new GameObject("Rooms_PAX045") { layer = layer }.transform;
            parent.SetParent(rootGo.transform, false);
            var systems = new GameObject("Systems");
            wiring = new object[] { systems.AddComponent<CheckpointManager>(), systems.AddComponent<RoomManager>(), systems.AddComponent<RoomDeath>(), new GameObject("Observers").AddComponent<ObserverSet>(), Motor() };
        }

        [TearDown] public void Close() => session?.Dispose();

        List<string> Sync(params object[] rooms)
        {
            Type definition = Type.GetType("Parallax.Editor.Setup.SoloRoomDefinition, Parallax.Editor");
            Array list = Array.CreateInstance(definition, rooms.Length);
            for (int i = 0; i < rooms.Length; i++) list.SetValue(rooms[i], i);
            var changes = new List<string>();
            Invoke(Setup, "SyncRooms", parent, root, list, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4], changes);
            return changes;
        }

        List<string> Differences(object room) =>
            (List<string>)Invoke(Setup, "DifferencesFromLayout", parent.Find("Room_1"), parent, root, room, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4]);

        // Seen red with the old rule (build only a missing room): the moved trigger stayed where it was.
        [Test]
        public void ARoomThatDiffersFromTheLayout_IsRebuiltToMatchIt()
        {
            Sync(Fixture("CameraTellRoom", 60f, 40f, 6f));
            object moved = Fixture("CameraTellRoom", 60f, 40f, 33f);
            Assert.IsNotEmpty(Differences(moved), "the comparison must see the moved trigger");
            List<string> changes = Sync(moved);
            TestContext.Out.WriteLine(string.Join("\n", changes));
            Assert.IsTrue(changes.Any(c => c.StartsWith("rebuilt Room_1")), string.Join("\n", changes));
            CollectionAssert.IsEmpty(Differences(moved));
            Assert.AreEqual(1, parent.childCount, "the old room is gone");
        }

        [Test]
        public void AMatchingRoom_IsLeftUntouched_AndASecondRunChangesNothing()
        {
            object room = Fixture("CameraTellRoom", 60f, 40f, 33f);
            Assert.IsNotEmpty(Sync(room), "first run builds the room");
            int id = parent.Find("Room_1").gameObject.GetInstanceID();
            CollectionAssert.IsEmpty(Sync(room));
            Assert.AreEqual(id, parent.Find("Room_1").gameObject.GetInstanceID(), "a matching room is not rebuilt");
        }

        // A fresh build of every Trap Lab room compares equal to itself, so the menu never rebuilds a room for nothing.
        [Test]
        public void EveryTrapLabRoom_BuiltFresh_MatchesItsLayout()
        {
            IList rooms = (IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);
            foreach (object room in rooms)
            {
                foreach (Transform child in parent.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                Sync(room);
                string name = $"Room_{(int)F(room, "Id") + 1}";
                var differences = (List<string>)Invoke(Setup, "DifferencesFromLayout", parent.Find(name), parent, root, room, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4]);
                CollectionAssert.IsEmpty(differences, name + ": " + string.Join("; ", differences));
            }
        }
    }
}
