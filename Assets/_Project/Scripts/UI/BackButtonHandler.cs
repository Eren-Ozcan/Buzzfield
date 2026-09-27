using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Android back button (Escape on desktop): closes the top-most panel; on the main
    /// screen it asks whether to quit.
    /// </summary>
    public sealed class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] private QueenPanelView queenPanel;
        [SerializeField] private WelcomeBackView welcomeBack;

        [Header("Quit dialog")]
        [SerializeField] private GameObject quitDialog;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button cancelButton;

        public bool IsQuitDialogOpen => quitDialog.activeSelf;

        /// <summary>True while any modal panel is on screen.</summary>
        public bool IsModalOpen => quitDialog.activeSelf || queenPanel.IsOpen || welcomeBack.IsOpen;

        private void Awake()
        {
            quitButton.onClick.AddListener(Quit);
            cancelButton.onClick.AddListener(CloseQuitDialog);
            quitDialog.SetActive(false);
        }

        private void OnDestroy()
        {
            if (quitButton != null)
                quitButton.onClick.RemoveListener(Quit);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(CloseQuitDialog);
        }

        private void Update()
        {
            // The Input System reports the Android back button as the Escape key.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                HandleBack();
        }

        /// <summary>One back press: the newest layer closes first.</summary>
        public void HandleBack()
        {
            if (quitDialog.activeSelf)
                CloseQuitDialog();
            else if (queenPanel.IsConfirming)
                queenPanel.CancelConfirm();
            else if (queenPanel.IsOpen)
                queenPanel.Close();
            else if (welcomeBack.IsOpen)
                welcomeBack.Close();
            else
                quitDialog.SetActive(true);
        }

        public void CloseQuitDialog() => quitDialog.SetActive(false);

        private void Quit()
        {
            quitDialog.SetActive(false);
            // OnApplicationQuit / pause save the game on the way out.
            Application.Quit();
        }
    }
}
