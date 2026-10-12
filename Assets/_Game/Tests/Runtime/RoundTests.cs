using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class RoundTests
    {
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(30)]
        public void GeneratedBombCannotBypassDefinitionValidation(int zone)
        {
            var configuration = WithExtraSlice(ZoneRules.GetTier(zone), new UnexpectedBombDefinition("extra"));
            var generator = new RoundGenerator(configuration, new SequenceRandom());

            Assert.Throws<InvalidOperationException>(() => generator.Generate(zone));
        }

        [Test]
        public void GeneratedSliceMustRetainItsConfiguredId()
        {
            var configuration = WithExtraSlice(WheelTier.Silver, new UnexpectedBombDefinition("extra", "different"));
            Assert.Throws<InvalidOperationException>(() => new RoundGenerator(configuration, new SequenceRandom()).Generate(5));
        }

        [TestCase(4, 5)]
        [TestCase(5, 6)]
        [TestCase(29, 30)]
        [TestCase(30, 31)]
        public void SharedRewardGrowsAcrossTierTransitions(int fromZone, int toZone)
        {
            var generator = new RoundGenerator(TestWheelFactory.Create(), new SequenceRandom());
            var before = generator.Generate(fromZone).Slices.OfType<RewardSliceData>()
                .Single(slice => slice.Reward.RewardId == "gold").Reward;
            var after = generator.Generate(toZone).Slices.OfType<RewardSliceData>()
                .Single(slice => slice.Reward.RewardId == "gold").Reward;

            Assert.That(before.Amount, Is.EqualTo(10 + fromZone - 1));
            Assert.That(after.Amount, Is.EqualTo(before.Amount + 1));
        }

        [TestCase(10, 2)]
        public void SameRewardCannotHaveDifferentTierGrowth(long amount, long increase)
        {
            var configuration = TestWheelFactory.Create();
            var silver = configuration.GetSlices(WheelTier.Silver).ToArray();
            silver[0] = new RewardSliceDefinition("gold", new RewardDefinition("gold", increase), amount);

            Assert.Throws<ArgumentException>(() => new WheelConfiguration(
                configuration.GetSlices(WheelTier.Bronze), silver, configuration.GetSlices(WheelTier.Gold)));
        }

        [TestCase(1, WheelTier.Bronze)]
        [TestCase(4, WheelTier.Bronze)]
        [TestCase(5, WheelTier.Silver)]
        [TestCase(10, WheelTier.Silver)]
        [TestCase(25, WheelTier.Silver)]
        [TestCase(30, WheelTier.Gold)]
        [TestCase(35, WheelTier.Silver)]
        [TestCase(60, WheelTier.Gold)]
        [TestCase(90, WheelTier.Gold)]
        [TestCase(1000, WheelTier.Silver)]
        public void TierDeterminesActualBombContents(int zone, WheelTier expected)
        {
            var round = new RoundGenerator(TestWheelFactory.Create(), new SequenceRandom()).Generate(zone);
            Assert.That(round.Tier, Is.EqualTo(expected));
            Assert.That(round.Slices.OfType<BombSliceData>().Count(), Is.EqualTo(expected == WheelTier.Bronze ? 1 : 0));
            Assert.That(round.Slices.OfType<RewardSliceData>().Any(s => s.IsSpecial), Is.EqualTo(expected == WheelTier.Gold));
        }

        [Test]
        public void RewardsGrowForEachTier()
        {
            var generator = new RoundGenerator(TestWheelFactory.Create(), new SequenceRandom());
            foreach (var zones in new[] { new[] { 1, 2 }, new[] { 5, 10 }, new[] { 30, 60 } })
            {
                var first = generator.Generate(zones[0]);
                var later = generator.Generate(zones[1]);
                for (var index = 0; index < first.Slices.Count; index++)
                {
                    if (first.Slices[index] is RewardSliceData firstReward)
                        Assert.That(((RewardSliceData)later.Slices[index]).Reward.Amount, Is.GreaterThan(firstReward.Reward.Amount));
                }
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidZoneIsRejected(int zone)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoneRules.GetTier(zone));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonGrowingRewardsAreRejected(long increase)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewardSliceDefinition("a", new RewardDefinition("gold", increase), 1));
        }

        [Test]
        public void ConfigurationAndSnapshotsCannotBeChangedThroughTheirCollections()
        {
            var valid = TestWheelFactory.Create();
            var bronze = valid.GetSlices(WheelTier.Bronze).ToArray();
            var config = new WheelConfiguration(bronze, valid.GetSlices(WheelTier.Silver), valid.GetSlices(WheelTier.Gold));
            bronze[0] = new BombSliceDefinition("replacement");
            Assert.That(config.GetSlices(WheelTier.Bronze)[0], Is.TypeOf<RewardSliceDefinition>());

            var slices = (IList<SliceDefinition>)config.GetSlices(WheelTier.Bronze);
            Assert.Throws<NotSupportedException>(() => slices[0] = bronze[0]);
            var round = new RoundGenerator(config, new SequenceRandom()).Generate(1);
            Assert.Throws<NotSupportedException>(() => ((IList<SliceData>)round.Slices).Clear());

            var rewards = new[] { new Reward("gold", 5) };
            var collection = new RewardCollection(rewards);
            rewards[0] = new Reward("gold", 99);
            Assert.That(collection.GetAmount("gold"), Is.EqualTo(5));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, long>)collection.Amounts)["gold"] = 99);
        }

        [TestCase(WheelTier.Bronze, 0)]
        [TestCase(WheelTier.Bronze, 2)]
        [TestCase(WheelTier.Silver, 1)]
        [TestCase(WheelTier.Gold, 1)]
        public void InvalidBombCountsAreRejected(WheelTier tier, int bombCount)
        {
            var valid = TestWheelFactory.Create();
            var changed = valid.GetSlices(tier).ToArray();
            for (var i = 0; i < changed.Length; i++)
                changed[i] = i < bombCount ? new BombSliceDefinition("bomb" + i) :
                    new RewardSliceDefinition("reward" + i, new RewardDefinition("exclusive" + i, 1), 1, tier == WheelTier.Gold);

            Assert.Throws<ArgumentException>(() => new WheelConfiguration(
                tier == WheelTier.Bronze ? changed : valid.GetSlices(WheelTier.Bronze),
                tier == WheelTier.Silver ? changed : valid.GetSlices(WheelTier.Silver),
                tier == WheelTier.Gold ? changed : valid.GetSlices(WheelTier.Gold)));
        }

        [Test]
        public void GoldMustContainAnExclusiveSpecialReward()
        {
            var valid = TestWheelFactory.Create();
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(valid.GetSlices(WheelTier.Bronze),
                valid.GetSlices(WheelTier.Silver), valid.GetSlices(WheelTier.Silver)));

            var gold = valid.GetSlices(WheelTier.Gold).ToArray();
            gold[2] = new RewardSliceDefinition("special", new RewardDefinition("gold", 1), 10, true);
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(valid.GetSlices(WheelTier.Bronze),
                valid.GetSlices(WheelTier.Silver), gold));
        }

        [Test]
        public void MismatchedCapacityIsRejected()
        {
            var valid = TestWheelFactory.Create();
            var larger = valid.GetSlices(WheelTier.Silver).Concat(new[]
            {
                new RewardSliceDefinition("extra", new RewardDefinition("gold", 1), 10)
            });
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(valid.GetSlices(WheelTier.Bronze),
                larger, valid.GetSlices(WheelTier.Gold)));
        }

        private static WheelConfiguration WithExtraSlice(WheelTier target, SliceDefinition extra)
        {
            var standard = TestWheelFactory.Create();
            var ordinary = new RewardSliceDefinition("extra", new RewardDefinition("gold", 1), 10);
            return new WheelConfiguration(
                standard.GetSlices(WheelTier.Bronze).Concat(new[] { target == WheelTier.Bronze ? extra : ordinary }),
                standard.GetSlices(WheelTier.Silver).Concat(new[] { target == WheelTier.Silver ? extra : ordinary }),
                standard.GetSlices(WheelTier.Gold).Concat(new[] { target == WheelTier.Gold ? extra : ordinary }));
        }
    }
}
