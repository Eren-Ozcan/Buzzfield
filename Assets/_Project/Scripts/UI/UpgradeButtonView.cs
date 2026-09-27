using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>One bottom-bar button: name, level or tier counts, cost. Greyed out when it cannot be bought.</summary>
    public sealed class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Color enabledColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color disabledColor = new Color(0.45f, 0.42f, 0.38f);

        private Action onPress;
        private bool interactable = true;

        public void Init(string title, Action pressed)
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
        }

        private void HandleClick() => onPress?.Invoke();

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}
