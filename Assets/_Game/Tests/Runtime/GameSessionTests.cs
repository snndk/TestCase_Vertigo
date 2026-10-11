using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Runtime
{
    public class GameSessionTests
    {
        private SequenceRandom m_Random;
        private TestProgressStore m_Store;
        private GameSession m_Session;

        [SetUp]
        public void SetUp()
        {
            m_Random = new SequenceRandom();
            m_Store = new TestProgressStore();
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store);
        }

        [Test]
        public void SpinAppliesRewardOnceAndCompletionAdvancesExactlyOnce()
        {
            var before = m_Session.GetSnapshot();
            var response = m_Session.RequestSpin();
            var pending = m_Session.GetSnapshot();

            Assert.That(response.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(response.Result.WinningIndex, Is.Zero);
            Assert.That(pending.Status, Is.EqualTo(SessionStatus.Resolving));
            Assert.That(pending.Round.Zone, Is.EqualTo(1));
            Assert.That(pending.RunLoot.GetAmount("gold"), Is.EqualTo(10));
            Assert.That(pending.PendingSpin, Is.SameAs(response.Result));
            Assert.That(before.RunLoot.Amounts, Is.Empty);

            Assert.That(m_Session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Random.Calls, Is.EqualTo(1));
            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.RunNotFinished));
            Assert.That(m_Session.CompleteResolution(response.Result.OperationId), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.CompleteResolution(response.Result.OperationId), Is.EqualTo(CommandStatus.StaleOperation));

            var after = m_Session.GetSnapshot();
            Assert.That(after.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(after.Round.Zone, Is.EqualTo(2));
            Assert.That(after.RunLoot.GetAmount("gold"), Is.EqualTo(10));
            Assert.That(after.PendingSpin, Is.Null);
            Assert.That(pending.Status, Is.EqualTo(SessionStatus.Resolving));
        }

        [Test]
        public void OldOrForeignOperationCannotCompleteCurrentSpin()
        {
            var previous = SpinAndComplete();
            var current = m_Session.RequestSpin().Result;

            Assert.That(m_Session.CompleteResolution(previous.OperationId), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(m_Session.CompleteResolution(new OperationId(Guid.NewGuid(), current.OperationId.Sequence)),
                Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(m_Session.CompleteResolution(default), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(m_Session.GetSnapshot().PendingSpin, Is.SameAs(current));
            Assert.That(m_Session.CompleteResolution(current.OperationId), Is.EqualTo(CommandStatus.Accepted));
        }

        [Test]
        public void BombClearsAllRunRewardsAndRestartKeepsPreviouslyCollectedProgress()
        {
            m_Store.Progress = new RewardCollection(new[] { new Reward("ticket", 9) });
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store);
            SpinAndComplete(0);
            SpinAndComplete(1);
            var before = m_Session.GetSnapshot();
            Assert.That(before.RunLoot.Amounts.Count, Is.EqualTo(2));

            m_Random.Enqueue(2);
            var bomb = m_Session.RequestSpin().Result;
            Assert.That(m_Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
            Assert.That(m_Session.CompleteResolution(bomb.OperationId), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(m_Session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));

            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.Accepted));
            var restarted = m_Session.GetSnapshot();
            Assert.That(restarted.RunId, Is.Not.EqualTo(before.RunId));
            Assert.That(restarted.Round.Zone, Is.EqualTo(1));
            Assert.That(restarted.Progress.GetAmount("ticket"), Is.EqualTo(9));
            Assert.That(restarted.RunLoot.Amounts, Is.Empty);

            var newSpin = m_Session.RequestSpin().Result;
            Assert.That(newSpin.OperationId.Sequence, Is.EqualTo(1));
            Assert.That(m_Session.CompleteResolution(bomb.OperationId), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(m_Session.GetSnapshot().PendingSpin, Is.SameAs(newSpin));
        }

        [Test]
        public void CashOutTransfersEveryRewardAndCannotBeRepeated()
        {
            AdvanceTo(5);
            var loot = m_Session.GetSnapshot().RunLoot;
            Assert.That(loot.Amounts.Count, Is.EqualTo(2));
            Assert.That(m_Session.GetSnapshot().CanCashOut, Is.True);
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));

            var collected = m_Session.GetSnapshot();
            CollectionAssert.AreEquivalent(loot.Amounts, collected.Progress.Amounts);
            Assert.That(collected.RunLoot.Amounts, Is.Empty);
            Assert.That(collected.Status, Is.EqualTo(SessionStatus.CashedOut));
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Store.Writes, Is.EqualTo(1));

            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.Accepted));
            CollectionAssert.AreEquivalent(loot.Amounts, m_Session.GetSnapshot().Progress.Amounts);
            CollectionAssert.AreEquivalent(loot.Amounts, m_Store.Load().Amounts);
        }

        [TestCase(5)]
        [TestCase(30)]
        public void CashOutReturnsAndRetainsTheExactCollectedRewards(int zone)
        {
            m_Store.Progress = new RewardCollection(new[] { new Reward("gold", 1000) });
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store);
            AdvanceTo(zone);
            var before = m_Session.GetSnapshot();

            var response = m_Session.RequestCashOut();
            Assert.That(response.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(response.Result.RunId, Is.EqualTo(before.RunId));
            Assert.That(response.Result.Zone, Is.EqualTo(zone));
            CollectionAssert.AreEquivalent(before.RunLoot.Amounts, response.Result.Rewards.Amounts);
            Assert.That(m_Session.GetSnapshot().LastCashOut, Is.SameAs(response.Result));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("gold"),
                Is.EqualTo(1000 + before.RunLoot.GetAmount("gold")));
            Assert.That(before.LastCashOut, Is.Null);

            var repeated = m_Session.RequestCashOut();
            Assert.That(repeated.Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(repeated.Result, Is.Null);
            Assert.That(m_Session.GetSnapshot().LastCashOut, Is.SameAs(response.Result));
            Assert.That(m_Store.Writes, Is.EqualTo(1));

            m_Session.StartNewRun();
            Assert.That(m_Session.GetSnapshot().LastCashOut, Is.Null);
            CollectionAssert.AreEquivalent(before.RunLoot.Amounts, response.Result.Rewards.Amounts);
        }

        [Test]
        public void FailedCashOutDoesNotPublishASuccessfulReceipt()
        {
            AdvanceTo(5);
            m_Store.FailWrites = true;
            var response = m_Session.RequestCashOut();

            Assert.That(response.Status, Is.EqualTo(CommandStatus.SaveFailed));
            Assert.That(response.Result, Is.Null);
            Assert.That(m_Session.GetSnapshot().LastCashOut, Is.Null);

            m_Store.FailWrites = false;
            var retry = m_Session.RequestCashOut();
            Assert.That(retry.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().LastCashOut, Is.SameAs(retry.Result));
        }

        [Test]
        public void FailedCashOutPreservesRunAndRetryCommitsOnce()
        {
            AdvanceTo(5);
            var before = m_Session.GetSnapshot();
            m_Store.FailWrites = true;

            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.SaveFailed));
            var failed = m_Session.GetSnapshot();
            Assert.That(failed.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(failed.RunId, Is.EqualTo(before.RunId));
            Assert.That(failed.Round, Is.SameAs(before.Round));
            Assert.That(failed.RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(failed.Progress.Amounts, Is.Empty);
            Assert.That(m_Store.Progress.Amounts, Is.Empty);

            m_Store.FailWrites = false;
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            CollectionAssert.AreEquivalent(before.RunLoot.Amounts, m_Store.Progress.Amounts);
        }

        [Test]
        public void CashOutCannotReenterSessionWhileStoreIsWriting()
        {
            AdvanceTo(5);
            m_Store.DuringSave = () =>
            {
                Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashingOut));
                Assert.That(m_Session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.RunNotFinished));
            };

            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Store.Writes, Is.EqualTo(1));
        }

        [Test]
        public void UnexpectedStoreExceptionDoesNotLeaveSessionBusy()
        {
            AdvanceTo(5);
            var before = m_Session.GetSnapshot();
            m_Store.DuringSave = () => throw new InvalidOperationException("Unexpected store bug");

            Assert.Throws<InvalidOperationException>(() => m_Session.RequestCashOut());
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(m_Session.GetSnapshot().Progress, Is.SameAs(before.Progress));
        }

        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(6, false)]
        [TestCase(10, true)]
        [TestCase(29, false)]
        [TestCase(30, true)]
        [TestCase(31, false)]
        public void CashOutIsAllowedOnlyWhenSafeAndReady(int zone, bool safe)
        {
            AdvanceTo(zone);
            Assert.That(m_Session.GetSnapshot().CanCashOut, Is.EqualTo(safe));
            if (!safe)
                Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.UnsafeZone));

            var result = m_Session.RequestSpin().Result;
            Assert.That(m_Session.GetSnapshot().CanCashOut, Is.False);
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Session.CompleteResolution(result.OperationId), Is.EqualTo(CommandStatus.Accepted));
        }

        [Test]
        public void FullRunReachesGoldCollectsSpecialRewardAndCashesOutAtNextSafeZone()
        {
            AdvanceTo(30);
            Assert.That(m_Session.GetSnapshot().Round.Tier, Is.EqualTo(WheelTier.Gold));
            SpinAndComplete(2);
            Assert.That(m_Session.GetSnapshot().RunLoot.GetAmount("special"), Is.EqualTo(30));
            AdvanceTo(35);
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("special"), Is.EqualTo(30));
        }

        [Test]
        public void InvalidRandomResultLeavesSessionUnchanged()
        {
            m_Random.Enqueue(3);
            var before = m_Session.GetSnapshot();
            Assert.Throws<InvalidOperationException>(() => m_Session.RequestSpin());
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(m_Session.GetSnapshot().PendingSpin, Is.Null);
        }

        [Test]
        public void LootOverflowDoesNotCommitPartialSpin()
        {
            var configuration = TestWheelFactory.Create(long.MaxValue - 1);
            m_Session = new GameSession(configuration, m_Random, m_Store);
            SpinAndComplete();
            var before = m_Session.GetSnapshot();

            Assert.Throws<OverflowException>(() => m_Session.RequestSpin());
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(m_Session.GetSnapshot().PendingSpin, Is.Null);
        }

        [Test]
        public void InMemoryStoreRetainsAllRewardTypesAcrossSessions()
        {
            var memory = new InMemoryProgressStore();
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, memory);
            AdvanceTo(5);
            var loot = m_Session.GetSnapshot().RunLoot;
            m_Session.RequestCashOut();

            var next = new GameSession(TestWheelFactory.Create(), new SequenceRandom(), memory);
            CollectionAssert.AreEquivalent(loot.Amounts, next.GetSnapshot().Progress.Amounts);
            Assert.That(next.GetSnapshot().RunLoot.Amounts, Is.Empty);
        }

        [Test]
        public void InvalidNextRoundRejectsSpinBeforeCommittingAnyState()
        {
            m_Session = new GameSession(TestWheelFactory.Create(long.MaxValue), m_Random, m_Store);
            var before = m_Session.GetSnapshot();
            m_Random.Enqueue(1);

            // The selected ticket fits; an unselected gold slice overflows in zone two.
            Assert.Throws<OverflowException>(() => m_Session.RequestSpin());

            var after = m_Session.GetSnapshot();
            Assert.That(after.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(after.RunId, Is.EqualTo(before.RunId));
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(after.RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(after.Progress, Is.SameAs(before.Progress));
            Assert.That(after.PendingSpin, Is.Null);
            Assert.That(m_Store.Writes, Is.Zero);

            var bomb = SpinAndComplete(2);
            Assert.That(bomb.OperationId.Sequence, Is.EqualTo(1));
        }

        [Test]
        public void InvalidNextRoundKeepsSafeZoneCashOutAvailable()
        {
            m_Session = new GameSession(TestWheelFactory.Create(long.MaxValue - 4), m_Random, m_Store);
            for (var zone = 1; zone < 5; zone++)
                SpinAndComplete(1);

            var before = m_Session.GetSnapshot();
            m_Random.Enqueue(1);
            Assert.Throws<OverflowException>(() => m_Session.RequestSpin());

            Assert.That(m_Session.GetSnapshot().CanCashOut, Is.True);
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            CollectionAssert.AreEquivalent(before.RunLoot.Amounts, m_Store.Progress.Amounts);
        }

        [Test]
        public void BombDoesNotRequireAValidNextRound()
        {
            m_Session = new GameSession(TestWheelFactory.Create(long.MaxValue), m_Random, m_Store);
            var bomb = SpinAndComplete(2);

            Assert.That(bomb.WinningSlice, Is.TypeOf<BombSlice>());
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.Accepted));
        }

        private SpinResult SpinAndComplete(int index = 0)
        {
            m_Random.Enqueue(index);
            var response = m_Session.RequestSpin();
            Assert.That(response.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.CompleteResolution(response.Result.OperationId), Is.EqualTo(CommandStatus.Accepted));
            return response.Result;
        }

        private void AdvanceTo(int zone)
        {
            while (m_Session.GetSnapshot().Round.Zone < zone)
                SpinAndComplete(m_Session.GetSnapshot().Round.Zone % 2);
        }



    }
}
