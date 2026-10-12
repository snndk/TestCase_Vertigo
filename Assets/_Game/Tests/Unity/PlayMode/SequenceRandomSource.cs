using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<int> m_Values;

        public SequenceRandomSource(IEnumerable<int> values)
        {
            m_Values = new Queue<int>(values);
        }

        public int Next(int exclusiveUpperBound) => m_Values.Count == 0 ? 0 : m_Values.Dequeue();
    }
}
