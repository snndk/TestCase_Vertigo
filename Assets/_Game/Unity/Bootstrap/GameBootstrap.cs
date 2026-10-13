using System;
using System.IO;
using UnityEngine;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;
using Vertigo.Unity._Game.Unity.Persistence;

namespace Vertigo.Unity._Game.Unity.Bootstrap
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField] private GameScreenView screen;
        [SerializeField] private TapToStartView startScreen;
        [SerializeField, Min(1)] private float presentationTimeout = 10;
        private GameScreenPresenter m_Presenter;
        private GameSession m_Session;
        private bool m_HasStarted;

        public GameSettings Settings => settings;

        public SessionSnapshot GetSnapshot() => m_Session?.GetSnapshot();

        public FileProgressStore CreateProgressStore()
        {
            return new FileProgressStore(Path.Combine(Application.persistentDataPath, "progress.json"),
                settings.CreateStartingRewards(), settings.Currency.Id);
        }

        private void Awake()
        {
            if (m_Presenter != null)
                return;

            try
            {
                ValidateReferences();
                var store = CreateProgressStore();
                Initialize(store);
                if (store.RecoveredFromBackup)
                    Debug.LogWarning("Saved progress was restored from its backup.", this);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                if (screen != null)
                    screen.ShowStartupError(error is IOException || error is InvalidDataException ||
                        error is UnauthorizedAccessException || error is NotSupportedException
                        ? "Saved progress could not be loaded. Please restart the game and try again."
                        : "Unable to start the game. Check the game settings.");
                enabled = false;
            }
        }

        public void Initialize(IProgressStore store)
        {
            if (m_Presenter != null)
                throw new InvalidOperationException("The game is already initialized.");

            ValidateReferences();
            var session = new GameSession(settings.WheelConfiguration.CreateRuntime(), new SystemRandomSource(),
                store, settings.CreateReviveCost(), settings.ReviveIncreasePercent);
            ReplaceSession(session);
        }

        public void ReplaceSession(GameSession session, bool startImmediately = false)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            ValidateReferences();
            m_Presenter?.Detach();
            screen.Initialize(settings.WheelConfiguration, settings.Currency);
            m_Session = session;
            m_Presenter = new GameScreenPresenter(session, screen, screen.Input,
                new SpinPresentationSequence(screen.Wheel, TimeSpan.FromSeconds(presentationTimeout)));
            if (m_HasStarted || startImmediately)
            {
                m_HasStarted = true;
                if (isActiveAndEnabled)
                    m_Presenter.Attach();
                startScreen.Dismiss();
            }
            else
                startScreen.Show();
        }

        public void RefreshPresentation() => m_Presenter?.Refresh();

        private void ValidateReferences()
        {
            if (settings == null || screen == null || startScreen == null)
                throw new InvalidOperationException("Game settings, game screen and start screen must be assigned.");
        }

        private void OnEnable()
        {
            startScreen.StartRequested += StartGame;
            if (m_HasStarted)
                m_Presenter?.Attach();
        }

        private void StartGame()
        {
            if (m_HasStarted)
                return;

            m_Presenter.Attach();
            m_HasStarted = true;
            startScreen.Dismiss();
        }

        private void OnDisable()
        {
            if (startScreen != null)
                startScreen.StartRequested -= StartGame;
            m_Presenter?.Detach();
        }

        private void Update()
        {
            if (m_HasStarted)
                m_Presenter?.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
        }
    }
}
