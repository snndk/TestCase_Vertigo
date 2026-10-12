using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    public class RevivePresentationTests
    {
        private PresentationTestContext m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Context = new PresentationTestContext(new Reward("coins", 25),
                new RewardCollection(new[] { new Reward("coins", 100) }));
            m_Context.Presenter.Attach();
            m_Context.Input.RequestSpin();
            m_Context.CompleteSpin();
            m_Context.Random.Index = 2;
            m_Context.Input.RequestSpin();
            m_Context.CompleteSpin();
        }

        [Test]
        public void SaveFailureKeepsPanelAndRetryRestoresLoot()
        {
            Assert.That(m_Context.View.State.AwaitingRevive, Is.True);
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            m_Context.Store.FailWrites = true;
            m_Context.Input.RequestRevive();
            Assert.That(m_Context.View.State.Error, Is.Not.Null);
            Assert.That(m_Context.View.State.CanRevive, Is.True);
            Assert.That(m_Context.View.State.Progress.GetAmount("coins"), Is.EqualTo(100));
            m_Context.Store.FailWrites = false;
            m_Context.Input.RequestRevive();
            Assert.That(m_Context.View.State.Error, Is.Null);
            Assert.That(m_Context.View.State.ShowBomb, Is.False);
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.View.State.Progress.GetAmount("coins"), Is.EqualTo(75));
        }

        [Test]
        public void DetachRemovesBonusCommandsAndReattachDoesNotDuplicatePayment()
        {
            m_Context.Presenter.Detach();
            m_Context.Input.RequestRevive();
            m_Context.Input.RequestGiveUp();
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            m_Context.Presenter.Attach();
            m_Context.Presenter.Attach();
            m_Context.Input.RequestRevive();
            Assert.That(m_Context.Session.GetSnapshot().Progress.GetAmount("coins"), Is.EqualTo(75));
            Assert.That(m_Context.Store.Writes, Is.EqualTo(2));
        }

        [Test]
        public void GiveUpStartsNewRunAndClearsPendingRewards()
        {
            var runId = m_Context.Session.GetSnapshot().RunId;
            m_Context.Input.RequestGiveUp();
            Assert.That(m_Context.Session.GetSnapshot().RunId, Is.Not.EqualTo(runId));
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(1));
            Assert.That(m_Context.View.State.Loot.Amounts, Is.Empty);
            Assert.That(m_Context.View.State.ShowBomb, Is.False);
            Assert.That(m_Context.View.State.Progress.GetAmount("coins"), Is.EqualTo(100));
        }
    }
}
