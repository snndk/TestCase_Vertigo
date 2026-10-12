using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal static class TestWheelFactory
    {
        internal static WheelConfiguration Create(long goldBaseAmount = 10)
        {
            var gold = new RewardDefinition("gold", goldBaseAmount, 1);
            var ticket = new RewardDefinition("ticket", 2, 1);
            return new WheelConfiguration(
                new SliceDefinition[]
                {
                    new RewardSliceDefinition("gold", gold),
                    new RewardSliceDefinition("ticket", ticket),
                    new BombSliceDefinition("bomb")
                },
                new[]
                {
                    new RewardSliceDefinition("gold", gold),
                    new RewardSliceDefinition("ticket", ticket),
                    new RewardSliceDefinition("gem", new RewardDefinition("gem", 1, 1))
                },
                new[]
                {
                    new RewardSliceDefinition("gold", gold),
                    new RewardSliceDefinition("ticket", ticket),
                    new RewardSliceDefinition("special", new RewardDefinition("special", 1, 1), true)
                });
        }
    }
}
