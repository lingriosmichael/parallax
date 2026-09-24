using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 (D-080) §5.1: the fake platform through the real game code (the route harness), the builder, and its
    // layout rules. The fixture room: a floor to x 10, the fake at x 10-12 (top 0) over a pit, floor again from x 20.
    public sealed class FakePlatformTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        const float TouchSkin = .05f;   // CollapsingFloorTrap's touchSkin (D-080: "overlap" is within it)
        static readonly Rect Fake = new(10f, -.5f, 2f, .5f);

        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string name, params object[] args) => Call(T("RouteFixtures"), name, args);
        static Vector2 Collider() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset").ColliderSize;

        // The cat's collider is a horizontal capsule (Cat_Player.prefab), so contact is the distance from its core
        // segment to the fake's box, grown by the touch skin, against the capsule's radius.
        static bool TouchesFake(Rec r)
        {
            CapsuleCollider2D capsule = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<CapsuleCollider2D>();
            Assert.AreEqual(CapsuleDirection2D.Horizontal, capsule.direction, "the model below assumes a horizontal capsule");
            float radius = capsule.size.y * .5f, half = capsule.size.x * .5f - radius;
            Rect skin = Rect.MinMaxRect(Fake.xMin - TouchSkin, Fake.yMin - TouchSkin, Fake.xMax + TouchSkin, Fake.yMax + TouchSkin);
            float dx = Mathf.Max(0f, Mathf.Max(skin.xMin - (r.X + half), (r.X - half) - skin.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(skin.yMin - r.Y, r.Y - skin.yMax));
            return dx * dx + dy * dy < radius * radius;
        }

        // ---------- §5.1 runtime ----------

        [TestCase(false, false)]   // the fake: never grounded on it
        [TestCase(true, true)]     // seen red: the same rect as a solid Floor is stood on
        public void WalkingOn_IsGroundedOnIt_OnlyWhenItIsSolid(bool solid, bool expectGrounded)
        {
            object replay = ReplayRoute(session, Fixture("FakeOverPitRoom", solid), Fixture("WalkRightUntilDead"));
            List<Rec> records = Records(replay);
            Assert.AreEqual(expectGrounded, records.Any(r => r.Grounded && r.Ground == "Fake"), Dump(replay));
            Assert.IsNotNull(F(replay, "Kill"), "the cat should end in the pit either way:\n" + Dump(replay));
        }

        [Test]
        public void IsNotSolid_TheCatFallsThroughItsSpan()
        {
            object replay = ReplayRoute(session, Fixture("FakeOverPitRoom", false), Fixture("WalkRightUntilDead"));
            List<Rec> records = Records(replay);
            Vector2 collider = Collider();
            // Below the fake's top while still inside its x span: nothing held the cat up.
            Assert.IsTrue(records.Any(r => r.X > Fake.xMin + collider.x * .5f && r.X < Fake.xMax && r.Y - collider.y * .5f < Fake.yMax - .1f), Dump(replay));
            Assert.AreEqual("Pit_Hazard", F(F(replay, "Kill"), "Killer"));
        }

        // "Touch + 1": the fake reveals on the first trap step after the first physics step that leaves the cat
        // overlapping it (its capsule against the fake's box grown by touchSkin), the same ordering as a
        // CollapsingFloor: ObserverSet.FixedUpdate steps the traps on the pose the previous Physics2D.Simulate left.
        [Test]
        public void Reveals_OnTheFirstTrapStepAfterTheFirstOverlappingPhysicsStep_AndNotBefore()
        {
            object replay = ReplayRoute(session, Fixture("FakeOverPitRoom", false), Fixture("WalkRightUntilDead"));
            List<Rec> records = Records(replay);
            IList raw = (IList)F(replay, "Records");
            int e = ElementIndex(replay, "Fake");
            int fire = records.FindIndex(r => r.FireTick[e] >= 0);
            int touch = records.FindIndex(TouchesFake);
            Assert.Greater(touch, 0, "the cat never touched the fake:\n" + Dump(replay));
            // The trap steps on the pose the previous tick's physics left, so the fire record follows the touch record.
            Assert.AreEqual(touch + 1, fire, $"touch at record {touch}, fire at record {fire}\n" + Dump(replay));
            int[] before = (int[])F(raw[0], "Signature"), prior = (int[])F(raw[fire - 1], "Signature"), after = (int[])F(raw[fire], "Signature");
            Assert.AreEqual(before[e], prior[e], "the fake changed visibly before the cat touched it");
            Assert.AreNotEqual(before[e], after[e], "the fake didn't change visibly on its fire tick");
        }

        [TestCase(8.9f, false)]    // stops short of the touch skin: no reveal
        [TestCase(9.6f, true)]     // seen red: stops inside it
        public void StoppingNextToIt_RevealsOnlyWithinTheTouchSkin(float brakeX, bool expectReveal)
        {
            object replay = ReplayRoute(session, Fixture("FakeOverPitRoom", false), Fixture("BrakeFrom", brakeX));
            List<Rec> records = Records(replay);
            int e = ElementIndex(replay, "Fake");
            Assert.AreEqual(expectReveal, records.Any(r => r.FireTick[e] >= 0), Dump(replay));
            Assert.AreEqual(expectReveal, (int)replay.GetType().GetMethod("FirstVisibleChange").Invoke(replay, new object[] { "Fake" }) >= 0, Dump(replay));
            if (!expectReveal) Assert.IsTrue(records.Any(r => r.X + Collider().x * .5f > Fake.xMin - .5f), "the cat never came near the fake:\n" + Dump(replay));
        }

        [Test]
        public void ResetsWithTheRoom_AfterTheDeathHold()
        {
            object options = Options(-1, "None", 0);
            Set(options, "RecordAfterDeathTicks", 60);
            object replay = ReplayRoute(session, Fixture("FakeOverPitRoom", false), Fixture("WalkRightUntilDead"), options);
            IList raw = (IList)F(replay, "Records");
            List<Rec> records = Records(replay);
            int e = ElementIndex(replay, "Fake");
            int kill = (int)F(F(replay, "Kill"), "Tick");
            int[] authored = (int[])F(raw[0], "Signature");
            int killIndex = records.FindIndex(r => r.Tick == kill);
            Assert.GreaterOrEqual(records[killIndex].FireTick[e], 0, "the fake had not fired when the cat died");
            Assert.AreNotEqual(authored[e], ((int[])F(raw[killIndex], "Signature"))[e], "the fake was visible when the cat died");
            int reset = records.FindIndex(killIndex, r => r.FireTick[e] < 0);
            Assert.Greater(reset, killIndex, "the fake never reset after the death:\n" + Dump(replay));
            Assert.AreEqual(authored[e], ((int[])F(raw[reset], "Signature"))[e], "after the reset the fake doesn't look as authored");
            Assert.Less(records[reset].X, 3f, "the reset record should show the cat back at the checkpoint");
        }

        // ---------- builder: looks like a Floor ----------

        [Test]
        public void IsBuilt_WithTheColourSortingOrderAndSizeOfAFloorFromTheSameRect_AndATriggerBody()
        {
            Type builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
            MethodInfo buildElement = builder.GetMethod("BuildElement", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(buildElement, "SoloRoomBuilder.BuildElement not found.");
            object room = Call(T("RouteFixtures"), "FlatRoom");
            var rootGo = new GameObject("PAX080_LookRoot");
            try
            {
                RealityRoot root = rootGo.AddComponent<RealityRoot>();
                foreach (string kind in new[] { "Floor", "FakePlatform" })
                    buildElement.Invoke(null, new object[] { rootGo.transform, root, room, Element(kind, kind + "_Built", new Vector2(11f, -.25f), new Vector2(2f, .5f)), null, null, null, null, null, new List<string>() });
                Transform floor = rootGo.transform.Find("Floor_Built"), fake = rootGo.transform.Find("FakePlatform_Built");
                Assert.NotNull(floor, "no Floor built"); Assert.NotNull(fake, "no FakePlatform built");
                SpriteRenderer a = floor.GetComponent<SpriteRenderer>(), b = fake.GetComponent<SpriteRenderer>();
                Assert.AreEqual(a.sprite, b.sprite, "sprite");
                Assert.AreEqual(a.color, b.color, "colour");
                Assert.AreEqual(a.sortingOrder, b.sortingOrder, "sortingOrder");
                Assert.AreEqual(a.sortingLayerID, b.sortingLayerID, "sorting layer");
                Assert.AreEqual(a.bounds.size, b.bounds.size, "rendered size");
                Assert.AreEqual(floor.localPosition, fake.localPosition, "position");
                BoxCollider2D fa = floor.GetComponent<BoxCollider2D>(), fb = fake.GetComponent<BoxCollider2D>();
                Assert.AreEqual(fa.size, fb.size, "collider size");
                Assert.IsFalse(fa.isTrigger, "the Floor is solid");
                Assert.IsTrue(fb.isTrigger, "the fake platform's body is a trigger");
                Assert.NotNull(fake.GetComponent("CollapsingFloorTrap"), "the fake platform is a CollapsingFloorTrap");
                var trap = new SerializedObject(fake.GetComponent("CollapsingFloorTrap"));
                Assert.AreEqual(0, trap.FindProperty("delayTicks").intValue, "delay 0");
                Assert.AreEqual(0, trap.FindProperty("triggerSource").enumValueIndex, "Overlap");
                Assert.AreEqual(0, trap.FindProperty("repeatMode").enumValueIndex, "Once");
            }
            finally { UnityEngine.Object.DestroyImmediate(rootGo); }
        }

        // ---------- layout rules ----------

        [TestCase(true)]
        [TestCase(false)]
        public void ARequiredJumpOntoAFakePlatform_FailsReach(bool named)   // seen red on the fixture
        {
            var errors = (List<string>)Validator.GetMethod("Validate").Invoke(null, new[] { "FIX", Fixture("JumpOntoFakeRoom", named) });
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains("lands on Fake, a fake platform")), string.Join("\n", errors));
        }

        [TestCase(true, false)]    // a fake platform as a chain source is rejected
        [TestCase(false, true)]    // control: an Overlap collapsing floor of the same rect is accepted
        public void AFakePlatformNamedAsAChainSource_FailsTheChainValidator(bool fromFake, bool valid)
        {
            object[] args = { Fixture("ChainedFromRoom", fromFake), null };
            bool ok = (bool)Type.GetType("Parallax.Editor.Setup.TrapLayoutValidator, Parallax.Editor").GetMethod("TryValidate").Invoke(null, args);
            TestContext.Out.WriteLine(ok ? "valid" : (string)args[1]);
            Assert.AreEqual(valid, ok, (string)args[1]);
            if (!valid) StringAssert.Contains("chain source must be a trap", (string)args[1]);
        }

        [TestCase("FakeWithDelayRoom", "non-default trap settings")]
        [TestCase("FakeWithTriggerBoxRoom", "secondary (trigger) box")]
        public void AFakePlatformWhoseDataDisagreesWithTheBuilder_Fails(string fixture, string expected)   // seen red on the fixture
        {
            var errors = (List<string>)Validator.GetMethod("ValidateFakePlatformSettings").Invoke(null, new[] { "FIX", Fixture(fixture) });
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains(expected)), string.Join("\n", errors));
        }

        [Test]
        public void AFakePlatformWithDefaultSettings_PassesTheSettingsRule()
        {
            var errors = (List<string>)Validator.GetMethod("ValidateFakePlatformSettings").Invoke(null, new[] { "FIX", Fixture("FakeOverPitRoom", false) });
            CollectionAssert.IsEmpty(errors);
        }

        static object Element(string kind, string name, Vector2 position, Vector2 size)
        {
            Type type = Type.GetType("Parallax.Editor.Setup.SoloRoomElement, Parallax.Editor");
            Type kindType = Type.GetType("Parallax.Editor.Setup.SoloRoomElementKind, Parallax.Editor");
            ConstructorInfo ctor = type.GetConstructors().Single(c => c.GetParameters().Length == 8);
            Type settings = ctor.GetParameters()[6].ParameterType, role = ctor.GetParameters()[7].ParameterType;
            return ctor.Invoke(new object[] { Enum.Parse(kindType, kind), name, position, size, Vector2.zero, Vector2.zero, Activator.CreateInstance(settings), Enum.ToObject(role, 0) });
        }
    }
}
