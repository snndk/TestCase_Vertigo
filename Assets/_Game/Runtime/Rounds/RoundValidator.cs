using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    internal static class RoundValidator
    {
        internal static void Validate(WheelConfiguration configuration, int zone, IReadOnlyList<SliceData> slices, IReadOnlyList<int> sourceIndices)
        {
            var tier = ZoneRules.GetTier(zone);
            var bombCount = 0;
            var rewardCount = 0;
            var specialCount = 0;
            var candidates = configuration.GetCandidates(tier);
            var used = new HashSet<int>();

            if (slices.Count != configuration.SliceCount || sourceIndices.Count != configuration.SliceCount)
                throw new InvalidOperationException("Generated wheel capacity does not match its configuration.");

            for (var index = 0; index < slices.Count; index++)
            {
                var sourceIndex = sourceIndices[index];
                if (sourceIndex < 0 || sourceIndex >= candidates.Count || !used.Add(sourceIndex) || candidates[sourceIndex].Weight == 0)
                    throw new InvalidOperationException("Generated wheel contains an invalid pool entry.");
                var definition = candidates[sourceIndex].Definition;
                var slice = slices[index];
                if (slice == null || slice.Id != definition.Id)
                    throw new InvalidOperationException("Generated slice IDs must match their definitions.");

                if (slice is BombSliceData)
                    bombCount++;
                if (slice is RewardSliceData reward)
                {
                    rewardCount++;
                    if (reward.IsSpecial)
                        specialCount++;
                    if (!(definition is RewardSliceDefinition source) ||
                        source.Reward.Id != reward.Reward.RewardId || source.IsSpecial != reward.IsSpecial)
                        throw new InvalidOperationException("Generated reward must match its configured identity and special status.");
                    if (source.Reward.AtZone(zone, source.BaseAmount).Amount != reward.Reward.Amount)
                        throw new InvalidOperationException("Generated reward does not match its slot's amount and shared growth policy.");
                }
            }

            var requiredBombs = tier == WheelTier.Bronze ? 1 : 0;
            if (bombCount != requiredBombs || rewardCount < 2 ||
                (tier == WheelTier.Gold ? specialCount == 0 : specialCount != 0))
                throw new InvalidOperationException("Generated wheel violates its tier's bomb or reward rules.");
        }

    }
}
