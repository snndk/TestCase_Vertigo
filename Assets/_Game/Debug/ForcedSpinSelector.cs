using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Debug._Game.Debug
{
    public class ForcedSpinSelector : ISpinSelector
    {
        private readonly ISpinSelector m_Default;
        private Round m_Round;
        private int m_Index;

        public string Pending => m_Round == null ? "Random" : $"Slot {m_Index + 1}: {m_Round.Slices[m_Index].Id}";

        public ForcedSpinSelector(ISpinSelector defaultSelector)
        {
            m_Default = defaultSelector ?? throw new ArgumentNullException(nameof(defaultSelector));
        }

        public void Arm(Round round, int index)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));
            if (index < 0 || index >= round.Slices.Count || round.SpinWeights[index] <= 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Choose a selectable slot in the current wheel.");
            m_Round = round;
            m_Index = index;
        }

        public void Clear() => m_Round = null;

        public int SelectSlice(Round round, Predicate<SliceData> eligible = null)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));
            var selectedRound = m_Round;
            var selectedIndex = m_Index;
            Clear();
            if (ReferenceEquals(selectedRound, round) &&
                (eligible == null || eligible(round.Slices[selectedIndex])))
                return selectedIndex;
            return m_Default.SelectSlice(round, eligible);
        }
    }
}
