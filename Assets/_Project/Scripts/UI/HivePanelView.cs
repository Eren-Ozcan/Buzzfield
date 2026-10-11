using System;
using System.Collections.Generic;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// The Hive (docs/DEWDROPS.md 4.2): the round hive button left of the goal pill, shown from the
    /// first Queen move with a dot while a species waits for a free tap or a step is affordable,
    /// and the Hive panel behind it with one card per rare bee species and the Dewdrops balance.
    /// Spends above the confirm limit need a second tap on the same card within a few seconds.
    /// Events only mark it dirty; the button and the open panel are redrawn once a frame at most.
    /// </summary>
    public sealed class HivePanelView : MonoBehaviour
    {
        [Header("Button")]
        [Tooltip("Round hive button left of the goal pill; hidden until the hive opens.")]
        [SerializeField] private GameObject buttonRoot;
        [SerializeField] private Button openButton;
        [Tooltip("Dot on the button while a species waits for a free tap or a step is affordable.")]
        [SerializeField] private GameObject badge;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text dewText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RareBeeCardView[] cards;

        private RareBeeManager rareBees;
        private DewManager dew;
        private PrestigeManager prestige;
        private DewSettings dewSettings;
        private Func<int, bool> advance;
        private Tweener tweener;
        private UiFeedbackSettings feedback;
        private string[] names;
        private bool dirty;
        private int confirmingIndex = -1;
        private float confirmLeft;

        public bool IsOpen => panel.activeSelf;

        public bool IsButtonShown => buttonRoot.activeSelf;

        public bool IsBadgeShown => badge.activeSelf;

        /// <summary>The card waiting for its second tap; -1 when none.</summary>
        public int ConfirmingIndex => confirmingIndex;

        public string Status => statusText.text;

        public IReadOnlyList<RareBeeCardView> Cards => cards;

        private int GardenNumber => prestige.GardenIndex + 1;

        /// <param name="tryAdvance">Pays and adds the next level of a species (joining at level 1); false when refused.</param>
        public void Init(RareBeeManager rareBeeManager, DewManager dewManager, PrestigeManager prestigeManager, DewSettings settings,
            Func<int, bool> tryAdvance, Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            Unsubscribe();
            rareBees = rareBeeManager;
            dew = dewManager;
            prestige = prestigeManager;
            dewSettings = settings;
            advance = tryAdvance;
            tweener = tweenRunner;
            feedback = feedbackSettings;
            rareBees.OnChanged += MarkDirty;
            dew.OnChanged += MarkDirty;
            prestige.OnJellyChanged += MarkDirty;

            names = new string[rareBees.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = Name(rareBees.Bees[i]);
            for (int i = 0; i < cards.Length; i++)
            {
                RareBeeCardView card = cards[i];
                bool known = card.BeeIndex < rareBees.Count;
                // Cards are built from the data; a card left over from older data is hidden.
                card.gameObject.SetActive(known);
                if (known)
                    card.Init(names[card.BeeIndex], Press);
            }

            AddListener(openButton, OpenFromButton);
            AddListener(closeButton, Close);
            panel.SetActive(false);
            buttonRoot.SetActive(false);
            badge.SetActive(false);
            MarkDirty();
        }

        /// <summary>Opens the panel; false while the hive is not open yet (before the first Queen move).</summary>
        public bool Open()
        {
            if (rareBees == null || !rareBees.IsHiveOpen(GardenNumber))
                return false;
            CancelConfirm();
            statusText.text = string.Empty;
            panel.SetActive(true);
            RefreshCards();
            return true;
        }

        public void Close()
        {
            CancelConfirm();
            panel.SetActive(false);
        }

        private void OpenFromButton() => Open();

        private void LateUpdate()
        {
            if (rareBees == null)
                return;
            if (dirty)
            {
                dirty = false;
                RefreshButton();
                if (panel.activeSelf)
                    RefreshCards();
            }
            if (confirmingIndex < 0)
                return;
            confirmLeft -= Time.unscaledDeltaTime;
            if (confirmLeft > 0f)
                return;
            CancelConfirm();
            if (panel.activeSelf)
                RefreshCards();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (openButton != null)
                openButton.onClick.RemoveListener(OpenFromButton);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        private void RefreshButton()
        {
            int garden = GardenNumber;
            bool show = rareBees.IsHiveOpen(garden);
            if (buttonRoot.activeSelf != show)
            {
                buttonRoot.SetActive(show);
                if (show)
                    tweener.PopIn(buttonRoot.transform, feedback.BadgePop, feedback.BadgePopDuration);
            }
            bool attention = show && (rareBees.FreeWaiting(garden) >= 0 || rareBees.AnyAffordable(garden, dew.Balance));
            if (badge.activeSelf == attention)
                return;
            badge.SetActive(attention);
            if (attention)
                tweener.PopIn(badge.transform, feedback.BadgePop, feedback.BadgePopDuration);
        }

        private void RefreshCards()
        {
            SetText(dewText, dew.Balance.ToString());
            int garden = GardenNumber;
            IReadOnlyList<RareBee> bees = rareBees.Bees;
            for (int c = 0; c < cards.Length; c++)
            {
                int index = cards[c].BeeIndex;
                if (index >= bees.Count)
                    continue;
                RareBee bee = bees[index];
                int level = rareBees.Level(index);
                long cost = rareBees.NextCost(index);
                string perLevel = EffectText(bee, rareBees.Effect(index, 1)) + Strings.PerLevelSuffix;

                if (!rareBees.IsAvailable(index, garden))
                {
                    cards[c].Show(RareBeeCardView.State.Locked, string.Empty, perLevel, string.Format(Strings.RareBeeLockedFormat, bee.UnlockGarden));
                    continue;
                }
                if (level == 0)
                {
                    if (cost == 0)
                    {
                        string welcome = string.Format(Strings.RareBeeFreeFormat, names[index].ToLowerInvariant());
                        cards[c].Show(RareBeeCardView.State.Free, string.Empty, welcome + "\n" + perLevel, Strings.RareBeeFree);
                    }
                    else
                    {
                        cards[c].Show(StepState(index, cost), string.Empty, perLevel, CostLabel(index, cost));
                    }
                    continue;
                }

                string levelLabel = string.Format(Strings.AbilityLevelFormat, level, bee.MaxLevel);
                string now = EffectText(bee, rareBees.Effect(index, level));
                if (rareBees.IsMaxed(index))
                {
                    cards[c].Show(RareBeeCardView.State.Maxed, levelLabel, now, Strings.Max);
                    continue;
                }
                string next = string.Format(Strings.RareBeeNextFormat, string.Format(
                    bee.Effect == RareBeeEffect.UpgradeDiscountPercent ? Strings.PercentDownFormat : Strings.PercentUpFormat,
                    Amount(rareBees.Effect(index, 1))));
                cards[c].Show(StepState(index, cost), levelLabel, now + "\n" + next, CostLabel(index, cost));
            }
        }

        private RareBeeCardView.State StepState(int index, long cost) =>
            !dew.CanAfford(cost) ? RareBeeCardView.State.TooExpensive
            : index == confirmingIndex ? RareBeeCardView.State.Confirming
            : RareBeeCardView.State.Buyable;

        private string CostLabel(int index, long cost) => index == confirmingIndex ? Strings.ConfirmSpend : cost.ToString();

        /// <summary>A card's button: a big spend asks for a second tap first, then the step is paid for.</summary>
        private bool Press(int index)
        {
            if (index < 0 || index >= rareBees.Count || !panel.activeSelf)
                return false;
            int levelBefore = rareBees.Level(index);
            long cost = rareBees.NextCost(index);
            if (cost < 0 || !rareBees.IsAvailable(index, GardenNumber))
                return false;
            if (cost > dewSettings.ConfirmAbove && confirmingIndex != index && dew.CanAfford(cost))
            {
                confirmingIndex = index;
                confirmLeft = dewSettings.ConfirmSeconds;
                RefreshCards();
                return true;
            }
            CancelConfirm();
            if (advance == null || !advance(index))
            {
                statusText.text = dew.CanAfford(cost) ? string.Empty : Strings.NotEnoughDew;
                RefreshCards();
                return false;
            }
            statusText.text = levelBefore == 0
                ? string.Format(Strings.RareBeeJoinedFormat, names[index])
                : string.Format(Strings.RareBeeLevelUpFormat, names[index], rareBees.Level(index));
            RefreshCards();
            return true;
        }

        private void CancelConfirm()
        {
            confirmingIndex = -1;
            confirmLeft = 0f;
        }

        private static string Name(RareBee bee)
        {
            switch (bee.Id)
            {
                case RareBeeIds.Bumblebee: return Strings.RareBumblebee;
                case RareBeeIds.BlueBandedBee: return Strings.RareBlueBandedBee;
                case RareBeeIds.MasonBee: return Strings.RareMasonBee;
                case RareBeeIds.OrchidBee: return Strings.RareOrchidBee;
                case RareBeeIds.LeafcutterBee: return Strings.RareLeafcutterBee;
                case RareBeeIds.CarpenterBee: return Strings.RareCarpenterBee;
                default: return bee.Id;
            }
        }

        /// <summary>Described by effect, so a new species that reuses an effect needs no new text.</summary>
        private static string EffectText(RareBee bee, double amount)
        {
            string format;
            switch (bee.Effect)
            {
                case RareBeeEffect.CarryCapacityPercent: format = Strings.RareEffectCarryFormat; break;
                case RareBeeEffect.ShakeHoneyPercent: format = Strings.RareEffectShakeFormat; break;
                case RareBeeEffect.OfflineEfficiencyPoints: format = Strings.RareEffectOfflineFormat; break;
                case RareBeeEffect.FlowerHoneyPercent: format = Strings.RareEffectFlowerFormat; break;
                case RareBeeEffect.UpgradeDiscountPercent: format = Strings.RareEffectDiscountFormat; break;
                case RareBeeEffect.RoyalJellyPercent: format = Strings.RareEffectJellyFormat; break;
                default: return string.Empty;
            }
            return string.Format(format, Amount(amount));
        }

        private static string Amount(double amount) => amount.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        private void MarkDirty() => dirty = true;

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

        private void Unsubscribe()
        {
            if (rareBees != null)
                rareBees.OnChanged -= MarkDirty;
            if (dew != null)
                dew.OnChanged -= MarkDirty;
            if (prestige != null)
                prestige.OnJellyChanged -= MarkDirty;
        }
    }
}
