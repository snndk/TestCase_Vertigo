namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class BombSliceDefinition : SliceDefinition
    {
        public BombSliceDefinition(string id) : base(id)
        {
        }

        protected override RoundSlice CreateSliceCore(int zone)
        {
            return new BombSlice(Id);
        }
    }
}
