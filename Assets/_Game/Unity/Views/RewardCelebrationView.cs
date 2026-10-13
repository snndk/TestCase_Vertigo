using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardCelebrationView : MonoBehaviour
    {
        [SerializeField] private PulseRingGraphic ring;
        [SerializeField] private Image flash;
        [SerializeField] private CanvasGroup wheelContents;
        [SerializeField, Min(0.01f)] private float highlightDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float arrivalDuration = 0.25f;

        // The reward outcome owns these tweens, including playback speed and cancellation.
        public Tween CreateHighlight(RectTransform source, Color color, float strength)
        {
            Prepare(source, color);
            var progress = 0f;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                DrawPulse(value, color, strength);
                wheelContents.alpha = 1f - Mathf.Sin(value * Mathf.PI) * strength * 0.65f;
            }, 1f, highlightDuration).SetEase(Ease.Linear);
        }

        public Tween CreateArrival(RectTransform target, Color color, float strength)
        {
            var progress = 0f;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                DrawPulse(value, color, strength);
            }, 1f, arrivalDuration).SetEase(Ease.OutQuad)
                .OnStart(() => Prepare(target, color));
        }

        private void Prepare(RectTransform target, Color color)
        {
            var parent = ring.rectTransform.parent;
            var size = Mathf.Max(target.rect.width * target.lossyScale.x,
                target.rect.height * target.lossyScale.y) / parent.lossyScale.x;
            ring.rectTransform.position = target.TransformPoint(target.rect.center);
            flash.rectTransform.position = ring.rectTransform.position;
            ring.rectTransform.sizeDelta = Vector2.one * (size * 1.25f);
            flash.rectTransform.sizeDelta = Vector2.one * (size * 2.5f);
            ring.gameObject.SetActive(true);
            flash.gameObject.SetActive(true);
            DrawPulse(0, color, 0);
        }

        private void DrawPulse(float progress, Color color, float strength)
        {
            color.a = Mathf.Sin(progress * Mathf.PI) * strength;
            ring.color = color;
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 2.4f, progress);
            color.a *= 0.75f;
            flash.color = color;
            flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, progress);
        }

        public void ResetVisuals()
        {
            wheelContents.alpha = 1f;
            ring.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetVisuals();
    }
}
