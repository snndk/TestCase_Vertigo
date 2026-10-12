namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class LuckySliceDefinition : SliceDefinition
    {
        public LuckySliceDefinition(string id) : base(id)
        {
        }

        protected override SliceData CreateDataCore(int zone)
        {
            return new LuckySliceData(Id);
        }

        protected override RoundSlice CreateBehaviorCore(SliceData data, SliceContext context)
        {
            return new LuckySlice(context);
        }
    }
}
