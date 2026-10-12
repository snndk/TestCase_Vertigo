using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vertigo.Runtime._Game.Runtime.Rewards
{
    public class RewardCollection
    {
        private readonly ReadOnlyDictionary<string, long> m_Amounts;

        public static RewardCollection Empty { get; } = new RewardCollection(Array.Empty<Reward>());

        public IReadOnlyDictionary<string, long> Amounts => m_Amounts;

        public RewardCollection(IEnumerable<Reward> rewards)
        {
            if (rewards == null)
                throw new ArgumentNullException(nameof(rewards));

            var values = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var reward in rewards)
            {
                if (reward == null)
                    throw new ArgumentException("Rewards cannot contain null.", nameof(rewards));

                values.TryGetValue(reward.RewardId, out var current);
                values[reward.RewardId] = checked(current + reward.Amount);
            }

            m_Amounts = new ReadOnlyDictionary<string, long>(values);
        }

        public long GetAmount(string rewardId)
        {
            return m_Amounts.TryGetValue(rewardId, out var amount) ? amount : 0;
        }

        public RewardCollection Add(Reward reward)
        {
            if (reward == null)
                throw new ArgumentNullException(nameof(reward));

            var combined = new Dictionary<string, long>(m_Amounts, StringComparer.Ordinal);
            combined[reward.RewardId] = checked(GetAmount(reward.RewardId) + reward.Amount);
            return new RewardCollection(combined);
        }

        public RewardCollection Combine(RewardCollection other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var combined = new Dictionary<string, long>(m_Amounts, StringComparer.Ordinal);
            foreach (var pair in other.m_Amounts)
            {
                combined.TryGetValue(pair.Key, out var current);
                combined[pair.Key] = checked(current + pair.Value);
            }
            return new RewardCollection(combined);
        }

        public RewardCollection Subtract(Reward cost)
        {
            if (cost == null)
                throw new ArgumentNullException(nameof(cost));
            if (GetAmount(cost.RewardId) < cost.Amount)
                throw new InvalidOperationException("Not enough currency.");

            var updated = new Dictionary<string, long>(m_Amounts, StringComparer.Ordinal);
            var remaining = updated[cost.RewardId] - cost.Amount;
            if (remaining == 0)
                updated.Remove(cost.RewardId);
            else
                updated[cost.RewardId] = remaining;
            return new RewardCollection(updated);
        }

        private RewardCollection(Dictionary<string, long> ownedAmounts)
        {
            m_Amounts = new ReadOnlyDictionary<string, long>(ownedAmounts);
        }
    }
}
