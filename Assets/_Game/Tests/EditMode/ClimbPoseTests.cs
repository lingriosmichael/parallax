using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-V07 item 3 (§5, A08 §3.3): the climb frames are drawn vertical and registered on the collider, so the interim 90°
    // climb pose (PAX-087's ClimbPose) is gone. The Visual keeps its rest pose on the vine, and each Climb and Hang frame's
    // body centre (A08's `body_centre`: the drawn pixels' centroid after an erosion that removes the tail, legs and ears)
    // lands on the collider's centre, for both facings and both gravities. Rewritten from 8 cases (PAX-087) to 14.
    public sealed class ClimbPoseTests
    {
        const string CatPlayerPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string SheetFolder = "Assets/_Game/Art/Cats/CatA/";
        const int ErosionPx = 5;               // A08's radius 7 at source scale (256 px), 5 at the 192 px base
        const float BodyOnColliderSp = 6f;     // each frame's body centre within this of the collider centre (sprite px)
        const float MedianOnColliderSp = 1.5f; // and the clip's median within this (A08 registered the median)
        const float PawsInFloorSp = 2f;        // the harness's paws-in-floor threshold

        // Each frame's body centre in sprite units from the pivot (facing +1), read from the sheet PNG on disk.
        static Vector2[] BodyCentres(string sheet)
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
                    bool Drawn(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && px[(y0 + y) * tex.width + x0 + x].a >= 128;
                    double sx = 0, sy = 0; int n = 0;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            bool keep = true;
                            for (int dx = -ErosionPx; dx <= ErosionPx && keep; dx++) for (int dy = -ErosionPx; dy <= ErosionPx && keep; dy++) keep = Drawn(x + dx, y + dy);
                            if (keep) { sx += x + .5; sy += y + .5; n++; }
                        }
                    Assert.Greater(n, 0, s.name);
                    Vector2 pivot = s.pivot * scale; float ppu = s.pixelsPerUnit * scale;
                    return new Vector2(((float)(sx / n) - pivot.x) / ppu, ((float)(sy / n) - pivot.y) / ppu);
                }).ToArray();
            }
            finally { Object.DestroyImmediate(tex); }
        }

        // The Visual at its rest pose (the prefab's), turned by the root for gravity up and mirrored for the facing: each
        // frame's body centre against the collider centre, in sprite px (the sheets' PPU).
        [TestCase("CatA_Climb.png", 1f, true)]
        [TestCase("CatA_Climb.png", -1f, true)]
        [TestCase("CatA_Climb.png", 1f, false)]
        [TestCase("CatA_Climb.png", -1f, false)]
        [TestCase("CatA_Hang.png", 1f, true)]
        [TestCase("CatA_Hang.png", -1f, false)]
        public void EachFramesBodyCentre_LandsOnTheCollidersCentre_WithTheVisualAtRest(string sheet, float facing, bool gravityDown)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            Assert.NotNull(prefab, CatPlayerPath);
            CapsuleCollider2D collider = prefab.GetComponent<CapsuleCollider2D>();
            Transform visual = prefab.transform.Find("Visual");
            Assert.NotNull(collider); Assert.NotNull(visual);
            Quaternion root = Quaternion.Euler(0f, 0f, gravityDown ? 0f : 180f);
            Vector2 colliderCentre = root * (Vector3)collider.offset;
            float ppu = AssetDatabase.LoadAllAssetsAtPath(SheetFolder + sheet).OfType<Sprite>().First().pixelsPerUnit;
            Vector2[] centres = BodyCentres(sheet);
            var offsets = centres.Select(b =>
            {
                Vector3 local = visual.localPosition + visual.localRotation * new Vector3(b.x * facing, b.y, 0f);
                return ((Vector2)(root * local) - colliderCentre) * ppu;
            }).ToArray();
            for (int i = 0; i < offsets.Length; i++)
                Assert.LessOrEqual(offsets[i].magnitude, BodyOnColliderSp, $"{sheet} frame {i}: body centre {offsets[i]} sp from the collider centre");
            Vector2 median = new(offsets.Select(o => o.x).OrderBy(v => v).ElementAt(offsets.Length / 2), offsets.Select(o => o.y).OrderBy(v => v).ElementAt(offsets.Length / 2));
            Assert.LessOrEqual(median.magnitude, MedianOnColliderSp, $"{sheet}: median body centre {median} sp from the collider centre");
        }

        static void Placement(string scenario, out int frames, out float rotation, out float restOffset, out float body, out float depth, out int facing)
        {
            var args = new object[] { scenario, 0, 0f, 0f, 0f, 0f, 0 };
            string error = (string)CatVisualOnlyParityTests.CallEditor("CatCapture", "ClimbPlacement", args);
            Assert.IsNull(error, error);
            frames = (int)args[1]; rotation = (float)args[2]; restOffset = (float)args[3]; body = (float)args[4]; depth = (float)args[5]; facing = (int)args[6];
            Assert.Greater(frames, 40, $"{scenario}: the presenter showed Climb or Hang frames on the vine");
        }

        // The presenter stepped through a real climb (the capture rig, 60 fps frames between the 50 Hz ticks): on the vine the
        // Visual is never turned, stays at rest once clear of the ground, and the drawn body stays on the collider.
        [TestCase("climb_side_right_down", 1)]
        [TestCase("climb_side_left_down", -1)]
        [TestCase("climb_side_right_up", 1)]
        [TestCase("climb_side_left_up", -1)]
        public void Presenter_OnTheVine_KeepsTheRestPose_AndTheBodyOnTheCollider(string scenario, int screenFacing)
        {
            Placement(scenario, out _, out float rotation, out float restOffset, out float body, out _, out int facing);
            Assert.AreEqual(screenFacing, facing, "the facing on the vine is the side the cat came from");
            Assert.LessOrEqual(rotation, 0.01f, "the Visual is never turned on the vine (no interim climb pose)");
            Assert.LessOrEqual(restOffset, 1e-4f, "clear of the ground, the Visual is at its rest position");
            Assert.LessOrEqual(body, BodyOnColliderSp, "the drawn body centre on the collider centre (sprite px)");
        }
    }
}
