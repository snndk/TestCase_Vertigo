using System;
using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class RoundGenerator
    {
        private readonly WheelConfiguration m_Configuration;

        public RoundGenerator(WheelConfiguration configuration)
        {
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Round Generate(int zone)
        {
            var tier = ZoneRules.GetTier(zone);
            var slices = new List<RoundSlice>(m_Configuration.SliceCount);
            foreach (var definition in m_Configuration.GetSlices(tier))
                slices.Add(definition.CreateSlice(zone));

            m_Configuration.ValidateRound(zone, slices);
            return new Round(zone, tier, slices);
        }
    }
}
