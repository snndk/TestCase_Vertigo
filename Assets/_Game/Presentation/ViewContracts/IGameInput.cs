using System;

namespace Vertigo.Presentation._Game.Presentation.ViewContracts
{
    public interface IGameInput
    {
        event Action SpinRequested;
        event Action CashOutRequested;
        event Action RestartRequested;
        event Action ReviveRequested;
        event Action GiveUpRequested;
    }
}
