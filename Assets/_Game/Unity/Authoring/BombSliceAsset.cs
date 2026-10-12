using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Slices/Bomb")]
    public class BombSliceAsset : SliceAsset
    {
        [SerializeField] private Sprite icon;
        public override Color CaptionColor => new Color(1, .3f, .25f);
        public override Sprite Icon => icon;
        public override SliceDefinition CreateDefinition() => new BombSliceDefinition(Id);
        public override string GetCaption(RoundSlice slice) => "BOMB";
    }
}
