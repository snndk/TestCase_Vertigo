using System;

namespace Vertigo.Runtime._Game.Runtime.Rewards
{
    public class Reward
    {
        public string RewardId { get; }
        public long Amount { get; }

        public Reward(string rewardId, long amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                throw new ArgumentException("A reward ID is required.", nameof(rewardId));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            RewardId = rewardId;
            Amount = amount;
        }
    }
}
