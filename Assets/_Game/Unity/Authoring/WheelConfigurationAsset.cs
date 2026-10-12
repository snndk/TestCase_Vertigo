using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Wheel configuration")]
    public class WheelConfigurationAsset : ScriptableObject
    {
        [SerializeField, Min(3)] private int sliceCount = 8;
        [SerializeField] private WheelTierAsset bronze;
        [SerializeField] private WheelTierAsset silver;
        [SerializeField] private WheelTierAsset gold;

        public WheelConfiguration CreateRuntime()
        {
            GetRewards();
            return new WheelConfiguration(sliceCount, bronze.CreateCandidates(), silver.CreateCandidates(), gold.CreateCandidates());
        }

        public IReadOnlyList<RewardAsset> GetRewards()
        {
            if (bronze == null || silver == null || gold == null)
                throw new InvalidOperationException("Every wheel tier must be assigned.");

            var rewards = new Dictionary<string, RewardAsset>(StringComparer.Ordinal);
            foreach (WheelTier tier in Enum.GetValues(typeof(WheelTier)))
            {
                var asset = GetTier(tier);
                for (var index = 0; index < asset.CandidateCount; index++)
                {
                    var slice = asset.GetSlice(index);
                    if (slice == null)
                        throw new InvalidOperationException(tier + " has a missing slice at " + index + ".");
                    foreach (var reward in slice.GetRewards())
                    {
                        if (reward == null || string.IsNullOrWhiteSpace(reward.Id))
                            throw new InvalidOperationException(tier + " has a missing reward or reward identity.");
                        if (rewards.TryGetValue(reward.Id, out var existing) && existing != reward)
                            throw new InvalidOperationException("Reward '" + reward.Id + "' must use the same RewardAsset in every tier.");
                        rewards[reward.Id] = reward;
                    }
                }
            }
            return new List<RewardAsset>(rewards.Values).AsReadOnly();
        }

        public WheelTierAsset GetTier(WheelTier tier)
        {
            switch (tier)
            {
                case WheelTier.Bronze: return bronze;
                case WheelTier.Silver: return silver;
                case WheelTier.Gold: return gold;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }
    }
}
