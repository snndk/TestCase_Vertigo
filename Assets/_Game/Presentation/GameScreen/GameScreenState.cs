using System;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;


namespace Vertigo.Presentation._Game.Presentation.GameScreen
{
    public class GameScreenState
    {
        public Round Round { get; }
        public RewardCollection Loot { get; }
        public RewardCollection Progress { get; }
        public bool CanSpin { get; }
        public bool CanCashOut { get; }
        public bool CanRestart { get; }
        public bool ShowBomb { get; }
        public CashOutResult CashOut { get; }
        public PresentationError Error { get; }

        public GameScreenState(SessionSnapshot snapshot, RewardCollection displayedLoot, PresentationError error)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Loot = displayedLoot ?? throw new ArgumentNullException(nameof(displayedLoot));
            Round = snapshot.Round;
            Progress = snapshot.Progress;
            CanSpin = snapshot.CanSpin;
            CanCashOut = snapshot.CanCashOut;
            CanRestart = snapshot.Status == SessionStatus.Lost || snapshot.Status == SessionStatus.CashedOut;
            ShowBomb = snapshot.Status == SessionStatus.Lost;
            CashOut = snapshot.LastCashOut;
            Error = error;
        }
    }
}
