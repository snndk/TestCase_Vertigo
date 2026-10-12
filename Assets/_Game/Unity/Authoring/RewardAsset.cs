using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Reward")]
    public class RewardAsset : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private long increasePerZone = 1;

        public string Id => rewardId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;

        public RewardDefinition CreateDefinition()
        {
            return new RewardDefinition(rewardId, increasePerZone);
        }
    }
}
