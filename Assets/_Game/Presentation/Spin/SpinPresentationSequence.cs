using System;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Presentation._Game.Presentation.Spin
{
    public class SpinPresentationSequence
    {
        private readonly ISpinView m_View;
        private readonly TimeSpan m_Timeout;
        private object m_Operation;
        private object m_Step;
        private SpinResult m_Result;
        private Action<VisualResult> m_Completed;
        private TimeSpan m_Elapsed;
        private bool m_IsFinishing;

        public bool IsRunning => m_Operation != null;

        public SpinPresentationSequence(ISpinView view, TimeSpan timeout)
        {
            m_View = view ?? throw new ArgumentNullException(nameof(view));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            m_Timeout = timeout;
        }

        public void Play(SpinResult result, Action<VisualResult> completed)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            if (IsRunning || m_IsFinishing)
                throw new InvalidOperationException("A spin presentation is already active.");

            m_Result = result;
            m_Completed = completed;
            m_Operation = new object();
            m_Elapsed = TimeSpan.Zero;
            RunStep(m_View.Spin, PlayHighlight);
        }

        public void Cancel()
        {
            if (IsRunning)
                Finish(VisualResult.Cancelled, true);
        }

        // The screen owner supplies unscaled elapsed time; tests advance it directly.
        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (!IsRunning)
                return;

            if (elapsed >= m_Timeout - m_Elapsed)
                Finish(VisualResult.Failed(new TimeoutException("Spin presentation did not finish.")), true);
            else
                m_Elapsed += elapsed;
        }

        private void PlayHighlight()
        {
            RunStep(m_View.Highlight, PlayOutcome);
        }

        private void PlayOutcome()
        {
            RunStep(m_View.PlayOutcome, () => Finish(VisualResult.Completed, false));
        }

        private void RunStep(Action<SpinResult, Action<VisualResult>> play, Action next)
        {
            var operation = m_Operation;
            var step = new object();
            m_Step = step;
            try
            {
                play(m_Result, result =>
                {
                    if (!ReferenceEquals(m_Operation, operation) || !ReferenceEquals(m_Step, step))
                        return;

                    // Invalidate this stage before starting another, even for synchronous callbacks.
                    m_Step = null;
                    if (result == null)
                        Finish(VisualResult.Failed(new InvalidOperationException("View returned no result.")), true);
                    else if (result.Status == VisualStatus.Completed)
                        next();
                    else
                        Finish(result, true);
                });
            }
            catch (Exception error)
            {
                // Exceptions from the completion consumer must not become a second completion.
                if (!ReferenceEquals(m_Operation, operation))
                    throw;

                Finish(VisualResult.Failed(error), true);
            }
        }

        private void Finish(VisualResult result, bool stopVisuals)
        {
            var completed = m_Completed;
            m_Operation = null;
            m_Step = null;
            m_Result = null;
            m_Completed = null;
            m_IsFinishing = true;
            try
            {
                if (stopVisuals)
                    m_View.Cancel();
            }
            catch (Exception error)
            {
                result = VisualResult.Failed(result.Error == null
                    ? error
                    : new AggregateException(result.Error, error));
            }
            finally
            {
                m_IsFinishing = false;
            }

            completed(result);
        }
    }
}
