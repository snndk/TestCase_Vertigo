using System;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal class TestSlice : RoundSlice
    {
        private readonly Action<SliceContext> m_Action;

        public TestSlice(SliceContext context, Action<SliceContext> action) : base(context)
        {
            m_Action = action;
        }

        protected override void ResolveCore() => m_Action(Context);
    }
}
