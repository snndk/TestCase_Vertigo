using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class LuckySlice : RoundSlice
    {
        public LuckySlice(SliceContext context) : base(context)
        {
        }

        protected override void ResolveCore()
        {
            var target = ZoneRules.GetSafeZoneProgress(checked(Context.Zone + 1)).TargetZone;
            Context.StartAutomaticRun(target, slice => slice is RewardSliceData, "LUCKY RUN");
            Context.ContinueToNextRound();
        }
    }
}
