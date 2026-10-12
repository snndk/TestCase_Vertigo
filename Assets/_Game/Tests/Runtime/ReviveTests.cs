using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class ReviveTests
    {
        private GameSession m_Session;
        private TestProgressStore m_Store;
        private SequenceRandom m_Random;

        [SetUp]
        public void SetUp()
        {
            m_Random = new SequenceRandom();
            m_Store = new TestProgressStore { Progress = new RewardCollection(new[] { new Reward("gold", 100), new Reward("ticket", 7) }) };
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store, new Reward("gold", 25));
            Spin(0);
            Spin(1);
            Spin(2);
        }

        [Test]
        public void ReviveRestoresEveryRewardAndChargesOnceWithoutChangingRun()
        {
            var before = m_Session.GetSnapshot();
            Assert.That(before.Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(before.RunLoot.Amounts, Is.Empty);
            Assert.That(before.RecoverableLoot.Amounts.Count, Is.EqualTo(2));
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            var after = m_Session.GetSnapshot();
            Assert.That(after.RunId, Is.EqualTo(before.RunId));
            Assert.That(after.Round.Zone, Is.EqualTo(4));
            Assert.That(after.RunLoot, Is.SameAs(before.RecoverableLoot));
            Assert.That(after.Progress.GetAmount("gold"), Is.EqualTo(75));
            Assert.That(after.Progress.GetAmount("ticket"), Is.EqualTo(7));
            Assert.That(after.ReviveCost.Amount, Is.EqualTo(50));
            Assert.That(before.ReviveCost.Amount, Is.EqualTo(25));
            Assert.That(after.RecoverableLoot.Amounts, Is.Empty);
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(50));
            Assert.That(m_Store.Writes, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedWriteKeepsPaymentLootAndZoneAvailableForRetry(bool throws)
        {
            var before = m_Session.GetSnapshot();
            m_Store.FailWrites = !throws;
            if (throws)
            {
                m_Store.DuringSave = () => throw new InvalidOperationException("Unavailable");
                Assert.Throws<InvalidOperationException>(() => m_Session.RequestRevive());
            }
            else
                Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.SaveFailed));
            var after = m_Session.GetSnapshot();
            Assert.That(after.Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(after.ReviveCost, Is.SameAs(before.ReviveCost));
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(after.Progress, Is.SameAs(before.Progress));
            Assert.That(after.RecoverableLoot, Is.SameAs(before.RecoverableLoot));
            m_Store.FailWrites = false;
            m_Store.DuringSave = null;
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("gold"), Is.EqualTo(75));
        }

        [Test]
        public void ReentrantCommandsCannotSpendOrDiscardDuringSave()
        {
            m_Store.DuringSave = () =>
            {
                Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.GiveUp(), Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.RunNotFinished));
            };
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(50));
            Assert.That(m_Store.Writes, Is.EqualTo(1));
        }

        [Test]
        public void GiveUpDiscardsRecoverableRewardsWithoutPayment()
        {
            Assert.That(m_Session.GiveUp(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().RecoverableLoot.Amounts, Is.Empty);
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.NotReady));
            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("gold"), Is.EqualTo(100));
            Assert.That(m_Store.Writes, Is.Zero);
        }

        [Test]
        public void InsufficientWalletCannotPayWithUncollectedRunRewards()
        {
            m_Store.Progress = RewardCollection.Empty;
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store, new Reward("gold", 1));
            Spin(0);
            Spin(2);
            Assert.That(m_Session.GetSnapshot().CanRevive, Is.False);
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.InsufficientFunds));
            Assert.That(m_Session.GetSnapshot().RecoverableLoot.GetAmount("gold"), Is.EqualTo(10));
            Assert.That(m_Store.Writes, Is.Zero);
        }

        [Test]
        public void InvalidNextRoundDoesNotConsumeRevivePayment()
        {
            m_Session = new GameSession(TestWheelFactory.Create(long.MaxValue - 1), m_Random,
                m_Store, new Reward("gold", 25));
            Spin(0);
            Spin(2);
            var before = m_Session.GetSnapshot();
            Assert.Throws<OverflowException>(() => m_Session.RequestRevive());
            Assert.That(m_Session.GetSnapshot().Round, Is.SameAs(before.Round));
            Assert.That(m_Session.GetSnapshot().RecoverableLoot, Is.SameAs(before.RecoverableLoot));
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(m_Store.Writes, Is.Zero);
        }

        [Test]
        public void EachSuccessfulReviveDoublesTheNextPriceAndChecksTheIncreasedCost()
        {
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            Spin(2);
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(50));
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("gold"), Is.EqualTo(25));
            Spin(0); // Leave the safe zone before hitting another bomb.
            Spin(2);
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(100));
            Assert.That(m_Session.GetSnapshot().CanRevive, Is.False);
            Assert.That(m_Session.RequestRevive(), Is.EqualTo(CommandStatus.InsufficientFunds));
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(100));
            Assert.That(m_Store.Writes, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewRunRestoresInitialPriceAfterGiveUpOrCashOut(bool cashOut)
        {
            m_Session.RequestRevive();
            if (cashOut)
            {
                Spin(0);
                Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            }
            else
            {
                Spin(2);
                Assert.That(m_Session.GiveUp(), Is.EqualTo(CommandStatus.Accepted));
            }
            Assert.That(m_Session.StartNewRun(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(25));
        }

        [Test]
        public void PriceOverflowCannotChargeOrChangeThePendingDecision()
        {
            m_Store.Progress = new RewardCollection(new[] { new Reward("gold", long.MaxValue) });
            m_Session = new GameSession(TestWheelFactory.Create(), m_Random, m_Store,
                new Reward("gold", long.MaxValue));
            Spin(2);
            var before = m_Session.GetSnapshot();
            Assert.Throws<OverflowException>(() => m_Session.RequestRevive());
            var after = m_Session.GetSnapshot();
            Assert.That(after.Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(after.ReviveCost, Is.SameAs(before.ReviveCost));
            Assert.That(after.Progress, Is.SameAs(before.Progress));
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(m_Store.Writes, Is.Zero);
        }

        private void Spin(int index)
        {
            m_Random.Enqueue(index);
            var result = m_Session.RequestSpin().Result;
            Assert.That(m_Session.CompleteResolution(result.OperationId), Is.EqualTo(CommandStatus.Accepted));
        }
    }
}
