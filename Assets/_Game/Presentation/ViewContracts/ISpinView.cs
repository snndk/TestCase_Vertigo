using System;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Presentation._Game.Presentation.ViewContracts
{
    // Calls and callbacks run on the screen's owning thread. Each stage reports
    // completion, cancellation or failure, including tween kill/disable paths.
    public interface ISpinView
    {
        void Spin(SpinResult result, Action<VisualResult> completed);
        void Highlight(SpinResult result, Action<VisualResult> completed);
        void PlayOutcome(SpinResult result, Action<VisualResult> completed);

        // Stops owned visuals and releases their resources before returning.
        // It may invoke callbacks synchronously; it must not apply game rules.
        void Cancel();
    }
}
