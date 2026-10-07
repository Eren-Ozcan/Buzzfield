using System;
using System.Collections.Generic;
using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// The shop's Boosts tab (docs/DEWDROPS.md 4.1): time skips and honey boosts for Dewdrops,
    /// the boost running now with its countdown and the next one in line. Locked with a short
    /// note until the first garden is complete. Spends above the confirm limit need a second
    /// tap on the same row within a few seconds. The shop panel refreshes it while it shows.
    /// </summary>
    public sealed class DewBoostsTabView : MonoBehaviour
    {
        [SerializeField] private DewOfferRowView[] rows;
        [Tooltip("Rows and status; hidden while the tab is locked.")]
        [SerializeField] private GameObject content;
        [SerializeField] private GameObject lockedNote;
        [SerializeField] private TMP_Text runningText;
        [SerializeField] private TMP_Text queueText;

        private DewSettings settings;
        private DewManager dew;
        private BoostManager boosts;
        private Func<bool> unlocked;
        private Func<int, BigNumber> timeSkipHoney;
        private Func<int, bool> buy;
        private Action<string> setStatus;
        private readonly BoostCountdown running = new BoostCountdown(Strings.BoostRunningFormat);
        private int confirmingIndex = -1;
        private float confirmLeft;
        private float bodyTimer;
        private bool queueDirty = true;
        private bool? shownUnlocked;

        public bool IsUnlocked => unlocked != null && unlocked();

        /// <summary>The row waiting for its second tap; -1 when none.</summary>
        public int ConfirmingIndex => confirmingIndex;

        public IReadOnlyList<DewOfferRowView> Rows => rows;

        /// <param name="unlockedNow">True once the tab may sell.</param>
        /// <param name="timeSkipHoneyFor">Honey the time skip at an offer index pays right now.</param>
        /// <param name="buyOffer">Spends the Dewdrops and hands out the offer; false when refused.</param>
        /// <param name="showStatus">Writes the shop's status line.</param>
        public void Init(DewSettings dewSettings, DewManager dewManager, BoostManager boostManager, Func<bool> unlockedNow,
            Func<int, BigNumber> timeSkipHoneyFor, Func<int, bool> buyOffer, Action<string> showStatus)
        {
            Unsubscribe();
            settings = dewSettings;
            dew = dewManager;
            boosts = boostManager;
            unlocked = unlockedNow;
            timeSkipHoney = timeSkipHoneyFor;
            buy = buyOffer;
            setStatus = showStatus;
            boosts.OnDewBoostsChanged += MarkQueueDirty;

            IReadOnlyList<DewOffer> offers = settings.Offers;
            for (int i = 0; i < rows.Length; i++)
            {
                DewOfferRowView row = rows[i];
                bool known = row.OfferIndex < offers.Count;
                row.gameObject.SetActive(known);
                if (!known)
                    continue;
                DewOffer offer = offers[row.OfferIndex];
                row.Init(Title(offer), offer.kind == DewOfferKind.HoneyBoost ? Strings.HoneyBoostBody : string.Empty,
                    offer.cost, Strings.ConfirmSpend, Press);
            }
            shownUnlocked = null;
            CancelConfirm();
        }

        /// <summary>The tab came into view: everything is redrawn now.</summary>
        public void OnShown()
        {
            CancelConfirm();
            bodyTimer = 0f;
            queueDirty = true;
            running.Reset();
            Refresh(0f);
        }

        /// <summary>Drops a pending second tap (tab change, shop closed).</summary>
        public void CancelConfirm()
        {
            confirmingIndex = -1;
            confirmLeft = 0f;
        }

        /// <summary>Called by the shop every frame while this tab shows.</summary>
        public void Refresh(float unscaledDeltaTime)
        {
            if (settings == null)
                return;
            bool open = IsUnlocked;
            if (open != shownUnlocked)
            {
                shownUnlocked = open;
                content.SetActive(open);
                lockedNote.SetActive(!open);
            }
            if (!open)
                return;

            if (confirmingIndex >= 0)
            {
                confirmLeft -= unscaledDeltaTime;
                if (confirmLeft <= 0f)
                    CancelConfirm();
            }
            IReadOnlyList<DewOffer> offers = settings.Offers;
            for (int i = 0; i < rows.Length; i++)
            {
                DewOfferRowView row = rows[i];
                if (row.OfferIndex >= offers.Count)
                    continue;
                DewOfferRowView.State state = !dew.CanAfford(offers[row.OfferIndex].cost) ? DewOfferRowView.State.TooExpensive
                    : row.OfferIndex == confirmingIndex ? DewOfferRowView.State.Confirming
                    : DewOfferRowView.State.Buyable;
                row.Show(state);
            }

            // The rate behind a time skip moves slowly; once a second is plenty.
            bodyTimer -= unscaledDeltaTime;
            if (bodyTimer <= 0f)
            {
                bodyTimer = 1f;
                RefreshTimeSkipBodies(offers);
            }

            DewBoostQueue line = boosts.DewBoosts;
            if (line.Count > 0)
                running.Show(runningText, line.Multiplier, line.SecondsLeft);
            else if (runningText.text != Strings.BoostNone)
            {
                runningText.text = Strings.BoostNone;
                running.Reset();
            }
            if (queueDirty)
                RefreshQueue(line);
        }

        private void RefreshTimeSkipBodies(IReadOnlyList<DewOffer> offers)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = rows[i].OfferIndex;
                if (index >= offers.Count || offers[index].kind != DewOfferKind.TimeSkip)
                    continue;
                rows[i].SetBody(string.Format(Strings.TimeSkipBodyFormat, TimeFormat.Duration(offers[index].seconds),
                    NumberFormat.Abbreviate(timeSkipHoney(index))));
            }
        }

        private void RefreshQueue(DewBoostQueue line)
        {
            queueDirty = false;
            if (line.Count < 2)
            {
                queueText.text = string.Empty;
                return;
            }
            DewBoostEntry next = line.Entries[1];
            string multiplier = next.multiplier.ToString("0.#");
            string duration = TimeFormat.Duration(next.secondsLeft);
            queueText.text = line.Count == 2
                ? string.Format(Strings.BoostQueuedFormat, multiplier, duration)
                : string.Format(Strings.BoostQueuedMoreFormat, multiplier, duration, line.Count - 2);
        }

        private void Press(int index)
        {
            IReadOnlyList<DewOffer> offers = settings.Offers;
            if (!IsUnlocked || index < 0 || index >= offers.Count)
                return;
            DewOffer offer = offers[index];
            if (offer.cost > settings.ConfirmAbove && confirmingIndex != index)
            {
                confirmingIndex = index;
                confirmLeft = settings.ConfirmSeconds;
                Refresh(0f);
                return;
            }
            CancelConfirm();
            BigNumber honey = offer.kind == DewOfferKind.TimeSkip ? timeSkipHoney(index) : BigNumber.Zero;
            if (!buy(index))
            {
                setStatus?.Invoke(Strings.NotEnoughDew);
                Refresh(0f);
                return;
            }
            setStatus?.Invoke(offer.kind == DewOfferKind.TimeSkip
                ? string.Format(Strings.TimeSkipDoneFormat, NumberFormat.Abbreviate(honey))
                : string.Format(Strings.BoostBoughtFormat, offer.multiplier.ToString("0.#"), TimeFormat.Duration(offer.seconds)));
            bodyTimer = 0f;
            Refresh(0f);
        }

        private static string Title(DewOffer offer) => offer.kind == DewOfferKind.TimeSkip
            ? string.Format(Strings.TimeSkipFormat, TimeFormat.Duration(offer.seconds))
            : string.Format(Strings.HoneyBoostFormat, offer.multiplier.ToString("0.#"), TimeFormat.Duration(offer.seconds));

        private void MarkQueueDirty() => queueDirty = true;

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (boosts != null)
                boosts.OnDewBoostsChanged -= MarkQueueDirty;
        }
    }
}
