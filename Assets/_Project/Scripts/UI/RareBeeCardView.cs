using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// One species in the Hive panel: its portrait, name, level, what it gives and the button for
    /// the next step (free, Dewdrops, "Tap again", MAX, or the garden it waits for). A species the
    /// player has not reached shows as a dark silhouette. Pops on a purchase, wiggles on a refusal.
    /// </summary>
    public sealed class RareBeeCardView : MonoBehaviour
    {
        public enum State
        {
            /// <summary>The player has not reached the species' garden yet.</summary>
            Locked,
            /// <summary>Joins for free with a tap.</summary>
            Free,
            Buyable,
            /// <summary>Waiting for the second tap of a big spend.</summary>
            Confirming,
            TooExpensive,
            Maxed,
        }

        [Tooltip("Index into RareBeeSettings.Bees.")]
        [SerializeField] private int beeIndex;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button buyButton;
        [SerializeField] private ButtonFeedback feedback;
        [SerializeField] private Image buyImage;
        [SerializeField] private TMP_Text costText;
        [Tooltip("Crystal icon beside a Dewdrops cost; hidden for every other label.")]
        [SerializeField] private GameObject costIcon;
        [Tooltip("Left inset of the cost text that keeps it clear of the icon.")]
        [SerializeField] private float costInset = 64f;
        [SerializeField] private Color buyColor = new Color(0.18f, 0.71f, 0.78f);
        [SerializeField] private Color confirmColor = new Color(0.49f, 0.77f, 0.35f);
        [SerializeField] private Color freeColor = new Color(0.49f, 0.77f, 0.35f);
        [SerializeField] private Color disabledColor = new Color(0.84f, 0.8f, 0.73f);
        [SerializeField] private Color maxedColor = new Color(0.49f, 0.77f, 0.35f);
        [Tooltip("Portrait tint of a species the player has not reached: a dark silhouette.")]
        [SerializeField] private Color lockedTint = new Color(0.25f, 0.17f, 0.08f, 0.55f);

        private Func<int, bool> press;
        private State? shownState;

        public int BeeIndex => beeIndex;

        /// <summary>The state the card shows; null before the first refresh.</summary>
        public State? ShownState => shownState;

        public Button BuyButton => buyButton;

        public string Cost => costText.text;

        public string Body => bodyText.text;

        /// <param name="onPress">The button was tapped: true when the tap was taken, false when refused.</param>
        public void Init(string title, Func<int, bool> onPress)
        {
            nameText.text = title;
            press = onPress;
            shownState = null;
            buyButton.onClick.RemoveListener(HandleClick);
            buyButton.onClick.AddListener(HandleClick);
        }

        /// <summary>Unchanged texts are skipped so the mesh is not rebuilt.</summary>
        public void Show(State state, string level, string body, string cost)
        {
            SetText(levelText, level);
            SetText(bodyText, body);
            SetText(costText, cost);
            if (state == shownState)
                return;
            shownState = state;
            bool dewCost = state == State.Buyable || state == State.TooExpensive;
            costIcon.SetActive(dewCost);
            RectTransform costRect = costText.rectTransform;
            costRect.offsetMin = new Vector2(dewCost ? costInset : 0f, costRect.offsetMin.y);
            buyButton.interactable = state == State.Free || state == State.Buyable || state == State.Confirming;
            buyImage.color = state switch
            {
                State.Free => freeColor,
                State.Buyable => buyColor,
                State.Confirming => confirmColor,
                State.Maxed => maxedColor,
                _ => disabledColor,
            };
            icon.color = state == State.Locked ? lockedTint : Color.white;
        }

        /// <summary>Same as a tap on the button.</summary>
        public void Press() => HandleClick();

        private void HandleClick()
        {
            if (press == null)
                return;
            if (press(beeIndex))
                feedback.Confirm();
            else
                feedback.Reject();
        }

        private void OnDestroy()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(HandleClick);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text.text != value)
                text.text = value;
        }
    }
}
