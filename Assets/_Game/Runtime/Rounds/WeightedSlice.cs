using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class WeightedSlice
    {
        public SliceDefinition Definition { get; }
        public int Weight { get; }
        public bool IsGuaranteed => Weight == 100;

        public WeightedSlice(SliceDefinition definition, int weight)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (weight < 0 || weight > 100)
                throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight;
        }
    }
}
