using Vertigo.Runtime._Game.Runtime.Contracts;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class FixedRandomSource : IRandomSource
    {
        public int Index { get; set; }
        public int Next(int exclusiveMaximum) => Index;
    }
}
