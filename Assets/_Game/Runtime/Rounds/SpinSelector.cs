using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class SpinSelector
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
            for (var index = 0; index < round.Slices.Count; index++)
                if (eligible == null || eligible(round.Slices[index]))
                    indices.Add(index);
            if (indices.Count == 0)
                throw new InvalidOperationException("The round has no eligible winning slices.");

            var selection = m_Random.Next(indices.Count);
            if (selection < 0 || selection >= indices.Count)
                throw new InvalidOperationException("The random source returned an invalid slice index.");

            return indices[selection];
        }
    }
}
