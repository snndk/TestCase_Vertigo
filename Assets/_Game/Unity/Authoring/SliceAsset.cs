using System.Collections.Generic;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    public abstract class SliceAsset : ScriptableObject
    {
        [SerializeField] private string sliceId;
        public string Id => sliceId;
        public virtual Color CaptionColor => Color.white;
        public virtual IEnumerable<RewardAsset> GetRewards()
        {
            yield break;
        }

        public abstract Sprite Icon { get; }
        public abstract SliceDefinition CreateDefinition();
        public abstract string GetCaption(SliceData slice);
    }
}
