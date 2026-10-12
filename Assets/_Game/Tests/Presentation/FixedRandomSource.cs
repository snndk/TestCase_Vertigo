using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Tests.Presentation.Assets._Game.Tests.Presentation
{
    internal class FixedRandomSource : IRandomSource
    {
        public int Index { get; set; }
        public int Calls { get; private set; }

        public int Next(int exclusiveUpperBound)
        {
            Calls++;
            return Index;
        }
    }
}
