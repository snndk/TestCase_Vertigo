namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class BombSliceDefinition : SliceDefinition
    {
        public BombSliceDefinition(string id) : base(id)
        {
        }

        protected override SliceData CreateDataCore(int zone)
        {
            return new BombSliceData(Id);
        }

        protected override RoundSlice CreateBehaviorCore(SliceData data, SliceContext context)
        {
            return new BombSlice(context);
        }
    }
}
