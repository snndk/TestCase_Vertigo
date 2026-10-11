using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class CashOutResult
    {
        public Guid RunId { get; }
        public int Zone { get; }
        public RewardCollection Rewards { get; }

        internal CashOutResult(Guid runId, int zone, RewardCollection rewards)
        {
            RunId = runId;
            Zone = zone;
            Rewards = rewards;
        }
    }
}
