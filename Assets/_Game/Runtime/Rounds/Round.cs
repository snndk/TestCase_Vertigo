using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class Round
    {
        public int Zone { get; }
        public WheelTier Tier { get; }
        public IReadOnlyList<RoundSlice> Slices { get; }

        public IReadOnlyList<int> SourceIndices { get; }

        internal Round(int zone, WheelTier tier, IList<RoundSlice> slices, IEnumerable<int> sourceIndices)
        {
            SourceIndices = new ReadOnlyCollection<int>(new List<int>(sourceIndices));
            Zone = zone;
            Tier = tier;
            Slices = new ReadOnlyCollection<RoundSlice>(new List<RoundSlice>(slices));
        }
    }
}
