using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class BombSlice : RoundSlice
    {
        public BombSlice(string id) : base(id)
        {
        }

        protected override SliceResolution ResolveCore(RewardCollection loot)
        {
            return SliceResolution.Lose();
        }
    }
}
