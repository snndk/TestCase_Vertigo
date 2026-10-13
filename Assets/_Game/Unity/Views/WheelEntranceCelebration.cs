using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WheelEntranceCelebration : MonoBehaviour
    {
        [SerializeField] private Image backdrop;
        [SerializeField] private TMP_Text announcement;
        [SerializeField, Min(0.01f)] private float duration = 0.85f;

        public Tween Create(string message, Color color, float strength)
        {
            announcement.text = message;
            announcement.color = color;
            announcement.alpha = 0;
            announcement.gameObject.SetActive(true);
            backdrop.gameObject.SetActive(true);
            var progress = 0f;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                var brightness = Mathf.Sin(value * Mathf.PI) * strength;
                backdrop.color = new Color(0.01f, 0.015f, 0.035f, brightness * 0.55f);
                announcement.alpha = Mathf.Min(1, brightness * 2);
                announcement.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.04f, value);
            }, 1f, duration).SetEase(Ease.Linear);
        }

        public void ResetVisuals()
        {
            if (backdrop != null)
                backdrop.gameObject.SetActive(false);
            if (announcement != null)
                announcement.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetVisuals();
    }
}
