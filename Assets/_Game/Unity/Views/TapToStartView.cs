using System;
using DG.Tweening;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class TapToStartView : MonoBehaviour
    {
        [SerializeField] private GameButton start;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private CanvasGroup prompt;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.85f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;
        private Tween m_Pulse;
        private Tween m_Fade;
        private bool m_IsClosing;
        public event Action StartRequested;

        public void Show()
        {
            m_IsClosing = false;
            group.alpha = 1;
            gameObject.SetActive(true);
        }

        public void Dismiss()
        {
            if (m_IsClosing)
                return;

            m_IsClosing = true;
            m_Pulse?.Kill();
            // Keep the full-screen raycast target alive until the opening tap has ended.
            m_Fade = DOTween.To(() => group.alpha, value => group.alpha = value, 0, fadeDuration).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            start.Clicked += OnStart;
            prompt.alpha = 1;
            m_Pulse = DOTween.To(() => prompt.alpha, value => prompt.alpha = value, 0.45f, pulseDuration)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void OnDisable()
        {
            start.Clicked -= OnStart;
            m_Pulse?.Kill();
            m_Fade?.Kill();
            m_Pulse = null;
            m_Fade = null;
            if (m_IsClosing)
                gameObject.SetActive(false);
        }

        private void OnStart()
        {
            if (!m_IsClosing)
                StartRequested?.Invoke();
        }
    }
}
