using System.Collections.Generic;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.EditMode
{
    public class AdditionalRewardSliceAsset : SliceAsset
    {
        private RewardAsset m_First;
        private RewardAsset m_Second;
        public override Sprite Icon => m_First.Icon;

        public void Initialize(RewardAsset first, RewardAsset second)
        {
            m_First = first;
            m_Second = second;
        }

        public override IEnumerable<RewardAsset> GetRewards()
        {
            yield return m_First;
            yield return m_Second;
        }

        public override SliceDefinition CreateDefinition() => new RewardSliceDefinition("additional", m_First.CreateDefinition(), 1);
        public override string GetCaption(RoundSlice slice) => "TEST";
    }
}
