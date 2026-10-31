using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-108 (D-157): the title intro's pure timing.
    public sealed class TitleIntroTimelineTests
    {
        static TitleIntroTimes Times() => new TitleIntroTimes
        {
            CoverFade = 1.4f, WordmarkStart = 0.9f, TrackingSeconds = 2.2f, TrackingFrom = 1.1f, TrackingTo = 0.42f,
            RuleStart = 2.3f, TaglineStart = 2.7f, MenuStart = 3.3f, Fade = 0.8f, InputLock = 0.2f,
        };

        [Test]
        public void AtTheStart_TheScreenIsBlack_AndNothingIsShown()
        {
            TitleIntroTimes t = Times();
            Assert.AreEqual(1f, TitleIntroTimeline.Cover(t, 0f));
            Assert.AreEqual(0f, TitleIntroTimeline.Wordmark(t, 0f));
            Assert.AreEqual(0f, TitleIntroTimeline.Rule(t, 0f));
            Assert.AreEqual(0f, TitleIntroTimeline.Tagline(t, 0f));
            Assert.AreEqual(0f, TitleIntroTimeline.Menu(t, 0f));
            Assert.AreEqual(1.1f, TitleIntroTimeline.Tracking(t, 0f), 1e-5f);
            Assert.IsTrue(TitleIntroTimeline.IsPlaying(t, 0f));
            Assert.IsFalse(TitleIntroTimeline.MenuInteractable(t, 0f));
        }

        [Test]
        public void SkipTo_IsTheFirstMomentEverythingIsFullyShown()
        {
            TitleIntroTimes t = Times();
            float end = TitleIntroTimeline.SkipTo(t);
            Assert.AreEqual(4.1f, end, 1e-5f, "the menu's fade ends last (3.3 + 0.8)");
            Assert.AreEqual(0f, TitleIntroTimeline.Cover(t, end), 1e-5f);
            Assert.AreEqual(1f, TitleIntroTimeline.Wordmark(t, end), 1e-5f);
            Assert.AreEqual(1f, TitleIntroTimeline.Rule(t, end), 1e-5f);
            Assert.AreEqual(1f, TitleIntroTimeline.Tagline(t, end), 1e-5f);
            Assert.AreEqual(1f, TitleIntroTimeline.Menu(t, end), 1e-5f);
            Assert.AreEqual(0.42f, TitleIntroTimeline.Tracking(t, end), 1e-5f);
            Assert.IsFalse(TitleIntroTimeline.IsPlaying(t, end));
        }

        [Test]
        public void TheTapThatSkips_CannotPressAButton_UntilTheInputLockPasses()
        {
            TitleIntroTimes t = Times();
            float end = TitleIntroTimeline.SkipTo(t);
            Assert.IsFalse(TitleIntroTimeline.MenuInteractable(t, end));
            Assert.IsFalse(TitleIntroTimeline.MenuInteractable(t, end + 0.19f));
            Assert.IsTrue(TitleIntroTimeline.MenuInteractable(t, end + 0.201f));
        }

        [Test]
        public void Tracking_ClosesSteadily_FromWideToTheKeyArtSpacing()
        {
            TitleIntroTimes t = Times();
            float previous = float.MaxValue;
            for (float s = 0f; s <= 4f; s += 0.05f)
            {
                float now = TitleIntroTimeline.Tracking(t, s);
                Assert.LessOrEqual(now, previous + 1e-6f, $"spacing grew at {s:0.00} s");
                Assert.GreaterOrEqual(now, t.TrackingTo - 1e-6f);
                previous = now;
            }
        }

        [Test]
        public void Drift_StartsAtTheCentre_AndStaysInsideItsRange()
        {
            Assert.AreEqual(0f, TitleIntroTimeline.Drift(0f, 2.5f, 48f), 1e-5f);
            Assert.AreEqual(2.5f, TitleIntroTimeline.Drift(12f, 2.5f, 48f), 1e-4f, "a quarter period is the far right");
            for (float s = 0f; s < 100f; s += 0.37f)
                Assert.LessOrEqual(System.Math.Abs(TitleIntroTimeline.Drift(s, 2.5f, 48f)), 2.5f + 1e-5f);
            Assert.AreEqual(0f, TitleIntroTimeline.Drift(5f, 2.5f, 0f), "no period, no pan");
        }

        // D-280.
        static TitleIntroTimes Flair()
        {
            TitleIntroTimes t = Times();
            t.LetterStep = 0.1f; t.MenuStep = 0.14f; t.SheenStart = 2.5f; t.SheenSeconds = 1.5f; t.SheenEvery = 9f;
            t.SlipDelay = 5f; t.SlipEvery = 13f; t.SlipSeconds = 1.7f;
            return t;
        }

        [Test]
        public void Letters_ArriveFromTheMiddleOut()
        {
            TitleIntroTimes t = Flair();
            float early = t.WordmarkStart + 0.2f;
            Assert.Greater(TitleIntroTimeline.Letter(t, early, 3, 8), TitleIntroTimeline.Letter(t, early, 0, 8));
            Assert.AreEqual(TitleIntroTimeline.Letter(t, early, 3, 8), TitleIntroTimeline.Letter(t, early, 4, 8), "mirrored about the middle");
            Assert.AreEqual(0f, TitleIntroTimeline.LetterRise(t, TitleIntroTimeline.SkipTo(t), 0, 8), "every letter is home when the intro ends");
            Assert.AreEqual(1f, TitleIntroTimeline.Letter(t, TitleIntroTimeline.SkipTo(t), 0, 8));
        }

        [Test]
        public void Buttons_FollowEachOther_AndASkipShowsThemAll()
        {
            TitleIntroTimes t = Flair();
            Assert.Greater(TitleIntroTimeline.MenuItem(t, t.MenuStart + 0.2f, 0), TitleIntroTimeline.MenuItem(t, t.MenuStart + 0.2f, 2));
            for (int i = 0; i < 3; i++) Assert.AreEqual(1f, TitleIntroTimeline.MenuItem(t, TitleIntroTimeline.SkipTo(t), i));
        }

        [Test]
        public void TheLight_Crosses_ThenComesBackLater()
        {
            TitleIntroTimes t = Flair();
            Assert.AreEqual(-1f, TitleIntroTimeline.Sheen(t, 1f));
            Assert.AreEqual(0.5f, TitleIntroTimeline.Sheen(t, 3.25f), 1e-5f);
            Assert.AreEqual(-1f, TitleIntroTimeline.Sheen(t, 5f));
            Assert.AreEqual(0.5f, TitleIntroTimeline.Sheen(t, 12.25f), 1e-5f);
        }

        [Test]
        public void NoLetterSlips_DuringTheIntro_OrBetweenSlips()
        {
            TitleIntroTimes t = Flair();
            int[] order = { 7, 2 };
            float first = TitleIntroTimeline.SkipTo(t) + t.SlipDelay;
            Assert.AreEqual(-1, TitleIntroTimeline.Slip(t, TitleIntroTimeline.SkipTo(t), order).Letter);
            Assert.AreEqual(-1, TitleIntroTimeline.Slip(t, first - 0.01f, order).Letter);
            Assert.AreEqual(-1, TitleIntroTimeline.Slip(t, first + t.SlipSeconds + 0.1f, order).Letter);
            Assert.AreEqual(-1, TitleIntroTimeline.Slip(t, 5f, null).Letter);
        }

        [Test]
        public void ALetter_Hangs_ThenComesHome_AndTheNextTurnIsAnother()
        {
            TitleIntroTimes t = Flair();
            int[] order = { 7, 2 };
            float first = TitleIntroTimeline.SkipTo(t) + t.SlipDelay;
            TitleSlip hanging = TitleIntroTimeline.Slip(t, first + t.SlipSeconds * 0.5f, order);
            Assert.AreEqual(7, hanging.Letter);
            Assert.AreEqual(1f, hanging.Drop);
            TitleSlip home = TitleIntroTimeline.Slip(t, first + t.SlipSeconds * 0.9999f, order);
            Assert.AreEqual(0f, home.Drop, 0.01f);
            Assert.AreEqual(0f, home.Tilt, 0.01f);
            Assert.AreEqual(2, TitleIntroTimeline.Slip(t, first + t.SlipEvery + 0.8f, order).Letter);
        }

        [Test]
        public void TheCamera_PushesIn_AndStops()
        {
            Assert.AreEqual(0f, TitleIntroTimeline.Push(0f, 6f));
            Assert.AreEqual(1f, TitleIntroTimeline.Push(6f, 6f));
            Assert.AreEqual(1f, TitleIntroTimeline.Push(60f, 6f));
        }
    }
}
