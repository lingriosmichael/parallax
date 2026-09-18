using NUnit.Framework;
using Parallax.Core;
using Parallax.DebugTools;

namespace Parallax.Tests
{
    public sealed class LabelFormatterTests
    {
        [TestCase(InputSourceKind.LocalHuman, false, 0f, false, "A · LIVE")]
        [TestCase(InputSourceKind.Inactive, false, 0f, false, "B · IDLE")]
        [TestCase(InputSourceKind.EchoReplay, false, 4.2f, false, "A · ECHO 4.2s")]
        [TestCase(InputSourceKind.EchoReplay, false, 4.2f, true, "A · ECHO HOLD")]
        public void Cat_FormatsDriverStates(InputSourceKind driver, bool seated, float seconds, bool holding, string expected)
        {
            Assert.That(LabelFormatter.Cat(expected[0] == 'A' ? ObserverId.A : ObserverId.B, driver, seated, seconds, holding), Is.EqualTo(expected));
        }

        [Test]
        public void Cat_AppendsSeatedState() => Assert.That(LabelFormatter.Cat(ObserverId.A, InputSourceKind.LocalHuman, true, 0f, false), Is.EqualTo("A · LIVE · seated"));

        [Test]
        public void Anchor_FormatsPendingTarget() => Assert.That(LabelFormatter.Anchor("K1", 1f, true), Is.EqualTo("[K1] →1"));

        [TestCase(false, false, "[K2] free")]
        [TestCase(true, false, "[K2] pressed")]
        [TestCase(true, true, "[K2] pressed (echo)")]
        public void Plate_FormatsState(bool pressed, bool echo, string expected) => Assert.That(LabelFormatter.Plate("K2", pressed, echo), Is.EqualTo(expected));

        [TestCase(false, ObserverId.A, 0f, "station: empty")]
        [TestCase(true, ObserverId.A, 0.42f, "station: A 0.42")]
        public void Station_FormatsState(bool occupied, ObserverId occupant, float value, string expected) => Assert.That(LabelFormatter.Station(occupied, occupant, value), Is.EqualTo(expected));

        [Test]
        public void AnchorTag_UsesAnchorId()
        {
            Assert.That(LabelFormatter.AnchorTag(new AnchorId(2)), Is.EqualTo("K2"));
            Assert.That(LabelFormatter.AnchorTag(new AnchorId(1)), Is.EqualTo("K1"));
        }
    }
}
