using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Tests.Runtime
{
    internal class TestProgressStore : IProgressStore
    {
        public RewardCollection Progress = new RewardCollection(Array.Empty<Reward>());
        public bool FailWrites;
        public int Writes;
        public Action DuringSave;
        public Func<RewardCollection> DuringLoad;

        public RewardCollection Load()
        {
            return DuringLoad == null ? Progress : DuringLoad();
        }

        public bool TrySave(RewardCollection progress)
        {
            Writes++;
            DuringSave?.Invoke();
            if (FailWrites)
                return false;

            Progress = progress;
            return true;
        }
    }
}
