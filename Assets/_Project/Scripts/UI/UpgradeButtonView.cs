using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// One bottom-bar card: icon, name, level or tier counts, cost. Dimmed when it cannot be
    /// bought (card, price pill and icon); pops on a purchase and wiggles on a refused tap.
    /// </summary>
    public sealed class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ButtonFeedback feedback;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Color enabledColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color disabledColor = new Color(0.45f, 0.42f, 0.38f);
        [Tooltip("Optional pill behind the cost.")]
        [SerializeField] private Image costBackground;
        [SerializeField] private Color costEnabledColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color costDisabledColor = new Color(0.45f, 0.42f, 0.38f);
        [Tooltip("Optional icon, faded while the card cannot be bought.")]
        [SerializeField] private Graphic icon;
        [SerializeField, Range(0f, 1f)] private float disabledIconAlpha = 0.45f;

        private Func<bool> onPress;
        private bool interactable = true;

        /// <param name="pressed">Tries the upgrade; true pops the button, false wiggles it.</param>
        public void Init(string title, Func<bool> pressed)
        {
            titleText.text = title;
            onPress = pressed;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        /// <summary>Changes the texts; unchanged values are skipped so the mesh is not rebuilt.</summary>
        public void SetTexts(string detail, string cost)
        {
            if (detailText.text != detail)
                detailText.text = detail;
            if (costText.text != cost)
                costText.text = cost;
        }

        public void SetInteractable(bool value)
        {
            if (interactable == value)
                return;
            interactable = value;
            button.interactable = value;
            background.color = value ? enabledColor : disabledColor;
            if (costBackground != null)
                costBackground.color = value ? costEnabledColor : costDisabledColor;
            if (icon != null)
            {
                Color tint = icon.color;
                tint.a = value ? 1f : disabledIconAlpha;
                icon.color = tint;
            }
        }

        private void HandleClick()
        {
            if (onPress == null)
                return;
            if (onPress())
                feedback.Confirm();
            else
                feedback.Reject();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}
