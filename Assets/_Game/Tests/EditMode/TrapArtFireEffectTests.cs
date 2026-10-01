using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.TrapArtTestKit;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§12 R10, §11 R4): a fire effect that never shows passes every parity check (showing less is always allowed),
    // so the fire effects and tells are checked to show, for exactly their window.
    //  - A re-arming flip (rearmOnExit, L004's visible flips) steps its own countdown, which doesn't feed the shared
    //    timing's LatestFireTick: its pulse must still show from the tick it flips the cat.
    //  - The inverter's cue on the cat: on from the fire tick, blinking over the last 30 ticks, off after, tick for tick
    //    against an independent ControlInversion (the tamper fixture proves a blink one tick early fails).
    //  - A crumbling disguise on a patterned host must sample it in world space (R8): ValidateTrapSkins rejects one that
    //    doesn't, since its shards would each tile from their own corner on the reveal tick.
    public sealed class TrapArtFireEffectTests
    {
        [Test]
        [Timeout(300000)]
        public void RearmingFlip_PulsesFromTheTickItFlipsTheCat()
        {
            object room = RouteValidatorTests.Room("L004"), routes = RouteValidatorTests.Routes("L004");
            object betrayal = ((IEnumerable)F(routes, "Betrayals")).Cast<object>().First(b => ((string)F(b, "Name")).StartsWith("T2:"));
            int firedAt = -1, pulsedAt = -1;
            bool rearming = false;
            using (IDisposable session = OpenSession())
            {
                object options = Activator.CreateInstance(T("ReplayOptions"));
                Set(options, "AfterTick", (Action<int>)(tick =>
                {
                    var art = Object.FindObjectsByType<FlipArt>(FindObjectsSortMode.None).FirstOrDefault(a => a.Trap != null && a.Trap.name == "Flip_A");
                    if (art == null) return;
                    rearming = (bool)Private(art.Trap, "rearmOnExit");
                    if (firedAt < 0 && art.Trap.State == TrapState.Fired) firedAt = tick;
                    art.Apply();
                    if (pulsedAt < 0 && art.Effects.Any(e => e.Group == "pulse" && Drawn(e.Renderer))) pulsedAt = tick;
                }));
                ReplayRoute(session, room, F(betrayal, "Route"), options);
            }
            Assert.IsTrue(rearming, "L004's Flip_A is no longer a re-arming flip; pick another for this test");
            Assert.That(firedAt, Is.GreaterThanOrEqualTo(0), "Flip_A never fired on L004's T2");
            Assert.AreEqual(firedAt, pulsedAt, $"Flip_A fired at t{firedAt}; its pulse first showed at t{pulsedAt} (-1: never)");
        }

        [Test]
        [Timeout(300000)]
        public void InverterCue_IsOnForExactlyItsWindow_BlinkingOverTheLast30()
        {
            (List<string> errors, int blinkTicks) = CueWindow(null);
            Assert.IsEmpty(errors, string.Join("\n", errors.Take(20)));
            Assert.That(blinkTicks, Is.GreaterThanOrEqualTo(ControlInversion.BlinkTicks), "L012's solution never reached Orb_A's blink");
        }

        [Test]
        [Timeout(300000)]
        public void InverterCueCheck_CatchesABlinkThatStartsOneTickEarly()
        {
            // The blink opens with an "off" run: the cue still drawn on that first off tick must fail.
            (List<string> errors, _) = CueWindow((mark, expected, left) => { if (left == ControlInversion.BlinkTicks - 1) mark.enabled = true; });
            Assert.IsNotEmpty(errors, "a cue drawn on the blink's first off tick passed");
        }

        // Replays L012's solution; each tick compares Orb_A's cue art (the mark) with an independent ControlInversion fed the
        // trap's fire tick. `tamper` gets the mark's art renderer, the expected visibility and the ticks left in the window.
        static (List<string>, int) CueWindow(Action<SpriteRenderer, bool, int> tamper)
        {
            object room = Room("L012"), routes = Routes("L012");
            var errors = new List<string>();
            var reference = new ControlInversion();
            int lastFire = -2, blinkTicks = 0;
            using (IDisposable session = OpenSession())
            {
                object options = Activator.CreateInstance(T("ReplayOptions"));
                Set(options, "AfterTick", (Action<int>)(tick =>
                {
                    var art = Object.FindObjectsByType<InverterArt>(FindObjectsSortMode.None).FirstOrDefault(a => a.Trap != null && a.Trap.name == "Orb_A");
                    var rooms = Object.FindFirstObjectByType<RoomManager>();
                    var death = Object.FindFirstObjectByType<RoomDeath>();
                    if (art == null || rooms == null || death == null) return;
                    var trap = (InverterTrap)art.Trap;
                    if (trap.LatestFireTick != lastFire)
                    {
                        lastFire = trap.LatestFireTick;
                        if (lastFire >= 0) reference.Fire(lastFire, (int)Private(trap, "durationTicks")); else reference.Clear();
                    }
                    bool live = (bool)typeof(RoomTrap).GetProperty("IsRoomLive", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(trap);
                    if (!live || death.IsHolding) return;
                    art.Apply();
                    var markGreybox = (SpriteRenderer)Private(trap, "mark");
                    SpriteRenderer mark = art.Bodies.First(b => b.Greybox == markGreybox).Art;
                    int roomTick = rooms.RoomLifeTick, left = reference.Remaining(roomTick);
                    bool expected = reference.IsCueVisible(roomTick);
                    if (reference.HasFired && roomTick >= reference.FireTick && left < ControlInversion.BlinkTicks) blinkTicks++;
                    tamper?.Invoke(mark, expected, reference.HasFired && roomTick >= reference.FireTick ? left : -1);
                    bool drawn = Drawn(mark);
                    if (drawn != expected) errors.Add($"t{tick} (room tick {roomTick}, {left} left): cue drawn {drawn}, expected {expected}");
                }));
                ReplayRoute(session, room, F(routes, "Solution"), options);
            }
            return (errors, blinkTicks);
        }

        // PAX-096: InverterTrap moves its grey-box cue onto the cat in LateUpdate and InverterArt mirrors it in LateUpdate;
        // the art must run after the trap, or it draws the cue where the cat was a frame ago. Seen red: both were order 0.
        [Test]
        public void InverterArt_RunsAfterInverterTrap_SoTheCueNeverLagsTheCat()
        {
            static int Order(Type t) => t.GetCustomAttribute<DefaultExecutionOrder>()?.order ?? 0;
            Assert.Greater(Order(typeof(InverterArt)), Order(typeof(InverterTrap)),
                $"InverterArt's execution order ({Order(typeof(InverterArt))}) must be later than InverterTrap's ({Order(typeof(InverterTrap))})");
        }

        [Test]
        public void ValidateTrapSkins_RejectsACrumblingDisguiseOnAPatternedHostThatIsntWorldTiled()
        {
            InScratchScene(() =>
            {
                Transform dressed = Build(Room("L002"), art: true);
                CollapsingFloorArt art = dressed.GetComponentsInChildren<CollapsingFloorArt>(true).First();
                SpriteRenderer host = dressed.Find(art.HostSkin.HostName).GetComponent<SpriteRenderer>();
                Vector2 size = host.size;
                host.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/RealityA/Environment/A_GAME_Platform_Fill.png");
                host.drawMode = SpriteDrawMode.Tiled; host.size = size;
                // PAX-A15: level hosts are world-tiled now; a plain sprite material makes this the host the test names.
                host.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
                art.Reskin(HostSkin.Of(host));   // the disguise follows its host exactly, tiling from its own corner
                Type validator = Type.GetType("Parallax.Editor.Art.TrapSkinValidator, Parallax.Editor");
                object[] args = { "L002 patterned", dressed, 0 };
                var errors = (List<string>)validator.GetMethod("ValidateTrapSkins").Invoke(null, args);
                Assert.IsTrue(errors.Any(e => e.Contains(art.name) && e.Contains("world")), "a Tiled patterned host passed: " + string.Join("\n", errors));
            });
        }
    }
}
