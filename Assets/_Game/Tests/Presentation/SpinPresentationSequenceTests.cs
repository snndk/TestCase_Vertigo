using System;
using System.Linq;
using NUnit.Framework;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    public class SpinPresentationSequenceTests
    {
        private PresentationTestContext m_Context;
        private int m_Completions;
        private VisualResult m_Result;

        [SetUp]
        public void SetUp()
        {
            m_Context = new PresentationTestContext();
            m_Completions = 0;
            m_Result = null;
        }

        [Test]
        public void StagesRunInOrderWithTheSameRuntimeResult()
        {
            var spin = m_Context.Session.RequestSpin().Result;
            m_Context.Sequence.Play(spin, Record);
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(1));
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(2));
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Completions, Is.Zero);
            m_Context.SpinView.CompleteLatest();

            CollectionAssert.AreEqual(new[] { "spin", "highlight", "outcome" },
                m_Context.SpinView.Requests.Select(request => request.Stage));
            Assert.That(m_Context.SpinView.Requests.All(request => ReferenceEquals(request.Spin, spin)), Is.True);
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Result.Status, Is.EqualTo(VisualStatus.Completed));
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
        }

        [Test]
        public void SynchronousViewsCompleteOnce()
        {
            m_Context.SpinView.CompleteSynchronously = true;
            Start();
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Result.Status, Is.EqualTo(VisualStatus.Completed));
        }

        [Test]
        public void DuplicateStageCallbacksDoNotSkipStagesOrRepeatCompletion()
        {
            Start();
            var first = m_Context.SpinView.Requests[0];
            first.Complete(VisualResult.Completed);
            first.Complete(VisualResult.Completed);
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(2));
            m_Context.SpinView.CompleteLatest();
            m_Context.SpinView.CompleteLatest();
            foreach (var request in m_Context.SpinView.Requests.ToArray())
                request.Complete(VisualResult.Completed);

            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CancelFinishesEvenWhenViewNeverCallsBack(int stage)
        {
            Start();
            for (var index = 0; index < stage; index++)
                m_Context.SpinView.CompleteLatest();

            m_Context.Sequence.Cancel();
            m_Context.Sequence.Cancel();
            m_Context.SpinView.CompleteLatest();
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Result.Status, Is.EqualTo(VisualStatus.Cancelled));
            Assert.That(m_Context.SpinView.CancelCalls, Is.EqualTo(1));
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(stage + 1));
        }

        [TestCase("spin")]
        [TestCase("highlight")]
        [TestCase("outcome")]
        public void ViewStartExceptionEndsTheSequence(string stage)
        {
            m_Context.SpinView.ThrowOnStage = stage;
            m_Context.SpinView.CompleteSynchronously = true;
            Start();
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Result.Status, Is.EqualTo(VisualStatus.Failed));
            Assert.That(m_Result.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(m_Context.SpinView.CancelCalls, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ReportedFailureDoesNotStartAnotherStage(int stage)
        {
            Start();
            for (var index = 0; index < stage; index++)
                m_Context.SpinView.CompleteLatest();
            var error = new InvalidOperationException("Tween failed");
            m_Context.SpinView.CompleteLatest(VisualResult.Failed(error));
            Assert.That(m_Result.Error, Is.SameAs(error));
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(stage + 1));
        }

        [Test]
        public void CancelCallbackCannotReenterTheSequence()
        {
            Start();
            m_Context.SpinView.DuringCancel = () => m_Context.SpinView.CompleteLatest();
            m_Context.Sequence.Cancel();
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(1));
            Assert.That(m_Completions, Is.EqualTo(1));
        }

        [Test]
        public void CleanupExceptionStillCompletesAndPreservesBothErrors()
        {
            Start();
            m_Context.SpinView.ThrowOnCancel = true;
            m_Context.SpinView.CompleteLatest(VisualResult.Failed(new TimeoutException()));
            Assert.That(m_Result.Error, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)m_Result.Error).InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
        }

        [Test]
        public void OldCallbackCannotCompleteANewerPresentation()
        {
            var spin = m_Context.Session.RequestSpin().Result;
            m_Context.Sequence.Play(spin, Record);
            var old = m_Context.SpinView.Requests[0];
            m_Context.Sequence.Cancel();
            m_Context.Sequence.Play(spin, Record);
            old.Complete(VisualResult.Completed);
            Assert.That(m_Context.Sequence.IsRunning, Is.True);
            Assert.That(m_Context.SpinView.Requests.Count, Is.EqualTo(2));
            Assert.That(m_Completions, Is.EqualTo(1));
            m_Context.CompleteSpin();
            Assert.That(m_Completions, Is.EqualTo(2));
        }

        [Test]
        public void TimeoutCoversTheWholeSequenceWithoutRealTimeWaiting()
        {
            Start();
            m_Context.Sequence.Advance(TimeSpan.FromSeconds(6));
            m_Context.SpinView.CompleteLatest();
            m_Context.Sequence.Advance(TimeSpan.FromSeconds(4));
            Assert.That(m_Result.Error, Is.TypeOf<TimeoutException>());
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
            Assert.That(m_Context.SpinView.CancelCalls, Is.EqualTo(1));
        }

        [Test]
        public void MissingResultFailsInsteadOfLeavingAStuckSpin()
        {
            Start();
            m_Context.SpinView.Requests[0].Complete(null);
            Assert.That(m_Result.Status, Is.EqualTo(VisualStatus.Failed));
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
        }

        [Test]
        public void OverlappingPlayIsRejectedWithoutReplacingTheActiveCallback()
        {
            var spin = m_Context.Session.RequestSpin().Result;
            m_Context.Sequence.Play(spin, Record);
            Assert.Throws<InvalidOperationException>(() => m_Context.Sequence.Play(spin, result => Assert.Fail()));
            m_Context.CompleteSpin();
            Assert.That(m_Completions, Is.EqualTo(1));
        }

        [Test]
        public void ConsumerExceptionDoesNotTriggerSecondCompletion()
        {
            m_Context.SpinView.CompleteSynchronously = true;
            var spin = m_Context.Session.RequestSpin().Result;
            Assert.Throws<InvalidOperationException>(() => m_Context.Sequence.Play(spin, result =>
            {
                m_Completions++;
                throw new InvalidOperationException("Consumer failed");
            }));
            Assert.That(m_Completions, Is.EqualTo(1));
            Assert.That(m_Context.Sequence.IsRunning, Is.False);
        }

        private void Start()
        {
            m_Context.Sequence.Play(m_Context.Session.RequestSpin().Result, Record);
        }

        private void Record(VisualResult result)
        {
            m_Completions++;
            m_Result = result;
        }
    }
}
