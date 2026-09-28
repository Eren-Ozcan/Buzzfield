using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// One Queen ability row: name, level, what a level does and the buy button with the
    /// Royal Jelly cost. Pops on a purchase and wiggles on a refused tap.
    /// </summary>
    public sealed class QueenAbilityRowView : MonoBehaviour
    {
        [Tooltip("Index into QueenSettings.Abilities.")]
        [SerializeField] private int abilityIndex;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button buyButton;
        [SerializeField] private ButtonFeedback feedback;
        [SerializeField] private Image buyImage;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Color buyColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color disabledColor = new Color(0.45f, 0.42f, 0.38f);
        [SerializeField] private Color maxedColor = new Color(0.45f, 0.9f, 0.35f);

        private Func<int, bool> buy;

        public int AbilityIndex => abilityIndex;

        /// <summary>True while the buy button can be pressed.</summary>
        public bool CanBuy => buyButton.interactable;

        public void Init(string title, string body, Func<int, bool> onBuy)
        {
            nameText.text = title;
            bodyText.text = body;
            buy = onBuy;
            buyButton.onClick.RemoveListener(HandleClick);
            buyButton.onClick.AddListener(HandleClick);
        }

        /// <summary>Unchanged texts are skipped so the mesh is not rebuilt.</summary>
        public void Show(string level, string cost, bool affordable, bool maxed)
        {
            SetText(levelText, level);
            SetText(costText, cost);
            // A maxed row is green, not grey, so it reads as done rather than unaffordable.
            buyButton.interactable = affordable && !maxed;
            buyImage.color = maxed ? maxedColor : affordable ? buyColor : disabledColor;
        }

        /// <summary>Same as a tap on the buy button.</summary>
        public void Press() => HandleClick();

        private void HandleClick()
        {
            if (buy == null)
                return;
            if (buy(abilityIndex))
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
