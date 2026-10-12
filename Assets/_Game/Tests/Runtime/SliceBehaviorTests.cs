using System;
using System.Linq;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class SliceBehaviorTests
    {
        [TestCase("gold")]
        [TestCase("cash")]
        public void RewardSliceAddsThroughContextWithoutMutatingEarlierLoot(string rewardId)
        {
            RewardCollection previous = null;
            var session = SliceContextTestFactory.Create(context =>
            {
                context.AddReward(new Reward(rewardId, 3));
                previous = context.Loot;
                new RewardSlice(context, new Reward(rewardId, 7)).Resolve();
            });
            var spin = session.RequestSpin().Result;
            Assert.That(session.GetSnapshot().RunLoot.GetAmount(rewardId), Is.EqualTo(10));
            Assert.That(previous.GetAmount(rewardId), Is.EqualTo(3));
            session.CompleteResolution(spin.OperationId);
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(2));
        }

        [Test]
        public void BombSliceLosesThroughContextWithoutMutatingEarlierLoot()
        {
            RewardCollection previous = null;
            var session = SliceContextTestFactory.Create(context =>
            {
                context.AddReward(new Reward("gold", 3));
                context.AddReward(new Reward("cash", 5));
                previous = context.Loot;
                new BombSlice(context).Resolve();
            });
            var spin = session.RequestSpin().Result;
            Assert.That(session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(previous.Amounts.Count, Is.EqualTo(2));
            session.CompleteResolution(spin.OperationId);
            Assert.That(session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
        }

        [Test]
        public void RewardDataCannotRepresentBombByAcceptingNullReward()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardSliceData("reward", null));
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
                    new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), 10),
                    new RewardSliceDefinition("ticket", new RewardDefinition("ticket", 1), 2),
                    new BombSliceDefinition("bomb")
                },
                standard.GetSlices(WheelTier.Silver).Concat(new[]
                {
                    new RewardSliceDefinition("extra", new RewardDefinition("gold", 1), 10)
                }),
                standard.GetSlices(WheelTier.Gold).Concat(new[]
                {
                    new RewardSliceDefinition("extra", new RewardDefinition("gold", 1), 10)
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
                    new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), 10),
                    new BombSliceDefinition("bomb")
                },
                standard.GetSlices(WheelTier.Silver),
                standard.GetSlices(WheelTier.Gold)));
        }
    }
}
