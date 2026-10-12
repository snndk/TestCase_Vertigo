using System;

namespace Vertigo.Runtime._Game.Runtime.Rewards
{
    public class RewardDefinition
    {
        public string Id { get; }
        public long IncreasePerZone { get; }

        public RewardDefinition(string id, long increasePerZone)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A reward ID is required.", nameof(id));
            if (increasePerZone <= 0)
                throw new ArgumentOutOfRangeException(nameof(increasePerZone));

            Id = id;
            IncreasePerZone = increasePerZone;
        }

        public Reward AtZone(int zone, long baseAmount)
        {
            if (baseAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseAmount));
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone));

            return new Reward(Id, checked(baseAmount + (zone - 1L) * IncreasePerZone));
        }
    }
}
