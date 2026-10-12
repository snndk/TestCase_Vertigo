using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal class UnexpectedBombDefinition : SliceDefinition
    {
        private readonly string m_GeneratedId;

        public UnexpectedBombDefinition(string id, string generatedId = null) : base(id)
        {
            m_GeneratedId = generatedId ?? id;
        }

        protected override SliceData CreateDataCore(int zone)
        {
            return new BombSliceData(m_GeneratedId);
        }
        protected override RoundSlice CreateBehaviorCore(SliceData data, SliceContext context)
        {
            return new BombSlice(context);
        }
    }
}
