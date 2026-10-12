using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class ProgressServiceTests
    {
        [Test]
        public void LoadFailureIsReportedWithoutReplacingStoredProgress()
        {
            var initial = new RewardCollection(new[] { new Reward("gold", 100) });
            var store = new TestProgressStore
            {
                Progress = initial,
                DuringLoad = () => throw new InvalidOperationException("Unreadable progress")
            };

            Assert.Throws<InvalidOperationException>(() => new ProgressService(store));
            Assert.That(store.Writes, Is.Zero);
            Assert.That(store.Progress, Is.SameAs(initial));
        }

        [Test]
        public void NullLoadIsRejectedWithoutWritingAnEmptySave()
        {
            var store = new TestProgressStore { DuringLoad = () => null };
            Assert.Throws<InvalidOperationException>(() => new ProgressService(store));
            Assert.That(store.Writes, Is.Zero);
        }

        [Test]
        public void FailedWritePreservesBothSnapshotsAndRetryAddsEveryReward()
        {
            var initial = new RewardCollection(new[] { new Reward("gold", 100) });
            var store = new TestProgressStore { Progress = initial, FailWrites = true };
            var service = new ProgressService(store);
            var collected = new RewardCollection(new[] { new Reward("gold", 10), new Reward("ticket", 2) });

            Assert.That(service.TryCollect(collected), Is.False);
            Assert.That(service.Rewards, Is.SameAs(initial));
            Assert.That(store.Progress, Is.SameAs(initial));

            store.FailWrites = false;
            Assert.That(service.TryCollect(collected), Is.True);
            Assert.That(service.Rewards.GetAmount("gold"), Is.EqualTo(110));
            Assert.That(service.Rewards.GetAmount("ticket"), Is.EqualTo(2));
            Assert.That(store.Progress, Is.SameAs(service.Rewards));
            Assert.That(initial.GetAmount("gold"), Is.EqualTo(100));
        }

        [Test]
        public void UnexpectedStoreFailurePropagatesAndAllowsRetry()
        {
            var store = new TestProgressStore();
            var service = new ProgressService(store);
            var before = service.Rewards;
            var collected = new RewardCollection(new[] { new Reward("gold", 10) });
            store.DuringSave = () => throw new InvalidOperationException("Store failure");

            Assert.Throws<InvalidOperationException>(() => service.TryCollect(collected));
            Assert.That(service.Rewards, Is.SameAs(before));
            Assert.That(store.Progress, Is.SameAs(before));

            store.DuringSave = null;
            Assert.That(service.TryCollect(collected), Is.True);
            Assert.That(service.Rewards.GetAmount("gold"), Is.EqualTo(10));
        }

        [Test]
        public void OverflowIsRejectedBeforeWriting()
        {
            var initial = new RewardCollection(new[] { new Reward("gold", long.MaxValue) });
            var store = new TestProgressStore { Progress = initial };
            var service = new ProgressService(store);

            Assert.Throws<OverflowException>(() =>
                service.TryCollect(new RewardCollection(new[] { new Reward("gold", 1) })));
            Assert.That(store.Writes, Is.Zero);
            Assert.That(service.Rewards, Is.SameAs(initial));
        }

        [Test]
        public void ReentrantCollectionCannotOverwriteTheInFlightWrite()
        {
            var store = new TestProgressStore();
            var service = new ProgressService(store);
            var reward = new RewardCollection(new[] { new Reward("gold", 10) });
            store.DuringSave = () =>
                Assert.Throws<InvalidOperationException>(() => service.TryCollect(reward));

            Assert.That(service.TryCollect(reward), Is.True);
            Assert.That(service.Rewards.GetAmount("gold"), Is.EqualTo(10));
            Assert.That(store.Writes, Is.EqualTo(1));
        }
    }
}
