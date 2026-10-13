using System;
using System.IO;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Persistence;

namespace Vertigo.Debug._Game.Debug
{
    public class GameDebugSession
    {
        private readonly GameBootstrap m_Game;
        private readonly Func<IProgressStore> m_NormalStore;
        private ProgressService m_Progress;
        private DebugProgressStore m_Store;
        private ForcedSpinSelector m_Selector;

        public string SavePath { get; }
        public bool IsTesting => m_Progress != null;
        public string Message { get; private set; } = "Enter tester mode to use controls. New sessions discard uncollected loot.";
        public SessionSnapshot Snapshot { get; private set; }
        public string Currency => m_Game.Settings.Currency.DisplayName;
        public string CurrencyId => m_Game.Settings.Currency.Id;
        public string PendingResult => m_Selector?.Pending ?? "Random";
        public bool SaveFailureArmed => m_Store != null && m_Store.FailNextSave;

        public GameDebugSession(GameBootstrap game, string savePath, Func<IProgressStore> normalStore)
        {
            m_Game = game ?? throw new ArgumentNullException(nameof(game));
            SavePath = Path.GetFullPath(savePath);
            m_NormalStore = normalStore ?? throw new ArgumentNullException(nameof(normalStore));
            Refresh();
        }

        public void Refresh() => Snapshot = m_Game.GetSnapshot();

        public void EnterTesterMode()
        {
            if (IsTesting)
                return;
            LoadTestSession();
            Message = "Tester mode active. Normal save is untouched.";
        }

        public void ReturnToNormal()
        {
            if (!IsTesting)
                return;
            var settings = m_Game.Settings;
            var session = new GameSession(settings.WheelConfiguration.CreateRuntime(), new SystemRandomSource(),
                m_NormalStore(), settings.CreateReviveCost(), settings.ReviveIncreasePercent);
            m_Game.ReplaceSession(session, true);
            ClearOverrides();
            m_Progress = null;
            m_Store = null;
            m_Selector = null;
            Refresh();
            Message = "Normal save loaded. A new run has started.";
        }

        public void StartAtZone(int zone)
        {
            RequireTesting();
            if (zone < 1 || zone > 1000000)
                throw new ArgumentOutOfRangeException(nameof(zone), "Choose a zone from 1 to 1,000,000.");
            var selector = new ForcedSpinSelector(new SpinSelector(new SystemRandomSource()));
            var session = CreateSession(m_Progress, selector, zone);
            m_Game.ReplaceSession(session, true);
            ClearOverrides();
            m_Selector = selector;
            Refresh();
            Message = $"New test run started at zone {zone}.";
        }

        public void StartBeforeSilver()
        {
            RequireTesting();
            var current = m_Game.GetSnapshot().Round.Zone;
            var target = checked((current / 5 + 1) * 5);
            if (ZoneRules.GetTier(target) == WheelTier.Gold)
                target = checked(target + 5);
            StartAtZone(target - 1);
        }

        public void StartBeforeGold()
        {
            RequireTesting();
            var current = m_Game.GetSnapshot().Round.Zone;
            StartAtZone(checked((current / 30 + 1) * 30 - 1));
        }

        public void ForceSlot(int index, Round expectedRound = null)
        {
            RequireTesting();
            var snapshot = m_Game.GetSnapshot();
            if (expectedRound != null && !ReferenceEquals(expectedRound, snapshot.Round))
                throw new InvalidOperationException("The wheel has changed. Choose a slot from the new wheel.");
            if (!snapshot.CanSpin)
                throw new InvalidOperationException("Wait until the wheel is ready and Lucky has finished.");
            m_Selector.Arm(snapshot.Round, index);
            Message = $"Next spin only: {m_Selector.Pending}.";
        }

        public void CancelForcedResult()
        {
            RequireTesting();
            m_Selector.Clear();
            Message = "Next result is random.";
        }

        public void SetGold(long amount)
        {
            RequireEconomyReady();
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            var current = m_Progress.Rewards.GetAmount(CurrencyId);
            if (current == amount)
                return;
            var saved = amount > current
                ? m_Progress.TryCollect(new RewardCollection(new[] { new Reward(CurrencyId, amount - current) }))
                : m_Progress.TrySpend(new Reward(CurrencyId, current - amount));
            if (!saved)
                throw new IOException("Test balance could not be saved. The balance is unchanged.");
            m_Game.RefreshPresentation();
            Refresh();
            Message = $"Test {Currency} balance set to {amount}.";
        }

        public void AddGold(long amount)
        {
            RequireEconomyReady();
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            SetGold(checked(m_Progress.Rewards.GetAmount(CurrencyId) + amount));
        }

        public void ArmSaveFailure()
        {
            RequireTesting();
            m_Store.FailNextSave = true;
            Message = "The next test save will fail once. Retry will work normally.";
        }

        public void CancelSaveFailure()
        {
            RequireTesting();
            m_Store.FailNextSave = false;
            Message = "Save failure cancelled.";
        }

        public void ReloadTestSave()
        {
            RequireTesting();
            LoadTestSession();
            Message = "Test save reloaded. Only saved Gold remains; run restarted.";
        }

        public void ResetTestSave()
        {
            RequireTesting();
            if (Directory.Exists(Path.GetDirectoryName(SavePath)))
            {
                File.Delete(SavePath);
                File.Delete(SavePath + ".bak");
                File.Delete(SavePath + ".tmp");
            }
            LoadTestSession();
            Message = "Test save reset to first-launch balance. Normal save is untouched.";
        }

        public void ClearOverrides()
        {
            m_Selector?.Clear();
            if (m_Store != null)
                m_Store.FailNextSave = false;
        }

        public void Execute(Action command)
        {
            try
            {
                command();
            }
            catch (Exception error)
            {
                Message = error.Message;
                if (!(error is ArgumentException || error is InvalidOperationException || error is IOException ||
                    error is UnauthorizedAccessException || error is OverflowException))
                    UnityEngine.Debug.LogException(error);
            }
            Refresh();
        }

        private void LoadTestSession()
        {
            var store = new DebugProgressStore(new FileProgressStore(SavePath, m_Game.Settings.CreateStartingRewards(), CurrencyId));
            var progress = new ProgressService(store);
            var selector = new ForcedSpinSelector(new SpinSelector(new SystemRandomSource()));
            var session = CreateSession(progress, selector, 1);
            m_Game.ReplaceSession(session, true);
            ClearOverrides();
            m_Store = store;
            m_Progress = progress;
            m_Selector = selector;
            Refresh();
        }

        private GameSession CreateSession(ProgressService progress, ForcedSpinSelector selector, int zone)
        {
            var settings = m_Game.Settings;
            return new GameSession(settings.WheelConfiguration.CreateRuntime(), new SystemRandomSource(), progress,
                settings.CreateReviveCost(), settings.ReviveIncreasePercent, zone, selector);
        }

        private void RequireTesting()
        {
            if (!IsTesting)
                throw new InvalidOperationException("Enter tester mode first. These controls use an isolated test save.");
        }

        private void RequireEconomyReady()
        {
            RequireTesting();
            var state = m_Game.GetSnapshot();
            if (state.Status == SessionStatus.Resolving || state.InputBlocked ||
                state.Status == SessionStatus.PreparingSpin || state.Status == SessionStatus.CashingOut || state.Status == SessionStatus.Reviving)
                throw new InvalidOperationException("Wait for the current operation to finish before changing Gold.");
        }
    }
}
