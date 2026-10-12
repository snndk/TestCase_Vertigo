using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class SafeAreaView : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        private Rect m_LastArea;
        private Vector2Int m_LastSize;
        private Vector2 m_AnchorMin;
        private Vector2 m_AnchorMax;
        private Vector2 m_OffsetMin;
        private Vector2 m_OffsetMax;

        private void Awake()
        {
            m_AnchorMin = target.anchorMin;
            m_AnchorMax = target.anchorMax;
            m_OffsetMin = target.offsetMin;
            m_OffsetMax = target.offsetMax;
        }

        private void OnEnable() => Refresh();
        private void Update() => Refresh();

        private void Refresh()
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size.x <= 0 || size.y <= 0 || area.width <= 0 || area.height <= 0)
                return;
            if (area == m_LastArea && size == m_LastSize)
                return;
            var minimum = new Vector2(area.xMin / size.x, area.yMin / size.y);
            var extent = new Vector2(area.width / size.x, area.height / size.y);
            // Preserve the source layout's padding inside the device's safe area.
            target.anchorMin = minimum + Vector2.Scale(m_AnchorMin, extent);
            target.anchorMax = minimum + Vector2.Scale(m_AnchorMax, extent);
            target.offsetMin = m_OffsetMin;
            target.offsetMax = m_OffsetMax;
            m_LastArea = area;
            m_LastSize = size;
        }
    }
}
