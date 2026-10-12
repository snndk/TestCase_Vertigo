using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSliceDefinition : SliceDefinition
    {
        public RewardDefinition Reward { get; }
        public long BaseAmount { get; }

        public RewardSliceDefinition(string id, RewardDefinition reward, long baseAmount) : base(id)
        {
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            if (baseAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseAmount));
            BaseAmount = baseAmount;
        }

        protected override SliceData CreateDataCore(int zone)
        {
            return new RewardSliceData(Id, Reward.AtZone(zone, BaseAmount));
        }

        protected override RoundSlice CreateBehaviorCore(SliceData data, SliceContext context)
        {
            return new RewardSlice(context, ((RewardSliceData)data).Reward);
        }
    }
}
