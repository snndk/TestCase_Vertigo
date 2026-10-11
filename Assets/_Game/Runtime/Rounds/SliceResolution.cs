using System;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class SliceResolution
    {
        public RewardCollection Loot { get; }
        public bool ContinuesRun { get; }

        private SliceResolution(RewardCollection loot, bool continuesRun)
        {
            Loot = loot ?? throw new ArgumentNullException(nameof(loot));
            ContinuesRun = continuesRun;
        }

        public static SliceResolution ContinueWith(RewardCollection loot)
        {
            return new SliceResolution(loot, true);
        }

        public static SliceResolution Lose()
        {
            return new SliceResolution(
                RewardCollection.Empty, false);
        }
    }
}
