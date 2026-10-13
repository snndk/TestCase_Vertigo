using System;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Runtime._Game.Runtime.Contracts
{
    public interface ISpinSelector
    {
        int SelectSlice(Round round, Predicate<SliceData> eligible = null);
    }
}
