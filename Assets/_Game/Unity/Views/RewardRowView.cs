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
        public RectTransform IconRect => icon.rectTransform;

        public void Bind(string id, RewardAsset reward, long total)
        {
            rewardName.text = reward != null ? reward.DisplayName : id;
            amount.text = total.ToString("N0");
            icon.enabled = reward != null;
            if (reward != null)
            {
                icon.sprite = reward.Icon;
                SpriteLayout.Fit(icon, new Vector2(42, 34));
            }
            group.alpha = 1;
        }

        public void Reserve() => group.alpha = 0;
    }
}
