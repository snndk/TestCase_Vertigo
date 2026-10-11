using System;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime
{
    public class SliceBehaviorTests
    {
        [TestCase("gold")]
        [TestCase("cash")]
        public void RewardSliceAddsItsRewardWithoutMutatingTheInput(string rewardId)
        {
            var loot = new RewardCollection(new[] { new Reward(rewardId, 3) });
            RoundSlice slice = new RewardSlice("reward", new Reward(rewardId, 7));

            var result = slice.Resolve(loot);

            Assert.That(result.Loot.GetAmount(rewardId), Is.EqualTo(10));
            Assert.That(loot.GetAmount(rewardId), Is.EqualTo(3));
            Assert.That(result.ContinuesRun, Is.True);
        }

        [Test]
        public void BombSliceProducesLossWithoutChangingTheInputLoot()
        {
            var loot = new RewardCollection(new[] { new Reward("gold", 3), new Reward("cash", 5) });
            RoundSlice slice = new BombSlice("bomb");

            var result = slice.Resolve(loot);

            Assert.That(result.Loot.Amounts, Is.Empty);
            Assert.That(result.ContinuesRun, Is.False);
            Assert.That(loot.Amounts.Count, Is.EqualTo(2));
        }

        [Test]
        public void RewardSliceCannotRepresentBombByAcceptingNullReward()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardSlice("reward", null));
        }

        [Test]
        public void SessionResolvesAnAdditionalSliceBehaviorExactlyOnce()
        {
            var calls = 0;
            var standard = TestWheelFactory.Create();
            var configuration = new WheelConfiguration(
                new SliceDefinition[]
                {
                    new TestSliceDefinition(() => calls++),
                    new RewardSliceDefinition("gold", new RewardDefinition("gold", 10, 1)),
                    new RewardSliceDefinition("ticket", new RewardDefinition("ticket", 2, 1)),
                    new BombSliceDefinition("bomb")
                },
                standard.GetSlices(WheelTier.Silver).Concat(new[]
                {
                    new RewardSliceDefinition("extra", new RewardDefinition("gold", 10, 1))
                }),
                standard.GetSlices(WheelTier.Gold).Concat(new[]
                {
                    new RewardSliceDefinition("extra", new RewardDefinition("gold", 10, 1))
                }));
            var session = new GameSession(configuration, new SequenceRandom(), new InMemoryProgressStore());

            var response = session.RequestSpin();
            Assert.That(response.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("test"), Is.EqualTo(7));
            Assert.That(session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
            Assert.That(session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(session.CompleteResolution(response.Result.OperationId), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(session.CompleteResolution(response.Result.OperationId), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("test"), Is.EqualTo(7));
        }

        [Test]
        public void AdditionalBehaviorDoesNotReplaceTheRequiredRewardSlices()
        {
            var standard = TestWheelFactory.Create();
            Assert.Throws<ArgumentException>(() => new WheelConfiguration(
                new SliceDefinition[]
                {
                    new TestSliceDefinition(() => { }),
                    new RewardSliceDefinition("gold", new RewardDefinition("gold", 10, 1)),
                    new BombSliceDefinition("bomb")
                },
                standard.GetSlices(WheelTier.Silver),
                standard.GetSlices(WheelTier.Gold)));
        }
    }
}
