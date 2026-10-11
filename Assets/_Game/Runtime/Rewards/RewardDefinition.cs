using System;

namespace Vertigo.Runtime._Game.Runtime.Rewards
{
    public class RewardDefinition
    {
        public string Id { get; }
        public long BaseAmount { get; }
        public long IncreasePerZone { get; }

        public RewardDefinition(string id, long baseAmount, long increasePerZone)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A reward ID is required.", nameof(id));
            if (baseAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseAmount));
            if (increasePerZone <= 0)
                throw new ArgumentOutOfRangeException(nameof(increasePerZone));

            Id = id;
            BaseAmount = baseAmount;
            IncreasePerZone = increasePerZone;
        }

        public Reward AtZone(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone));

            return new Reward(Id, checked(BaseAmount + (zone - 1L) * IncreasePerZone));
        }
    }
}
