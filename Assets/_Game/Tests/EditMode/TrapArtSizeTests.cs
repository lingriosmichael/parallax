using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A18 (D-151, the developer: "Make sure sizes are always consistent throughout"): every trap kind draws at one size in
    // every level. Each level L001-L020 is built with its art; the drawn size of every spike blade (on a standard 0.3 u strip),
    // honest launcher, inverter orb and flip ring is collected, and each kind must have one size across all twenty.
    public sealed class TrapArtSizeTests
    {
        const float Tolerance = 2e-3f;
        const float StandardStrip = 0.3f;

        static readonly List<string> Levels = Enumerable.Range(1, 20).Select(i => $"L{i:000}").ToList();

        [Test]
        public void EveryTrapKind_DrawsAtOneSize_InEveryLevel()
        {
            var sizes = new Dictionary<string, List<(string where, Vector2 size)>>();
            var reached = new List<string>();
            void Note(string kind, string where, Vector2 size)
            {
                if (!sizes.TryGetValue(kind, out var list)) sizes[kind] = list = new List<(string, Vector2)>();
                list.Add((where, size));
            }
            TrapArtTestKit.InScratchScene(() =>
            {
                foreach (string id in Levels)
                {
                    Transform room = TrapArtTestKit.Build(TrapArtTestKit.Level(id), true);
                    foreach (TrapArt art in room.GetComponentsInChildren<TrapArt>(true))
                    {
                        var so = new SerializedObject(art);
                        // Spikes as revealed (hidden and periodic ones draw nothing while down, so their blades aren't posed).
                        if (art is SpikeArt && so.FindProperty("greyboxBody").objectReferenceValue is SpriteRenderer shown) shown.enabled = true;
                        art.Apply();
                        switch (art)
                        {
                            case SpikeArt spikes:
                            {
                                var greybox = (SpriteRenderer)so.FindProperty("greyboxBody").objectReferenceValue;
                                if (greybox == null) break;
                                bool side = so.FindProperty("pointsSide").intValue != 0;
                                float strip = side ? greybox.size.x : greybox.size.y;
                                if (Mathf.Abs(strip - StandardStrip) > 0.01f) break;
                                // The developer (2026-10-16): a strip hidden under a floor draws smaller rather than show through it.
                                // A blade's scale (its sprite's own size times it is what's drawn); the same for every blade.
                                for (int i = 0; i < spikes.Blades.Count; i++)
                                {
                                    SpriteRenderer b = spikes.Blades[i];
                                    if (spikes.Reached(i)) { reached.Add($"{id} {art.name}/{b.name} ({Mathf.Abs(b.transform.lossyScale.x):F2})"); continue; }
                                    if (b != null && b.sprite != null) Note(spikes.IsBack(i) ? "spike back-row blade scale" : "spike blade scale", $"{id} {art.name}", new Vector2(Mathf.Abs(b.transform.lossyScale.x), 0f));
                                }
                                break;
                            }
                            case ArrowArt arrow when !arrow.Disguised && arrow.LauncherArt != null && arrow.LauncherArt.sprite != null:
                                Note("launcher", $"{id} {art.name}", Unrotated(arrow.LauncherArt));
                                break;
                            case InverterArt:
                                if (so.FindProperty("orb").objectReferenceValue is SpriteRenderer orb && orb.sprite != null)
                                {
                                    // Without its breathing (a pure function of the room clock, the same everywhere).
                                    Note("inverter orb", $"{id} {art.name}", new Vector2(orb.sprite.bounds.size.x * Mathf.Abs(orb.transform.lossyScale.x) / Breath(art), 0f));
                                }
                                break;
                            case FlipArt:
                                if (so.FindProperty("ring").objectReferenceValue is SpriteRenderer ring && ring.sprite != null && so.FindProperty("zoneSize").vector2Value is Vector2 zone && Mathf.Min(zone.x, zone.y) >= 0.95f)
                                    Note("flip ring", $"{id} {art.name}", Unrotated(ring));
                                break;
                        }
                    }
                    Object.DestroyImmediate(room.root.gameObject);
                }
            });
            var failures = new List<string>();
            foreach (var pair in sizes)
            {
                Vector2 first = pair.Value[0].size;
                var off = pair.Value.Where(v => (v.size - first).magnitude > Tolerance).Take(8).ToList();
                if (off.Count > 0)
                    failures.Add($"{pair.Key}: {pair.Value.Count} drawn; {off.Count}+ differ from {pair.Value[0].where} ({first.x:F3} x {first.y:F3}): " +
                                 string.Join(", ", off.Select(o => $"{o.where} ({o.size.x:F3} x {o.size.y:F3})")));
            }
            TestContext.WriteLine($"Blades drawn smaller to stay inside their cover ({reached.Count}): {string.Join(", ", reached)}");
            Assert.IsNotEmpty(sizes, "no trap art was measured");
            Assert.IsEmpty(failures, "PAX-A18 (D-151): a trap kind draws at more than one size:\n" + string.Join("\n", failures));
        }

        // The developer (2026-10-16), about tips showing above the floor that hides them: "This is the type of things I would
        // like to avoid visually." Every spike blade, revealed at the level's start: its tip lies inside a solid exactly when
        // its foot does. Spikes hidden under a floor stay inside it; spikes in the open pierce nothing.
        [Test]
        public void EverySpikeBlade_StaysInsideItsCover_AndPiercesNothing()
        {
            var failures = new List<string>();
            int covered = 0, measured = 0;
            Type skin = Type.GetType("Parallax.Editor.Setup.SoloRoomSkin, Parallax.Editor");
            Assert.NotNull(skin, "SoloRoomSkin not found");
            TrapArtTestKit.InScratchScene(() =>
            {
                foreach (string id in Levels)
                {
                    object level = TrapArtTestKit.Level(id);
                    var solids = ((IEnumerable)skin.GetMethod("Solids").Invoke(null, new[] { level })).Cast<object>()
                        .Select(s => ((string)s.GetType().GetField("Name").GetValue(s), (Rect)s.GetType().GetField("Rect").GetValue(s))).ToList();
                    Transform room = TrapArtTestKit.Build(level, true);
                    foreach (SpikeArt spikes in room.GetComponentsInChildren<SpikeArt>(true))
                    {
                        var so = new SerializedObject(spikes);
                        if (so.FindProperty("greyboxBody").objectReferenceValue is SpriteRenderer g) g.enabled = true;
                        spikes.Apply();
                        foreach (SpriteRenderer b in spikes.Blades)
                        {
                            if (b == null || b.sprite == null) continue;
                            measured++;
                            Vector2 dir = b.transform.up * (b.flipY ? -1f : 1f);
                            Vector2 foot = (Vector2)b.transform.position + dir * 0.02f;
                            Vector2 tip = (Vector2)b.transform.position + dir * (b.sprite.bounds.max.y * Mathf.Abs(b.transform.lossyScale.y));
                            var atFoot = solids.Where(s => s.Item2.Contains(foot)).Select(s => s.Item1).ToList();
                            var atTip = solids.Where(s => s.Item2.Contains(tip)).Select(s => s.Item1).ToList();
                            if (atFoot.Count > 0) covered++;
                            if (!atFoot.OrderBy(n => n).SequenceEqual(atTip.OrderBy(n => n)))
                                failures.Add($"{id} {spikes.name}/{b.name}: foot in [{string.Join(",", atFoot)}], tip at ({tip.x:F2}, {tip.y:F2}) in [{string.Join(",", atTip)}]");
                        }
                    }
                    Object.DestroyImmediate(room.root.gameObject);
                }
            });
            Assert.Greater(measured, 0, "no spike blade was measured");
            Assert.Greater(covered, 0, "no covered strip found (L019 Thorns_0 lies under Hinge_0): the solids and the blades aren't in one frame");
            Assert.IsEmpty(failures, "a spike blade shows past its cover or pierces a solid:\n" + string.Join("\n", failures.Take(40)));
        }

        static Vector2 Unrotated(SpriteRenderer r) =>
            r.sprite == null ? Vector2.zero : Vector2.Scale(r.sprite.bounds.size, new Vector2(Mathf.Abs(r.transform.lossyScale.x), Mathf.Abs(r.transform.lossyScale.y)));

        // InverterArt's breathing at the room's tick (0.92 + 0.08 sin(tick x 0.08)); in a freshly built room the tick is 0.
        static float Breath(TrapArt art) => 0.92f;
    }
}
