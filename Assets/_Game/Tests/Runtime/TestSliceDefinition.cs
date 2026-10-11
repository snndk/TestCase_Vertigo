using System;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime
{
    internal class TestSliceDefinition : SliceDefinition
    {
        private readonly Action m_OnResolve;

        public TestSliceDefinition(Action onResolve) : base("test")
        {
            m_OnResolve = onResolve;
        }

        protected override RoundSlice CreateSliceCore(int zone)
        {
            return new TestSlice(m_OnResolve);
        }
    }
}
