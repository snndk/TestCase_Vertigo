using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSliceDefinition : SliceDefinition
    {
        public RewardDefinition Reward { get; }
        public bool IsSpecial { get; }

        public RewardSliceDefinition(string id, RewardDefinition reward, bool isSpecial = false) : base(id)
        {
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            IsSpecial = isSpecial;
        }

        protected override RoundSlice CreateSliceCore(int zone)
        {
            return new RewardSlice(Id, Reward.AtZone(zone), IsSpecial);
        }
    }
}
