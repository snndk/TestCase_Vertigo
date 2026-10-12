using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    internal static class WheelPoolValidator
    {
        internal static void Validate(WheelConfiguration configuration)
        {
            var rewards = new Dictionary<string, RewardDefinition>(StringComparer.Ordinal);
            var ordinaryRewards = new HashSet<string>(StringComparer.Ordinal);
            var specialRewards = new HashSet<string>(StringComparer.Ordinal);
            foreach (WheelTier tier in Enum.GetValues(typeof(WheelTier)))
            {
                var candidates = configuration.GetCandidates(tier);
                ValidateTier(candidates, tier, configuration.SliceCount);
                foreach (var candidate in candidates)
                {
                    // Disabled entries remain authored data, but cannot affect gameplay constraints.
                    if (candidate.Weight == 0 || !(candidate.Definition is RewardSliceDefinition slice))
                        continue;
                    var reward = slice.Reward;
                    if (rewards.TryGetValue(reward.Id, out var existing) &&
                        existing.IncreasePerZone != reward.IncreasePerZone)
                        throw new ArgumentException("All active tiers must share the same growth policy for reward " + reward.Id + ".");
                    rewards[reward.Id] = reward;
                    if (tier != WheelTier.Gold)
                        ordinaryRewards.Add(reward.Id);
                    if (slice.IsSpecial)
                        specialRewards.Add(reward.Id);
                }
            }
            if (ordinaryRewards.Overlaps(specialRewards))
                throw new ArgumentException("Special gold rewards must be exclusive to the gold wheel.");
        }

        private static void ValidateTier(IReadOnlyList<WeightedSlice> candidates, WheelTier tier, int sliceCount)
        {
            var eligible = 0;
            var guaranteed = 0;
            var bombs = 0;
            var guaranteedRewards = 0;
            var optionalNonRewards = 0;
            var guaranteedSpecials = 0;
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                    throw new ArgumentException("A pool cannot contain null entries.");
                if (candidate.Weight == 0)
                    continue;
                eligible++;
                if (candidate.IsGuaranteed)
                    guaranteed++;
                if (candidate.Definition is BombSliceDefinition)
                {
                    bombs++;
                    if (tier != WheelTier.Bronze || !candidate.IsGuaranteed)
                        throw new ArgumentException("Only bronze may include a bomb, and its weight must be 100.");
                }
                if (candidate.Definition is RewardSliceDefinition reward)
                {
                    if (candidate.IsGuaranteed)
                        guaranteedRewards++;
                    if (reward.IsSpecial)
                    {
                        if (tier != WheelTier.Gold)
                            throw new ArgumentException("Special rewards belong to the gold wheel.");
                        if (candidate.IsGuaranteed)
                            guaranteedSpecials++;
                    }
                }
                else if (!candidate.IsGuaranteed)
                    optionalNonRewards++;
            }
            if (guaranteed > sliceCount || eligible < sliceCount)
                throw new ArgumentException(tier + " has too many guaranteed or too few eligible entries.");
            if (bombs != (tier == WheelTier.Bronze ? 1 : 0))
                throw new ArgumentException(tier + " has an invalid bomb count.");
            if (guaranteedRewards + Math.Max(0, sliceCount - guaranteed - optionalNonRewards) < 2)
                throw new ArgumentException("Every selection must contain at least two reward slices.");
            if (tier == WheelTier.Gold && guaranteedSpecials == 0)
                throw new ArgumentException("Gold requires at least one special reward with weight 100.");
        }

    }
}
