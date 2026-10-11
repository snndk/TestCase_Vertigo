using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RewardSlice : RoundSlice
    {
        public Reward Reward { get; }
        public bool IsSpecial { get; }

        public RewardSlice(string id, Reward reward, bool isSpecial = false) : base(id)
        {
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            IsSpecial = isSpecial;
        }

        protected override SliceResolution ResolveCore(RewardCollection loot)
        {
            var updatedLoot = loot.Add(Reward);
            return SliceResolution.ContinueWith(updatedLoot);
        }
    }
}
