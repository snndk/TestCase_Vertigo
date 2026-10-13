using DG.Tweening;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    public abstract class WheelAnimationAsset : ScriptableObject
    {
        // Assets compose the animation; scene views own playback and mutable visual state.
        public abstract void BuildEntrance(Sequence sequence, WheelTierTransition view, WheelTierAsset tier);

        public virtual void InsertHighlight(Sequence sequence, RewardCelebrationView view,
            RectTransform source, WheelTierAsset tier) { }

        public virtual void InsertArrival(Sequence sequence, float arrivalTime, RewardCelebrationView view,
            RectTransform target, WheelTierAsset tier) { }
    }
}
