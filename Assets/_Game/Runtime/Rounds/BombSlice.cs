namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class BombSlice : RoundSlice
    {
        public BombSlice(SliceContext context) : base(context)
        {
        }

        protected override void ResolveCore()
        {
            Context.LoseRun();
        }
    }
}
