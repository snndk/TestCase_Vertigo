using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class GameScreenView : MonoBehaviour, IGameScreenView
    {
        [SerializeField] private WheelView wheel;
        [SerializeField] private GameButton spin;
        [SerializeField] private GameButton cashOut;
        [SerializeField] private TMP_Text exitLabel;
        [SerializeField] private TMP_Text wheelTitle;
        [SerializeField] private TMP_Text wallet;
        [SerializeField] private Image walletIcon;
        [SerializeField] private Vector2 walletIconBounds = new Vector2(40, 34);
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text error;
        [SerializeField] private RewardListView loot;
        [SerializeField] private ZoneProgressView zones;
        [SerializeField] private ResultPanelsView panels;
        private WheelConfigurationAsset m_Configuration;
        private RewardAsset m_Currency;
        private PresentationError m_LastError;
        public int SliceCount => wheel.SliceCount;
        public WheelView Wheel => wheel;
        public event Action SpinRequested;
        public event Action CashOutRequested;
        public event Action RestartRequested;
        public event Action ReviveRequested;
        public event Action GiveUpRequested;

        public void Initialize(WheelConfigurationAsset configuration, RewardAsset currency)
        {
            if (currency == null || string.IsNullOrWhiteSpace(currency.Id) || currency.Icon == null)
                throw new ArgumentException("A currency with an identity and icon must be assigned.", nameof(currency));
            var catalog = new RewardCatalog(configuration);
            var authoredCurrency = catalog.Find(currency.Id);
            if (authoredCurrency != null && authoredCurrency != currency)
                throw new ArgumentException("Currency must reference the canonical reward asset.", nameof(currency));
            m_Configuration = configuration;
            m_Currency = currency;
            walletIcon.sprite = currency.Icon;
            SpriteLayout.Fit(walletIcon, walletIconBounds);
            loot.Initialize(catalog);
            panels.Initialize(catalog, currency);
            wheel.Initialize(configuration);
        }

        public void ShowStartupError(string message)
        {
            spin.SetInteractable(false);
            cashOut.SetInteractable(false);
            error.text = message;
        }

        public void Render(GameScreenState state)
        {
            wheel.SetInputBlocked(state.InputBlocked);
            wheel.Bind(state.Round);
            var tier = m_Configuration.GetTier(state.Round.Tier);
            wheelTitle.text = tier.Title;
            wheelTitle.color = tier.TitleColor;
            wallet.text = state.Progress.GetAmount(m_Currency.Id).ToString("N0");
            loot.Show(state.Loot);
            zones.Show(state.Round.Zone);
            spin.SetInteractable(state.CanSpin);
            cashOut.SetInteractable(state.CanCashOut);
            exitLabel.text = state.CanCashOut ? "COLLECT & EXIT" : "EXIT AT SAFE ZONE";
            status.text = state.AutomaticRunMessage ?? tier.StatusMessage;
            panels.Show(state);
            error.text = state.Error?.Message ?? "";
            if (state.Error?.Cause != null && !ReferenceEquals(m_LastError, state.Error))
                Debug.LogException(state.Error.Cause, this);
            m_LastError = state.Error;
        }

        private void OnEnable()
        {
            spin.Clicked += OnSpin;
            cashOut.Clicked += OnCashOut;
            panels.RestartRequested += OnRestart;
            panels.ReviveRequested += OnRevive;
            panels.GiveUpRequested += OnGiveUp;
        }

        private void OnDisable()
        {
            spin.Clicked -= OnSpin;
            cashOut.Clicked -= OnCashOut;
            panels.RestartRequested -= OnRestart;
            panels.ReviveRequested -= OnRevive;
            panels.GiveUpRequested -= OnGiveUp;
        }

        private void OnSpin() => SpinRequested?.Invoke();
        private void OnCashOut() => CashOutRequested?.Invoke();
        private void OnRestart() => RestartRequested?.Invoke();
        private void OnRevive() => ReviveRequested?.Invoke();
        private void OnGiveUp() => GiveUpRequested?.Invoke();
    }
}
