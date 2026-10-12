using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class ZoneCellView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text number;
        [SerializeField] private Image safeIcon;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite silverIcon;
        [SerializeField] private Sprite goldIcon;
        public RectTransform Rect => (RectTransform)transform;

        public void Show(int zone, int current)
        {
            gameObject.SetActive(zone > 0);
            if (zone <= 0)
                return;
            var tier = ZoneRules.GetTier(zone);
            var safe = tier != WheelTier.Bronze;
            var accent = tier == WheelTier.Gold ? new Color(1, .77f, .22f) :
                tier == WheelTier.Silver ? new Color(.6f, 1, .4f) : Color.white;
            background.sprite = zone == current && !safe ? currentSprite : normalSprite;
            background.color = safe ? (tier == WheelTier.Gold ? new Color(.38f, .25f, .04f) : new Color(.12f, .28f, .08f)) :
                zone == current ? Color.white : new Color(.07f, .12f, .18f);
            number.text = zone.ToString();
            number.color = zone == current ? Color.white : accent;
            number.rectTransform.anchoredPosition = new Vector2(safe ? 8 : 0, 0);
            number.rectTransform.sizeDelta = new Vector2(safe ? 36 : 52, 36);
            safeIcon.gameObject.SetActive(safe);
            safeIcon.enabled = safe;
            safeIcon.sprite = tier == WheelTier.Gold ? goldIcon : silverIcon;
            safeIcon.color = accent;
            group.alpha = zone < current ? .45f : 1;
        }
    }
}
