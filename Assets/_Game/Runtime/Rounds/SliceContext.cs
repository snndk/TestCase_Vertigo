using System;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class SliceContext
    {
        private readonly RoundGenerator m_Rounds;
        private readonly Round m_Round;
        private bool m_HasTransition;
        private bool m_Closed;
        public int Zone => m_Round.Zone;
        public RewardCollection Loot { get; private set; }
        internal Round NextRound { get; private set; }
        internal bool ContinuesRun { get; private set; }
        internal AutomaticRun AutomaticRun { get; private set; }

        internal SliceContext(RoundGenerator rounds, Round round, RewardCollection loot, AutomaticRun automaticRun)
        {
            m_Rounds = rounds;
            m_Round = round;
            Loot = loot;
            AutomaticRun = automaticRun;
        }

        public void AddReward(Reward reward)
        {
            EnsureWritable();
            Loot = Loot.Add(reward);
        }

        public void StartAutomaticRun(int targetZone, Predicate<SliceData> eligible, string displayName)
        {
            EnsureWritable();
            if (targetZone <= Zone)
                throw new ArgumentOutOfRangeException(nameof(targetZone));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("An automatic run display name is required.", nameof(displayName));
            AutomaticRun = new AutomaticRun(targetZone, eligible, displayName);
        }

        public void ContinueToNextRound()
        {
            EnsureWritable();
            // Generate before publishing any state; a bad next round aborts the whole operation.
            var nextRound = m_Rounds.Generate(checked(Zone + 1));
            NextRound = nextRound;
            AutomaticRun = AutomaticRun?.AtZone(nextRound.Zone);
            ContinuesRun = true;
            m_HasTransition = true;
        }

        public void LoseRun()
        {
            EnsureWritable();
            Loot = RewardCollection.Empty;
            AutomaticRun = null;
            NextRound = m_Round;
            ContinuesRun = false;
            m_HasTransition = true;
        }

        internal void EnsureWritable()
        {
            if (m_Closed || m_HasTransition)
                throw new InvalidOperationException("This slice operation no longer accepts commands.");
        }

        internal void ValidateComplete()
        {
            if (!m_HasTransition)
                throw new InvalidOperationException("A slice must choose a round transition.");
        }

        internal void Close() => m_Closed = true;
    }
}
