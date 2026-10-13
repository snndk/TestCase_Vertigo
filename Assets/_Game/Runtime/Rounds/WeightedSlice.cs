using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class WeightedSlice
    {
        public SliceDefinition Definition { get; }
        public int Weight { get; }
        public int SpinWeight { get; }
        public bool IsGuaranteed => Weight == 100;

        public WeightedSlice(SliceDefinition definition, int weight, int spinWeight = 1)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (weight < 0 || weight > 100)
                throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight;
            if (spinWeight < 1)
                throw new ArgumentOutOfRangeException(nameof(spinWeight));
            SpinWeight = spinWeight;
        }
    }
}
