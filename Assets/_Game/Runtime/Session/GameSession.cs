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
        private Guid m_RunId;
        private long m_OperationSequence;
        private SessionStatus m_Status;
        private Round m_Round;
        private RewardCollection m_RunLoot;
        private SpinResult m_PendingSpin;
        private SliceResolution m_PendingResolution;
        private Round m_PreparedNextRound;
        private CashOutResult m_LastCashOut;

        public GameSession(WheelConfiguration configuration, IRandomSource random, IProgressStore store)
        {
            m_Rounds = new RoundGenerator(configuration, random);
            m_Spins = new SpinSelector(random);
            m_Progress = new ProgressService(store);
            BeginRun();
        }

        public SessionSnapshot GetSnapshot()
        {
            return new SessionSnapshot(m_RunId, m_Status, m_Round, m_RunLoot, m_Progress.Rewards, m_PendingSpin, m_LastCashOut);
        }

        public SpinResponse RequestSpin()
        {
            if (m_Status != SessionStatus.Ready)
                return SpinResponse.Reject(CommandStatus.NotReady);

            var winningIndex = m_Spins.SelectSlice(m_Round);
            var winner = m_Round.Slices[winningIndex];
            // Resolve and prepare the transition before committing any session state.
            var resolution = winner.Resolve(m_RunLoot);
            var nextRound = resolution.ContinuesRun ? m_Rounds.Generate(checked(m_Round.Zone + 1)) : m_Round;
            var nextSequence = checked(m_OperationSequence + 1);
            var result = new SpinResult(new OperationId(m_RunId, nextSequence), m_Round, winningIndex);

            m_RunLoot = resolution.Loot;
            m_OperationSequence = nextSequence;
            m_PendingSpin = result;
            m_PendingResolution = resolution;
            m_PreparedNextRound = nextRound;
            m_Status = SessionStatus.Resolving;
            return SpinResponse.Accept(result);
        }

        public CommandStatus CompleteResolution(OperationId operationId)
        {
            if (m_Status != SessionStatus.Resolving || m_PendingSpin == null ||
                !m_PendingSpin.OperationId.Equals(operationId))
                return CommandStatus.StaleOperation;

            m_Round = m_PreparedNextRound;
            m_Status = m_PendingResolution.ContinuesRun ? SessionStatus.Ready : SessionStatus.Lost;

            m_PendingSpin = null;
            m_PendingResolution = null;
            m_PreparedNextRound = null;
            return CommandStatus.Accepted;
        }

        public CashOutResponse RequestCashOut()
        {
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

        private void BeginRun()
        {
            var firstRound = m_Rounds.Generate(1);
            m_RunId = Guid.NewGuid();
            m_OperationSequence = 0;
            m_Round = firstRound;
            m_RunLoot = RewardCollection.Empty;
            m_PendingSpin = null;
            m_PendingResolution = null;
            m_PreparedNextRound = null;
            m_LastCashOut = null;
            m_Status = SessionStatus.Ready;
        }
    }
}
