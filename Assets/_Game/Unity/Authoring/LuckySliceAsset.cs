using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Slices/Lucky")]
    public class LuckySliceAsset : SliceAsset
    {
        [SerializeField] private Sprite icon;
        public override Color CaptionColor => new Color(1f, .85f, .35f);
        public override Sprite Icon => icon;
        public override SliceDefinition CreateDefinition() => new LuckySliceDefinition(Id);
        public override string GetCaption(SliceData slice) => "LUCKY";
    }
}
