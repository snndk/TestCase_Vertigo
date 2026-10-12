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
        [SerializeField] private GameButton revive;
        [SerializeField] private TMP_Text reviveLabel;
        [SerializeField] private GameButton giveUp;
        [SerializeField] private TMP_Text giveUpLabel;
        [SerializeField] private GameObject cashOutPanel;
        [SerializeField] private RectTransform cashOutMotion;
        [SerializeField] private TMP_Text cashOutTitle;
        [SerializeField] private RewardListView cashOutRewards;
        [SerializeField] private GameButton restart;
        private Tween m_BombTween;
        private Tween m_CashOutTween;
        private bool m_AwaitingRevive;
        private RewardCatalog m_Catalog;
        private RewardAsset m_Currency;
        public event Action ReviveRequested;
        public event Action GiveUpRequested;
        public event Action RestartRequested;

        public void Initialize(RewardCatalog catalog, RewardAsset currency)
        {
            m_Catalog = catalog;
            m_Currency = currency;
            cashOutRewards.Initialize(catalog);
        }

        public void Show(GameScreenState state)
        {
            m_AwaitingRevive = state.AwaitingRevive;
            ShowPanel(bombPanel, bombMotion, state.ShowBomb, ref m_BombTween);
            ShowPanel(cashOutPanel, cashOutMotion, state.CashOut != null, ref m_CashOutTween);
            revive.gameObject.SetActive(state.AwaitingRevive);
            revive.SetInteractable(state.CanRevive);
            reviveLabel.text = state.ReviveCost == null ? "REVIVE" : "REVIVE " + state.ReviveCost.Amount.ToString("N0") + " " + GetCurrencyName(state.ReviveCost.RewardId);
            giveUpLabel.text = state.AwaitingRevive ? "GIVE UP" : "RESTART";
            giveUp.SetInteractable(state.ShowBomb);
            bombBody.text = state.AwaitingRevive ?
                "Revive to keep your rewards and continue.\nGive up to start a new run." :
                "Your run rewards were lost.\nStart a new run and try again!";
            restart.SetInteractable(state.CanRestart);
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

        private void OnEnable()
        {
            revive.Clicked += OnRevive;
            giveUp.Clicked += OnGiveUp;
            restart.Clicked += OnRestart;
        }

        private void OnDisable()
        {
            revive.Clicked -= OnRevive;
            giveUp.Clicked -= OnGiveUp;
            restart.Clicked -= OnRestart;
            m_BombTween?.Kill();
            m_CashOutTween?.Kill();
            m_BombTween = null;
            m_CashOutTween = null;
            bombMotion.localScale = Vector3.one;
            cashOutMotion.localScale = Vector3.one;
        }

        private void OnRevive() => ReviveRequested?.Invoke();
        private void OnRestart() => RestartRequested?.Invoke();
        private void OnGiveUp()
        {
            if (m_AwaitingRevive)
                GiveUpRequested?.Invoke();
            else
                RestartRequested?.Invoke();
        }
    }
}
