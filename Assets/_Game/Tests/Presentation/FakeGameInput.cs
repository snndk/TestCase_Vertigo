using System;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FakeGameInput : IGameInput
    {
        public event Action SpinRequested;
        public event Action CashOutRequested;
        public event Action RestartRequested;
        public event Action ReviveRequested;
        public event Action GiveUpRequested;
        public void RequestRevive() => ReviveRequested?.Invoke();
        public void RequestGiveUp() => GiveUpRequested?.Invoke();

        public int SpinListeners => SpinRequested?.GetInvocationList().Length ?? 0;
        public int CashOutListeners => CashOutRequested?.GetInvocationList().Length ?? 0;
        public int RestartListeners => RestartRequested?.GetInvocationList().Length ?? 0;

        public void RequestSpin()
        {
            SpinRequested?.Invoke();
        }

        public void RequestCashOut()
        {
            CashOutRequested?.Invoke();
        }

        public void RequestRestart()
        {
            RestartRequested?.Invoke();
        }
    }
}
