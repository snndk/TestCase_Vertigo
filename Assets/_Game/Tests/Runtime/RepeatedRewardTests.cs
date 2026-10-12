using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class RepeatedRewardTests
    {
        [TestCase(1, 10, 20)]
        [TestCase(5, 14, 24)]
        [TestCase(6, 15, 25)]
        [TestCase(30, 39, 49)]
        [TestCase(31, 40, 50)]
        public void SameRewardHasIndependentAmountsAndSharedGrowth(int zone, long first, long second)
        {
            var round = new RoundGenerator(CreateConfiguration(), new SequenceRandom()).Generate(zone);
            Assert.That(((RewardSlice)round.Slices[0]).Reward.Amount, Is.EqualTo(first));
            Assert.That(((RewardSlice)round.Slices[1]).Reward.Amount, Is.EqualTo(second));
            Assert.That(round.Slices[0].Id, Is.EqualTo(round.Slices[1].Id));
        }

        [Test]
        public void ReusedDefinitionCreatesDistinctSlotsThatCanBothBeSelected()
        {
            var configuration = CreateConfiguration();
            var definitions = configuration.GetSlices(WheelTier.Silver);
            Assert.That(definitions[0], Is.SameAs(definitions[2]));
            var round = new RoundGenerator(configuration, new SequenceRandom()).Generate(5);
            Assert.That(round.Slices[0], Is.Not.SameAs(round.Slices[2]));
            var selector = new SpinSelector(CreateRandom(0, 2));
            Assert.That(selector.SelectSlice(round), Is.EqualTo(0));
            Assert.That(selector.SelectSlice(round), Is.EqualTo(2));
        }

        [Test]
        public void WinningSlotDeterminesAmountAndCashOutCombinesTheRewardIdentity()
        {
            var session = new GameSession(CreateConfiguration(), CreateRandom(0, 1, 0, 1), new InMemoryProgressStore());
            var expected = new long[] { 10, 21, 12, 23 };
            for (var index = 0; index < expected.Length; index++)
            {
                var spin = session.RequestSpin().Result;
                Assert.That(spin.WinningIndex, Is.EqualTo(index % 2));
                Assert.That(((RewardSlice)spin.WinningSlice).Reward.Amount, Is.EqualTo(expected[index]));
                session.CompleteResolution(spin.OperationId);
            }
            Assert.That(session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(1));
            Assert.That(session.GetSnapshot().Progress.GetAmount("gold"), Is.EqualTo(66));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void SliceAmountsMustBePositive(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), amount));
        }

        [Test]
        public void SliceGrowthOverflowIsRejected()
        {
            var slice = new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), long.MaxValue);
            Assert.Throws<OverflowException>(() => slice.CreateSlice(2));
        }

        private static SequenceRandom CreateRandom(params int[] indices)
        {
            var random = new SequenceRandom();
            foreach (var index in indices)
                random.Enqueue(index);
            return random;
        }

        private static WheelConfiguration CreateConfiguration()
        {
            var reward = new RewardDefinition("gold", 1);
            var small = new RewardSliceDefinition("gold", reward, 10);
            var large = new RewardSliceDefinition("gold", reward, 20);
            var special = new RewardSliceDefinition("special", new RewardDefinition("special", 1), 1, true);
            return new WheelConfiguration(
                new SliceDefinition[] { small, large, new BombSliceDefinition("bomb") },
                new[] { small, large, small },
                new[] { small, large, special });
        }
    }
}
