using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-089 B, R4: an Inverter and a snap ClimbVine as chain sources (TrapLayoutValidator), fired through the route harness
    // (the real builder, room step and trap timing), and as trigger-coverage roots (TriggerCoverage). Rooms are
    // ChainKitFixtures'.
    public sealed class ChainKitTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.ChainKitFixtures, Parallax.Editor");
        static readonly Type Chains = Type.GetType("Parallax.Editor.Setup.TrapLayoutValidator, Parallax.Editor");
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string name)
        {
            Assert.NotNull(Fixtures, "ChainKitFixtures not found");
            return Call(Fixtures, name);
        }

        static int Const(string name) => (int)Fixtures.GetField(name).GetValue(null);

        static bool TryValidateChains(object room, out string error)
        {
            object[] args = { room, null };
            bool ok = (bool)Chains.GetMethod("TryValidate").Invoke(null, args);
            error = (string)args[1];
            return ok;
        }

        static List<string> Coverage(string fixture)
        {
            List<string> errors = Rule("ValidateTriggerCoverage", "FIX", Fixture(fixture), Motor(), Gravity(), new List<string>());
            TestContext.Out.WriteLine(string.Join("\n", errors));
            return errors;
        }

        // ---------- B: the validator accepts them as sources; the chain fires on the right tick ----------

        [TestCase("InverterChainsBlock")]
        [TestCase("SnapVineChainsSpikes")]
        public void AnInverterOrASnapVine_IsAcceptedAsAChainSource(string fixture)
        {
            Assert.IsTrue(TryValidateChains(Fixture(fixture), out string error), error);
        }

        [Test]
        public void APlainVine_IsRejectedAsAChainSource_SayingItNeverFires()
        {
            Assert.IsFalse(TryValidateChains(Fixture("PlainVineChainsSpikes"), out string error));
            StringAssert.Contains("Spikes", error);
            StringAssert.Contains("plain vine", error);
            StringAssert.Contains("never fires", error);
        }

        [TestCase("InverterFiresBlock", "Inverter", "Block", "BlockDelay")]
        [TestCase("SnapVineFiresSpikes", "Vine", "Spikes", "SpikesDelay")]
        public void TheChainedTarget_FiresItsDelayAfterTheSource(string routeCase, string source, string target, string delay)
        {
            object replay = Replay(session, Fixture(routeCase));
            List<Rec> records = Records(replay);
            int s = ElementIndex(replay, source), t = ElementIndex(replay, target);
            Assert.GreaterOrEqual(s, 0, source + " was not built\n" + Dump(replay));
            Assert.GreaterOrEqual(t, 0, target + " was not built\n" + Dump(replay));
            Rec last = records[records.Count - 1];
            TestContext.Out.WriteLine($"{source} fired t{last.FireTick[s]}, {target} fired t{last.FireTick[t]}");
            Assert.GreaterOrEqual(last.FireTick[s], 0, source + " never fired\n" + Dump(replay));
            Assert.AreEqual(last.FireTick[s] + Const(delay), last.FireTick[t], "the target fires its delay after the source\n" + Dump(replay));
        }

        // ---------- R4: both are coverage roots ----------

        [Test] public void AnInverterWhoseChainedDangerLiesBeyondItsCut_IsCovered() => CollectionAssert.IsEmpty(Coverage("InverterChainsBlock"));

        [Test]
        public void AnInverterWhoseChainedDangerLiesBeforeIt_IsRejected()
        {
            List<string> errors = Coverage("InverterChainsSpikesBefore");
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("Spikes (chained from Inverter)", errors[0]);
            StringAssert.Contains("lies before Inverter's trigger", errors[0]);
        }

        [Test] public void AFullHeightSnapVineWhoseChainedDangerLiesBeyondIt_IsCovered() => CollectionAssert.IsEmpty(Coverage("SnapVineChainsSpikes"));

        // D-074's cut model as it stands: a vine that doesn't span the cat's band isn't a cut, so its chained danger is
        // reachable without touching it (a jump passes over the vine).
        [Test]
        public void AShortSnapVine_IsNotACut_AndIsRejected()
        {
            List<string> errors = Coverage("ShortSnapVineChainsSpikes");
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("Vine's trigger", errors[0]);
            StringAssert.Contains("does not cut", errors[0]);
            StringAssert.Contains("Spikes", errors[0]);
        }
    }
}
