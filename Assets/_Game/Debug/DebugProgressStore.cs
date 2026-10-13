using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Debug._Game.Debug
{
    public class DebugProgressStore : IProgressStore
    {
        private readonly IProgressStore m_Store;
        public bool FailNextSave { get; set; }

        public DebugProgressStore(IProgressStore store)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public RewardCollection Load() => m_Store.Load();

        public bool TrySave(RewardCollection progress)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                return false;
            }
            return m_Store.TrySave(progress);
        }
    }
}
