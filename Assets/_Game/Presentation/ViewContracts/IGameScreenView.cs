using System;
using Vertigo.Presentation._Game.Presentation.GameScreen;

namespace Vertigo.Presentation._Game.Presentation.ViewContracts
{
    public interface IGameScreenView
    {
        event Action SpinRequested;
        event Action CashOutRequested;
        event Action RestartRequested;

        int SliceCount { get; }

        // Replaces the whole screen state, including panel visibility and errors.
        // Error.Message is user-facing; Error.Cause is available for diagnostics.
        void Render(GameScreenState state);
    }
}
