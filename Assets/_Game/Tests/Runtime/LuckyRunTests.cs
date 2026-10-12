using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class LuckyRunTests
    {
        private SequenceRandom m_Random;
        private GameSession m_Session;

        [SetUp]
        public void SetUp()
        {
            m_Random = new SequenceRandom();
            m_Session = new GameSession(CreateConfiguration(), m_Random, new TestProgressStore());
        }

        [TestCase(1, 5)]
        [TestCase(4, 5)]
        [TestCase(5, 10)]
        [TestCase(6, 10)]
        [TestCase(10, 15)]
        [TestCase(25, 30)]
        [TestCase(26, 30)]
        [TestCase(29, 30)]
        [TestCase(30, 35)]
        [TestCase(31, 35)]
        [TestCase(60, 65)]
        public void LuckyStopsOnArrivalAndLeavesSafeSpinToPlayer(int zone, int target)
        {
            AdvanceTo(zone);
            var previousLoot = m_Session.GetSnapshot().RunLoot;
            var lucky = WinLucky();
            Assert.That(lucky.WinningSlice, Is.TypeOf<LuckySliceData>());
            Assert.That(lucky.IsAutomatic, Is.False);
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(previousLoot));
            Assert.That(m_Session.GetSnapshot().AutomaticRun, Is.Null);
            m_Session.CompleteResolution(lucky.OperationId);

            while (m_Session.GetSnapshot().Round.Zone < target)
            {
                var snapshot = m_Session.GetSnapshot();
                Assert.That(snapshot.AutomaticRun.TargetZone, Is.EqualTo(target));
                Assert.That(snapshot.CanSpin, Is.False);
                Assert.That(snapshot.CanAutomaticSpin, Is.True);
                Assert.That(m_Session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
                Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));
                var spin = m_Session.RequestAutomaticSpin();
                Assert.That(spin.Result.IsAutomatic, Is.True);
                Assert.That(spin.Result.WinningSlice, Is.TypeOf<RewardSliceData>());
                Assert.That(m_Session.RequestAutomaticSpin().Status, Is.EqualTo(CommandStatus.NotReady));
                m_Session.CompleteResolution(spin.Result.OperationId);
            }

            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(target));
            Assert.That(m_Session.GetSnapshot().AutomaticRun, Is.Null);
            Assert.That(m_Session.GetSnapshot().CanSpin, Is.True);
            Assert.That(m_Session.GetSnapshot().CanCashOut, Is.True);
            Assert.That(m_Session.RequestAutomaticSpin().Status, Is.EqualTo(CommandStatus.NotReady));
            var safeSpin = m_Session.RequestSpin();
            Assert.That(safeSpin.Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(safeSpin.Result.Round.Zone, Is.EqualTo(target));
            Assert.That(safeSpin.Result.IsAutomatic, Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(1, 2)]
        public void AutomaticSelectionUsesOriginalRewardIndicesAndOneRandomDraw(int selection, int expectedIndex)
        {
            var lucky = WinLucky();
            m_Session.CompleteResolution(lucky.OperationId);
            var calls = m_Random.Calls;
            m_Random.Enqueue(selection);
            var spin = m_Session.RequestAutomaticSpin();
            Assert.That(spin.Result.WinningIndex, Is.EqualTo(expectedIndex));
            Assert.That(m_Random.Calls, Is.EqualTo(calls + 1));
            Assert.That(m_Session.CompleteResolution(lucky.OperationId), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(m_Session.GetSnapshot().PendingSpin, Is.SameAs(spin.Result));
        }

        [Test]
        public void InvalidFilteredRandomDoesNotMutateSession()
        {
            m_Session.CompleteResolution(WinLucky().OperationId);
            var before = m_Session.GetSnapshot();
            m_Random.Enqueue(2);
            Assert.Throws<InvalidOperationException>(() => m_Session.RequestAutomaticSpin());
            var after = m_Session.GetSnapshot();
            Assert.That(after.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(after.RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(after.AutomaticRun, Is.SameAs(before.AutomaticRun));
            Assert.That(m_Session.RequestAutomaticSpin().Status, Is.EqualTo(CommandStatus.Accepted));
        }

        [Test]
        public void BonusEndsBeforeCashOutAndNewRunRestoresBombRisk()
        {
            m_Session.CompleteResolution(WinLucky().OperationId);
            while (m_Session.GetSnapshot().CanAutomaticSpin)
                m_Session.CompleteResolution(m_Session.RequestAutomaticSpin().Result.OperationId);
            var earned = m_Session.GetSnapshot().RunLoot;
            Assert.That(m_Session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
            Assert.That(m_Session.GetSnapshot().Progress.Amounts, Is.EqualTo(earned.Amounts));
            m_Session.StartNewRun();
            Assert.That(m_Session.GetSnapshot().AutomaticRun, Is.Null);
            Assert.That(m_Session.RequestAutomaticSpin().Status, Is.EqualTo(CommandStatus.NotReady));
            m_Random.Enqueue(3);
            var bomb = m_Session.RequestSpin();
            Assert.That(bomb.Result.WinningSlice, Is.TypeOf<BombSliceData>());
            m_Session.CompleteResolution(bomb.Result.OperationId);
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
        }

        [Test]
        public void EmptyEligibilityIsRejectedWithoutConsumingRandom()
        {
            var selector = new SpinSelector(m_Random);
            var calls = m_Random.Calls;
            Assert.Throws<InvalidOperationException>(() => selector.SelectSlice(m_Session.GetSnapshot().Round, slice => false));
            Assert.That(m_Random.Calls, Is.EqualTo(calls));
        }

        private SpinResult WinLucky()
        {
            m_Random.Enqueue(1);
            return m_Session.RequestSpin().Result;
        }

        private void AdvanceTo(int zone)
        {
            while (m_Session.GetSnapshot().Round.Zone < zone)
                m_Session.CompleteResolution(m_Session.RequestSpin().Result.OperationId);
        }

        private static WheelConfiguration CreateConfiguration()
        {
            var coins = new RewardSliceDefinition("coins", new RewardDefinition("coins", 1), 10);
            var tickets = new RewardSliceDefinition("tickets", new RewardDefinition("tickets", 1), 2);
            var gems = new RewardSliceDefinition("gems", new RewardDefinition("gems", 1), 1);
            var lucky = new LuckySliceDefinition("lucky");
            return new WheelConfiguration(
                new SliceDefinition[] { coins, lucky, tickets, new BombSliceDefinition("bomb") },
                new SliceDefinition[] { coins, lucky, tickets, gems },
                new SliceDefinition[] { coins, lucky, tickets,
                    new RewardSliceDefinition("special", new RewardDefinition("special", 1), 1, true) });
        }
    }
}
