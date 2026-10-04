using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-V09 (the developer's ruling (B)): every world-tiled draw uses a whole texture that repeats along the axes it tiles
    // (a trim one tile tall or wide may clamp the other way);
    // an atlased sprite fails. The world-tile shader wraps an atlased sprite with frac() inside its rectangle, and at the wrap
    // the UV jumps across the sprite between two neighbouring pixels: with anisotropic filtering on (the Editor's Ultra quality
    // forces it), the sampler's taps along that jump reach the atlas padding, a 1-px see-through column at every tile wrap (a
    // moving trap behind a disguise showed through it: a D-085 tell). A half-texel inset of the rectangle didn't remove it
    // (PAX-V09 §8). The real fix, sampling with the unwrapped UV's gradients, is in PAX-A17's open items, for a biome tile
    // that must be atlased. The environment's world-UV shader has no rectangle: its material anchors u, v or both to the
    // world (_Axis 0 both, 1 across, 2 down); _Axis 3 (dressing) draws with the sprite's own UV and doesn't tile.
    public sealed class WorldTileSkinTests
    {
        const string A02Fill = "Assets/_Game/Art/RealityA/Environment/A_GAME_Platform_Fill.png";   // atlased (Atlas_RealityA_Env)
        const string KitFill = "Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A3.png";        // a whole texture
        const string EnvWorldUVShader = "Parallax/2D/Env-Sprite-Lit-WorldUV";
        static readonly Vector4 WholeRect = new(0f, 0f, 1f, 1f);

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // The former render check (an atlased skin drawn alone was 30% see-through at its tile wrap), now a guard case.
        [Test]
        public void AnAtlasedSprite_FailsAsATile()
        {
            Sprite sprite = Load(A02Fill);
            Assert.NotNull(sprite, A02Fill);
            Assume.That(!Whole(sprite), "the A02 fill isn't atlased in this Editor (sprite packing off?)");
            StringAssert.Contains("atlased", Problem(sprite, WholeRect, repeatU: true, repeatV: true));
            StringAssert.Contains("atlased", Problem(sprite, null, repeatU: true, repeatV: false));
        }

        [Test]
        public void AWholeRepeatingTexture_PassesAsATile()
        {
            Sprite sprite = Load(KitFill);
            Assert.NotNull(sprite, KitFill);
            Assert.IsNull(Problem(sprite, WholeRect, repeatU: true, repeatV: true));
            StringAssert.Contains("part rectangle", Problem(sprite, new Vector4(0f, 0f, .5f, .5f), repeatU: true, repeatV: true));
        }

        public static IEnumerable<TestCaseData> Scenes()
        {
            foreach (string path in Directory.GetFiles("Assets/_Game/Scenes/Levels", "Level_*.unity").OrderBy(p => p))
                yield return new TestCaseData(path.Replace('\\', '/')).SetName("WorldTileSkin:" + Path.GetFileNameWithoutExtension(path));
            yield return new TestCaseData("Assets/Sandbox_TrapLab.unity").SetName("WorldTileSkin:Sandbox_TrapLab");
        }

        // Every world-tiled draw in the scene: the trap art's host skins, the rest-pose tiles (WorldTileSampling: a moving trap's
        // trims) and every renderer on either world shader (trap skins, the stone, trims).
        [TestCaseSource(nameof(Scenes))]
        public void EveryWorldTiledDraw_UsesAWholeRepeatingTexture(string scenePath)
        {
            var problems = new List<string>();
            int checkedCount = 0;
            void Check(string what, string problem) { checkedCount++; if (problem != null) problems.Add($"{what}: {problem}"); }
            WithScene(scenePath, scene =>
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (TrapArt art in root.GetComponentsInChildren<TrapArt>(true))
                        if (art is IHostSkinned skinned && skinned.HostSkin.IsSet && skinned.HostSkin.WorldTiled)
                            Check($"{PathOf(art.transform)} (host skin)", Problem(skinned.HostSkin.Sprite, skinned.HostSkin.UVRect, repeatU: true, repeatV: true));
                    foreach (WorldTileSampling sampling in root.GetComponentsInChildren<WorldTileSampling>(true))
                    {
                        var r = sampling.GetComponent<SpriteRenderer>();
                        if (r == null || r.sprite == null) continue;
                        Vector4 uvRect = new SerializedObject(sampling).FindProperty("uvRect").vector4Value;
                        Check($"{PathOf(sampling.transform)} (rest-pose tile)", Problem(r.sprite, uvRect, repeatU: !OneTileWide(r), repeatV: !OneTileTall(r)));
                    }
                    foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        Shader shader = r.sharedMaterial != null ? r.sharedMaterial.shader : null;
                        if (r.sprite == null || shader == null) continue;
                        if (shader.name == HostSkin.WorldTileShader)
                            Check($"{PathOf(r.transform)} ({shader.name})", Problem(r.sprite, null, repeatU: !OneTileWide(r), repeatV: !OneTileTall(r)));
                        else if (shader.name == EnvWorldUVShader)
                        {
                            int axis = Mathf.RoundToInt(r.sharedMaterial.GetFloat("_Axis"));
                            if (axis >= 3) continue;   // the sprite's own UV: dressing, not a tile
                            Check($"{PathOf(r.transform)} ({shader.name}, '{r.sharedMaterial.name}', _Axis {axis})", Problem(r.sprite, null, repeatU: axis != 2, repeatV: axis != 1));
                        }
                    }
                }
            });
            TestContext.WriteLine($"{scenePath}: {checkedCount} world-tiled draws checked");
            Assert.Greater(checkedCount, 0, "no world-tiled draw found; the scene isn't built with the kit?");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
        }

        static bool Whole(Sprite sprite) =>
            Mathf.Approximately(sprite.textureRect.width, sprite.texture.width) && Mathf.Approximately(sprite.textureRect.height, sprite.texture.height);

        // A trim drawn one tile tall (a cap, an underside) never wraps down, and one drawn one tile wide (a side, a post) never
        // wraps across, so its texture may clamp that way.
        static bool OneTileTall(SpriteRenderer r) => r.size.y <= r.sprite.bounds.size.y + 1e-3f;
        static bool OneTileWide(SpriteRenderer r) => r.size.x <= r.sprite.bounds.size.x + 1e-3f;

        // Null when `sprite`, drawn as a tile with `uvRect` (null: not stored here), wraps inside its own pixels along the axes
        // it tiles; otherwise why not.
        static string Problem(Sprite sprite, Vector4? uvRect, bool repeatU, bool repeatV)
        {
            Texture t = sprite.texture;
            if (!Whole(sprite)) return $"'{sprite.name}' is atlased ('{t.name}'); a tile must be a whole texture (PAX-V09)";
            if (uvRect.HasValue && uvRect.Value != WholeRect) return $"'{sprite.name}' is a whole texture with a part rectangle {uvRect.Value}";
            if (repeatU && t.wrapModeU != TextureWrapMode.Repeat) return $"'{sprite.name}' doesn't repeat across (wrap U {t.wrapModeU})";
            if (repeatV && t.wrapModeV != TextureWrapMode.Repeat) return $"'{sprite.name}' doesn't repeat down (wrap V {t.wrapModeV})";
            return null;
        }

        static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        static void WithScene(string path, Action<Scene> body)
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try { body(EditorSceneManager.OpenScene(path, OpenSceneMode.Single)); }
            finally { RestoreSetup(setup); }
        }

        static void RestoreSetup(string[] paths)
        {
            if (paths.Length == 0)
            {
                // PAX-075 R21: the Test Runner's untitled scene isn't in the setup list; recreate it rather than leave a scene loaded.
                Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                return;
            }
            EditorSceneManager.RestoreSceneManagerSetup(paths.Select((p, i) => new SceneSetup { path = p, isActive = i == 0, isLoaded = true }).ToArray());
        }
    }
}
