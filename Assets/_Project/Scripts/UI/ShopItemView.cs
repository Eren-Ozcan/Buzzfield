using System;
using Buzzfield.Ads;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>One product row in the shop: name, what it gives and the buy button with the price.</summary>
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private string productId;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buyImage;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Color buyColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color ownedColor = new Color(0.45f, 0.9f, 0.35f);
        [SerializeField] private Color disabledColor = new Color(0.45f, 0.42f, 0.38f);

        private Action<string> buy;
        private ShopItemState? shownState;
        private string shownPrice;

        public string ProductId => productId;

        /// <summary>The state the row shows; null before the first refresh.</summary>
        public ShopItemState? ShownState => shownState;

        private void Awake()
        {
            buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Init(string title, string body, Action<string> onBuy)
        {
            titleText.text = title;
            bodyText.text = body;
            buy = onBuy;
            shownState = null;
        }

        public void Show(ShopItemState state, string price)
        {
            if (state == shownState && price == shownPrice)
                return;
            shownState = state;
            shownPrice = price;
            gameObject.SetActive(state != ShopItemState.Hidden);
            buyButton.interactable = state == ShopItemState.Buyable;
            switch (state)
            {
                case ShopItemState.Owned:
                    priceText.text = Strings.Owned;
                    buyImage.color = ownedColor;
                    break;
                case ShopItemState.Busy:
                    priceText.text = Strings.StoreBusy;
                    buyImage.color = disabledColor;
                    break;
                case ShopItemState.Unavailable:
                    priceText.text = Strings.StoreUnavailable;
                    buyImage.color = disabledColor;
                    break;
                default:
                    priceText.text = price;
                    buyImage.color = buyColor;
                    break;
            }
        }

        private void HandleClick() => buy?.Invoke(productId);
    }
}
