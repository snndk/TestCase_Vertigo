using System;
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

        public int SelectSlice(Round round)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            var index = m_Random.Next(round.Slices.Count);
            if (index < 0 || index >= round.Slices.Count)
                throw new InvalidOperationException("The random source returned an invalid slice index.");

            return index;
        }
    }
}
