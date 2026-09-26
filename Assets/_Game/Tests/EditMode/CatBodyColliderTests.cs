using System;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-089 E.2, R5: the cat's body collider is its first non-trigger Collider2D, picked by one shared helper
    /// (CatBodyCollider) in RoomTrap's overlap and bounds checks, StormCloudTrap and the route harness. The prefab has one
    /// collider, so nothing changes today; these tests put a trigger collider first, as a second collider on the prefab
    /// would.</summary>
    public sealed class CatBodyColliderTests : PauseTestBase
    {
        static readonly Type Helper = Type.GetType("Parallax.Gameplay.Player.CatBodyCollider, Parallax.Gameplay");

        PauseTestRig rig;

        sealed class Still : ICatCommandSource
        {
            public CatCommand Read() => default;
            public void ResetTransientState() { }
        }

        [SetUp]
        public void Build()
        {
            rig = PauseTestRig.Build(realInput: true);
            PauseTestRig.GetPrivate<List<ICatCommandSource>>(rig.Router, "validSources").Add(new Still());
        }

        [TearDown] public void Teardown() => rig?.Dispose();

        // A small trigger collider moved above the body box, so GetComponent<Collider2D>() returns it.
        CircleCollider2D TriggerFirst(GameObject go)
        {
            CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .05f;
            while (go.GetComponent<Collider2D>() != trigger) Assert.IsTrue(ComponentUtility.MoveComponentUp(trigger), "couldn't move the trigger up");
            return trigger;
        }

        static Collider2D Of(Component cat)
        {
            Assert.NotNull(Helper, "CatBodyCollider not found");
            return (Collider2D)Helper.GetMethod("Of").Invoke(null, new object[] { cat });
        }

        [Test]
        public void TheHelper_ReturnsTheFirstNonTriggerCollider()
        {
            var go = new GameObject("Probe");
            try
            {
                BoxCollider2D body = go.AddComponent<BoxCollider2D>();
                TriggerFirst(go);
                Assert.AreSame(body, Of(go.transform));
                Object.DestroyImmediate(body);
                Assert.IsNull(Of(go.transform), "only a trigger left: no body collider");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // RoomTrap.IsLocalHumanOverlapping (RoomTrap.cs:102) used GetComponent<Collider2D>(): with a trigger first, the
        // overlap query (triggers excluded) never matched the cat and the inverter never fired.
        [Test]
        public void AnOverlapTrap_FiresOnTheCatsBody_WhenATriggerColliderComesFirst()
        {
            TriggerFirst(rig.CatGo);
            var go = new GameObject("Inverter");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.layer = rig.CatGo.layer;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.one;
            SpriteRenderer ring = new GameObject("Cue_Ring").AddComponent<SpriteRenderer>();
            ring.transform.SetParent(go.transform, false);
            SpriteRenderer mark = new GameObject("Cue_Mark").AddComponent<SpriteRenderer>();
            mark.transform.SetParent(go.transform, false);
            InverterTrap trap = go.AddComponent<InverterTrap>();
            PauseTestRig.SetPrivate(trap, "roomId", 0);
            PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(trap, "observers", rig.Observers);
            PauseTestRig.SetPrivate(trap, "triggerSource", TrapTriggerSource.Overlap);
            PauseTestRig.SetPrivate(trap, "trigger", box);
            PauseTestRig.SetPrivate(trap, "ring", ring);
            PauseTestRig.SetPrivate(trap, "mark", mark);
            PauseTestRig.Invoke(trap, "Awake");
            PauseTestRig.Invoke(trap, "OnEnable");
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
            go.transform.position = rig.CatBody.position + rig.CatGo.GetComponent<BoxCollider2D>().offset;
            Physics2D.SyncTransforms();

            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            Assert.GreaterOrEqual(trap.LatestFireTick, 0, "the inverter overlaps the cat's body box but never fired");
        }
    }
}
