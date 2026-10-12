using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Wallet
{
    public class ProgressService
    {
        private readonly IProgressStore m_Store;
        private bool m_IsWriting;

        public RewardCollection Rewards { get; private set; }

        public ProgressService(IProgressStore store)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
            Rewards = store.Load() ?? throw new InvalidOperationException("The progress store returned null.");
        }

        public bool TryCollect(RewardCollection rewards)
        {
            if (rewards == null)
                throw new ArgumentNullException(nameof(rewards));
            return TryCommit(Rewards.Combine(rewards));
        }

        public bool TrySpend(Reward cost)
        {
            return TryCommit(Rewards.Subtract(cost));
        }

        private bool TryCommit(RewardCollection updated)
        {
            if (m_IsWriting)
                throw new InvalidOperationException("A progress write is already in progress.");
            m_IsWriting = true;
            try
            {
                if (!m_Store.TrySave(updated))
                    return false;

                Rewards = updated;
                return true;
            }
            finally
            {
                m_IsWriting = false;
            }
        }
    }
}
