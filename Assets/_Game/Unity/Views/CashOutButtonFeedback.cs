using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class CashOutButtonFeedback : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private Image background;
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite unavailableSprite;
        [SerializeField, Min(1f)] private float entranceScale = 1.08f;
        [SerializeField, Min(1f)] private float breathingScale = 1.025f;
        [SerializeField, Min(0.01f)] private float breathingDuration = 0.9f;
        private Tween m_Entrance;
        private Tween m_Breathing;
        private bool m_Available;

        public void SetAvailable(bool available)
        {
            if (m_Available == available)
                return;
            m_Available = available;
            background.sprite = available ? availableSprite : unavailableSprite;
            StopFeedback();
            if (available && isActiveAndEnabled)
                PlayEntrance();
        }

        private void PlayEntrance()
        {
            m_Entrance = DOTween.Sequence().SetUpdate(true)
                .Append(motion.DOScale(entranceScale, 0.18f).SetEase(Ease.OutQuad))
                .Append(motion.DOScale(1f, 0.22f).SetEase(Ease.InOutSine))
                .OnComplete(StartBreathing);
        }

        private void StartBreathing()
        {
            m_Entrance = null;
            m_Breathing = motion.DOScale(breathingScale, breathingDuration)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void StopFeedback()
        {
            m_Entrance?.Kill(false);
            m_Breathing?.Kill(false);
            m_Entrance = null;
            m_Breathing = null;
            motion.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            if (m_Available)
                PlayEntrance();
        }

        private void OnDisable() => StopFeedback();
    }
}
