using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.TrapArtTestKit;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§9.1, §11 R4/R5): art parity on every level's declared routes (the solution and every betrayal), tick by
    // tick, through the route harness (ReplayOptions.AfterTick). After each tick every trap's art is applied (as the
    // game's LateUpdate would) and checked:
    //  - a body is drawn exactly when its grey-box renderer is enabled, inside the grey-box's bounds (1 px at 128 px/unit),
    //    grown by (BodyScale - 1) of its size on each side (D-101);
    //  - an effect is drawn only while the event it shows is happening, read from trap state (a crumble after its floor is
    //    gone, a glint in the arrow's tell, splinters after it stopped, the geyser's tell in its tell and its burst while
    //    erupting, a block's landing dust once it has landed, a mover's dust while it moves, a door's scrape while it moves,
    //    a pulse, flare or snap within its length of the fire; TrapArtMath holds the lengths);
    //  - a death effect is drawn only in the hold, and only the effect of what killed the cat (§3 rule 6);
    //  - the arrow's glint and the geyser's tell are on for exactly their declared window;
    //  - a disguised trap draws exactly its host's skin until it reveals (P10);
    //  - at most 150 effect sprites are drawn at once (R6's interim particle budget);
    //  - a checkpoint rewind puts every trap's art back exactly as it was at the gate tick (D-091).
    // A trap whose art turns visible even one tick early fails, naming the level, the trap and the tick.
    public sealed class TrapArtParityTests
    {
        const int MaxLiveEffects = 150;

        public static IEnumerable<TestCaseData> Levels()
        {
            foreach (string id in RoutedRoomIds()) yield return new TestCaseData(id).SetName("Parity:" + id);
        }

        [TestCaseSource(nameof(Levels))]
        [Timeout(900000)]
        public void ArtMatchesTheGreyboxTickForTick_OnEveryDeclaredRoute(string id)
        {
            object room = Room(id), routes = Routes(id);
            var named = new List<(string name, object route)> { ("solution", F(routes, "Solution")) };
            foreach (object b in (IEnumerable)F(routes, "Betrayals")) named.Add(((string)F(b, "Name"), F(b, "Route")));

            var errors = new List<string>();
            int gated = 0, maxLive = 0, rewindsChecked = 0;
            using (IDisposable session = OpenSession())
            {
                foreach ((string name, object route) in named)
                {
                    var probe = new Probe(id, name, room, errors);
                    object options = Activator.CreateInstance(T("ReplayOptions"));
                    Set(options, "AfterTick", (Action<int>)probe.Check);
                    ReplayRoute(session, room, route, options);
                    gated = Math.Max(gated, probe.Gated);
                    maxLive = Math.Max(maxLive, probe.MaxLive);
                    rewindsChecked += probe.Rewinds;
                    if (errors.Count > 40) break;
                }
            }
            TestContext.WriteLine($"{id}: {named.Count} routes, {gated} gated traps, most effects drawn at once {maxLive}, rewinds checked {rewindsChecked}");
            Assert.IsEmpty(errors, $"{id}:\n" + string.Join("\n", errors.Take(40)));
            Assert.That(maxLive, Is.LessThanOrEqualTo(MaxLiveEffects), $"{id}: {maxLive} effect sprites drawn at once (R6: at most {MaxLiveEffects})");
        }

        // The seen-red fixtures: one route, with `tamper` run on each art after it's applied and before it's checked.
        [Test]
        [Timeout(300000)]
        public void Parity_CatchesABlocksLandingDustOnItsReleaseTick()
        {
            List<string> errors = RunOne("L001", null, (art, tick) =>
            {
                if (art is not SolidArt solid || art.Trap.name != "Block_1" || TicksSince(art.Trap) != 0) return;
                SpriteRenderer dust = art.Effects.First(e => e.Group == "land").Renderer;
                dust.enabled = true; dust.sprite = Config().Dust[0]; dust.color = Color.white;
            });
            Assert.IsTrue(errors.Any(e => e.Contains("Block_1") && e.Contains("(land)")), "landing dust on the release tick passed:\n" + string.Join("\n", errors));
        }

        [Test]
        [Timeout(300000)]
        public void Parity_CatchesAnotherKillersDeathEffect()
        {
            // L015 T3: Arrow_3 kills (a pierce); a crush's dust drawn in the hold must fail.
            List<string> errors = RunOne("L015", "T3 [", (art, tick) =>
            {
                if (art is not DeathArt || !Object.FindFirstObjectByType<RoomDeath>().IsHolding) return;
                SpriteRenderer puff = art.Effects[0].Renderer;
                puff.enabled = true; puff.sprite = Config().Dust.Last(); puff.color = Color.white;
            });
            Assert.IsTrue(errors.Any(e => e.Contains("Death") && e.Contains("killed by")), "another killer's death effect passed:\n" + string.Join("\n", errors));
        }

        static List<string> RunOne(string id, string betrayal, Action<TrapArt, int> tamper)
        {
            object room = Room(id), routes = Routes(id);
            object route = betrayal == null ? F(routes, "Solution")
                : F(((IEnumerable)F(routes, "Betrayals")).Cast<object>().First(b => ((string)F(b, "Name")).StartsWith(betrayal)), "Route");
            var errors = new List<string>();
            using IDisposable session = OpenSession();
            var probe = new Probe(id, betrayal ?? "solution", room, errors) { Tamper = tamper };
            object options = Activator.CreateInstance(T("ReplayOptions"));
            Set(options, "AfterTick", (Action<int>)probe.Check);
            ReplayRoute(session, room, route, options);
            return errors;
        }

        static TrapArtConfig config;
        static TrapArtConfig Config() => config != null ? config : config = UnityEditor.AssetDatabase.LoadAssetAtPath<TrapArtConfig>("Assets/_Game/Data/TrapArtConfig.asset");

        // Ticks since `trap` fired (a re-arming flip's own countdown included); -1 before.
        static int TicksSince(RoomTrap trap)
        {
            if (trap is GravityFlipTrap flip && flip.RearmTicksSinceFire >= 0) return flip.RearmTicksSinceFire;
            RoomManager rooms = Object.FindFirstObjectByType<RoomManager>();
            return trap == null || trap.LatestFireTick < 0 || rooms == null ? -1 : rooms.RoomLifeTick - trap.LatestFireTick;
        }

        /// <summary>The death effect for a killer (§2's killer kinds): a pit floor or no killer (out of bounds) is a fall.</summary>
        static DeathKind Expected(Object killer, ISet<string> pits) => killer switch
        {
            null => DeathKind.Fall,
            Component c when pits.Contains(c.name) => DeathKind.Fall,
            StormCloudTrap => DeathKind.Lightning,
            ArrowTrap => DeathKind.Pierce,
            FallingBlockTrap => DeathKind.Crush,
            MovingTrap m => m.GetComponent<Hazard>() != null ? DeathKind.Spikes : DeathKind.Crush,
            _ => DeathKind.Spikes,
        };

        /// <summary>The sprites a death of this kind may draw (TrapArtSetup.BuildDeath's choice from the config).</summary>
        static ISet<Sprite> DeathSprites(DeathKind kind)
        {
            TrapArtConfig c = Config();
            return new HashSet<Sprite>(kind switch
            {
                DeathKind.Spikes => new[] { c.Glint, c.Dust.First() },
                DeathKind.Crush => new[] { c.Dust.Last() },
                DeathKind.Pierce => new[] { c.Glint },
                DeathKind.Lightning => new[] { c.Flash, c.Scorch, c.Glint },
                _ => new[] { c.Spray.First() },
            });
        }

        sealed class Probe
        {
            readonly string id, route;
            readonly List<string> errors;
            readonly HashSet<string> pits;
            RoomManager rooms;
            RoomDeath death;
            TrapArt[] arts;
            int previousRoomTick = -1;
            readonly Dictionary<int, string> firstPass = new();
            readonly Dictionary<TrapArt, Motion> motion = new();
            public int Gated, MaxLive, Rewinds;
            public Action<TrapArt, int> Tamper;

            // What a trap's grey-box (or its door) did, in room ticks (they stand still in a death hold, as the room does):
            // when it last moved or changed size, and when a falling block came to rest at the end of its travel. Read from
            // trap state, never from the art.
            sealed class Motion { public Vector3 Position; public Vector2 Size; public int Changed = -1000, Landed = -1, Fire = -1; public bool Seen; }

            public Probe(string id, string route, object room, List<string> errors)
            {
                this.id = id; this.route = route; this.errors = errors;
                pits = new HashSet<string>(((IEnumerable)room.GetType().GetField("Elements").GetValue(room)).Cast<object>()
                    .Where(e => e.GetType().GetField("HazardRole").GetValue(e).ToString() == "OpeningBottom")
                    .Select(e => (string)e.GetType().GetField("Name").GetValue(e)));
            }

            void Fail(int tick, string what) { if (errors.Count <= 40) errors.Add($"{id} [{route}] t{tick}: {what}"); }

            public void Check(int tick)
            {
                if (rooms == null)
                {
                    rooms = Object.FindFirstObjectByType<RoomManager>();
                    arts = Object.FindObjectsByType<TrapArt>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(a => a.name).ToArray();
                    var drawn = new HashSet<RoomTrap>(arts.Select(a => a.Trap).Where(t => t != null));
                    var drawnNames = new HashSet<string>(arts.Select(a => a.name));
                    foreach (RoomTrap trap in Object.FindObjectsByType<RoomTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (!IsGated(trap)) continue;
                        Gated++;
                        if (!drawn.Contains(trap)) Fail(tick, $"{trap.GetType().Name} '{trap.name}' has no art");
                    }
                    foreach (Hazard hazard in Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (!IsBareHazard(hazard)) continue;
                        Gated++;
                        if (!drawnNames.Contains(hazard.name)) Fail(tick, $"Hazard '{hazard.name}' has no art");
                    }
                    death = Object.FindFirstObjectByType<RoomDeath>();
                    if (!arts.Any(a => a is DeathArt)) Fail(tick, "the room has no death effects");
                }
                int roomTick = rooms != null ? rooms.RoomLifeTick : 0;
                int live = 0;
                var hash = new StringBuilder();
                foreach (TrapArt art in arts)
                {
                    if (art == null) continue;
                    art.Apply();
                    Track(art, roomTick);
                    Tamper?.Invoke(art, tick);
                    CheckBodies(art, tick);
                    live += CheckEffects(art, tick, roomTick);
                    CheckSkin(art, tick);
                    Hash(art, hash);
                }
                MaxLive = Math.Max(MaxLive, live);

                // D-091: the room clock goes back only at a reset or a rewind; the art must be as it was at that tick.
                string h = hash.ToString();
                if (previousRoomTick >= 0 && roomTick < previousRoomTick && firstPass.TryGetValue(roomTick, out string before))
                {
                    Rewinds++;
                    if (before != h) Fail(tick, $"after the rewind to room tick {roomTick}, the art differs from the first pass at that tick");
                }
                else if (!firstPass.ContainsKey(roomTick)) firstPass[roomTick] = h;
                previousRoomTick = roomTick;
            }

            void CheckBodies(TrapArt art, int tick)
            {
                foreach (TrapArt.Body b in art.Bodies)
                {
                    bool greybox = Shown(b.Greybox), drawn = Shown(b.Art) && b.Art.sprite != null;
                    if (greybox != drawn) { Fail(tick, $"{art.name}: body {(b.Art != null ? b.Art.name : "null")} drawn {drawn}, grey-box {(b.Greybox != null ? b.Greybox.name : "null")} shown {greybox}"); continue; }
                    if (!drawn) continue;
                    // D-101/D-105: a body may draw past its grey-box by its BodyGrowth (a fraction of the grey-box's size) on each side.
                    Bounds limit = b.Greybox.bounds;
                    Vector2 g = art.BodyGrowth;
                    limit.Expand(2f * Vector3.Scale(new Vector3(g.x, g.y, 0f), limit.size) + Vector3.one * (2f * TrapArtMath.BoundsTolerance));
                    Bounds a = b.Art.bounds;
                    if (!limit.Contains(a.min) || !limit.Contains(a.max)) Fail(tick, $"{art.name}: body {b.Art.name} {a} outside its grey-box {b.Greybox.bounds}");
                }
            }

            void Track(TrapArt art, int roomTick)
            {
                Transform watched = art is DoorRetreatArt ? (Transform)Private(art.Trap, "doorRoot")
                    : art is SolidArt && art.Bodies[0].Greybox != null ? art.Bodies[0].Greybox.transform : null;
                if (watched == null) return;
                if (!motion.TryGetValue(art, out Motion m)) motion[art] = m = new Motion();
                SpriteRenderer body = art is SolidArt ? art.Bodies[0].Greybox : null;
                Vector2 size = body != null ? (Vector2)body.bounds.size : Vector2.zero;
                if (m.Seen && ((watched.position - m.Position).sqrMagnitude > 1e-10f || (size - m.Size).sqrMagnitude > 1e-10f)) m.Changed = roomTick;
                m.Position = watched.position; m.Size = size; m.Seen = true;
                if (m.Fire != art.Trap.LatestFireTick) { m.Fire = art.Trap.LatestFireTick; m.Landed = -1; }
                if (art.Trap is FallingBlockTrap block && m.Landed < 0 && TicksSince(block) >= 0)
                {
                    Vector2 start = (Vector2)Private(block, "start"), at = block.GetComponent<Rigidbody2D>().position;
                    if (Vector2.Distance(start, at) >= (float)Private(block, "travelDistance") - 1e-4f) m.Landed = roomTick;
                }
            }

            int CheckEffects(TrapArt art, int tick, int roomTick)
            {
                int live = 0;
                RoomTrap trap = art.Trap;
                int s = TicksSince(trap);
                motion.TryGetValue(art, out Motion m);
                bool movedLately = m != null && roomTick - m.Changed <= 1;   // it moved this room tick or the last
                DeathKind? killedBy = death != null && death.IsHolding ? Expected(death.HoldKiller, pits) : null;
                bool anyTell = false, anyGlint = false, anyCharge = false, anyHole = false;
                foreach (TrapArt.Effect e in art.Effects)
                {
                    if (!Drawn(e.Renderer)) continue;
                    live++;
                    bool allowed;
                    switch (e.Group)
                    {
                        case "crumble": case "shard": allowed = !Shown(art.Bodies[0].Greybox); break;
                        case "glint": anyGlint = true; allowed = ArrowFired(art) && TrapArtMath.ArrowGlint(s, (int)Private(trap, "tellTicks")); break;
                        case "impact": allowed = ArrowFired(art) && s >= (int)Private(trap, "tellTicks") + ArrowMath.FlightTicks((float)Private(trap, "travel"), (float)Private(trap, "unitsPerTick")); break;
                        case "tell": anyTell = true; allowed = trap is GeyserTrap g && g.Phase == GeyserPhase.Tell; break;
                        case "burst": allowed = trap is GeyserTrap g2 && g2.Phase == GeyserPhase.Erupt; break;
                        case "reveal": allowed = ArrowFired(art); break;
                        case "grit": allowed = s >= 0 && s < 12 && Shown(art.Bodies[0].Greybox); break;
                        // From its release until it has lain still for the crack's fade.
                        case "crack": allowed = s >= 0 && (m == null || m.Landed < 0 || roomTick - m.Landed < TrapArtMath.CrackFadeTicks); break;
                        case "land": allowed = s >= 0 && m != null && m.Landed >= 0 && roomTick - m.Landed < TrapArtMath.LandDustTicks; break;
                        // PAX-102: a roof block's hole, once the block is its own height away from its place.
                        case "hole": anyHole = true; allowed = BlockHasLeft(art); break;
                        // Dust while the grey-box moves; a shrinker's chips while it shrinks (from its fire tick).
                        case "move": allowed = s >= 0 && movedLately; break;
                        case "erode": allowed = s >= 0 && (s == 0 || movedLately); break;
                        case "scrape": allowed = s >= 0 && m != null && (s == 0 || roomTick - m.Changed <= TrapArtMath.DoorDustTicks); break;
                        case "pulse": allowed = s >= 0 && s < TrapArtMath.FlipPulseTicks; break;
                        case "flare": allowed = s >= 0 && s < TrapArtMath.InverterFlareTicks; break;
                        case "snap": allowed = trap is ClimbVine vine && vine.IsSnapped && s >= 0 && s < TrapArtMath.VineFallTicks; break;
                        case "charge": anyCharge = true; allowed = trap is StormCloudTrap c && c.Phase == StormCloudPhase.Charge; break;
                        case "flash": case "scorch": allowed = trap is StormCloudTrap c2 && c2.Phase == StormCloudPhase.Strike; break;
                        case "death":
                            allowed = killedBy != null;
                            if (allowed && !DeathSprites(killedBy.Value).Contains(e.Renderer.sprite))
                                Fail(tick, $"{art.name}: death effect {e.Renderer.name} draws '{(e.Renderer.sprite != null ? e.Renderer.sprite.name : "none")}', but the cat was killed by {(death.HoldKiller != null ? death.HoldKiller.name : "nothing")} ({killedBy})");
                            break;
                        default: allowed = false; Fail(tick, $"{art.name}: effect group '{e.Group}' has no parity rule"); break;
                    }
                    if (!allowed) Fail(tick, $"{art.name}: effect {e.Renderer.name} ({e.Group}) drawn outside its event (room tick {roomTick}, {s} since fire)");
                }
                // The tells, on for exactly their window.
                if (trap is ArrowTrap && ArrowFired(art) && TrapArtMath.ArrowGlint(s, (int)Private(trap, "tellTicks")) && !anyGlint)
                    Fail(tick, $"{art.name}: no glint in the tell ({s} since fire)");
                if (trap is GeyserTrap geyser && geyser.Phase == GeyserPhase.Tell && !anyTell)
                    Fail(tick, $"{art.name}: no tell art in the geyser's tell");
                if (art.Effects.Any(e => e.Group == "hole") && BlockHasLeft(art) && !anyHole)
                    Fail(tick, $"{art.name}: the block has left its place but its hole doesn't show");
                if (trap is StormCloudTrap cloud && cloud.Phase == StormCloudPhase.Charge && !anyCharge)
                    Fail(tick, $"{art.name}: no charge art in the storm's 25-tick charge");
                return live;
            }

            static bool ArrowFired(TrapArt art) => art.Bodies.Count > 1 && Shown(art.Bodies[1].Greybox);

            // PAX-102: the block's grey-box is at least its own height from where it was built.
            static bool BlockHasLeft(TrapArt art) => art.Trap is FallingBlockTrap block && art.Bodies[0].Greybox != null
                && Vector2.Distance((Vector2)Private(block, "start"), art.Bodies[0].Greybox.transform.position) >= art.Bodies[0].Greybox.size.y - 1e-3f;

            void CheckSkin(TrapArt art, int tick)
            {
                if (art is CollapsingFloorArt floor && Shown(floor.Skin))
                {
                    string d = floor.HostSkin.Difference(floor.Skin);
                    if (d != null) Fail(tick, $"{art.name}: before its reveal it doesn't draw its host's skin: {d}");
                }
                if (art is SolidArt solid && Shown(solid.Skin) && (solid.Trap.LatestFireTick < 0 || solid.Kind == SolidArtKind.Shrinker && rooms.RoomLifeTick < solid.Trap.LatestFireTick))
                {
                    string d = solid.HostSkin.Difference(solid.Skin);
                    if (d != null) Fail(tick, $"{art.name}: before it moves it doesn't draw its host's skin: {d}");
                }
                if (art is ArrowArt arrow && arrow.Disguised)
                {
                    string d = arrow.HostSkin.Difference(arrow.LauncherArt);
                    if (d != null) Fail(tick, $"{art.name}: the unfired launcher doesn't draw its host's skin: {d}");
                }
            }

            static void Hash(TrapArt art, StringBuilder into)
            {
                into.Append(art.name).Append('{');
                foreach (TrapArt.Body b in art.Bodies) into.Append(Signature(b.Art)).Append(';');
                foreach (TrapArt.Effect e in art.Effects) into.Append(Drawn(e.Renderer) ? Signature(e.Renderer) : "-").Append(';');
                into.Append('}');
            }
        }
    }
}
