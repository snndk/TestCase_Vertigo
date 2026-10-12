using System;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class PresentationTestContext
    {
        public FixedRandomSource Random { get; } = new FixedRandomSource();
        public FakeProgressStore Store { get; } = new FakeProgressStore();
        public FakeGameScreenView View { get; } = new FakeGameScreenView();
        public FakeSpinView SpinView { get; } = new FakeSpinView();
        public GameSession Session { get; }
        public SpinPresentationSequence Sequence { get; }
        public GameScreenPresenter Presenter { get; }

        public PresentationTestContext()
        {
            var coins = new RewardSliceDefinition("coins", new RewardDefinition("coins", 10, 1));
            var tickets = new RewardSliceDefinition("tickets", new RewardDefinition("tickets", 2, 1));
            var configuration = new WheelConfiguration(
                new SliceDefinition[] { coins, tickets, new BombSliceDefinition("bomb") },
                new[] { coins, tickets, new RewardSliceDefinition("gems", new RewardDefinition("gems", 1, 1)) },
                new[] { coins, tickets, new RewardSliceDefinition("special", new RewardDefinition("special", 1, 1), true) });

            Session = new GameSession(configuration, Random, Store);
            Sequence = new SpinPresentationSequence(SpinView, TimeSpan.FromSeconds(10));
            Presenter = new GameScreenPresenter(Session, View, Sequence);
        }

        public void CompleteSpin()
        {
            for (var stage = 0; stage < 3; stage++)
                SpinView.CompleteLatest(VisualResult.Completed);
        }

        public void AdvanceTo(int zone)
        {
            while (Session.GetSnapshot().Round.Zone < zone)
            {
                Random.Index = Session.GetSnapshot().Round.Zone % 2;
                View.RequestSpin();
                CompleteSpin();
            }
        }
    }
}
