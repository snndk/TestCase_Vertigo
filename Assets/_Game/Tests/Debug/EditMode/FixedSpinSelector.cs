using System;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Debug._Game.Tests.Debug.EditMode
{
    public class FixedSpinSelector : ISpinSelector
    {
        public int Index { get; set; }
        public int SelectSlice(Round round, Predicate<SliceData> eligible = null) => Index;
    }
}
