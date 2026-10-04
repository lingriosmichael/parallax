using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.TrapArtTestKit;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§12 R8): for every disguised element that wears its host's skin, the frame on its reveal tick is pixel-identical
    // to the frame on the tick before, except for effects that start on that tick (dust, a launcher's slot and glint); only
    // after that does anything move. Checked with the hosts re-skinned with a textured tile (the A02 fill, until the ENV-10
    // tiles land), since a flat grey-box would pass anything. Each level's declared routes are replayed twice: once to find
    // every reveal tick, once to render the element's area on that tick and the one before.
    // PAX-104 (the developer's ruling): every other trap's bodies are hidden while one element's frames are compared. The test
    // checks that element's disguise, not what moves behind it: L010's Drop_10, sinking behind Pit10_Cover, showed through a
    // 1-px semi-transparent column of the tiled skin at a pixel boundary. That column is a real in-game tell, tracked in
    // PAX-V09, not here.
    public sealed class TrapArtRevealFrameTests
    {
        const int PixelsPerUnit = 128;
        const float Tolerance = 3f / 255f;
        static readonly Type Preview = Type.GetType("Parallax.Editor.Art.TrapArtPreview, Parallax.Editor");

        public static IEnumerable<TestCaseData> Levels()
        {
            foreach (string id in RoutedRoomIds()) yield return new TestCaseData(id).SetName("RevealFrame:" + id);
        }

        [TestCaseSource(nameof(Levels))]
        [Timeout(900000)]
        public void RevealFrame_IsIdenticalToTheFrameBefore_OnATexturedHost(string id)
        {
            object room = Room(id), routes = Routes(id);
            var named = new List<(string name, object route)> { ("solution", F(routes, "Solution")) };
            foreach (object b in (IEnumerable)F(routes, "Betrayals")) named.Add(((string)F(b, "Name"), F(b, "Route")));
            var errors = new List<string>();
            int compared = 0;
            using (IDisposable session = OpenSession())
            {
                foreach ((string name, object route) in named)
                {
                    // Pass 1: every skinned element's reveal ticks (its fire tick).
                    var reveals = new List<(string art, int tick)>();
                    object find = Activator.CreateInstance(T("ReplayOptions"));
                    Set(find, "AfterTick", (Action<int>)(tick =>
                    {
                        var rooms = Object.FindFirstObjectByType<RoomManager>();
                        foreach (TrapArt art in Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None))
                            if (Skinned(art) && art.Trap.LatestFireTick >= 0 && art.Trap.LatestFireTick == rooms.RoomLifeTick && tick > 1)
                                reveals.Add((art.name, tick));
                    }));
                    ReplayRoute(session, room, route, find);
                    if (reveals.Count == 0) continue;

                    // Pass 2: the same replay (the harness is deterministic), rendering each reveal and the tick before it.
                    var before = new Dictionary<string, Color[]>();
                    object capture = Activator.CreateInstance(T("ReplayOptions"));
                    bool reskinned = false;
                    Set(capture, "AfterTick", (Action<int>)(tick =>
                    {
                        if (!reskinned) { Assert.NotNull(Preview, "TrapArtPreview not found"); Preview.GetMethod("ReskinHostsWithA02").Invoke(null, null); reskinned = true; }
                        foreach (TrapArt art in Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None)) art.Apply();
                        foreach ((string artName, int revealTick) in reveals)
                        {
                            if (tick != revealTick - 1 && tick != revealTick) continue;
                            TrapArt art = Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None).First(a => a.name == artName);
                            Color[] frame = Render(art, startingNow: tick == revealTick);
                            if (tick == revealTick - 1) { before[artName + "@" + revealTick] = frame; continue; }
                            if (!before.TryGetValue(artName + "@" + revealTick, out Color[] previous)) continue;
                            compared++;
                            float worst = 0f; int changed = 0;
                            for (int i = 0; i < frame.Length; i++)
                            {
                                float d = Mathf.Max(Mathf.Abs(frame[i].r - previous[i].r), Mathf.Abs(frame[i].g - previous[i].g), Mathf.Abs(frame[i].b - previous[i].b), Mathf.Abs(frame[i].a - previous[i].a));
                                if (d > Tolerance) changed++;
                                worst = Mathf.Max(worst, d);
                            }
                            if (changed > 0) errors.Add($"{id} [{name}] {artName}: reveal tick {revealTick} differs from the tick before in {changed} of {frame.Length} pixels (worst {worst * 255f:F0}/255)");
                        }
                    }));
                    ReplayRoute(session, room, route, capture);
                    if (errors.Count > 20) break;
                }
            }
            TestContext.WriteLine($"{id}: {compared} reveals compared");
            Assert.IsEmpty(errors, string.Join("\n", errors.Take(20)));
        }

        static bool Skinned(TrapArt art) => art is IHostSkinned s && s.HostSkin.IsSet && art.Trap != null;

        // The element's grey-box area at 128 px per unit, with the cat, every other trap's bodies (PAX-104) and every effect
        // hidden, except this element's shards on its reveal tick (they are the frame; everything else that starts that tick
        // may differ).
        static Color[] Render(TrapArt art, bool startingNow)
        {
            Bounds area = art.Bodies[0].Greybox.bounds;
            var hidden = new List<Renderer>();
            foreach (CatMotor2D cat in Object.FindObjectsByType<CatMotor2D>(FindObjectsSortMode.None))
                foreach (Renderer r in cat.GetComponentsInChildren<Renderer>()) if (!r.forceRenderingOff) { r.forceRenderingOff = true; hidden.Add(r); }
            foreach (TrapArt other in Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None))
            {
                if (other != art)
                {
                    // Its whole art (skin, trims, launcher) and its grey-box sprites.
                    var bodies = new List<Renderer>(other.GetComponentsInChildren<Renderer>());
                    foreach (TrapArt.Body b in other.Bodies) { bodies.Add(b.Art); bodies.Add(b.Greybox); }
                    foreach (Renderer r in bodies)
                        if (r != null && !r.transform.IsChildOf(art.transform) && !r.forceRenderingOff) { r.forceRenderingOff = true; hidden.Add(r); }
                }
                foreach (TrapArt.Effect e in other.Effects)
                    if (e.Renderer != null && !(other == art && startingNow && e.Group == "shard") && !e.Renderer.forceRenderingOff) { e.Renderer.forceRenderingOff = true; hidden.Add(e.Renderer); }
            }
            var go = new GameObject("__RevealCamera");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.clear;
                cam.transform.position = new Vector3(area.center.x, area.center.y, -20f);
                cam.orthographicSize = area.extents.y;
                int w = Mathf.Max(8, Mathf.RoundToInt(area.size.x * PixelsPerUnit)), h = Mathf.Max(8, Mathf.RoundToInt(area.size.y * PixelsPerUnit));
                cam.aspect = w / (float)h;
                var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                RenderTexture previous = RenderTexture.active;
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                RenderTexture.active = previous; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                Color[] pixels = tex.GetPixels();
                Object.DestroyImmediate(tex);
                return pixels;
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach (Renderer r in hidden) r.forceRenderingOff = false;
            }
        }
    }
}
