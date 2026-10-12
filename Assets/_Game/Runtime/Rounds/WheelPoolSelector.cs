using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class WheelPoolSelector
    {
        private readonly IRandomSource m_Random;

        public WheelPoolSelector(IRandomSource random)
        {
            m_Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<int> Select(IReadOnlyList<WeightedSlice> candidates, int count)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            var selected = new List<int>();
            var optional = new List<int>();
            var totalWeight = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index] ?? throw new ArgumentException("Missing pool candidate.", nameof(candidates));
                if (candidate.IsGuaranteed)
                    selected.Add(index);
                else if (candidate.Weight > 0)
                {
                    optional.Add(index);
                    totalWeight = checked(totalWeight + candidate.Weight);
                }
            }
            if (selected.Count > count || selected.Count + optional.Count < count)
                throw new ArgumentException("The pool cannot fill the requested wheel capacity.", nameof(candidates));

            while (selected.Count < count)
            {
                var roll = m_Random.Next(totalWeight);
                if (roll < 0 || roll >= totalWeight)
                    throw new InvalidOperationException("The random source returned an invalid pool roll.");
                for (var index = 0; index < optional.Count; index++)
                {
                    var candidateIndex = optional[index];
                    var weight = candidates[candidateIndex].Weight;
                    if (roll < weight)
                    {
                        selected.Add(candidateIndex);
                        optional.RemoveAt(index);
                        totalWeight -= weight;
                        break;
                    }
                    roll -= weight;
                }
            }
            // Preserve Inspector ordering; repeated references remain distinct pool entries.
            selected.Sort();
            return selected.AsReadOnly();
        }
    }
}
