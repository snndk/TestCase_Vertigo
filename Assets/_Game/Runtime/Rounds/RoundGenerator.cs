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
            var slices = new List<SliceData>(m_Configuration.SliceCount);
            var candidates = m_Configuration.GetCandidates(tier);
            var indices = m_Selector.Select(candidates, m_Configuration.SliceCount);
            foreach (var index in indices)
                slices.Add(candidates[index].Definition.CreateData(zone));

            RoundValidator.Validate(m_Configuration, zone, slices, indices);
            return new Round(zone, tier, slices, indices);
        }

        internal RoundSlice CreateBehavior(Round round, int index, SliceContext context)
        {
            var source = round.SourceIndices[index];
            var definition = m_Configuration.GetCandidates(round.Tier)[source].Definition;
            return definition.CreateBehavior(round.Slices[index], context);
        }
    }
}
