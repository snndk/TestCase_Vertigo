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

        internal Round(int zone, WheelTier tier, IList<RoundSlice> slices)
        {
            Zone = zone;
            Tier = tier;
            Slices = new ReadOnlyCollection<RoundSlice>(new List<RoundSlice>(slices));
        }
    }
}
