using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-V07 gauntlet item 0 (§11 R8, R9, R14): the capture harness's pure math (60 fps frames between 50 Hz ticks,
    // Rigidbody2D-style interpolation, a sprite pixel's world position for both facings and both gravities, the depth of a
    // point along gravity, paw clusters, and the transitions the contact sheets are cut around), and CatVisualPresenter.Present
    // stepped in EditMode. The harness lives in the Editor assembly, which this one doesn't reference: reached by reflection.
    public sealed class CatCaptureTests
    {
        static Type Math => Type.GetType("Parallax.Editor.Art.CatCaptureMath, Parallax.Editor");

        static object Call(string method, params object[] args)
        {
            Assert.NotNull(Math, "Parallax.Editor.Art.CatCaptureMath not found.");
            MethodInfo m = Math.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "CatCaptureMath." + method + " not found.");
            try { return m.Invoke(null, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }

        static object Get(object o, string field) => o.GetType().GetField(field).GetValue(o);

        // ---------- frames between ticks ----------

        [Test]
        public void TicksDue_At60FpsAgainst50Hz_FollowThePlayerLoop()
        {
            // Frame n is at n/60 s; tick k ends at k/50 s. A frame runs every tick whose end is at or before it.
            int[] expected = { 0, 0, 1, 2, 3, 4, 5, 5, 6, 7, 8, 9, 10 };
            for (int n = 0; n < expected.Length; n++)
                Assert.AreEqual(expected[n], (int)Call("TicksDueBy", n / 60.0, 0.02), "frame " + n);
        }

        [Test]
        public void InterpolationAlpha_IsTheFractionOfATickSinceTheLastOne()
        {
            Assert.AreEqual(0.5f, (float)Call("InterpolationAlpha", 0.03, 1, 0.02), 1e-5f);
            Assert.AreEqual(0f, (float)Call("InterpolationAlpha", 0.02, 1, 0.02), 1e-5f);
            Assert.AreEqual(5f / 6f, (float)Call("InterpolationAlpha", 1 / 60.0, 0, 0.02), 1e-5f);
            Assert.AreEqual(1f, (float)Call("InterpolationAlpha", 0.05, 1, 0.02), 1e-5f, "clamped");
        }

        [Test]
        public void InterpolatedPose_LerpsPositionAndRotation_AndSnapsATeleport()
        {
            object[] args = { new Vector2(0f, 0f), 0f, new Vector2(0.12f, -0.2f), 180f, 0.25f, 1f, null, null };
            MethodInfo m = Math.GetMethod("InterpolatedPose");
            Assert.NotNull(m, "CatCaptureMath.InterpolatedPose not found.");
            m.Invoke(null, args);
            Vector2 p = (Vector2)args[6];
            Assert.AreEqual(0.03f, p.x, 1e-6f);
            Assert.AreEqual(-0.05f, p.y, 1e-6f);
            Assert.AreEqual(45f, (float)args[7], 1e-4f);

            object[] teleport = { new Vector2(0f, 0f), 0f, new Vector2(5f, 0f), 0f, 0.25f, 1f, null, null };
            m.Invoke(null, teleport);
            Assert.AreEqual(5f, ((Vector2)teleport[6]).x, 1e-6f, "a move of a teleport distance or more shows the new pose at once");
        }

        // ---------- a sprite pixel in the world ----------

        // The prefab's hierarchy: root (rotation 0 gravity down, 180 gravity up) -> Visual at (0, -0.4), localScale.x = facing.
        static Matrix4x4 VisualToWorld(Vector2 root, bool gravityDown, float facing) =>
            Matrix4x4.TRS(root, Quaternion.Euler(0f, 0f, gravityDown ? 0f : 180f), Vector3.one)
            * Matrix4x4.TRS(new Vector3(0f, -0.4f, 0f), Quaternion.identity, new Vector3(facing, 1f, 1f));

        [TestCase(1f, true)]
        [TestCase(-1f, true)]
        [TestCase(1f, false)]
        [TestCase(-1f, false)]
        public void PixelToWorld_PlacesThePixelCentreFromThePivot(float facing, bool gravityDown)
        {
            const float ppu = 100f;
            var root = new Vector2(10f, 3f);
            // 20 px right of the pivot, on its row: 0.205 u ahead of the paw point (pixel centres are at +0.5).
            Vector2 w = (Vector2)Call("PixelToWorld", 120, 40, new Vector2(100f, 40f), ppu, VisualToWorld(root, gravityDown, facing));
            float ahead = facing * (gravityDown ? 1f : -1f);   // the cat's facing direction, in world x
            Vector2 paw = root + (gravityDown ? new Vector2(0f, -0.4f) : new Vector2(0f, 0.4f));
            Assert.AreEqual(paw.x + ahead * 0.205f, w.x, 1e-5f);
            Assert.AreEqual(paw.y + (gravityDown ? 0.005f : -0.005f), w.y, 1e-5f);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DepthAlongGravity_IsPositiveInsideTheSurface(bool gravityDown)
        {
            Vector2 down = gravityDown ? Vector2.down : Vector2.up;
            Vector2 surface = new(4f, gravityDown ? 0f : 7f);
            Vector2 inside = surface + down * 0.02f, above = surface - down * 0.03f;
            Assert.AreEqual(0.02f, (float)Call("DepthAlongGravity", inside, surface, down), 1e-6f);
            Assert.AreEqual(-0.03f, (float)Call("DepthAlongGravity", above, surface, down), 1e-6f);
        }

        [Test]
        public void Clusters_SplitWhereTheGapIsWiderThanTheLimit()
        {
            float[] along = { 1.00f, 1.01f, 1.02f, 1.30f, 1.31f, 2.00f };
            var clusters = (Vector2[])Call("Clusters", along, 0.05f);   // (centre, count)
            Assert.AreEqual(3, clusters.Length);
            Assert.AreEqual(1.01f, clusters[0].x, 1e-5f); Assert.AreEqual(3f, clusters[0].y);
            Assert.AreEqual(1.305f, clusters[1].x, 1e-5f); Assert.AreEqual(2f, clusters[1].y);
            Assert.AreEqual(2.00f, clusters[2].x, 1e-5f); Assert.AreEqual(1f, clusters[2].y);
        }

        // ---------- the contact sheets' transitions ----------

        [Test]
        public void SelectTransitions_CutsAWindowAroundEveryStateChange_AndMarksTakeoffsAndLandings()
        {
            // 0-19 Idle, 20-39 Walk, 40-44 Rise (airborne from 40), 45-49 Fall, 50-59 Land (grounded from 50), 60-99 Idle.
            var states = new string[100];
            var grounded = new bool[100];
            for (int i = 0; i < 100; i++)
            {
                states[i] = i < 20 ? "Idle" : i < 40 ? "Walk" : i < 45 ? "Rise" : i < 50 ? "Fall" : i < 60 ? "Land" : "Idle";
                grounded[i] = i < 40 || i >= 50;
            }
            var list = (Array)Call("SelectTransitions", states, grounded, 8, 24, 3);
            string Describe(object t) => $"{Get(t, "Kind")}@{Get(t, "Frame")}[{Get(t, "Start")}..{Get(t, "End")}]{((bool)Get(t, "Strip") ? " strip" : "")}";
            var got = new string[list.Length];
            for (int i = 0; i < list.Length; i++) got[i] = Describe(list.GetValue(i));
            CollectionAssert.AreEqual(new[]
            {
                "Idle-to-Walk@20[12..44]",
                "takeoff@40[32..64] strip",
                "Walk-to-Rise@40[32..64]",
                "Rise-to-Fall@45[37..69]",
                "landing@50[42..74] strip",
                "Fall-to-Land@50[42..74]",
                "Land-to-Idle@60[52..84]",
            }, got);
        }

        [Test]
        public void SelectTransitions_ClampsToTheCapture_AndKeepsAtMostNPerKind()
        {
            var states = new string[30];
            var grounded = new bool[30];
            for (int i = 0; i < 30; i++) { states[i] = i % 4 < 2 ? "Idle" : "Walk"; grounded[i] = true; }
            var list = (Array)Call("SelectTransitions", states, grounded, 8, 24, 2);
            Assert.AreEqual(4, list.Length, "two Idle-to-Walk and two Walk-to-Idle");
            object first = list.GetValue(0);
            Assert.AreEqual(2, (int)Get(first, "Frame"));
            Assert.AreEqual(0, (int)Get(first, "Start"), "clamped to frame 0");
            Assert.AreEqual(26, (int)Get(first, "End"));
            Assert.AreEqual(29, (int)Get(list.GetValue(3), "End"), "clamped to the last frame");
        }

        [Test]
        public void SelectStepCuts_CutWhereAMarkedStepStarts()
        {
            string[] steps = { "stand", "stand", "walk", "walk", "run", "run", "run", "stop", "stop", "run" };
            var list = (Array)Call("SelectStepCuts", steps, new[] { "run", "stop" }, 2, 3);
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual(4, (int)Get(list.GetValue(0), "Frame")); Assert.AreEqual("step: run", (string)Get(list.GetValue(0), "Kind"));
            Assert.AreEqual(2, (int)Get(list.GetValue(0), "Start")); Assert.AreEqual(7, (int)Get(list.GetValue(0), "End"));
            Assert.AreEqual(7, (int)Get(list.GetValue(1), "Frame"));
            Assert.AreEqual(9, (int)Get(list.GetValue(2), "End"), "clamped to the last frame");
        }

        [Test]
        public void SelectEventCuts_FindTurnsWalkOffsDeathsAndRespawns()
        {
            // 0-9 facing right on the ground; 10 turns left; 20 jumps (jumped at 19), airborne 20-29; 40 walks off a ledge;
            // 50 lands; 60-95 death hold; 96 respawn.
            int n = 120;
            var facing = new int[n]; var grounded = new bool[n]; var jumped = new bool[n]; var holding = new bool[n];
            for (int i = 0; i < n; i++)
            {
                facing[i] = i < 10 ? 1 : -1;
                grounded[i] = !(i >= 20 && i < 30) && !(i >= 40 && i < 50);
                jumped[i] = i == 19;
                holding[i] = i >= 60 && i < 96;
            }
            var list = (Array)Call("SelectEventCuts", facing, grounded, jumped, holding, 8, 24, 40, 3, 3);
            var got = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                object t = list.GetValue(i);
                got[i] = $"{Get(t, "Kind")}@{Get(t, "Frame")}[{Get(t, "Start")}..{Get(t, "End")}]{((bool)Get(t, "Strip") ? " strip" : "")}";
            }
            CollectionAssert.AreEqual(new[] { "turn@10[2..34]", "walk-off@40[32..64] strip", "death@60[52..100]", "respawn@96[88..119]" }, got);
        }

        // ---------- the presenter, stepped in EditMode ----------

        const string CatPrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        GameObject rootGo;

        [TearDown]
        public void TearDown()
        {
            if (rootGo != null) Object.DestroyImmediate(rootGo);
        }

        CatVisualPresenter BuildCat(out CatMotor2D motor)
        {
            rootGo = new GameObject("RealityRoot_A_CaptureTest");
            RealityRoot reality = rootGo.AddComponent<RealityRoot>();
            typeof(RealityRoot).GetField("id", Instance).SetValue(reality, ObserverId.A);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
            Assert.NotNull(prefab, CatPrefabPath);
            GameObject cat = Object.Instantiate(prefab, rootGo.transform);
            motor = cat.GetComponent<CatMotor2D>();
            Awake(cat.GetComponent<GravityReceiver>());
            Awake(motor);
            CatVisualPresenter presenter = cat.GetComponentInChildren<CatVisualPresenter>(true);
            Assert.NotNull(presenter, "Cat_Player.prefab has no CatVisualPresenter.");
            Awake(presenter);
            return presenter;
        }

        static void Awake(Component c) =>
            c.GetType().GetMethod("Awake", Instance, null, Type.EmptyTypes, null)?.Invoke(c, null);

        static void SetGrounded(CatMotor2D motor, bool grounded) =>
            typeof(CatMotor2D).GetField("<IsGrounded>k__BackingField", Instance).SetValue(motor, grounded);

        [Test]
        public void Present_StandingStill_ShowsIdle_ThenWalkingAdvancesTheWalkFrames()
        {
            CatVisualPresenter presenter = BuildCat(out CatMotor2D motor);
            SetGrounded(motor, true);
            SpriteRenderer body = presenter.GetComponent<SpriteRenderer>();
            Transform root = motor.transform;
            presenter.Present(1f / 60f);
            Assert.AreEqual(CatAnimState.Idle, presenter.State);
            Assert.AreEqual("Idle", presenter.ClipName);
            Assert.AreEqual(0, presenter.FrameIndex);
            StringAssert.StartsWith("CatA_Idle_", body.sprite.name);
            // At 6 u/s (the reference speed) Walk plays at its authored 10 fps: 0.22 s of 1/60 s frames is on its third frame.
            Sprite first = null;
            for (int i = 0; i < 14; i++)
            {
                root.position += new Vector3(0.1f, 0f, 0f);
                presenter.Present(1f / 60f);
                if (i == 0) first = body.sprite;
            }
            Assert.AreEqual("Walk", presenter.ClipName);
            Assert.AreEqual(2, presenter.FrameIndex);
            Assert.AreNotEqual(first, body.sprite, "the sprite advanced");
        }

        [Test]
        public void Present_MovingOnTheGround_ShowsWalk_AndFacesTheMotion()
        {
            CatVisualPresenter presenter = BuildCat(out CatMotor2D motor);
            SetGrounded(motor, true);
            SpriteRenderer body = presenter.GetComponent<SpriteRenderer>();
            Transform root = motor.transform;
            presenter.Present(1f / 60f);
            for (int i = 0; i < 10; i++) { root.position += new Vector3(-0.1f, 0f, 0f); presenter.Present(1f / 60f); }   // 6 u/s left
            Assert.AreEqual(CatAnimState.Walk, presenter.State);
            Assert.AreEqual("Walk", presenter.ClipName);
            StringAssert.StartsWith("CatA_Walk_", body.sprite.name);
            Assert.AreEqual(-1, presenter.Facing, "moving left faces left");
        }

        [Test]
        public void Present_Airborne_ShowsFall_ThenLandOnTouchdown()
        {
            CatVisualPresenter presenter = BuildCat(out CatMotor2D motor);
            SetGrounded(motor, false);
            Transform root = motor.transform;
            presenter.Present(1f / 60f);
            for (int i = 0; i < 5; i++) { root.position += new Vector3(0f, -0.1f, 0f); presenter.Present(1f / 60f); }
            Assert.AreEqual(CatAnimState.Fall, presenter.State);
            Assert.AreEqual("Fall", presenter.ClipName);
            SetGrounded(motor, true);
            presenter.Present(1f / 60f);
            Assert.AreEqual(CatAnimState.Land, presenter.State);
            Assert.AreEqual("Land", presenter.ClipName);
        }
    }
}
