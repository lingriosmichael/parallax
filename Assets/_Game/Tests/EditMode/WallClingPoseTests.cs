using System.IO;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110, §2.4): the art check. On a wall the presenter reuses the vine art (CatA_Hang clinging, CatA_Climb
    // backwards sliding, CatA_Leap from frame 1 jumping off), mirrored to face the wall, with the drawing placed so the paws
    // meet the face. Each clip's reach toward the wall is a CatVisualConfig value measured from the sheets (pinned here); the
    // capture rig then plays real wall scenarios on both sides (60 fps frames between the 50 Hz ticks, the interpolated pose)
    // and checks every frame: clinging, the paws within 0.03 u of the face and nothing more than 0.02 u inside the wall; a wall
    // jump, nothing more than 0.02 u inside, and the push-off frame's back edge within 0.03 u of the face. Vine residue is
    // judged by eye on the wall_* contact sheet (no automated measure exists; ruled 2026-10-04).
    public sealed class WallClingPoseTests
    {
        const string SheetFolder = "Assets/_Game/Art/Cats/CatA/";
        const float Short = .03f, Inside = .02f;

        static CatVisualConfig Config()
        {
            var c = AssetDatabase.LoadAssetAtPath<CatVisualConfig>("Assets/_Game/Data/CatA_VisualConfig.asset");
            Assert.NotNull(c, "CatA_VisualConfig");
            return c;
        }

        // Each frame's drawn extent right and left of the pivot (units, facing +1), read from the sheet PNG on disk.
        static (float right, float left)[] Extents(string sheet)
        {
            string path = SheetFolder + sheet;
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
            Assert.IsNotEmpty(sprites, path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), path);
                Color32[] px = tex.GetPixels32();
                float scale = tex.width / (float)sprites[0].texture.width;
                return sprites.Select(s =>
                {
                    Rect r = s.rect;
                    int x0 = Mathf.RoundToInt(r.x * scale), y0 = Mathf.RoundToInt(r.y * scale), w = Mathf.RoundToInt(r.width * scale), h = Mathf.RoundToInt(r.height * scale);
                    int min = int.MaxValue, max = int.MinValue;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if (px[(y0 + y) * tex.width + x0 + x].a >= 128) { min = Mathf.Min(min, x); max = Mathf.Max(max, x + 1); }
                    float pivot = s.pivot.x * scale, ppu = s.pixelsPerUnit * scale;
                    return ((max - pivot) / ppu, (pivot - min) / ppu);
                }).ToArray();
            }
            finally { Object.DestroyImmediate(tex); }
        }

        static float OnePixel(string sheet) => 1f / AssetDatabase.LoadAllAssetsAtPath(SheetFolder + sheet).OfType<Sprite>().First().pixelsPerUnit;

        [Test]
        public void TheHangReach_IsCatAHangsPaws()
        {
            var e = Extents("CatA_Hang.png");
            TestContext.Out.WriteLine($"Hang reaches {e[0].right:F4} u right of its pivot");
            Assert.AreEqual(e[0].right, Config().WallHangReach, OnePixel("CatA_Hang.png"));
        }

        [Test]
        public void TheSlideReach_IsTheMiddleOfCatAClimbsFrames_AndEveryFrameIsWithinTheChecksOfIt()
        {
            var e = Extents("CatA_Climb.png");
            float min = e.Min(x => x.right), max = e.Max(x => x.right), reach = Config().WallSlideReach;
            TestContext.Out.WriteLine($"Climb reaches {min:F4}-{max:F4} u right of its pivot");
            Assert.AreEqual((min + max) * .5f, reach, OnePixel("CatA_Climb.png"));
            Assert.LessOrEqual(reach - min, Short, "the shortest frame's paws are within 0.03 u of the face");
            Assert.LessOrEqual(max - reach, Inside, "the longest frame is no more than 0.02 u inside the wall");
        }

        [Test]
        public void TheLeapBackReach_IsEachCatALeapFramesBackEdge()
        {
            var e = Extents("CatA_Leap.png");
            CatVisualConfig c = Config();
            for (int i = 0; i < e.Length; i++)
            {
                TestContext.Out.WriteLine($"Leap {i}: back edge {e[i].left:F4} u behind its pivot");
                Assert.AreEqual(e[i].left, c.WallJumpBackReach(i), OnePixel("CatA_Leap.png"), $"frame {i}");
            }
        }

        static void Placement(string scenario, out int cling, out float minD, out float maxD, out int jump, out float pushOff, out float maxJump, out int[] sides)
        {
            var args = new object[] { scenario, 0, 0f, 0f, 0, 0f, 0f, null };
            string error = (string)CatVisualOnlyParityTests.CallEditor("CatCapture", "WallPlacement", args);
            Assert.IsNull(error, error);
            cling = (int)args[1]; minD = (float)args[2]; maxD = (float)args[3]; jump = (int)args[4]; pushOff = (float)args[5]; maxJump = (float)args[6]; sides = (int[])args[7];
            TestContext.Out.WriteLine($"{scenario}: {cling} clinging frames, d {minD:F4} to {maxD:F4}; {jump} wall-jump frames, push-off d {pushOff:F4}, max {maxJump:F4}; sides {string.Join(",", sides)}");
        }

        [TestCase("wall_right_down", 1)]
        [TestCase("wall_left_down", -1)]
        [TestCase("wall_shaft_down", 0)]
        public void OnAWall_ThePawsMeetTheFace_AndNothingIsDrawnInsideIt(string scenario, int side)
        {
            Placement(scenario, out int cling, out float minD, out float maxD, out int jump, out float pushOff, out float maxJump, out int[] sides);
            Assert.Greater(cling, 20, "the presenter showed WallCling or WallSlide");
            Assert.Greater(jump, 0, "the presenter showed WallJump");
            if (side != 0) CollectionAssert.AreEqual(new[] { side }, sides);
            else CollectionAssert.AreEqual(new[] { -1, 1 }, sides);
            Assert.GreaterOrEqual(minD, -Short, "clinging: the paws within 0.03 u of the face");
            Assert.LessOrEqual(maxD, Inside, "clinging: nothing more than 0.02 u inside the wall");
            Assert.That(pushOff, Is.InRange(-Short, Inside), "the push-off frame's back edge at the face");
            Assert.LessOrEqual(maxJump, Inside, "a wall jump: nothing more than 0.02 u inside the wall");
        }
    }
}
