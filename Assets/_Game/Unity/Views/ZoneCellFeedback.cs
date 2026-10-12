using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class ZoneCellFeedback : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private Image glow;
        [SerializeField, Min(1f)] private float arrivalScale = 1.1f;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.09f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float glowStrength = 0.65f;

        // ZoneProgressView owns playback so movement and arrival share cancellation.
        public Tween CreateArrival(bool illuminate, Color color)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(motion.DOScale(arrivalScale, riseDuration).SetEase(Ease.OutQuad));
            sequence.Append(motion.DOScale(1f, settleDuration).SetEase(Ease.OutCubic));
            if (illuminate)
            {
                var progress = 0f;
                sequence.Insert(0, DOTween.To(() => progress, value =>
                {
                    progress = value;
                    color.a = Mathf.Sin(value * Mathf.PI) * glowStrength;
                    glow.color = color;
                }, 1f, riseDuration + settleDuration).SetEase(Ease.Linear));
                sequence.OnStart(() => glow.gameObject.SetActive(true));
            }
            sequence.OnComplete(ResetVisuals);
            return sequence;
        }

        public void ResetVisuals()
        {
            motion.localScale = Vector3.one;
            glow.gameObject.SetActive(false);
            var color = glow.color;
            color.a = 0f;
            glow.color = color;
        }

        private void OnDisable() => ResetVisuals();
    }
}
