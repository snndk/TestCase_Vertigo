using System;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime
{
    internal class UnexpectedBombDefinition : SliceDefinition
    {
        private readonly string m_GeneratedId;

        public UnexpectedBombDefinition(string id, string generatedId = null) : base(id)
        {
            m_GeneratedId = generatedId ?? id;
        }

        protected override RoundSlice CreateSliceCore(int zone)
        {
            return new BombSlice(m_GeneratedId);
        }
    }
}
