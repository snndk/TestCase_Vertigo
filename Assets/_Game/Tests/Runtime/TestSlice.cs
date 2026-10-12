using System;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal class TestSlice : RoundSlice
    {
        private readonly Action m_OnResolve;

        public TestSlice(Action onResolve) : base("test")
        {
            m_OnResolve = onResolve;
        }

        protected override SliceResolution ResolveCore(
            RewardCollection loot)
        {
            m_OnResolve();
            return SliceResolution.ContinueWith(
                loot.Add(new Reward("test", 7)));
        }
    }
}
