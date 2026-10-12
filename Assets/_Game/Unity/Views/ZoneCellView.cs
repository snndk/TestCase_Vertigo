using DG.Tweening;
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
        [SerializeField] private ZoneCellFeedback feedback;
        private static readonly Color m_GoldAccent = new Color(1f, .77f, .22f);
        private static readonly Color m_SilverAccent = new Color(.6f, 1f, .4f);
        private static readonly Color m_GoldBackground = new Color(.38f, .25f, .04f);
        private static readonly Color m_SilverBackground = new Color(.12f, .28f, .08f);
        private static readonly Color m_NormalBackground = new Color(.07f, .12f, .18f);
        private static readonly Color m_GoldArrivalColor = new Color(1f, .78f, .25f);
        private static readonly Color m_SilverArrivalColor = new Color(.7f, .9f, 1f);
        private static readonly Vector2 m_NormalNumberSize = new Vector2(52f, 36f);
        private static readonly Vector2 m_SafeNumberSize = new Vector2(36f, 36f);
        private static readonly Vector2 m_SafeNumberOffset = new Vector2(8f, 0f);
        private const float m_CompletedAlpha = .45f;
        private WheelTier m_Tier;
        public RectTransform Rect => (RectTransform)transform;

        public void Show(int zone, int current)
        {
            feedback.ResetVisuals();
            gameObject.SetActive(zone > 0);
            if (zone <= 0)
                return;

            m_Tier = ZoneRules.GetTier(zone);
            var isCurrent = zone == current;
            var isSafe = m_Tier != WheelTier.Bronze;
            var accent = GetAccentColor();
            ApplyBackground(isCurrent, isSafe);
            ApplyNumber(zone, isCurrent, isSafe, accent);
            ApplySafeIcon(isSafe, accent);
            group.alpha = zone < current ? m_CompletedAlpha : 1f;
        }

        private void ApplyBackground(bool isCurrent, bool isSafe)
        {
            background.sprite = isCurrent && !isSafe ? currentSprite : normalSprite;
            background.color = GetBackgroundColor(isCurrent);
        }

        private void ApplyNumber(int zone, bool isCurrent, bool isSafe, Color accent)
        {
            number.text = zone.ToString();
            number.color = isCurrent ? Color.white : accent;
            number.rectTransform.anchoredPosition = isSafe ? m_SafeNumberOffset : Vector2.zero;
            number.rectTransform.sizeDelta = isSafe ? m_SafeNumberSize : m_NormalNumberSize;
        }

        private void ApplySafeIcon(bool isSafe, Color accent)
        {
            safeIcon.gameObject.SetActive(isSafe);
            safeIcon.enabled = isSafe;
            safeIcon.sprite = m_Tier == WheelTier.Gold ? goldIcon : silverIcon;
            safeIcon.color = accent;
        }

        private Color GetAccentColor()
        {
            switch (m_Tier)
            {
                case WheelTier.Gold:
                    return m_GoldAccent;
                case WheelTier.Silver:
                    return m_SilverAccent;
                default:
                    return Color.white;
            }
        }

        private Color GetBackgroundColor(bool isCurrent)
        {
            switch (m_Tier)
            {
                case WheelTier.Gold:
                    return m_GoldBackground;
                case WheelTier.Silver:
                    return m_SilverBackground;
                default:
                    return isCurrent ? Color.white : m_NormalBackground;
            }
        }

        public Tween CreateArrival()
        {
            var color = m_Tier == WheelTier.Gold ? m_GoldArrivalColor : m_SilverArrivalColor;
            return feedback.CreateArrival(m_Tier != WheelTier.Bronze, color);
        }
    }
}
