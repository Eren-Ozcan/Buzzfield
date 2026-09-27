using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Welcome back" panel: time away and the honey earned meanwhile. The honey is already
    /// credited when the panel opens, so closing the app with it open loses nothing.
    /// </summary>
    public sealed class WelcomeBackView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text awayText;
        [SerializeField] private TMP_Text honeyText;
        [SerializeField] private TMP_Text capText;
        [SerializeField] private Button collectButton;

        public bool IsOpen => panel.activeSelf;

        private void Awake()
        {
            collectButton.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (collectButton != null)
                collectButton.onClick.RemoveListener(Close);
        }

        public void Show(double awaySeconds, BigNumber honey, bool capReached, double capSeconds)
        {
            awayText.text = string.Format(Strings.AwayFormat, TimeFormat.Duration(awaySeconds));
            honeyText.text = string.Format(Strings.OfflineHoneyFormat, NumberFormat.Abbreviate(honey));
            capText.gameObject.SetActive(capReached);
            if (capReached)
                capText.text = string.Format(Strings.OfflineCapFormat, TimeFormat.Duration(capSeconds));
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);
    }
}
