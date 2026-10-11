using System;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Tests.Runtime
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
