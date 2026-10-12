using System;
using UnityEngine;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Bootstrap
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField] private GameScreenView screen;
        [SerializeField, Min(1)] private float presentationTimeout = 10;
        private GameScreenPresenter m_Presenter;

        private void Awake()
        {
            try
            {
                if (settings == null || screen == null)
                    throw new InvalidOperationException("Game settings and screen must be assigned.");
                var progress = settings.CreateStartingRewards();
                var session = new GameSession(settings.WheelConfiguration.CreateRuntime(), new SystemRandomSource(),
                    new InMemoryProgressStore(progress), settings.CreateReviveCost(), settings.ReviveIncreasePercent);
                screen.Initialize(settings.WheelConfiguration, settings.Currency);
                m_Presenter = new GameScreenPresenter(session, screen, screen.Input,
                    new SpinPresentationSequence(screen.Wheel, TimeSpan.FromSeconds(presentationTimeout)));
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                if (screen != null)
                    screen.ShowStartupError("Unable to start the game. Check the game settings.");
                enabled = false;
            }
        }
        private void OnEnable() => m_Presenter?.Attach();
        private void OnDisable() => m_Presenter?.Detach();
        private void Update() => m_Presenter?.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
    }
}
