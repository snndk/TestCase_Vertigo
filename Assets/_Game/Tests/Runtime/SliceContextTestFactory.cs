using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal static class SliceContextTestFactory
    {
        internal static GameSession Create(Action<SliceContext> action, Action<RoundSlice> capture = null,
            IRandomSource random = null, long baseAmount = 10)
        {
            var coins = new RewardSliceDefinition("coins", new RewardDefinition("coins", 1), baseAmount);
            var tickets = new RewardSliceDefinition("tickets", new RewardDefinition("tickets", 1), 2);
            var custom = new TestSliceDefinition(action, capture);
            var configuration = new WheelConfiguration(
                new SliceDefinition[] { custom, coins, tickets, new BombSliceDefinition("bomb") },
                new SliceDefinition[] { custom, coins, tickets, coins },
                new SliceDefinition[] { custom, coins, tickets,
                    new RewardSliceDefinition("special", new RewardDefinition("special", 1), 1) });
            return new GameSession(configuration, random ?? new SequenceRandom(), new InMemoryProgressStore());
        }
    }
}
