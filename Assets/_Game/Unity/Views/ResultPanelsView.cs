using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Presentation._Game.Presentation.GameScreen;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class ResultPanelsView : MonoBehaviour
    {
        [SerializeField] private GameObject bombPanel;
        [SerializeField] private RectTransform bombMotion;
        [SerializeField] private TMP_Text bombBody;
        [SerializeField] private TMP_Text bombRewardsTitle;
        [SerializeField] private RewardListView bombRewards;
        [SerializeField] private TMP_Text reviveLabel;
        [SerializeField] private TMP_Text giveUpLabel;
        [SerializeField] private GameObject cashOutPanel;
        [SerializeField] private RectTransform cashOutMotion;
        [SerializeField] private TMP_Text cashOutTitle;
        [SerializeField] private RewardListView cashOutRewards;
        private Tween m_BombTween;
        private Tween m_CashOutTween;
        private RewardCatalog m_Catalog;
        private RewardAsset m_Currency;

        public void Initialize(RewardCatalog catalog, RewardAsset currency)
        {
            m_Catalog = catalog;
            m_Currency = currency;
            cashOutRewards.Initialize(catalog);
            bombRewards.Initialize(catalog);
        }

        public void Show(GameScreenState state)
        {
            ShowBomb(state);
            ShowCashOut(state);
        }

        private void ShowBomb(GameScreenState state)
        {
            ShowPanel(bombPanel, bombMotion, state.ShowBomb, ref m_BombTween);
            if (!state.ShowBomb)
                return;

            var hasRewards = state.BombRewards.Amounts.Count > 0;
            bombRewards.gameObject.SetActive(hasRewards);
            bombRewards.Show(state.BombRewards);
            bombRewardsTitle.text = hasRewards
                ? (state.AwaitingRevive ? "REWARDS AT RISK" : "REWARDS LOST")
                : "NO REWARDS COLLECTED YET";
            reviveLabel.text = state.ReviveCost == null ? "REVIVE" :
                "REVIVE " + state.ReviveCost.Amount.ToString("N0") + " " + GetCurrencyName(state.ReviveCost.RewardId);
            giveUpLabel.text = state.AwaitingRevive ? "GIVE UP" : "RESTART";
            bombBody.text = state.AwaitingRevive ?
                "Give up and lose all rewards collected in this run.\nRevive to keep them and continue!" :
                "Your run rewards were lost.\nStart a new run and try again!";
        }

        private void ShowCashOut(GameScreenState state)
        {
            ShowPanel(cashOutPanel, cashOutMotion, state.CashOut != null, ref m_CashOutTween);
            if (state.CashOut != null)
            {
                cashOutTitle.text = "REWARDS COLLECTED - ZONE " + state.CashOut.Zone;
                cashOutRewards.Show(state.CashOut.Rewards);
            }
        }

        private string GetCurrencyName(string id)
        {
            var reward = m_Currency.Id == id ? m_Currency : m_Catalog.Find(id);
            return reward == null || string.IsNullOrWhiteSpace(reward.DisplayName) ? id : reward.DisplayName;
        }

        private static void ShowPanel(GameObject panel, RectTransform motion, bool visible, ref Tween tween)
        {
            if (panel.activeSelf == visible)
                return;
            tween?.Kill();
            panel.SetActive(visible);
            if (!visible)
                return;
            motion.localScale = Vector3.one * .92f;
            tween = DOTween.To(() => motion.localScale, value => motion.localScale = value,
                Vector3.one, .25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            m_BombTween?.Kill();
            m_CashOutTween?.Kill();
            m_BombTween = null;
            m_CashOutTween = null;
            bombMotion.localScale = Vector3.one;
            cashOutMotion.localScale = Vector3.one;
        }

    }
}
