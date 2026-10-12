using System;
using System.Linq;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    internal static class RewardGrowthValidator
    {
        internal static void Validate(WheelConfiguration configuration)
        {
            var rewards = Enum.GetValues(typeof(WheelTier)).Cast<WheelTier>()
                .SelectMany(configuration.GetCandidates)
                .Where(candidate => candidate.Weight > 0)
                .Select(candidate => candidate.Definition)
                .OfType<RewardSliceDefinition>()
                .GroupBy(slice => slice.Reward.Id, StringComparer.Ordinal);

            foreach (var slices in rewards)
            {
                var increase = slices.First().Reward.IncreasePerZone;
                if (slices.Any(slice => slice.Reward.IncreasePerZone != increase))
                    throw new ArgumentException("All active tiers must share the same growth policy for reward " + slices.Key + ".");

                // The smallest next-zone variant must exceed the largest previous-zone variant,
                // even when the pool selection or tier changes.
                var minimumIncrease = slices.Max(slice => slice.BaseAmount) - slices.Min(slice => slice.BaseAmount) + 1;
                if (increase < minimumIncrease)
                    throw new ArgumentException("Reward '" + slices.Key + "' requires IncreasePerZone >= " +
                        minimumIncrease + " so amounts grow across all active slice variants.");
            }
        }
    }
}
