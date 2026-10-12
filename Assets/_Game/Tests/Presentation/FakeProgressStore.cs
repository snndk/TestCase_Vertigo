using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FakeProgressStore : IProgressStore
    {
        public RewardCollection Saved { get; private set; } = RewardCollection.Empty;
        public bool FailWrites { get; set; }
        public Exception WriteFailure { get; set; }
        public int Writes { get; private set; }

        public RewardCollection Load()
        {
            return Saved;
        }

        public bool TrySave(RewardCollection progress)
        {
            Writes++;
            if (WriteFailure != null)
                throw WriteFailure;
            if (FailWrites)
                return false;

            Saved = progress;
            return true;
        }
    }
}
