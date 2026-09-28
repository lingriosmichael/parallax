using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (L014): T1's reveal is Lip_1 giving way at the cat's feet. A reveal that leaves no way out would only satisfy
    // the validator, so a cat that stops on the lip and drops with it still escapes: some ticks after the lip goes it holds
    // Right and Down and catches V2 just below the lip, and finishes the level.
    public sealed class L014EscapeTests
    {
        static readonly Type Routes = Type.GetType("Parallax.Editor.Levels.L014Routes, Parallax.Editor");
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        [TestCase(1)] [TestCase(10)] [TestCase(20)] [TestCase(30)]
        public void AfterTheLipGivesWay_HoldingDownCatchesV2(int reaction)
        {
            object replay = ReplayRoute(session, RouteValidatorTests.Room("L014"), Call(Routes, "EscapeLip", reaction));
            List<Rec> records = Records(replay);
            Assert.IsTrue((bool)F(replay, "Completed"), $"EscapeLip({reaction}) doesn't finish the level\n" + Dump(replay));
            int lip = ElementIndex(replay, "Lip_1");
            var raw = (IList)F(replay, "Records");
            int gone = -1;
            for (int i = 1; i < raw.Count && gone < 0; i++)
                if (((int[])F(raw[i], "Signature"))[lip] != ((int[])F(raw[i - 1], "Signature"))[lip]) gone = i;
            Assert.Greater(gone, 0, "Lip_1 never gave way");
            int grab = -1;
            for (int i = gone; i < raw.Count && grab < 0; i++) if ((bool)F(raw[i], "IsClimbing")) grab = i;
            Assert.Greater(grab, gone, "no grab onto V2 after the lip gave way");
            TestContext.Out.WriteLine($"EscapeLip({reaction}): Lip_1 gives way t{records[gone].Tick}, the cat catches V2 at t{records[grab].Tick} (y {records[grab].Y:F2}), completes t{records[records.Count - 1].Tick}");
        }
    }
}
