using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-089 E.1: the storm cloud's object carries no collider of its own (the trap never used the box CreateTrap gave it);
    // its wake trigger is the child box. Builds Trap Lab room 10 in the route session's empty scene with the wiring the
    // Trap Lab menu finds, as TrapLabSyncTests does.
    public sealed class StormCloudBuilderTests
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

        List<string> Sync()
        {
            Type definition = Type.GetType("Parallax.Editor.Setup.SoloRoomDefinition, Parallax.Editor");
            Array list = Array.CreateInstance(definition, 1);
            list.SetValue(TrapLabRoom10Tests.Room(), 0);
            var changes = new List<string>();
            Invoke(Setup, "SyncRooms", parent, root, list, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4], changes);
            return changes;
        }

        [Test]
        public void TheBuiltCloud_HasNoColliderOfItsOwn_AndItsWakeTriggerIsTheChildBox()
        {
            Sync();
            StormCloudTrap cloud = parent.GetComponentInChildren<StormCloudTrap>(true);
            Assert.NotNull(cloud, "room 10 has a storm cloud");
            Assert.IsNull(cloud.GetComponent<Collider2D>(), "the cloud's object keeps no box at its authored pose");
            BoxCollider2D trigger = PauseTestRig.GetPrivate<BoxCollider2D>(cloud, "trigger");
            Assert.NotNull(trigger, "the wake trigger is wired");
            Assert.AreNotSame(cloud.gameObject, trigger.gameObject, "the wake trigger is the child box");
            Assert.IsTrue(trigger.isTrigger);
        }

        // Idempotent: a second run changes nothing (CreateTrap's repair branch doesn't put the box back).
        [Test]
        public void ASecondRun_ChangesNothing()
        {
            Sync();
            List<string> second = Sync();
            CollectionAssert.IsEmpty(second, string.Join("\n", second));
            Assert.IsNull(parent.GetComponentInChildren<StormCloudTrap>(true).GetComponent<Collider2D>());
        }
    }
}
