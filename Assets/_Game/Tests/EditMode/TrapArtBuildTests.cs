using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEngine;
using static Parallax.Tests.EditMode.TrapArtTestKit;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§9.1, §11 R5): the art never changes what the game or the route harness sees. Each level and Trap Lab room
    // is built twice, the grey-box only and with the art: every collider, and every renderer the harness reads (every
    // SpriteRenderer under an element), must be identical, and all the art must sit outside every element. Then
    // ValidateTrapSkins (P10): every disguised trap's art draws exactly its host's skin before its reveal.
    public sealed class TrapArtBuildTests
    {
        static readonly Type SkinValidator = Type.GetType("Parallax.Editor.Art.TrapSkinValidator, Parallax.Editor");

        public static IEnumerable<TestCaseData> Rooms() => AllRooms();

        [TestCaseSource(nameof(Rooms))]
        public void ArtBuild_LeavesCollidersAndHarnessRenderersIdentical_AndDrawsEveryGatedTrap(string id)
        {
            InScratchScene(() =>
            {
                object room = Room(id);
                Transform bare = Build(room, art: false, "Bare"), dressed = Build(room, art: true, "Dressed");
                var errors = new List<string>();
                errors.AddRange(CompareColliders(bare, dressed));
                errors.AddRange(CompareElementRenderers(room, bare, dressed));
                errors.AddRange(ArtOutsideElements(room, dressed));
                errors.AddRange(GatedTrapsWithoutArt(dressed));
                Assert.IsEmpty(errors, id + ":\n" + string.Join("\n", errors));
            });
        }

        // The comparer itself, seen red: art put under an element must be caught.
        [Test]
        public void ArtBuildComparer_CatchesArtUnderAnElement()
        {
            InScratchScene(() =>
            {
                object room = Room("L002");
                Transform bare = Build(room, art: false, "Bare"), dressed = Build(room, art: true, "Dressed");
                string name = ElementNames(room).First(n => dressed.Find(n)?.GetComponent<CollapsingFloorTrap>() != null);
                var stray = new GameObject("StrayArt");
                stray.transform.SetParent(dressed.Find(name), false);
                stray.AddComponent<SpriteRenderer>().sprite = dressed.Find(name).GetComponent<SpriteRenderer>().sprite;
                Assert.IsNotEmpty(CompareElementRenderers(room, bare, dressed), "a renderer added under an element went unnoticed");
            });
        }

        [TestCaseSource(nameof(Rooms))]
        public void ValidateTrapSkins_PassesOnEveryRoom(string id)
        {
            InScratchScene(() =>
            {
                Transform dressed = Build(Room(id), art: true);
                List<string> errors = Validate(id, dressed, out int checkedCount);
                Assert.IsEmpty(errors, string.Join("\n", errors));
                int disguised = dressed.GetComponentsInChildren<RoomTrap>(true).Count(t => t is CollapsingFloorTrap || t is FallingBlockTrap || t is ShrinkingFloorTrap
                    || (t is MovingTrap m && m.GetComponent<Hazard>() == null && Private(m, "kind").ToString() == "Solid") || (t is ArrowTrap a && Disguised(a)));
                Assert.AreEqual(disguised, checkedCount, $"{id}: {disguised} disguised gated traps, {checkedCount} skins checked");
            });
        }

        // Seen red on a deliberately mismatched fixture (§9.1): one collapsing floor's pre-reveal art tinted off its host.
        [Test]
        public void ValidateTrapSkins_CatchesASkinThatDiffersFromItsHost()
        {
            InScratchScene(() =>
            {
                Transform dressed = Build(Room("L002"), art: true);
                CollapsingFloorArt art = dressed.GetComponentsInChildren<CollapsingFloorArt>(true).FirstOrDefault();
                Assert.NotNull(art, "L002 has no collapsing floor art to mismatch");
                art.Skin.color = art.Skin.color * 0.98f;
                List<string> errors = Validate("L002 mismatched", dressed, out _);
                Assert.IsTrue(errors.Any(e => e.Contains(art.name)), "the tinted skin passed: " + string.Join("\n", errors));
            });
        }

        static bool Disguised(ArrowTrap arrow)
        {
            // A disguised launcher's unfired colour is its host's (SoloRoomBuilder.HostColor); an honest one is LauncherGrey.
            SpriteRenderer launcher = (SpriteRenderer)Private(arrow, "launcher");
            return launcher != null && launcher.color != (Color)Private(arrow, "honestColor");
        }

        static List<string> Validate(string id, Transform roomRoot, out int checkedCount)
        {
            Assert.NotNull(SkinValidator, "Parallax.Editor.Art.TrapSkinValidator not found.");
            MethodInfo m = SkinValidator.GetMethod("ValidateTrapSkins", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "TrapSkinValidator.ValidateTrapSkins not found.");
            object[] args = { id, roomRoot, 0 };
            var errors = (List<string>)m.Invoke(null, args);
            checkedCount = (int)args[2];
            return errors;
        }

        static IEnumerable<string> CompareColliders(Transform bare, Transform dressed)
        {
            Dictionary<string, string> Describe(Transform room) => room.GetComponentsInChildren<Collider2D>(true)
                .ToDictionary(c => Path(c.transform, room) + "#" + c.GetType().Name + "#" + Array.IndexOf(c.GetComponents<Collider2D>(), c), c =>
                {
                    string shape = c is BoxCollider2D b ? $"{b.size}" : c is CapsuleCollider2D k ? $"{k.size}" : c is CircleCollider2D r ? $"{r.radius}" : "";
                    return $"{c.enabled}|{c.isTrigger}|{c.gameObject.layer}|{c.offset}|{shape}|{c.transform.position}|{c.transform.lossyScale}";
                });
            Dictionary<string, string> a = Describe(bare), b = Describe(dressed);
            foreach (string k in a.Keys.Union(b.Keys).OrderBy(k => k))
            {
                if (!a.ContainsKey(k)) yield return "collider added by the art: " + k;
                else if (!b.ContainsKey(k)) yield return "collider missing with the art: " + k;
                else if (a[k] != b[k]) yield return $"collider {k} differs: {a[k]} vs {b[k]}";
            }
        }

        static IEnumerable<string> CompareElementRenderers(object room, Transform bare, Transform dressed)
        {
            foreach (string name in ElementNames(room))
            {
                Transform x = bare.Find(name), y = dressed.Find(name);
                if (x == null && y == null) continue;
                if (x == null || y == null) { yield return $"element {name} built in only one of the builds"; continue; }
                SpriteRenderer[] rx = x.GetComponentsInChildren<SpriteRenderer>(true), ry = y.GetComponentsInChildren<SpriteRenderer>(true);
                if (rx.Length != ry.Length) { yield return $"element {name}: {rx.Length} renderers without the art, {ry.Length} with it"; continue; }
                for (int i = 0; i < rx.Length; i++)
                {
                    string px = Path(rx[i].transform, bare), py = Path(ry[i].transform, dressed);
                    if (px != py || Signature(rx[i]) != Signature(ry[i]) || rx[i].forceRenderingOff != ry[i].forceRenderingOff)
                        yield return $"element {name}: renderer {px} differs: {Signature(rx[i])} vs {py} {Signature(ry[i])}";
                }
            }
        }

        static IEnumerable<string> ArtOutsideElements(object room, Transform dressed)
        {
            var elements = ElementNames(room).Select(n => dressed.Find(n)).Where(t => t != null).ToList();
            foreach (TrapArt art in dressed.GetComponentsInChildren<TrapArt>(true))
                foreach (Transform e in elements)
                    if (art.transform.IsChildOf(e)) yield return $"art {art.name} is under element {e.name}; art lives beside its trap (R5)";
        }

        static IEnumerable<string> GatedTrapsWithoutArt(Transform dressed)
        {
            TrapArt[] arts = dressed.GetComponentsInChildren<TrapArt>(true);
            var drawn = new HashSet<RoomTrap>(arts.Select(a => a.Trap).Where(t => t != null));
            var drawnNames = new HashSet<string>(arts.Select(a => a.name));
            foreach (RoomTrap trap in dressed.GetComponentsInChildren<RoomTrap>(true))
                if (IsGated(trap) && !drawn.Contains(trap)) yield return $"{trap.GetType().Name} '{trap.name}' has no art";
            foreach (Hazard hazard in dressed.GetComponentsInChildren<Hazard>(true))
                if (IsBareHazard(hazard) && !drawnNames.Contains(hazard.name)) yield return $"Hazard '{hazard.name}' has no art";
            if (!arts.Any(a => a is DeathArt)) yield return "the room has no death effects";
        }
    }
}
