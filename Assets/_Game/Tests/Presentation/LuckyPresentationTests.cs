using System;
using NUnit.Framework;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    public class LuckyPresentationTests
    {
        private PresentationTestContext m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Context = new PresentationTestContext(includeLucky: true);
            m_Context.Presenter.Attach();
            m_Context.Random.Index = 2;
            m_Context.Input.RequestSpin();
            m_Context.CompleteSpin();
            m_Context.Random.Index = 0;
        }

        [TearDown]
        public void TearDown() => m_Context.Presenter.Detach();

        [Test]
        public void BlockedInputCannotStartSpinAndEachTickStartsAtMostOneAutomaticRound()
        {
            Assert.That(m_Context.View.State.InputBlocked, Is.True);
            var calls = m_Context.Random.Calls;
            m_Context.Input.RequestSpin();
            m_Context.Input.RequestCashOut();
            m_Context.Input.RequestRestart();
            m_Context.Input.RequestRevive();
            m_Context.Input.RequestGiveUp();
            Assert.That(m_Context.Random.Calls, Is.EqualTo(calls));
            m_Context.SpinView.CompleteSynchronously = true;
            for (var zone = 3; zone <= 5; zone++)
            {
                m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
                Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(zone));
                Assert.That(m_Context.SpinView.Requests[m_Context.SpinView.Requests.Count - 1].Spin.IsAutomatic, Is.True);
            }
            Assert.That(m_Context.View.State.InputBlocked, Is.False);
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(m_Context.View.State.CanCashOut, Is.True);
            m_Context.Presenter.AdvancePresentation(TimeSpan.FromSeconds(20));
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
        }

        [Test]
        public void DetachSettlesOnlyPendingRoundAndAttachResumesWithoutReplayingLoot()
        {
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            var oldCallback = m_Context.SpinView.Requests[m_Context.SpinView.Requests.Count - 1].Complete;
            m_Context.Presenter.Detach();
            var loot = m_Context.Session.GetSnapshot().RunLoot;
            m_Context.Presenter.AdvancePresentation(TimeSpan.FromSeconds(20));
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            m_Context.Presenter.Attach();
            oldCallback(VisualResult.Completed);
            Assert.That(m_Context.Session.GetSnapshot().RunLoot, Is.SameAs(loot));
            Assert.That(m_Context.View.State.InputBlocked, Is.True);
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin.Round.Zone, Is.EqualTo(3));
            oldCallback(VisualResult.Completed);
            m_Context.CompleteSpin();
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(4));
        }

        [TestCase("spin")]
        [TestCase("highlight")]
        [TestCase("outcome")]
        public void AnimationFailureStillFinishesBonusAndUnlocksAtSafeZone(string failingStage)
        {
            m_Context.SpinView.ThrowOnStage = failingStage;
            m_Context.SpinView.CompleteSynchronously = true;
            for (var index = 0; index < 3; index++)
                m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(m_Context.View.State.InputBlocked, Is.False);
            Assert.That(m_Context.View.State.Error, Is.Not.Null);
        }

        [Test]
        public void PreparationFailurePausesAutomaticAttemptsWithoutChangingRuntimeState()
        {
            var before = m_Context.Session.GetSnapshot();
            m_Context.Random.Index = -1;
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            var calls = m_Context.Random.Calls;
            var error = m_Context.View.State.Error;
            for (var index = 0; index < 20; index++)
                m_Context.Presenter.AdvancePresentation(TimeSpan.FromSeconds(1));

            Assert.That(m_Context.Random.Calls, Is.EqualTo(calls));
            Assert.That(m_Context.View.State.Error, Is.SameAs(error));
            Assert.That(error.Cause, Is.TypeOf<InvalidOperationException>());
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(m_Context.View.State.CanCashOut, Is.False);
            Assert.That(m_Context.View.State.InputBlocked, Is.False);
            var after = m_Context.Session.GetSnapshot();
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(after.RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(after.AutomaticRun, Is.SameAs(before.AutomaticRun));
            Assert.That(after.PendingSpin, Is.Null);
            Assert.That(after.CanSpin, Is.False, "The retry button must not enable a manual runtime spin.");
        }

        [Test]
        public void ExplicitRetryResumesProtectedAutomaticRunAndRepeatedFailureStaysPaused()
        {
            m_Context.Random.Index = -1;
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            var calls = m_Context.Random.Calls;
            m_Context.Input.RequestSpin();
            Assert.That(m_Context.Random.Calls, Is.EqualTo(calls + 1));
            m_Context.Presenter.Detach();
            m_Context.Presenter.Attach();
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            Assert.That(m_Context.Random.Calls, Is.EqualTo(calls + 1));
            m_Context.Input.RequestCashOut();
            m_Context.Input.RequestRestart();
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(2));

            m_Context.Random.Index = 0;
            m_Context.Input.RequestSpin();
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin.IsAutomatic, Is.True);
            Assert.That(m_Context.View.State.InputBlocked, Is.True);
            Assert.That(m_Context.View.State.CanSpin, Is.False);
            Assert.That(m_Context.View.State.Error, Is.Null);
            m_Context.CompleteSpin();
            m_Context.SpinView.CompleteSynchronously = true;
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(m_Context.View.State.CanCashOut, Is.True);
        }

        [Test]
        public void TimeoutAndDuplicateCallbacksCannotAwardRoundTwice()
        {
            m_Context.Presenter.AdvancePresentation(TimeSpan.Zero);
            var callback = m_Context.SpinView.Requests[m_Context.SpinView.Requests.Count - 1].Complete;
            m_Context.Presenter.AdvancePresentation(TimeSpan.FromSeconds(11));
            var pending = m_Context.Session.GetSnapshot().PendingSpin;
            var loot = m_Context.Session.GetSnapshot().RunLoot;
            callback(VisualResult.Completed);
            callback(VisualResult.Completed);
            Assert.That(pending.Round.Zone, Is.EqualTo(3));
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin, Is.SameAs(pending));
            Assert.That(m_Context.Session.GetSnapshot().RunLoot, Is.SameAs(loot));
        }
    }
}
