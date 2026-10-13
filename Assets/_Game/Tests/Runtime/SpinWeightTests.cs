using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class SpinWeightTests
    {
        [Test]
        public void EveryRollMapsToItsConfiguredWeightIncludingIntervalBoundaries()
        {
            var round = Generate(2, 5, 3);
            var random = new SequenceRandom();
            var selector = new SpinSelector(random);
            var counts = new int[3];
            for (var roll = 0; roll < 10; roll++)
            {
                random.Enqueue(roll);
                counts[selector.SelectSlice(round)]++;
            }
            Assert.That(counts, Is.EqualTo(new[] { 2, 5, 3 }));
            Assert.Throws<NotSupportedException>(() => ((IList<int>)round.SpinWeights)[0] = 99);
        }

        [Test]
        public void AutomaticEligibilityRemovesBombAndLuckyBeforeWeighting()
        {
            var source = TestWheelFactory.Create();
            var bronze = source.GetCandidates(WheelTier.Bronze)
                .Select((entry, index) => new WeightedSlice(entry.Definition, 100, index == 0 ? 2 : index == 1 ? 5 : 1000))
                .Concat(new[] { new WeightedSlice(new LuckySliceDefinition("lucky"), 100, 1000) });
            var safe = source.GetCandidates(WheelTier.Silver).Concat(new[] { source.GetCandidates(WheelTier.Silver)[0] });
            var configuration = new WheelConfiguration(4, bronze, safe, safe);
            var round = new RoundGenerator(configuration, new SequenceRandom()).Generate(1);
            var random = new SequenceRandom();
            var selector = new SpinSelector(random);
            var counts = new int[4];
            for (var roll = 0; roll < 7; roll++)
            {
                random.Enqueue(roll);
                counts[selector.SelectSlice(round, slice => slice is RewardSliceData)]++;
            }
            Assert.That(counts, Is.EqualTo(new[] { 2, 5, 0, 0 }));
        }

        [Test]
        public void PoolSelectionCarriesTheSelectedEntriesWeightsWithoutMergingIdenticalRewards()
        {
            var source = TestWheelFactory.Create();
            var definition = source.GetCandidates(WheelTier.Silver)[0].Definition;
            var pool = new[]
            {
                new WeightedSlice(definition, 0, 999),
                new WeightedSlice(definition, 10, 2),
                new WeightedSlice(definition, 20, 7),
                new WeightedSlice(definition, 30, 11)
            };
            var configuration = new WheelConfiguration(3, source.GetCandidates(WheelTier.Bronze), pool, pool);
            var round = new RoundGenerator(configuration, new SequenceRandom()).Generate(5);
            Assert.That(round.SourceIndices, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(round.SpinWeights, Is.EqualTo(new[] { 2, 7, 11 }));
            var random = new SequenceRandom();
            random.Enqueue(9);
            Assert.That(new SpinSelector(random).SelectSlice(round), Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void OutOfRangeRandomRollIsRejected(int roll)
        {
            var random = new SequenceRandom();
            random.Enqueue(roll);
            Assert.Throws<InvalidOperationException>(() => new SpinSelector(random).SelectSlice(Generate(2, 5, 3)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EveryDisplayedSliceMustHaveAPositiveWinningWeight(int weight)
        {
            var definition = TestWheelFactory.Create().GetCandidates(WheelTier.Silver)[0].Definition;
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightedSlice(definition, 100, weight));
        }

        [Test]
        public void WeightOverflowAndEmptyEligibilityFailBeforeRandomSelection()
        {
            var random = new SequenceRandom();
            var selector = new SpinSelector(random);
            Assert.Throws<OverflowException>(() => selector.SelectSlice(Generate(int.MaxValue, 1, 1)));
            Assert.Throws<InvalidOperationException>(() => selector.SelectSlice(Generate(1, 1, 1), slice => false));
            Assert.That(random.Calls, Is.Zero);
        }

        private static Round Generate(params int[] weights)
        {
            var source = TestWheelFactory.Create();
            var pool = source.GetCandidates(WheelTier.Silver)
                .Select((entry, index) => new WeightedSlice(entry.Definition, 100, weights[index]));
            var configuration = new WheelConfiguration(3, source.GetCandidates(WheelTier.Bronze), pool, pool);
            return new RoundGenerator(configuration, new SequenceRandom()).Generate(5);
        }
    }
}
