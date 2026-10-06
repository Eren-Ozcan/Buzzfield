using System;

namespace Buzzfield.Economy
{
    /// <summary>
    /// The Dewdrops balance, the premium currency: earned from goals, finished gardens, the daily
    /// streak and the butterfly's pouch, bought in packs, spent on boosts and rare bees. Survives
    /// the Queen move. Plain class; the game logs where each one came from and went.
    /// </summary>
    public sealed class DewManager
    {
        public long Balance { get; private set; }

        /// <summary>Every Dewdrop ever earned or bought; the counter shows from the first one.</summary>
        public long TotalEarned { get; private set; }

        public event Action OnChanged;

        public void Earn(long amount)
        {
            if (amount <= 0)
                return;
            Balance += amount;
            TotalEarned += amount;
            OnChanged?.Invoke();
        }

        public bool CanAfford(long cost) => cost >= 0 && Balance >= cost;

        /// <summary>Removes <paramref name="cost"/> if the balance covers it.</summary>
        public bool TrySpend(long cost)
        {
            if (!CanAfford(cost))
                return false;
            Balance -= cost;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Loads saved values; a damaged save never gives a negative balance or a total below it.</summary>
        public void Restore(long balance, long totalEarned)
        {
            Balance = Math.Max(0, balance);
            TotalEarned = Math.Max(Balance, totalEarned);
            OnChanged?.Invoke();
        }
    }
}
