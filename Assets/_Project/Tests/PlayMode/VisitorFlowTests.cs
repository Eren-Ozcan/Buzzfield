using System.Collections;
using System.Reflection;
using Buzzfield.Ads;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>The butterfly against the real Main scene: catching it, its gift, the "double it" ad and missing it.</summary>
    public class VisitorFlowTests
    {
        const float MockDelay = 0.05f;
        const float Timeout = 5f;

        static readonly FieldInfo MockDelayField =
            typeof(AdSettings).GetField("mockDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);

        GameManager game;
        AdSettings adSettings;
        float savedDelay;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            adSettings = Hidden<AdSettings>(game, "adSettings");
            savedDelay = (float)MockDelayField.GetValue(adSettings);
            MockDelayField.SetValue(adSettings, MockDelay);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            if (adSettings != null)
                MockDelayField.SetValue(adSettings, savedDelay);
            yield return null;
        }

        static T Hidden<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        IEnumerator WaitForAd()
        {
            float end = Time.realtimeSinceStartup + Timeout;
            while (!game.Ads.CanShowRewarded() && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(game.Ads.CanShowRewarded(), "No rewarded ad became available.");
        }

        /// <summary>A tap on the butterfly where the camera sees it, as the EventSystem would send it.</summary>
        void TapButterfly()
        {
            Vector2 screen = Camera.main.WorldToScreenPoint(game.Visitors.Position);
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = screen };
            catcher.OnPointerDown(pointer);
            catcher.OnPointerUp(pointer);
        }

        [UnityTest]
        public IEnumerator NewPlayer_GetsNoButterfly()
        {
            VisitorSettings settings = Hidden<VisitorSettings>(game, "visitorSettings");
            Assert.That(game.Stats.playSeconds, Is.LessThan(settings.FirstAfterPlaySeconds));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(game.Visitors.IsFlying, Is.False);
        }

        [UnityTest]
        public IEnumerator TapOnButterfly_PaysTheGift_AndOffersToDoubleIt()
        {
            yield return WaitForAd();
            Assert.That(game.DebugLaunchButterfly());
            Assert.That(game.Visitors.IsFlying);
            Assert.That(game.Sound.PlayedCount(Sfx.ButterflyAppear), Is.EqualTo(1));
            // Let it fly into the garden first.
            yield return new WaitForSeconds(3f);
            Assert.That(game.Visitors.IsFlying);

            double gift = game.VisitorGift().ToDouble();
            double before = game.Economy.Honey.ToDouble();
            TapButterfly();
            Assert.That(game.Visitors.IsFlying, Is.False);
            Assert.That(game.Economy.Honey.ToDouble() - before, Is.EqualTo(gift).Within(gift * 1e-6 + 0.5));
            Assert.That(game.Sound.PlayedCount(Sfx.ButterflyCatch), Is.EqualTo(1));
            var offer = Object.FindAnyObjectByType<GiftOfferView>();
            Assert.That(offer.IsShowing);

            double afterCatch = game.Economy.Honey.ToDouble();
            Assert.That(game.TryWatchGiftAd());
            float end = Time.realtimeSinceStartup + Timeout;
            while (offer.IsShowing && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(offer.IsShowing, Is.False);
            Assert.That(game.Economy.Honey.ToDouble() - afterCatch, Is.GreaterThanOrEqualTo(gift * 0.99), "The ad pays the gift once more.");
            Assert.That(game.TryWatchGiftAd(), Is.False, "One ad per butterfly.");
        }

        [UnityTest]
        public IEnumerator TapBesideTheButterfly_DoesNotCatchIt()
        {
            Assert.That(game.DebugLaunchButterfly());
            yield return new WaitForSeconds(3f);
            Vector2 screen = Camera.main.WorldToScreenPoint(game.Visitors.Position);
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = screen + new Vector2(0f, Screen.height * 0.3f) };
            catcher.OnPointerDown(pointer);
            catcher.OnPointerUp(pointer);
            Assert.That(game.Visitors.IsFlying);
        }

        [UnityTest]
        public IEnumerator MissedButterfly_FliesOff_AndIsGone()
        {
            Assert.That(game.DebugLaunchButterfly());
            Time.timeScale = 8f;
            VisitorSettings settings = Hidden<VisitorSettings>(game, "visitorSettings");
            float end = Time.realtimeSinceStartup + settings.CrossSeconds / 8f + 3f;
            while (game.Visitors.IsFlying && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(game.Visitors.IsFlying, Is.False);
            Assert.That(Object.FindAnyObjectByType<GiftOfferView>().IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator QueenMove_TakesTheButterflyAway()
        {
            Assert.That(game.DebugLaunchButterfly());
            game.DebugCompleteGarden();
            game.Economy.Grant(BigNumber.FromDouble(1e9));
            yield return null;
            Assert.That(game.TryMoveQueen());
            Assert.That(game.Visitors.IsFlying, Is.False);
        }
    }
}
