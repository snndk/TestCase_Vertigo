using System;
using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Bootstrap
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private WheelConfigurationAsset configuration;
        [SerializeField] private GameScreenView screen;
        [SerializeField, Min(1)] private float presentationTimeout = 10;
        [SerializeField] private RewardAsset currency;
        [FormerlySerializedAs("startingGold")]
        [SerializeField, Min(0)] private long startingBalance = 250;
        [SerializeField, Min(1)] private long reviveCost = 25;
        private GameScreenPresenter m_Presenter;

        private void Awake()
        {
            try
            {
                if (configuration == null || screen == null || currency == null)
                    throw new InvalidOperationException("Game configuration, screen and currency must be assigned.");
                var progress = startingBalance == 0 ? RewardCollection.Empty :
                    new RewardCollection(new[] { new Reward(currency.Id, startingBalance) });
                var session = new GameSession(configuration.CreateRuntime(), new SystemRandomSource(),
                    new InMemoryProgressStore(progress), new Reward(currency.Id, reviveCost));
                screen.Initialize(configuration, currency);
                m_Presenter = new GameScreenPresenter(session, screen,
                    new SpinPresentationSequence(screen.Wheel, TimeSpan.FromSeconds(presentationTimeout)));
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                if (screen != null)
                    screen.ShowStartupError("Unable to start the game. Check the wheel configuration.");
                enabled = false;
            }
        }
        private void OnEnable() => m_Presenter?.Attach();
        private void OnDisable() => m_Presenter?.Detach();
        private void Update() => m_Presenter?.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
    }
}
