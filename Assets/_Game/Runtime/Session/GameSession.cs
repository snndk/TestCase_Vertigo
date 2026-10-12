using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class GameSession
    {
        private readonly RoundGenerator m_Rounds;
        private readonly SpinSelector m_Spins;
        private readonly ProgressService m_Progress;
        private readonly Reward m_InitialReviveCost;
        private readonly int m_ReviveIncreasePercent;
        private Reward m_ReviveCost;
        private RewardCollection m_RecoverableLoot;
        private Guid m_RunId;
        private long m_OperationSequence;
        private SessionStatus m_Status;
        private Round m_Round;
        private RewardCollection m_RunLoot;
        private SpinResult m_PendingSpin;
        private SliceContext m_PendingContext;
        private CashOutResult m_LastCashOut;
        private AutomaticRun m_AutomaticRun;

        public GameSession(WheelConfiguration configuration, IRandomSource random, IProgressStore store,
            Reward reviveCost = null, int reviveIncreasePercent = 100)
        {
            if (reviveIncreasePercent < 0)
                throw new ArgumentOutOfRangeException(nameof(reviveIncreasePercent));
            m_ReviveIncreasePercent = reviveIncreasePercent;
            m_Rounds = new RoundGenerator(configuration, random);
            m_Spins = new SpinSelector(random);
            m_Progress = new ProgressService(store);
            m_InitialReviveCost = reviveCost;
            BeginRun();
        }

        public SessionSnapshot GetSnapshot()
        {
            return new SessionSnapshot(m_RunId, m_Status, m_Round, m_RunLoot, m_Progress.Rewards,
                m_PendingSpin, m_LastCashOut, m_ReviveCost, m_RecoverableLoot, m_AutomaticRun?.Snapshot);
        }

        public SpinResponse RequestSpin()
        {
            if (m_AutomaticRun != null)
                return SpinResponse.Reject(CommandStatus.NotReady);
            return StartSpin(false);
        }

        public SpinResponse RequestAutomaticSpin()
        {
            if (m_AutomaticRun == null)
                return SpinResponse.Reject(CommandStatus.NotReady);
            return StartSpin(true);
        }

        private SpinResponse StartSpin(bool automatic)
        {
            if (m_Status != SessionStatus.Ready)
                return SpinResponse.Reject(CommandStatus.NotReady);

            // Reject reentrant commands while a behavior prepares its local transaction.
            m_Status = SessionStatus.PreparingSpin;
            try
            {
                return PrepareSpin(automatic);
            }
            finally
            {
                if (m_Status == SessionStatus.PreparingSpin)
                    m_Status = SessionStatus.Ready;
            }
        }

        private SpinResponse PrepareSpin(bool automatic)
        {
            Predicate<SliceData> eligible = null;
            if (m_AutomaticRun != null)
                eligible = m_AutomaticRun.CanSelect;
            var winningIndex = m_Spins.SelectSlice(m_Round, eligible);
            var context = ResolveWinner(winningIndex);
            return CommitSpin(winningIndex, automatic, context);
        }

        private SliceContext ResolveWinner(int winningIndex)
        {
            var context = new SliceContext(m_Rounds, m_Round, m_RunLoot, m_AutomaticRun);
            try
            {
                var winner = m_Rounds.CreateBehavior(m_Round, winningIndex, context);
                winner.Resolve();
                context.ValidateComplete();
            }
            finally
            {
                // Escaped behavior/context references cannot write after success or failure.
                context.Close();
            }

            return context;
        }

        private SpinResponse CommitSpin(int winningIndex, bool automatic, SliceContext context)
        {
            var nextSequence = checked(m_OperationSequence + 1);
            var result = new SpinResult(new OperationId(m_RunId, nextSequence), m_Round, winningIndex, automatic);
            m_RecoverableLoot = !context.ContinuesRun && m_ReviveCost != null ? m_RunLoot : RewardCollection.Empty;
            m_RunLoot = context.Loot;
            m_OperationSequence = nextSequence;
            m_PendingSpin = result;
            m_PendingContext = context;
            m_Status = SessionStatus.Resolving;
            return SpinResponse.Accept(result);
        }

        public CommandStatus CompleteResolution(OperationId operationId)
        {
            if (m_Status != SessionStatus.Resolving || m_PendingSpin == null ||
                !m_PendingSpin.OperationId.Equals(operationId))
                return CommandStatus.StaleOperation;

            m_Round = m_PendingContext.NextRound;
            m_AutomaticRun = m_PendingContext.AutomaticRun;
            m_Status = m_PendingContext.ContinuesRun ? SessionStatus.Ready : SessionStatus.Lost;
            if (m_Status == SessionStatus.Lost && m_ReviveCost != null)
                m_Status = SessionStatus.AwaitingRevive;

            m_PendingSpin = null;
            m_PendingContext = null;
            return CommandStatus.Accepted;
        }

        public CashOutResponse RequestCashOut()
        {
            if (m_AutomaticRun != null)
                return CashOutResponse.Reject(CommandStatus.NotReady);
            if (m_Status != SessionStatus.Ready)
                return CashOutResponse.Reject(CommandStatus.NotReady);
            if (!ZoneRules.IsSafe(m_Round.Zone))
                return CashOutResponse.Reject(CommandStatus.UnsafeZone);

            var result = new CashOutResult(m_RunId, m_Round.Zone, m_RunLoot);
            var response = CashOutResponse.Accept(result);
            m_Status = SessionStatus.CashingOut;
            try
            {
                if (!m_Progress.TryCollect(m_RunLoot))
                    return CashOutResponse.Reject(CommandStatus.SaveFailed);

                m_AutomaticRun = null;
                m_LastCashOut = result;
                m_RunLoot = RewardCollection.Empty;
                m_Status = SessionStatus.CashedOut;
                return response;
            }
            finally
            {
                // A failed write leaves all rewards available for the same cash-out retry.
                if (m_Status == SessionStatus.CashingOut)
                    m_Status = SessionStatus.Ready;
            }
        }

        public CommandStatus StartNewRun()
        {
            if (m_Status != SessionStatus.Lost && m_Status != SessionStatus.CashedOut)
                return CommandStatus.RunNotFinished;

            BeginRun();
            return CommandStatus.Accepted;
        }

        public CommandStatus RequestRevive()
        {
            if (m_Status != SessionStatus.AwaitingRevive)
                return CommandStatus.NotReady;
            if (m_Progress.Rewards.GetAmount(m_ReviveCost.RewardId) < m_ReviveCost.Amount)
                return CommandStatus.InsufficientFunds;

            // Prepare both values before charging; overflow or generation failure must not consume payment.
            var nextCost = GetNextReviveCost();
            var nextRound = m_Rounds.Generate(checked(m_Round.Zone + 1));
            m_Status = SessionStatus.Reviving;
            try
            {
                if (!m_Progress.TrySpend(m_ReviveCost))
                    return CommandStatus.SaveFailed;
                m_ReviveCost = nextCost;
                m_RunLoot = m_RecoverableLoot;
                m_RecoverableLoot = RewardCollection.Empty;
                m_Round = nextRound;
                m_Status = SessionStatus.Ready;
                return CommandStatus.Accepted;
            }
            finally
            {
                if (m_Status == SessionStatus.Reviving)
                    m_Status = SessionStatus.AwaitingRevive;
            }
        }

        private Reward GetNextReviveCost()
        {
            // Currency uses whole units; round fractional increases up without losing long precision.
            var multiplier = 1m + m_ReviveIncreasePercent / 100m;
            var amount = checked((long)decimal.Ceiling(m_ReviveCost.Amount * multiplier));
            return new Reward(m_ReviveCost.RewardId, amount);
        }

        public CommandStatus GiveUp()
        {
            if (m_Status != SessionStatus.AwaitingRevive)
                return CommandStatus.NotReady;
            m_RecoverableLoot = RewardCollection.Empty;
            m_Status = SessionStatus.Lost;
            return CommandStatus.Accepted;
        }

        private void BeginRun()
        {
            var firstRound = m_Rounds.Generate(1);
            m_ReviveCost = m_InitialReviveCost;
            m_RunId = Guid.NewGuid();
            m_OperationSequence = 0;
            m_Round = firstRound;
            m_RunLoot = RewardCollection.Empty;
            m_RecoverableLoot = RewardCollection.Empty;
            m_PendingSpin = null;
            m_PendingContext = null;
            m_LastCashOut = null;
            m_AutomaticRun = null;
            m_Status = SessionStatus.Ready;
        }
    }
}
