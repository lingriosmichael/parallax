using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Parallax.Tests
{
    public sealed class ArtSeatingTests
    {
        [Test]
        public void FindLowestOpaqueRow_UsesTextureRowsAndStrictAlphaThreshold()
        {
            var pixels = new Color32[16];
            pixels[1 * 4 + 2] = new Color32(255, 255, 255, 25);
            pixels[2 * 4 + 1] = new Color32(255, 255, 255, 26);
            pixels[3 * 4 + 0] = new Color32(255, 255, 255, 255);

            Assert.That(FindLowestOpaqueRow(pixels, 4, new RectInt(0, 0, 4, 4), 25), Is.EqualTo(2));
        }

        static int FindLowestOpaqueRow(Color32[] pixels, int textureWidth, RectInt textureRect, byte alphaThreshold)
        {
            var type = System.Type.GetType("Parallax.Editor.Setup.PaxV02Setup, Parallax.Editor");
            var method = type.GetMethod("FindLowestOpaqueRow", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(Color32[]), typeof(int), typeof(RectInt), typeof(byte) }, null);
            return (int)method.Invoke(null, new object[] { pixels, textureWidth, textureRect, alphaThreshold });
        }
    }
}
