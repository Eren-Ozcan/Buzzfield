using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// One row of the shop's Boosts tab: name, what it gives and a buy button with the
    /// Dewdrops cost. A spend above the confirm limit turns the button into "Tap again" first.
    /// </summary>
    public sealed class DewOfferRowView : MonoBehaviour
    {
        public enum State
        {
            Buyable,
            /// <summary>Waiting for the second tap.</summary>
            Confirming,
            TooExpensive,
        }

        [SerializeField] private int offerIndex;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buyImage;
        [SerializeField] private TMP_Text costText;
        [Tooltip("Crystal icon beside the cost; hidden while asking for the second tap.")]
        [SerializeField] private GameObject costIcon;
        [Tooltip("Left inset of the cost text that keeps it clear of the icon.")]
        [SerializeField] private float costInset = 70f;
        [SerializeField] private Color buyColor = new Color(0.18f, 0.71f, 0.78f);
        [SerializeField] private Color confirmColor = new Color(0.49f, 0.77f, 0.35f);
        [SerializeField] private Color disabledColor = new Color(0.84f, 0.8f, 0.73f);

        private Action<int> press;
        private State? shownState;
        private string costLabel;
        private string confirmLabel;

        public int OfferIndex => offerIndex;

        /// <summary>The state the row shows; null before the first refresh.</summary>
        public State? ShownState => shownState;

        public Button BuyButton => buyButton;

        private void Awake()
        {
            buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Init(string title, string body, int cost, string confirmText, Action<int> onPress)
        {
            titleText.text = title;
            bodyText.text = body;
            costLabel = cost.ToString();
            confirmLabel = confirmText;
            press = onPress;
            shownState = null;
        }

        /// <summary>Body text that changes while the shop is open (a time skip's honey).</summary>
        public void SetBody(string body)
        {
            if (bodyText.text != body)
                bodyText.text = body;
        }

        public void Show(State state)
        {
            if (state == shownState)
                return;
            shownState = state;
            buyButton.interactable = state != State.TooExpensive;
            bool confirming = state == State.Confirming;
            costIcon.SetActive(!confirming);
            RectTransform costRect = costText.rectTransform;
            costRect.offsetMin = new Vector2(confirming ? 0f : costInset, costRect.offsetMin.y);
            costText.text = confirming ? confirmLabel : costLabel;
            buyImage.color = state == State.Confirming ? confirmColor : state == State.Buyable ? buyColor : disabledColor;
        }

        private void HandleClick() => press?.Invoke(offerIndex);
    }
}
