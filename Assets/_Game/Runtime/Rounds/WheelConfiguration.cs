using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class WheelConfiguration
    {
        private readonly ReadOnlyCollection<WeightedSlice> m_Bronze;
        private readonly ReadOnlyCollection<WeightedSlice> m_Silver;
        private readonly ReadOnlyCollection<WeightedSlice> m_Gold;
        public int SliceCount { get; }

        // Fixed templates are pools whose entries are all guaranteed.
        public WheelConfiguration(IEnumerable<SliceDefinition> bronze,
            IEnumerable<SliceDefinition> silver, IEnumerable<SliceDefinition> gold)
            : this(ToGuaranteedPool(bronze), ToGuaranteedPool(silver), ToGuaranteedPool(gold))
        {
        }

        private WheelConfiguration(WeightedSlice[] bronze, WeightedSlice[] silver, WeightedSlice[] gold)
            : this(bronze.Length, bronze, silver, gold)
        {
        }

        public WheelConfiguration(int sliceCount, IEnumerable<WeightedSlice> bronze,
            IEnumerable<WeightedSlice> silver, IEnumerable<WeightedSlice> gold)
        {
            if (sliceCount < 3)
                throw new ArgumentOutOfRangeException(nameof(sliceCount));
            SliceCount = sliceCount;
            m_Bronze = Copy(bronze);
            m_Silver = Copy(silver);
            m_Gold = Copy(gold);
            WheelPoolValidator.Validate(this);
        }

        public IReadOnlyList<SliceDefinition> GetSlices(WheelTier tier)
        {
            return GetCandidates(tier).Select(candidate => candidate.Definition).ToList().AsReadOnly();
        }

        public IReadOnlyList<WeightedSlice> GetCandidates(WheelTier tier)
        {
            switch (tier)
            {
                case WheelTier.Bronze:
                    return m_Bronze;
                case WheelTier.Silver:
                    return m_Silver;
                case WheelTier.Gold:
                    return m_Gold;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static ReadOnlyCollection<WeightedSlice> Copy(IEnumerable<WeightedSlice> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new List<WeightedSlice>(source).AsReadOnly();
        }

        private static WeightedSlice[] ToGuaranteedPool(IEnumerable<SliceDefinition> slices)
        {
            if (slices == null)
                throw new ArgumentNullException(nameof(slices));
            return slices.Select(slice => new WeightedSlice(slice, 100)).ToArray();
        }
    }
}
