using System;
using System.Collections.Generic;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FakeGameScreenView : IGameScreenView
    {
        public int SliceCount { get; set; } = 3;
        public GameScreenState State { get; private set; }
        public List<GameScreenState> History { get; } = new List<GameScreenState>();
        public Exception RenderFailure { get; set; }
        public Action DuringRender { get; set; }

        public void Render(GameScreenState state)
        {
            if (RenderFailure != null)
                throw RenderFailure;

            State = state;
            History.Add(state);
            DuringRender?.Invoke();
        }

    }
}
