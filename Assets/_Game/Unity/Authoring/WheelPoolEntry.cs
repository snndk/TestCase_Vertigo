using System;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [Serializable]
    public class WheelPoolEntry
    {
        [SerializeField] private SliceAsset slice;
        [SerializeField, Range(0, 100)]
        [Tooltip("100: guaranteed. 0: excluded. 1-99: relative inclusion weight, not the chance of winning a spin.")]
        private int weight = 100;
        [SerializeField, Min(1)]
        [Tooltip("Relative chance of winning after this entry is selected for the wheel. Minimum 1; 100 is not guaranteed. Equal values give equal chances.")]
        private int spinWeight = 1;

        public SliceAsset Slice => slice;
        public int Weight => weight;
        public int SpinWeight => spinWeight;

        public WeightedSlice CreateRuntime()
        {
            if (slice == null || slice.Icon == null)
                throw new InvalidOperationException("A pool entry needs a slice and its icon.");
            return new WeightedSlice(slice.CreateDefinition(), weight, spinWeight);
        }
    }
}
