using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class Round
    {
        public int Zone { get; }
        public WheelTier Tier { get; }
        public IReadOnlyList<SliceData> Slices { get; }

        public IReadOnlyList<int> SourceIndices { get; }
        public IReadOnlyList<int> SpinWeights { get; }

        internal Round(int zone, WheelTier tier, IList<SliceData> slices, IEnumerable<int> sourceIndices,
            IEnumerable<int> spinWeights)
        {
            SourceIndices = new ReadOnlyCollection<int>(new List<int>(sourceIndices));
            SpinWeights = new ReadOnlyCollection<int>(new List<int>(spinWeights));
            Zone = zone;
            Tier = tier;
            Slices = new ReadOnlyCollection<SliceData>(new List<SliceData>(slices));
        }
    }
}
