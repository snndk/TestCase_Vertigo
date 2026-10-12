using DG.Tweening;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class ZoneProgressView : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ZoneCellView[] cells;
        [SerializeField] private RectTransform indicator;
        private int m_Zone;
        private int m_FirstZone = 1;
        private Sequence m_Transition;

        public void Show(int zone)
        {
            if (zone == m_Zone)
                return;
            var animate = m_Zone > 0 && zone == m_Zone + 1;
            m_Zone = zone;
            Arrange(animate && isActiveAndEnabled);
        }

        private void Arrange(bool animate)
        {
            m_Transition?.Kill();
            var visible = cells.Length - 2;
            var middle = visible / 2;
            var first = Mathf.Max(1, m_Zone - middle);
            var spacing = (viewport.rect.width - 70) / (visible - 1);
            var shift = animate ? (first - m_FirstZone) * spacing : 0;
            m_FirstZone = first;
            m_Transition = animate ? DOTween.Sequence().SetUpdate(true) : null;
            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                cell.Show(first + index - 1, m_Zone);
                var end = new Vector2((index - 1 - middle) * spacing, 0);
                cell.Rect.anchoredPosition = end + new Vector2(shift, 0);
                if (animate)
                    m_Transition.Join(DOTween.To(() => cell.Rect.anchoredPosition,
                        value => cell.Rect.anchoredPosition = value, end, .35f).SetEase(Ease.OutCubic));
            }
            var target = new Vector2((m_Zone - first - middle) * spacing, 0);
            if (animate)
                m_Transition.Join(DOTween.To(() => indicator.anchoredPosition,
                    value => indicator.anchoredPosition = value, target, .35f).SetEase(Ease.OutCubic));
            else
                indicator.anchoredPosition = target;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && m_Zone > 0)
                Arrange(false);
        }

        private void OnEnable()
        {
            if (m_Zone > 0)
                Arrange(false);
        }

        private void OnDisable()
        {
            m_Transition?.Kill();
            m_Transition = null;
            if (m_Zone > 0)
                Arrange(false);
        }
    }
}
