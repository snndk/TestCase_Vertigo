using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    internal static class WheelPoolValidator
    {
        internal static void Validate(WheelConfiguration configuration)
        {
            foreach (WheelTier tier in Enum.GetValues(typeof(WheelTier)))
                ValidateTier(configuration.GetCandidates(tier), tier, configuration.SliceCount);
            RewardGrowthValidator.Validate(configuration);
        }

        private static void ValidateTier(IReadOnlyList<WeightedSlice> candidates, WheelTier tier, int sliceCount)
        {
            var eligible = 0;
            var guaranteed = 0;
            var bombs = 0;
            var guaranteedRewards = 0;
            var optionalNonRewards = 0;
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
                if (candidate.Definition is RewardSliceDefinition)
                {
                    if (candidate.IsGuaranteed)
                        guaranteedRewards++;
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
        }

    }
}
