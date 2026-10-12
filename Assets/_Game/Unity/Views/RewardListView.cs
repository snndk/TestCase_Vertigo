using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardListView : MonoBehaviour
    {
        [SerializeField] private RewardRowView rowPrefab;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollView;
        private readonly Dictionary<string, RewardRowView> m_Rows = new Dictionary<string, RewardRowView>();
        private readonly List<string> m_Removed = new List<string>();
        private RewardCatalog m_Catalog;

        public void Initialize(RewardCatalog catalog) => m_Catalog = catalog;

        public RewardRowView Reserve(string id)
        {
            if (m_Rows.TryGetValue(id, out var existing))
                return existing;
            var row = Instantiate(rowPrefab, content);
            row.name = "ui_group_reward_row_" + id + "_value";
            row.Reserve();
            m_Rows.Add(id, row);
            return row;
        }

        public void Show(RewardCollection rewards)
        {
            m_Removed.Clear();
            foreach (var pair in m_Rows)
                if (!rewards.Amounts.ContainsKey(pair.Key))
                    m_Removed.Add(pair.Key);
            foreach (var id in m_Removed)
            {
                m_Rows[id].gameObject.SetActive(false);
                Destroy(m_Rows[id].gameObject);
                m_Rows.Remove(id);
            }
            foreach (var pair in rewards.Amounts)
                Reserve(pair.Key).Bind(pair.Key, m_Catalog.Find(pair.Key), pair.Value);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        public RectTransform PrepareArrival(string id)
        {
            var row = Reserve(id);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollView.StopMovement();
            var viewport = scrollView.viewport;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, row.transform);
            var delta = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y :
                bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0;
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + delta, 0, Mathf.Max(0, content.rect.height - viewport.rect.height));
            content.anchoredPosition = position;
            return row.IconRect;
        }
    }
}
