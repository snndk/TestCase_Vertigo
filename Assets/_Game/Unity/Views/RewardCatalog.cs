using System.Collections.Generic;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardCatalog
    {
        private readonly Dictionary<string, RewardAsset> m_Rewards = new Dictionary<string, RewardAsset>();

        public RewardCatalog(WheelConfigurationAsset configuration)
        {
            foreach (var reward in configuration.GetRewards())
                m_Rewards.Add(reward.Id, reward);
        }

        public RewardAsset Find(string id) => m_Rewards.TryGetValue(id, out var reward) ? reward : null;
    }
}
