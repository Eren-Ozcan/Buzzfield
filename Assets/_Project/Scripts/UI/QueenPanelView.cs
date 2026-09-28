using System;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Queen button in the top bar and the Queen panel behind it: Royal Jelly, the "Move the
    /// Queen" gate, cost and jelly preview, and the confirm dialog. Events only mark it dirty;
    /// the button badge is re-checked once per frame at most, the open panel's texts on a
    /// slow timer because honey changes with every deposit. The badge pops in when it appears.
    /// </summary>
    public sealed class QueenPanelView : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button openButton;
        [Tooltip("Shown on the Queen button while a move is possible.")]
        [SerializeField] private GameObject readyBadge;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text jellyText;
        [SerializeField] private TMP_Text gardenText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text previewText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private Button moveButton;
        [SerializeField] private Image moveButtonImage;
        [SerializeField] private Color moveEnabledColor = new Color(0.98f, 0.76f, 0.2f);
        [SerializeField] private Color moveDisabledColor = new Color(0.45f, 0.42f, 0.38f);

        [Header("Confirm")]
        [SerializeField] private GameObject confirm;
        [SerializeField] private TMP_Text confirmBodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.25f;

        private PrestigeManager prestige;
        private EconomyManager economy;
        private GardenBloomManager bloom;
        private Func<bool> moveQueen;
        private Tweener tweener;
        private UiFeedbackSettings feedback;
        private bool dirty;
        private float refreshTimer;
        private bool shownReady;

        public bool IsOpen => panel.activeSelf;
        public bool IsConfirming => confirm.activeSelf;

        public void Init(PrestigeManager prestigeManager, EconomyManager economyManager, GardenBloomManager bloomManager,
            Func<bool> onMove, Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            Unsubscribe();
            prestige = prestigeManager;
            economy = economyManager;
            bloom = bloomManager;
            moveQueen = onMove;
            tweener = tweenRunner;
            feedback = feedbackSettings;

            economy.OnHoneyChanged += HandleHoneyChanged;
            bloom.OnBloomChanged += MarkDirty;
            prestige.OnQueenMoved += HandleQueenMoved;

            AddListener(openButton, Open);
            AddListener(closeButton, Close);
            AddListener(moveButton, AskConfirm);
            AddListener(confirmButton, ConfirmMove);
            AddListener(cancelButton, CancelConfirm);

            panel.SetActive(false);
            confirm.SetActive(false);
            shownReady = false;
            readyBadge.SetActive(false);
            MarkDirty();
        }

        public void Open()
        {
            panel.SetActive(true);
            confirm.SetActive(false);
            RefreshPanel();
        }

        public void Close()
        {
            confirm.SetActive(false);
            panel.SetActive(false);
        }

        /// <summary>Move button: shows the confirm dialog if the move is still possible.</summary>
        public void AskConfirm()
        {
            if (!CanMove())
                return;
            confirmBodyText.text = string.Format(Strings.MoveConfirmBody, prestige.GardenIndex + 2);
            confirm.SetActive(true);
        }

        public void ConfirmMove()
        {
            confirm.SetActive(false);
            if (moveQueen != null && moveQueen())
                Close();
            else
                RefreshPanel();
        }

        public void CancelConfirm() => confirm.SetActive(false);

        private void LateUpdate()
        {
            if (prestige == null)
                return;
            if (dirty)
            {
                dirty = false;
                bool ready = CanMove();
                if (ready != shownReady)
                {
                    shownReady = ready;
                    readyBadge.SetActive(ready);
                    if (ready)
                        tweener.PopIn(readyBadge.transform, feedback.BadgePop, feedback.BadgePopDuration);
                }
            }
            if (!panel.activeSelf)
                return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
                RefreshPanel();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveListener(openButton, Open);
            RemoveListener(closeButton, Close);
            RemoveListener(moveButton, AskConfirm);
            RemoveListener(confirmButton, ConfirmMove);
            RemoveListener(cancelButton, CancelConfirm);
        }

        private bool CanMove() => prestige.CanMove(bloom.Fraction, economy.Honey);

        private void RefreshPanel()
        {
            refreshTimer = refreshSeconds;
            float fraction = bloom.Fraction;
            bool unlocked = prestige.IsUnlocked(fraction);
            BigNumber cost = prestige.MoveCost;

            SetText(jellyText, string.Format(Strings.RoyalJellyFormat, NumberFormat.Abbreviate(prestige.RoyalJelly)));
            SetText(gardenText, string.Format(Strings.GardenNumberFormat, prestige.GardenIndex + 1));
            SetText(requirementText, unlocked
                ? string.Format(Strings.MoveBloomReadyFormat, bloom.Percent)
                : string.Format(Strings.MoveNeedsBloomFormat, Mathf.CeilToInt(prestige.MoveUnlockBloom * 100f - 1e-3f), bloom.Percent));
            SetText(costText, string.Format(Strings.MoveCostFormat, NumberFormat.Abbreviate(cost)));
            SetText(previewText, string.Format(Strings.JellyPreviewFormat,
                NumberFormat.Abbreviate(prestige.PreviewJelly(economy.RunHoneyEarned, fraction))));
            SetText(bonusText, string.Format(Strings.BloomBonusFormat, Mathf.RoundToInt((float)prestige.BloomBonus(fraction) * 100f)));

            bool ready = unlocked && economy.Honey >= cost;
            moveButton.interactable = ready;
            moveButtonImage.color = ready ? moveEnabledColor : moveDisabledColor;
            if (!ready && confirm.activeSelf)
                confirm.SetActive(false);
        }

        private void HandleHoneyChanged(BigNumber _) => dirty = true;

        private void HandleQueenMoved(BigNumber _) => MarkDirty();

        private void MarkDirty()
        {
            dirty = true;
            refreshTimer = 0f;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text.text != value)
                text.text = value;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private void Unsubscribe()
        {
            if (economy != null)
                economy.OnHoneyChanged -= HandleHoneyChanged;
            if (bloom != null)
                bloom.OnBloomChanged -= MarkDirty;
            if (prestige != null)
                prestige.OnQueenMoved -= HandleQueenMoved;
        }
    }
}
