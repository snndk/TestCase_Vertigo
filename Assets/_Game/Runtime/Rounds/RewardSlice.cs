using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSlice : RoundSlice
    {
        private readonly Reward m_Reward;

        public RewardSlice(SliceContext context, Reward reward) : base(context)
        {
            m_Reward = reward ?? throw new ArgumentNullException(nameof(reward));
        }

        protected override void ResolveCore()
        {
            Context.AddReward(m_Reward);
            Context.ContinueToNextRound();
        }
    }
}
