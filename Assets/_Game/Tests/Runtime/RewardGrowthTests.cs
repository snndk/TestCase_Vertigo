using System;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class RewardGrowthTests
    {
        [TestCase(2)]
        [TestCase(10)]
        public void GrowthThatAllowsADecreaseOrTieIsRejected(long increase)
        {
            var error = Assert.Throws<ArgumentException>(() => CreateConfiguration(increase));
            Assert.That(error.Message, Does.Contain("gold").And.Contain("11"));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(59)]
        [TestCase(60)]
        public void SmallestNextRewardExceedsLargestPreviousReward(int zone)
        {
            var generator = new RoundGenerator(CreateConfiguration(11), new SequenceRandom());
            var before = generator.Generate(zone).Slices.OfType<RewardSliceData>()
                .Where(slice => slice.Reward.RewardId == "gold").Max(slice => slice.Reward.Amount);
            var after = generator.Generate(zone + 1).Slices.OfType<RewardSliceData>()
                .Where(slice => slice.Reward.RewardId == "gold").Min(slice => slice.Reward.Amount);
            Assert.That(after, Is.GreaterThan(before));
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        public void EvenRareCandidatesParticipateButDisabledCandidatesDoNot(int weight, bool rejects)
        {
            var valid = CreateConfiguration(11);
            var large = new WeightedSlice(new RewardSliceDefinition("rare-gold",
                new RewardDefinition("gold", 11), 1000), weight);
            TestDelegate create = () => new WheelConfiguration(3,
                valid.GetCandidates(WheelTier.Bronze).Concat(new[] { large }),
                valid.GetCandidates(WheelTier.Silver), valid.GetCandidates(WheelTier.Gold));
            if (rejects)
                Assert.Throws<ArgumentException>(create);
            else
                Assert.DoesNotThrow(create);
        }

        [Test]
        public void SeparateTierVariantsMustAlsoShareANonOverlappingRange()
        {
            var valid = CreateConfiguration(11);
            var silver = valid.GetCandidates(WheelTier.Silver).ToArray();
            silver[0] = new WeightedSlice(new RewardSliceDefinition("silver-gold",
                new RewardDefinition("gold", 11), 30), 100);
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(3,
                valid.GetCandidates(WheelTier.Bronze), silver, valid.GetCandidates(WheelTier.Gold)));
        }

        [Test]
        public void ValidationHandlesTheLargestPositiveBaseRangeWithoutOverflow()
        {
            var reward = new RewardDefinition("gold", long.MaxValue);
            var small = new RewardSliceDefinition("small", reward, 1);
            var large = new RewardSliceDefinition("large", reward, long.MaxValue);
            Assert.DoesNotThrow(() => CreateConfiguration(small, large));
            Assert.Throws<OverflowException>(() => small.CreateData(2));
        }

        private static WheelConfiguration CreateConfiguration(long increase)
        {
            var reward = new RewardDefinition("gold", increase);
            return CreateConfiguration(new RewardSliceDefinition("small", reward, 10),
                new RewardSliceDefinition("large", reward, 20));
        }

        private static WheelConfiguration CreateConfiguration(RewardSliceDefinition small, RewardSliceDefinition large)
        {
            var special = new RewardSliceDefinition("special", new RewardDefinition("special", 1), 1);
            return new WheelConfiguration(
                new SliceDefinition[] { small, large, new BombSliceDefinition("bomb") },
                new[] { small, large, small }, new[] { small, large, special });
        }
    }
}
