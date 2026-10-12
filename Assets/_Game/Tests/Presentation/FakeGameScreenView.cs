using System;
using System.Collections.Generic;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FakeGameScreenView : IGameScreenView
    {
        public event Action SpinRequested;
        public event Action CashOutRequested;
        public event Action RestartRequested;
        public event Action ReviveRequested;
        public event Action GiveUpRequested;
        public void RequestRevive() => ReviveRequested?.Invoke();
        public void RequestGiveUp() => GiveUpRequested?.Invoke();

        public int SliceCount { get; set; } = 3;
        public GameScreenState State { get; private set; }
        public List<GameScreenState> History { get; } = new List<GameScreenState>();
        public Exception RenderFailure { get; set; }
        public Action DuringRender { get; set; }
        public int SpinListeners => SpinRequested?.GetInvocationList().Length ?? 0;
        public int CashOutListeners => CashOutRequested?.GetInvocationList().Length ?? 0;
        public int RestartListeners => RestartRequested?.GetInvocationList().Length ?? 0;

        public void Render(GameScreenState state)
        {
            if (RenderFailure != null)
                throw RenderFailure;

            State = state;
            History.Add(state);
            DuringRender?.Invoke();
        }

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
