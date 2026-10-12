using System;
using System.Collections.Generic;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FakeSpinView : ISpinView
    {
        public List<VisualRequest> Requests { get; } = new List<VisualRequest>();
        public int CancelCalls { get; private set; }
        public bool CompleteSynchronously { get; set; }
        public string ThrowOnStage { get; set; }
        public bool ThrowOnCancel { get; set; }
        public Action DuringCancel { get; set; }

        public void Spin(SpinResult result, Action<VisualResult> completed)
        {
            Start("spin", result, completed);
        }

        public void Highlight(SpinResult result, Action<VisualResult> completed)
        {
            Start("highlight", result, completed);
        }

        public void PlayOutcome(SpinResult result, Action<VisualResult> completed)
        {
            Start("outcome", result, completed);
        }

        public void Cancel()
        {
            CancelCalls++;
            DuringCancel?.Invoke();
            if (ThrowOnCancel)
                throw new InvalidOperationException("Visual cleanup failed.");
        }

        public void CompleteLatest(VisualResult result = null)
        {
            Requests[Requests.Count - 1].Complete(result ?? VisualResult.Completed);
        }

        private void Start(string stage, SpinResult result, Action<VisualResult> completed)
        {
            Requests.Add(new VisualRequest(stage, result, completed));
            if (ThrowOnStage == stage)
                throw new InvalidOperationException("Visual start failed: " + stage);
            if (CompleteSynchronously)
                completed(VisualResult.Completed);
        }
    }
}
