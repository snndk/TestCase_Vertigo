using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Slices/Reward")]
    public class RewardSliceAsset : SliceAsset
    {
        [SerializeField] private RewardAsset reward;
        [SerializeField, Min(1)] private long baseAmount = 1;
        public RewardAsset Reward => reward;
        public override Sprite Icon => reward != null ? reward.Icon : null;

        public override IEnumerable<RewardAsset> GetRewards()
        {
            yield return reward;
        }

        public override SliceDefinition CreateDefinition()
        {
            if (reward == null)
                throw new InvalidOperationException(name + " needs a reward asset.");
            return new RewardSliceDefinition(Id, reward.CreateDefinition(), baseAmount);
        }

        public override string GetCaption(SliceData slice)
        {
            return "x" + ((RewardSliceData)slice).Reward.Amount.ToString("N0");
        }
    }
}
