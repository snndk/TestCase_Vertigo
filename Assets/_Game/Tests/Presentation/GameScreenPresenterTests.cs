using System;
using NUnit.Framework;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    public class GameScreenPresenterTests
    {
        private PresentationTestContext m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Context = new PresentationTestContext();
            m_Context.Presenter.Attach();
        }

        [Test]
        public void AttachIsIdempotentAndDetachRemovesAllListeners()
        {
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.SpinListeners, Is.EqualTo(1));
            Assert.That(m_Context.View.CashOutListeners, Is.EqualTo(1));
            Assert.That(m_Context.View.RestartListeners, Is.EqualTo(1));
            m_Context.Presenter.Detach();
            m_Context.Presenter.Detach();
            Assert.That(m_Context.View.SpinListeners, Is.Zero);
            Assert.That(m_Context.View.CashOutListeners, Is.Zero);
            Assert.That(m_Context.View.RestartListeners, Is.Zero);
            m_Context.View.RequestSpin();
            Assert.That(m_Context.Random.Calls, Is.Zero);
            m_Context.Presenter.Attach();
            m_Context.View.RequestSpin();
            Assert.That(m_Context.Random.Calls, Is.EqualTo(1));
        }

        [Test]
        public void LootAppearsOnlyAfterTheOutcomeAndNextRoundIsNotShownEarly()
        {
            var initial = m_Context.View.State;
            m_Context.View.RequestSpin();
            Assert.That(m_Context.Session.GetSnapshot().RunLoot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.View.State.Loot.Amounts, Is.Empty);
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(1));
            Assert.That(m_Context.View.State.CanSpin, Is.False);
            Assert.That(m_Context.View.State.CanCashOut, Is.False);
            m_Context.SpinView.CompleteLatest();
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Context.View.State.Loot.Amounts, Is.Empty);
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(initial.Loot.Amounts, Is.Empty);
        }

        [Test]
        public void ExtraInputsDuringSpinCannotAlterTheResultOrDisplayLootEarly()
        {
            m_Context.View.RequestSpin();
            m_Context.View.RequestSpin();
            m_Context.View.RequestCashOut();
            m_Context.View.RequestRestart();
            Assert.That(m_Context.Random.Calls, Is.EqualTo(1));
            Assert.That(m_Context.Store.Writes, Is.Zero);
            Assert.That(m_Context.View.State.Loot.Amounts, Is.Empty);
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DetachAtAnyStageKeepsEarnedLootAndSkipsTheRest(int stage)
        {
            m_Context.View.RequestSpin();
            for (var index = 0; index < stage; index++)
                m_Context.SpinView.CompleteLatest();
            var renders = m_Context.View.History.Count;
            m_Context.Presenter.Detach();
            var snapshot = m_Context.Session.GetSnapshot();
            Assert.That(snapshot.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(snapshot.Round.Zone, Is.EqualTo(2));
            Assert.That(snapshot.RunLoot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.View.History.Count, Is.EqualTo(renders));
            Assert.That(m_Context.SpinView.CancelCalls, Is.EqualTo(1));
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(stage + 1));
        }

        [Test]
        public void OldCallbackAfterReattachCannotFinishTheNewSpin()
        {
            m_Context.View.RequestSpin();
            var old = m_Context.SpinView.Requests[0];
            m_Context.Presenter.Detach();
            m_Context.Presenter.Attach();
            m_Context.View.RequestSpin();
            var pending = m_Context.Session.GetSnapshot().PendingSpin;
            old.Complete(VisualResult.Completed);
            old.Complete(VisualResult.Failed(new Exception("Old failure")));
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin, Is.SameAs(pending));
            Assert.That(m_Context.View.State.Error, Is.Null);
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            m_Context.CompleteSpin();
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(3));
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(21));
        }

        [Test]
        public void NewPresenterRestoresPendingRuntimeResultWithoutReplayingIt()
        {
            m_Context.Presenter.Detach();
            m_Context.Session.RequestSpin();
            var replacement = new GameScreenPresenter(m_Context.Session, m_Context.View,
                new SpinPresentationSequence(m_Context.SpinView, TimeSpan.FromSeconds(10)));
            replacement.Attach();
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin, Is.Null);
            Assert.That(m_Context.SpinView.Requests, Is.Empty);
        }

        [Test]
        public void CancellationReportedByViewResolvesThePendingSpin()
        {
            m_Context.View.RequestSpin();
            m_Context.SpinView.CompleteLatest(VisualResult.Cancelled);
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(m_Context.View.State.Error, Is.Null);
        }

        [Test]
        public void FailureReportsItsCauseAndKeepsTheCommittedReward()
        {
            m_Context.View.RequestSpin();
            var error = new InvalidOperationException("Tween error");
            m_Context.SpinView.CompleteLatest(VisualResult.Failed(error));
            Assert.That(m_Context.View.State.Error.Cause, Is.SameAs(error));
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            m_Context.View.RequestSpin();
            Assert.That(m_Context.View.State.Error, Is.Null);
        }

        [Test]
        public void HungViewTimesOutAndLateCallbackDoesNothing()
        {
            m_Context.View.RequestSpin();
            m_Context.Presenter.AdvancePresentation(TimeSpan.FromSeconds(10));
            Assert.That(m_Context.View.State.Error.Cause, Is.TypeOf<TimeoutException>());
            Assert.That(m_Context.View.State.CanSpin, Is.True);
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.Random.Calls, Is.EqualTo(1));
        }

        [Test]
        public void BombPanelWaitsForOutcomeThenRestartCreatesACleanRun()
        {
            m_Context.View.RequestSpin();
            m_Context.CompleteSpin();
            var oldRun = m_Context.Session.GetSnapshot().RunId;
            m_Context.Random.Index = 2;
            m_Context.View.RequestSpin();
            var bombRequest = m_Context.SpinView.Requests[3];
            Assert.That(m_Context.View.State.ShowBomb, Is.False);
            Assert.That(m_Context.View.State.Loot.GetAmount("coins"), Is.EqualTo(10));
            Assert.That(m_Context.Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            m_Context.CompleteSpin();
            Assert.That(m_Context.View.State.ShowBomb, Is.True);
            Assert.That(m_Context.View.State.CanRestart, Is.True);
            Assert.That(m_Context.View.State.CanSpin, Is.False);
            Assert.That(m_Context.View.State.Loot.Amounts, Is.Empty);
            m_Context.View.RequestRestart();
            Assert.That(m_Context.Session.GetSnapshot().RunId, Is.Not.EqualTo(oldRun));
            Assert.That(m_Context.View.State.ShowBomb, Is.False);
            Assert.That(m_Context.View.State.Round.Zone, Is.EqualTo(1));
            m_Context.Random.Index = 0;
            m_Context.View.RequestSpin();
            bombRequest.Complete(VisualResult.Completed);
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
        }

        [Test]
        public void DetachingOnBombStillShowsLossWhenReattached()
        {
            m_Context.Random.Index = 2;
            m_Context.View.RequestSpin();
            m_Context.Presenter.Detach();
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.State.ShowBomb, Is.True);
            Assert.That(m_Context.View.State.CanRestart, Is.True);
        }

        [TestCase(5)]
        [TestCase(30)]
        public void CashOutDisplaysTheExactReceiptAndRestoresItOnReattach(int zone)
        {
            m_Context.AdvanceTo(zone);
            var loot = m_Context.Session.GetSnapshot().RunLoot;
            Assert.That(m_Context.View.State.CanCashOut, Is.True);
            m_Context.View.RequestCashOut();
            var receipt = m_Context.View.State.CashOut;
            Assert.That(receipt, Is.Not.Null);
            CollectionAssert.AreEquivalent(loot.Amounts, receipt.Rewards.Amounts);
            CollectionAssert.AreEquivalent(loot.Amounts, m_Context.View.State.Progress.Amounts);
            Assert.That(m_Context.View.State.CanRestart, Is.True);
            Assert.That(m_Context.View.State.CanCashOut, Is.False);
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.Store.Writes, Is.EqualTo(1));
            m_Context.Presenter.Detach();
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.State.CashOut, Is.SameAs(receipt));
            m_Context.View.RequestRestart();
            Assert.That(m_Context.View.State.CashOut, Is.Null);
            CollectionAssert.AreEquivalent(loot.Amounts, m_Context.View.State.Progress.Amounts);
        }

        [Test]
        public void UnsafeCashOutIsRejectedByRuntimeWithoutWriting()
        {
            Assert.That(m_Context.View.State.CanCashOut, Is.False);
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.Store.Writes, Is.Zero);
            Assert.That(m_Context.View.State.CashOut, Is.Null);
        }

        [Test]
        public void FailedSaveShowsVisibleErrorAndAllowsRetryWithAllLootIntact()
        {
            m_Context.AdvanceTo(5);
            var loot = m_Context.View.State.Loot;
            m_Context.Store.FailWrites = true;
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.View.State.Error.Message, Does.Contain("saved"));
            Assert.That(m_Context.View.State.CashOut, Is.Null);
            Assert.That(m_Context.View.State.Loot, Is.SameAs(loot));
            Assert.That(m_Context.View.State.CanCashOut, Is.True);
            m_Context.Store.FailWrites = false;
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.View.State.Error, Is.Null);
            CollectionAssert.AreEquivalent(loot.Amounts, m_Context.View.State.CashOut.Rewards.Amounts);
        }

        [Test]
        public void UnexpectedSaveExceptionIsVisibleAndDoesNotLoseLoot()
        {
            m_Context.AdvanceTo(5);
            var loot = m_Context.View.State.Loot;
            var error = new InvalidOperationException("Disk error");
            m_Context.Store.WriteFailure = error;
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.View.State.Error.Cause, Is.SameAs(error));
            Assert.That(m_Context.View.State.Loot, Is.SameAs(loot));
            Assert.That(m_Context.View.State.CanCashOut, Is.True);
            m_Context.Store.WriteFailure = null;
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.View.State.CashOut, Is.Not.Null);
        }

        [Test]
        public void RenderingFailureAfterCashOutCannotCollectTheRewardsAgain()
        {
            m_Context.AdvanceTo(5);
            m_Context.View.RenderFailure = new InvalidOperationException("Screen unavailable");
            Assert.Throws<InvalidOperationException>(() => m_Context.View.RequestCashOut());
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
            Assert.That(m_Context.Store.Writes, Is.EqualTo(1));
            m_Context.View.RenderFailure = null;
            m_Context.Presenter.Detach();
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.State.CashOut, Is.Not.Null);
            m_Context.View.RequestCashOut();
            Assert.That(m_Context.Store.Writes, Is.EqualTo(1));
        }

        [Test]
        public void RenderingFailureBeforeAnimationDoesNotLeaveRuntimeResolving()
        {
            m_Context.View.RenderFailure = new InvalidOperationException("Screen unavailable");
            Assert.Throws<InvalidOperationException>(() => m_Context.View.RequestSpin());
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Context.Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
        }

        [Test]
        public void DetachDuringInitialSpinRenderDoesNotStartAnOrphanAnimation()
        {
            m_Context.View.DuringRender = m_Context.Presenter.Detach;
            m_Context.View.RequestSpin();
            Assert.That(m_Context.SpinView.Requests, Is.Empty);
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Context.View.SpinListeners, Is.Zero);
        }

        [Test]
        public void ReentrantInputDuringRenderIsIgnored()
        {
            m_Context.View.DuringRender = m_Context.View.RequestSpin;
            m_Context.View.RequestSpin();
            m_Context.CompleteSpin();
            Assert.That(m_Context.Random.Calls, Is.EqualTo(1));
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
        }

        [Test]
        public void InvalidViewCapacityIsRejectedBeforeSubscribingOrResolving()
        {
            m_Context.Presenter.Detach();
            var pending = m_Context.Session.RequestSpin().Result;
            m_Context.View.SliceCount = 8;
            Assert.Throws<InvalidOperationException>(() => m_Context.Presenter.Attach());
            Assert.That(m_Context.View.SpinListeners, Is.Zero);
            Assert.That(m_Context.Session.GetSnapshot().PendingSpin, Is.SameAs(pending));
            Assert.That(m_Context.Session.GetSnapshot().Round.Slices.Count, Is.EqualTo(3));
        }

        [Test]
        public void FailedAttachCleansUpSubscriptionsAndCanBeRetried()
        {
            m_Context.Presenter.Detach();
            m_Context.View.RenderFailure = new InvalidOperationException("Missing view reference");
            Assert.Throws<InvalidOperationException>(() => m_Context.Presenter.Attach());
            Assert.That(m_Context.View.SpinListeners, Is.Zero);
            Assert.That(m_Context.View.CashOutListeners, Is.Zero);
            Assert.That(m_Context.View.RestartListeners, Is.Zero);
            m_Context.View.RenderFailure = null;
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.SpinListeners, Is.EqualTo(1));
        }

        [Test]
        public void CleanupErrorWhileDetachedIsRetainedForTheReturningScreen()
        {
            m_Context.View.RequestSpin();
            m_Context.SpinView.ThrowOnCancel = true;
            m_Context.Presenter.Detach();
            Assert.That(m_Context.Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            m_Context.Presenter.Attach();
            Assert.That(m_Context.View.State.Error.Cause, Is.TypeOf<InvalidOperationException>());
        }
    }
}
