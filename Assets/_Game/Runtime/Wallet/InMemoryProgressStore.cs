using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Wallet
{
    public class InMemoryProgressStore : IProgressStore
    {
        private RewardCollection m_Progress;

        public InMemoryProgressStore()
            : this(RewardCollection.Empty)
        {
        }

        public InMemoryProgressStore(RewardCollection initialProgress)
        {
            m_Progress = initialProgress ?? throw new ArgumentNullException(nameof(initialProgress));
        }

        public RewardCollection Load()
        {
            return m_Progress;
        }

        public bool TrySave(RewardCollection updatedProgress)
        {
            m_Progress = updatedProgress ?? throw new ArgumentNullException(nameof(updatedProgress));
            return true;
        }
    }
}
