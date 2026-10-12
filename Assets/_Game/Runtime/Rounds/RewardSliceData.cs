using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSliceData : SliceData
    {
        public Reward Reward { get; }
        public bool IsSpecial { get; }

        public RewardSliceData(string id, Reward reward, bool isSpecial = false) : base(id)
        {
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            IsSpecial = isSpecial;
        }
    }
}
