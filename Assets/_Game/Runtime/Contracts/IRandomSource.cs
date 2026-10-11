namespace Vertigo.Runtime._Game.Runtime.Contracts
{
    public interface IRandomSource
    {
        int Next(int exclusiveUpperBound);
    }
}
