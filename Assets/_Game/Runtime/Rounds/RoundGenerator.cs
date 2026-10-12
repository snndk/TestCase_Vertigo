using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RoundGenerator
    {
        private readonly WheelConfiguration m_Configuration;

        private readonly WheelPoolSelector m_Selector;

        public RoundGenerator(WheelConfiguration configuration, IRandomSource random)
        {
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            m_Selector = new WheelPoolSelector(random);
        }

        public Round Generate(int zone)
        {
            var tier = ZoneRules.GetTier(zone);
            var slices = new List<RoundSlice>(m_Configuration.SliceCount);
            var candidates = m_Configuration.GetCandidates(tier);
            var indices = m_Selector.Select(candidates, m_Configuration.SliceCount);
            foreach (var index in indices)
                slices.Add(candidates[index].Definition.CreateSlice(zone));

            RoundValidator.Validate(m_Configuration, zone, slices, indices);
            return new Round(zone, tier, slices, indices);
        }
    }
}
