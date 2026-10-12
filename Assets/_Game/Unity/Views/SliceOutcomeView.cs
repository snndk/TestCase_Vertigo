using DG.Tweening;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public abstract class SliceOutcomeView : MonoBehaviour
    {
        public abstract bool CanPresent(RoundSlice slice);
        public abstract Tween Play(WheelSliceView slice);
        public abstract void ResetVisuals();
    }
}
