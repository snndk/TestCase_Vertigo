using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class WheelConfiguration
    {
        private readonly ReadOnlyCollection<SliceDefinition> m_Bronze;
        private readonly ReadOnlyCollection<SliceDefinition> m_Silver;
        private readonly ReadOnlyCollection<SliceDefinition> m_Gold;
        private readonly Dictionary<string, RewardDefinition> m_Rewards =
            new Dictionary<string, RewardDefinition>(StringComparer.Ordinal);

        public int SliceCount => m_Bronze.Count;

        public WheelConfiguration(IEnumerable<SliceDefinition> bronze,
            IEnumerable<SliceDefinition> silver, IEnumerable<SliceDefinition> gold)
        {
            m_Bronze = CopyAndValidate(bronze, WheelTier.Bronze);
            m_Silver = CopyAndValidate(silver, WheelTier.Silver);
            m_Gold = CopyAndValidate(gold, WheelTier.Gold);

            RegisterRewards(m_Bronze);
            RegisterRewards(m_Silver);
            RegisterRewards(m_Gold);

            if (m_Bronze.Count != m_Silver.Count || m_Bronze.Count != m_Gold.Count)
                throw new ArgumentException("All wheels must have the configured slice count.");

            var ordinaryRewards = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slice in m_Bronze)
            {
                if (slice is RewardSliceDefinition reward)
                    ordinaryRewards.Add(reward.Reward.Id);
            }
            foreach (var slice in m_Silver)
            {
                if (slice is RewardSliceDefinition reward)
                    ordinaryRewards.Add(reward.Reward.Id);
            }

            foreach (var slice in m_Gold)
            {
                if (slice is RewardSliceDefinition reward && reward.IsSpecial &&
                    ordinaryRewards.Contains(reward.Reward.Id))
                    throw new ArgumentException("Special gold rewards must be exclusive to the gold wheel.");
            }
        }

        public IReadOnlyList<SliceDefinition> GetSlices(WheelTier tier)
        {
            switch (tier)
            {
                case WheelTier.Bronze:
                    return m_Bronze;
                case WheelTier.Silver:
                    return m_Silver;
                case WheelTier.Gold:
                    return m_Gold;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        internal void ValidateRound(int zone, IReadOnlyList<RoundSlice> slices)
        {
            var tier = ZoneRules.GetTier(zone);
            var bombCount = 0;
            var rewardCount = 0;
            var specialCount = 0;
            var definitions = GetSlices(tier);

            if (slices.Count != SliceCount)
                throw new InvalidOperationException("Generated wheel capacity does not match its configuration.");

            for (var index = 0; index < slices.Count; index++)
            {
                var slice = slices[index];
                if (slice == null || slice.Id != definitions[index].Id)
                    throw new InvalidOperationException("Generated slice IDs must match their definitions.");

                if (slice is BombSlice)
                    bombCount++;
                if (slice is RewardSlice reward)
                {
                    rewardCount++;
                    if (reward.IsSpecial)
                        specialCount++;
                    if (!m_Rewards.TryGetValue(reward.Reward.RewardId, out var definition) ||
                        definition.AtZone(zone).Amount != reward.Reward.Amount)
                        throw new InvalidOperationException("Generated reward does not match the shared growth policy.");
                    if (!(definitions[index] is RewardSliceDefinition source) ||
                        source.Reward.Id != reward.Reward.RewardId || source.IsSpecial != reward.IsSpecial)
                        throw new InvalidOperationException("Generated reward must match its configured identity and special status.");
                }
            }

            var requiredBombs = tier == WheelTier.Bronze ? 1 : 0;
            if (bombCount != requiredBombs || rewardCount < 2 ||
                (tier == WheelTier.Gold ? specialCount == 0 : specialCount != 0))
                throw new InvalidOperationException("Generated wheel violates its tier's bomb or reward rules.");
        }

        private void RegisterRewards(IEnumerable<SliceDefinition> slices)
        {
            foreach (var slice in slices)
            {
                if (!(slice is RewardSliceDefinition rewardSlice))
                    continue;

                var reward = rewardSlice.Reward;
                if (m_Rewards.TryGetValue(reward.Id, out var existing) &&
                    (existing.BaseAmount != reward.BaseAmount || existing.IncreasePerZone != reward.IncreasePerZone))
                    throw new ArgumentException("All tiers must share the same growth policy for reward " + reward.Id + ".");

                m_Rewards[reward.Id] = reward;
            }
        }

        private static ReadOnlyCollection<SliceDefinition> CopyAndValidate(
            IEnumerable<SliceDefinition> source, WheelTier tier)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var slices = new List<SliceDefinition>(source);
            if (slices.Count < 3)
                throw new ArgumentException("A wheel needs at least three slices.", nameof(source));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var bombCount = 0;
            var rewardCount = 0;
            var specialCount = 0;
            foreach (var slice in slices)
            {
                if (slice == null || !ids.Add(slice.Id))
                    throw new ArgumentException("Slice IDs must be non-null and unique within a wheel.");

                if (slice is BombSliceDefinition)
                    bombCount++;
                if (slice is RewardSliceDefinition reward)
                {
                    rewardCount++;
                    if (reward.IsSpecial)
                        specialCount++;
                }
            }

            var requiredBombs = tier == WheelTier.Bronze ? 1 : 0;
            if (bombCount != requiredBombs)
                throw new ArgumentException(tier + " requires exactly " + requiredBombs + " bomb slices.");
            if (rewardCount < 2)
                throw new ArgumentException("A wheel requires at least two reward slices.");
            if (tier == WheelTier.Gold && specialCount == 0)
                throw new ArgumentException("Gold requires a special reward.");
            if (tier != WheelTier.Gold && specialCount != 0)
                throw new ArgumentException("Special rewards belong to the gold wheel.");

            return slices.AsReadOnly();
        }
    }
}
