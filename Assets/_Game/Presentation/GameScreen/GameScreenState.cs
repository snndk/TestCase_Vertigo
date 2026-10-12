using System;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;


namespace Vertigo.Presentation._Game.Presentation.GameScreen
{
    public class GameScreenState
    {
        public Round Round { get; }
        public SafeZoneProgress SafeZone { get; }
        public RewardCollection Loot { get; }
        public RewardCollection Progress { get; }
        public string AutomaticRunMessage { get; }
        public bool InputBlocked { get; }
        public bool CanSpin { get; }
        public bool CanCashOut { get; }
        public bool CanRestart { get; }
        public bool ShowBomb { get; }
        public bool AwaitingRevive { get; }
        public bool CanRevive { get; }
        public Reward ReviveCost { get; }
        public CashOutResult CashOut { get; }
        public PresentationError Error { get; }

        public GameScreenState(SessionSnapshot snapshot, RewardCollection displayedLoot, PresentationError error,
            bool automaticSpinPaused = false)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Loot = displayedLoot ?? throw new ArgumentNullException(nameof(displayedLoot));
            AwaitingRevive = snapshot.Status == SessionStatus.AwaitingRevive;
            if (AwaitingRevive)
                Loot = snapshot.RecoverableLoot;
            CanRevive = snapshot.CanRevive;
            ReviveCost = snapshot.ReviveCost;
            Round = snapshot.Round;
            SafeZone = ZoneRules.GetSafeZoneProgress(Round.Zone);
            Progress = snapshot.Progress;
            var canRetryAutomaticSpin = automaticSpinPaused && snapshot.CanAutomaticSpin;
            InputBlocked = snapshot.InputBlocked && !canRetryAutomaticSpin;
            if (snapshot.AutomaticRun != null)
            {
                AutomaticRunMessage = snapshot.AutomaticRun.DisplayName;
                AutomaticRunMessage += " - ZONE " + snapshot.AutomaticRun.TargetZone;
            }
            CanSpin = snapshot.CanSpin || canRetryAutomaticSpin;
            CanCashOut = snapshot.CanCashOut;
            CanRestart = snapshot.Status == SessionStatus.Lost || snapshot.Status == SessionStatus.CashedOut;
            ShowBomb = snapshot.Status == SessionStatus.Lost || AwaitingRevive;
            CashOut = snapshot.LastCashOut;
            Error = error;
        }
    }
}
