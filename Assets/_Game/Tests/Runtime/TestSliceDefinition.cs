using System;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal class TestSliceDefinition : SliceDefinition
    {
        private readonly Action<SliceContext> m_Action;
        private readonly Action<RoundSlice> m_Capture;

        public TestSliceDefinition(Action onResolve) : this(context =>
        {
            onResolve();
            context.AddReward(new Reward("test", 7));
            context.ContinueToNextRound();
        })
        {
        }

        public TestSliceDefinition(Action<SliceContext> action, Action<RoundSlice> capture = null) : base("test")
        {
            m_Action = action;
            m_Capture = capture;
        }

        protected override SliceData CreateDataCore(int zone) => new SliceData(Id);

        protected override RoundSlice CreateBehaviorCore(SliceData data, SliceContext context)
        {
            var slice = new TestSlice(context, m_Action);
            m_Capture?.Invoke(slice);
            return slice;
        }
    }
}
