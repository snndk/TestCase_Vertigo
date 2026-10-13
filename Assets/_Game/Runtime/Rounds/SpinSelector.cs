using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class SpinSelector : ISpinSelector
    {
        private readonly IRandomSource m_Random;

        public SpinSelector(IRandomSource random)
        {
            m_Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int SelectSlice(Round round, Predicate<SliceData> eligible = null)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            var indices = new List<int>(round.Slices.Count);
            var totalWeight = 0;
            for (var index = 0; index < round.Slices.Count; index++)
            {
                if (eligible == null || eligible(round.Slices[index]))
                {
                    indices.Add(index);
                    totalWeight = checked(totalWeight + round.SpinWeights[index]);
                }
            }
            if (indices.Count == 0)
                throw new InvalidOperationException("The round has no eligible winning slices.");

            var selection = m_Random.Next(totalWeight);
            if (selection < 0 || selection >= totalWeight)
                throw new InvalidOperationException("The random source returned an invalid weighted roll.");

            foreach (var index in indices)
            {
                var weight = round.SpinWeights[index];
                if (selection < weight)
                    return index;
                selection -= weight;
            }

            throw new InvalidOperationException("The weighted roll did not match a slice.");
        }
    }
}
