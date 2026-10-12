using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text rewardName;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RewardRowFeedback feedback;
        private long m_Total;
        private bool m_HasTotal;
        private bool m_RevealOnArrival;
        public RectTransform IconRect => icon.rectTransform;

        public void Bind(string id, RewardAsset reward, long total)
        {
            ResetArrival();
            SetReward(id, reward);
            var changed = !m_HasTotal || m_Total != total;
            if (changed)
                feedback.ShowTotal(total);
            m_Total = total;
            m_HasTotal = true;
            m_RevealOnArrival = false;
            group.alpha = 1;
        }

        public void PrepareArrival(string id, RewardAsset reward)
        {
            SetReward(id, reward);
        }

        public Tween CreateArrivalCount(long gainedAmount, float duration)
        {
            return feedback.CreateCountTween(m_Total, checked(m_Total + gainedAmount), duration);
        }

        public Tween Reveal(float duration)
        {
            if (!m_RevealOnArrival)
                return DOTween.Sequence().AppendInterval(duration);
            m_RevealOnArrival = false;
            return feedback.CreateEntranceTween(duration);
        }

        private void SetReward(string id, RewardAsset reward)
        {
            rewardName.text = reward != null ? reward.DisplayName : id;
            icon.enabled = reward != null;
            if (reward != null)
            {
                icon.sprite = reward.Icon;
                SpriteLayout.Fit(icon, new Vector2(42, 34));
            }
        }

        public void PulseArrival() => feedback.PulseIcon(icon.rectTransform);
        public void SetPlaybackSpeed(float speed) => feedback.SetPlaybackSpeed(speed);

        public void ResetArrival() => feedback.ResetArrival();

        public void Reserve()
        {
            m_RevealOnArrival = true;
            amount.text = string.Empty;
            group.alpha = 0;
        }

        private void OnDisable()
        {
            feedback.ResetVisuals();
        }
    }
}
