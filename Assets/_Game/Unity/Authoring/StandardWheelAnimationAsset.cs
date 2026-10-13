using DG.Tweening;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Wheel animations/Standard")]
    public class StandardWheelAnimationAsset : WheelAnimationAsset
    {
        public override void BuildEntrance(Sequence sequence, WheelTierTransition view, WheelTierAsset tier)
        {
            view.AppendTransition(sequence, tier);
        }
    }
}
