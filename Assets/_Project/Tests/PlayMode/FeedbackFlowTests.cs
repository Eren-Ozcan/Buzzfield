using System.Collections;
using System.Reflection;
using Buzzfield.Core;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>UI feedback tweens against the real Main scene: purchase pop, refused wiggle, honey pop.</summary>
    public class FeedbackFlowTests
    {
        GameManager game;
        UiFeedbackSettings feedback;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            feedback = Hidden<UiFeedbackSettings>(game, "feedbackSettings");
        }

        [Test]
        public void EveryButton_HasFeedback()
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.That(button.GetComponent<ButtonFeedback>(), Is.Not.Null, button.name);
        }

        [UnityTest]
        public IEnumerator Purchase_PopsButton_ThenSettles()
        {
            game.Economy.Grant(game.Upgrades.AddBeeCost);
            yield return null;
            Button button = AddBeeButton();
            Vector3 rest = button.transform.localScale;

            button.onClick.Invoke();
            Assert.That(game.Bees.Count, Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(feedback.PurchasePopDuration * 0.4f);
            Assert.That(button.transform.localScale.x, Is.GreaterThan(rest.x));

            yield return new WaitForSecondsRealtime(feedback.PurchasePopDuration);
            Assert.That(button.transform.localScale, Is.EqualTo(rest));
        }

        [UnityTest]
        public IEnumerator TapOnUnaffordable_Wiggles_ThenSettles()
        {
            Button button = AddBeeButton();
            yield return null;
            Assert.That(button.interactable, Is.False);

            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(game.Bees.Count, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(feedback.ShakeDuration * 0.1f);
            Assert.That(Quaternion.Angle(button.transform.localRotation, Quaternion.identity), Is.GreaterThan(0.1f));

            yield return new WaitForSecondsRealtime(feedback.ShakeDuration);
            Assert.That(Quaternion.Angle(button.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator HoneyGain_PopsCounter()
        {
            // Past the cooldown from the first deposits of the run.
            yield return new WaitForSecondsRealtime(feedback.HoneyPopInterval + 0.05f);
            Transform honey = Hidden<TMP_Text>(Hidden<HudView>(game, "hud"), "honeyText").transform;
            Vector3 rest = honey.localScale;

            game.Economy.Grant(BigNumber.Create(1, 3));
            yield return null;
            yield return new WaitForSecondsRealtime(feedback.HoneyPopDuration * 0.4f);
            Assert.That(honey.localScale.x, Is.GreaterThan(rest.x));

            yield return new WaitForSecondsRealtime(feedback.HoneyPopDuration + feedback.HoneyPopInterval);
            Assert.That(honey.localScale.x, Is.LessThanOrEqualTo(rest.x * (1f + feedback.HoneyPop)));
        }

        Button AddBeeButton() =>
            Hidden<Button>(Hidden<UpgradeButtonView>(Hidden<BottomBarView>(game, "bottomBar"), "addBeeButton"), "button");

        static T Hidden<T>(object owner, string name)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(owner);
        }
    }
}
