using System;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class SessionSnapshot
    {
        public Guid RunId { get; }
        public SessionStatus Status { get; }
        public Round Round { get; }
        public RewardCollection RunLoot { get; }
        public RewardCollection Progress { get; }
        public SpinResult PendingSpin { get; }
        public CashOutResult LastCashOut { get; }
        public Reward ReviveCost { get; }
        public RewardCollection RecoverableLoot { get; }
        public bool CanRevive => Status == SessionStatus.AwaitingRevive && ReviveCost != null &&
            Progress.GetAmount(ReviveCost.RewardId) >= ReviveCost.Amount;
        public AutomaticRunSnapshot AutomaticRun { get; }
        public bool InputBlocked => AutomaticRun != null;
        public bool CanAutomaticSpin => Status == SessionStatus.Ready && AutomaticRun != null;
        public bool CanSpin => Status == SessionStatus.Ready && !InputBlocked;
        public bool CanCashOut => CanSpin && ZoneRules.IsSafe(Round.Zone);

        internal SessionSnapshot(Guid runId, SessionStatus status, Round round,
            RewardCollection runLoot, RewardCollection progress, SpinResult pendingSpin, CashOutResult lastCashOut,
            Reward reviveCost = null, RewardCollection recoverableLoot = null, AutomaticRunSnapshot automaticRun = null)
        {
            RunId = runId;
            Status = status;
            Round = round;
            RunLoot = runLoot;
            Progress = progress;
            PendingSpin = pendingSpin;
            LastCashOut = lastCashOut;
            ReviveCost = reviveCost;
            RecoverableLoot = recoverableLoot ?? RewardCollection.Empty;
            AutomaticRun = automaticRun;
        }
    }
}
