using System;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Unity._Game.Unity.Bootstrap
{
    public class SystemRandomSource : IRandomSource
    {
        private readonly Random m_Random = new Random();
        public int Next(int exclusiveUpperBound) => m_Random.Next(exclusiveUpperBound);
    }
}
