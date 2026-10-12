using DG.Tweening;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class ZoneProgressView : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ZoneCellView[] cells;
        [SerializeField] private RectTransform indicator;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.35f;
        private const float m_VerticalPadding = 46f;
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
            StopTransition();
            var visible = cells.Length - 2;
            var middle = visible / 2;
            var first = Mathf.Max(1, m_Zone - middle);
            var spacing = (viewport.rect.height - m_VerticalPadding) / (visible - 1);
            var shift = animate ? (first - m_FirstZone) * spacing : 0;
            m_FirstZone = first;
            m_Transition = animate ? DOTween.Sequence().SetUpdate(true) : null;
            ArrangeCells(middle, spacing, shift);
            ArrangeIndicator(middle, spacing);
        }

        private void ArrangeCells(int middle, float spacing, float shift)
        {
            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                cell.Show(m_FirstZone + index - 1, m_Zone);
                var end = Vector2.up * ((index - 1 - middle) * spacing);
                cell.Rect.anchoredPosition = end + Vector2.up * shift;
                if (m_Transition != null)
                    m_Transition.Join(CreateSlide(cell.Rect, end));
            }
        }

        private void ArrangeIndicator(int middle, float spacing)
        {
            var target = Vector2.up * ((m_Zone - m_FirstZone - middle) * spacing);
            if (m_Transition != null)
            {
                m_Transition.Join(CreateSlide(indicator, target));
                var currentCell = cells[m_Zone - m_FirstZone + 1];
                m_Transition.Insert(slideDuration, currentCell.CreateArrival());
            }
            else
                indicator.anchoredPosition = target;
        }

        private Tween CreateSlide(RectTransform rect, Vector2 target)
        {
            return DOTween.To(() => rect.anchoredPosition,
                value => rect.anchoredPosition = value, target, slideDuration).SetEase(Ease.OutQuart);
        }

        private void StopTransition()
        {
            m_Transition?.Kill(false);
            m_Transition = null;
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
            if (m_Zone > 0)
                Arrange(false);
            else
                StopTransition();
        }
    }
}
