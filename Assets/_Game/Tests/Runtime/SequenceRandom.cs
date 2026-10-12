using System.Collections.Generic;
using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    internal class SequenceRandom : IRandomSource
    {
        private readonly Queue<int> m_Values = new Queue<int>();
        public int Calls { get; private set; }

        public void Enqueue(int value)
        {
            m_Values.Enqueue(value);
        }

        public int Next(int exclusiveUpperBound)
        {
            Calls++;
            return m_Values.Count == 0 ? 0 : m_Values.Dequeue();
        }
    }
}
