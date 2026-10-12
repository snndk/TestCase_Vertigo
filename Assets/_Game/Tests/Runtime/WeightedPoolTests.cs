using System;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class WeightedPoolTests
    {
        [TestCase(0, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 1)]
        [TestCase(39, 1)]
        public void RelativeWeightsDetermineSelectionIntervals(int roll, int expectedIndex)
        {
            var random = new SequenceRandom();
            random.Enqueue(roll);
            var selected = new WheelPoolSelector(random).Select(new[] { RewardEntry(10), RewardEntry(30) }, 1);
            Assert.That(selected, Is.EqualTo(new[] { expectedIndex }));
        }

        [Test]
        public void GuaranteedEntriesAreIncludedAndZeroWeightIsExcluded()
        {
            var random = new SequenceRandom();
            var selected = new WheelPoolSelector(random).Select(
                new[] { RewardEntry(0), RewardEntry(100), RewardEntry(5), RewardEntry(100) }, 3);
            Assert.That(selected, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RepeatedAssetEntriesRemainSeparateCandidatesWithoutReplacement()
        {
            var entry = RewardEntry(20);
            var selected = new WheelPoolSelector(new SequenceRandom()).Select(new[] { entry, entry, entry }, 2);
            Assert.That(selected, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void FiftyCandidatesProduceEightSlotsWithTheirOriginalPoolIndices()
        {
            var pool = Enumerable.Range(1, 50).Select(RewardEntry).ToArray();
            var random = new SequenceRandom();
            var total = 1275;
            for (var weight = 50; weight > 42; weight--)
            {
                random.Enqueue(total - 1);
                total -= weight;
            }
            var round = new RoundGenerator(Configuration(8, pool), random).Generate(5);
            Assert.That(round.Slices.Count, Is.EqualTo(8));
            Assert.That(round.SourceIndices, Is.EqualTo(Enumerable.Range(42, 8)));
            Assert.That(round.Slices.All(slice => slice is RewardSlice), Is.True);
            Assert.That(random.Calls, Is.EqualTo(8));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void OutOfRangeWeightsAreRejected(int weight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardEntry(weight));
        }

        [TestCase(-1)]
        [TestCase(40)]
        public void InvalidRandomRollsAreRejected(int roll)
        {
            var random = new SequenceRandom();
            random.Enqueue(roll);
            Assert.Throws<InvalidOperationException>(() =>
                new WheelPoolSelector(random).Select(new[] { RewardEntry(10), RewardEntry(30) }, 1));
        }

        [Test]
        public void TooManyGuaranteedOrTooFewEligibleEntriesAreRejectedBeforePlay()
        {
            Assert.Throws<ArgumentException>(() => Configuration(3,
                new[] { RewardEntry(100), RewardEntry(100), RewardEntry(100), RewardEntry(100) }));
            Assert.Throws<ArgumentException>(() => Configuration(3,
                new[] { RewardEntry(100), RewardEntry(10), RewardEntry(0) }));
        }

        [Test]
        public void BronzeBombMustBeGuaranteedAndSafeWheelsCannotIncludeBombs()
        {
            var valid = Configuration(3, new[] { RewardEntry(10), RewardEntry(20), RewardEntry(30) });
            var bronze = valid.GetCandidates(WheelTier.Bronze).ToArray();
            bronze[2] = new WeightedSlice(new BombSliceDefinition("bomb"), 99);
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(3, bronze,
                valid.GetCandidates(WheelTier.Silver), valid.GetCandidates(WheelTier.Gold)));
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(3,
                valid.GetCandidates(WheelTier.Bronze), valid.GetCandidates(WheelTier.Bronze),
                valid.GetCandidates(WheelTier.Gold)));
        }

        [Test]
        public void GoldMustGuaranteeASpecialReward()
        {
            var valid = Configuration(3, new[] { RewardEntry(10), RewardEntry(20), RewardEntry(30) });
            var gold = valid.GetCandidates(WheelTier.Gold).ToArray();
            gold[2] = new WeightedSlice(gold[2].Definition, 99);
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(3,
                valid.GetCandidates(WheelTier.Bronze), valid.GetCandidates(WheelTier.Silver), gold));
        }

        [Test]
        public void DisabledCandidatesDoNotParticipateInCrossTierGameplayRules()
        {
            var valid = Configuration(3, new[] { RewardEntry(100), RewardEntry(100), RewardEntry(100) });
            var disabledSpecialIdentity = new WeightedSlice(new RewardSliceDefinition("disabled-special",
                new RewardDefinition("special", 99), 1), 0);
            var disabledGrowthConflict = new WeightedSlice(new RewardSliceDefinition("disabled-growth",
                new RewardDefinition("gold", 99), 1), 0);
            var bronze = valid.GetCandidates(WheelTier.Bronze).Concat(new[] { disabledSpecialIdentity, disabledGrowthConflict });
            var configuration = new WheelConfiguration(3, bronze, valid.GetCandidates(WheelTier.Silver), valid.GetCandidates(WheelTier.Gold));
            var generator = new RoundGenerator(configuration, new SequenceRandom());
            Assert.That(generator.Generate(1).SourceIndices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(generator.Generate(30).Slices.OfType<RewardSlice>().Any(slice => slice.IsSpecial), Is.True);
        }

        [Test]
        public void EnablingAConflictingCandidateStillRejectsTheConfiguration()
        {
            var valid = Configuration(3, new[] { RewardEntry(100), RewardEntry(100), RewardEntry(100) });
            var bronze = valid.GetCandidates(WheelTier.Bronze).ToArray();
            bronze[0] = new WeightedSlice(new RewardSliceDefinition("special-in-bronze",
                new RewardDefinition("special", 1), 1), 100);
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(3, bronze,
                valid.GetCandidates(WheelTier.Silver), valid.GetCandidates(WheelTier.Gold)));
        }

        private static WeightedSlice RewardEntry(int weight)
        {
            return new WeightedSlice(new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), 10), weight);
        }

        private static WheelConfiguration Configuration(int count, WeightedSlice[] silver)
        {
            var rewards = Enumerable.Range(0, count - 1).Select(_ => RewardEntry(100)).ToArray();
            return new WheelConfiguration(count,
                rewards.Concat(new[] { new WeightedSlice(new BombSliceDefinition("bomb"), 100) }), silver,
                rewards.Concat(new[] { new WeightedSlice(new RewardSliceDefinition("special",
                    new RewardDefinition("special", 1), 1, true), 100) }));
        }
    }
}
