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

        public SliceAsset Slice => slice;
        public int Weight => weight;

        public WeightedSlice CreateRuntime()
        {
            if (slice == null || slice.Icon == null)
                throw new InvalidOperationException("A pool entry needs a slice and its icon.");
            return new WeightedSlice(slice.CreateDefinition(), weight);
        }
    }
}
