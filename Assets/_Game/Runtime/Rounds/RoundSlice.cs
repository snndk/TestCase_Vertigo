using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public abstract class RoundSlice
    {
        public string Id { get; }

        protected RoundSlice(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A slice ID is required.", nameof(id));

            Id = id;
        }

        public SliceResolution Resolve(RewardCollection loot)
        {
            if (loot == null)
                throw new ArgumentNullException(nameof(loot));
            return ResolveCore(loot)
                ?? throw new InvalidOperationException("A slice must produce a resolution.");
        }

        protected abstract SliceResolution ResolveCore(RewardCollection loot);
    }
}
