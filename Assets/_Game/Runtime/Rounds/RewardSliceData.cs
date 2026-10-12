using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSliceData : SliceData
    {
        public Reward Reward { get; }

        public RewardSliceData(string id, Reward reward) : base(id)
        {
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
        }
    }
}
