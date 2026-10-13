#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SRDebugger;
using SRF.Service;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Debug._Game.Debug;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Debug._Game.Tests.Debug.PlayMode
{
    public class TesterPanelTests
    {
        private GameObject m_Root;
        private GameBootstrap m_Game;
        private GameDebugSession m_Tester;
        private InMemoryProgressStore m_NormalStore;
        private string m_Directory;
        private string m_Currency;

        [SetUp]
        public void SetUp()
        {
            m_Directory = Path.Combine(Path.GetTempPath(), "VertigoDebuggerTests", Guid.NewGuid().ToString("N"));
            m_Root = new GameObject("Tester integration");
            m_Root.SetActive(false);
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab"), m_Root.transform);
            m_Game = instance.GetComponent<GameBootstrap>();
            m_Currency = m_Game.Settings.Currency.Id;
            m_NormalStore = new InMemoryProgressStore(new RewardCollection(new[] { new Reward(m_Currency, 999) }));
            m_Game.Initialize(m_NormalStore);
            SetDuration(instance.GetComponentInChildren<WheelView>(), "spinDuration", 0.03f);
            SetDuration(instance.GetComponentInChildren<RewardOutcomeView>(), "flightDuration", 0.03f);
            m_Root.SetActive(true);
            m_Tester = new GameDebugSession(m_Game, Path.Combine(m_Directory, "progress.json"), () => m_NormalStore);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
                Object.DestroyImmediate(m_Root);
            if (Directory.Exists(m_Directory))
                Directory.Delete(m_Directory, true);
        }

        [Test]
        public void TesterControlsAreIsolatedAndResetReloadReturnUseTheRightSave()
        {
            Assert.Throws<InvalidOperationException>(() => m_Tester.SetGold(1));
            m_Tester.EnterTesterMode();
            m_Tester.ResetTestSave();
            m_Tester.SetGold(123);
            m_Tester.AddGold(7);
            m_Tester.ReloadTestSave();
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(130));
            m_Tester.ResetTestSave();
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency),
                Is.EqualTo(m_Game.Settings.CreateStartingRewards().GetAmount(m_Currency)));
            Assert.That(m_NormalStore.Load().GetAmount(m_Currency), Is.EqualTo(999));
            m_Tester.ReturnToNormal();
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(999));
            Assert.That(m_Tester.IsTesting, Is.False);
        }

        [Test]
        public void ZoneShortcutsSkipGoldForSilverAndRejectStaleSlotButtons()
        {
            m_Tester.EnterTesterMode();
            m_Tester.StartAtZone(29);
            m_Tester.StartBeforeSilver();
            Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(34));
            m_Tester.StartAtZone(1);
            m_Tester.StartBeforeGold();
            Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(29));
            var previous = m_Game.GetSnapshot().Round;
            m_Tester.StartAtZone(30);
            Assert.That(m_Game.GetSnapshot().Round.Slices.Any(slice => slice is BombSliceData), Is.False);
            Assert.Throws<InvalidOperationException>(() => m_Tester.ForceSlot(0, previous));
        }

        [UnityTest]
        public IEnumerator ForcedBombAllowsLiveGoldEditingAndOneFailedReviveThenSuccessfulRetry()
        {
            m_Tester.EnterTesterMode();
            m_Tester.SetGold(0);
            m_Tester.ForceSlot(FindSlot(slice => slice is BombSliceData));
            yield return Spin();
            Assert.That(m_Game.GetSnapshot().Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            var run = m_Game.GetSnapshot().RunId;
            Assert.That(Button("bomb_revive").interactable, Is.False);
            m_Tester.SetGold(100);
            Assert.That(m_Game.GetSnapshot().RunId, Is.EqualTo(run));
            Assert.That(Button("bomb_revive").interactable, Is.True);
            m_Tester.ArmSaveFailure();
            Button("bomb_revive").onClick.Invoke();
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(100));
            Assert.That(Text("error"), Does.Contain("could not be saved"));
            Assert.That(m_Tester.SaveFailureArmed, Is.False);
            Button("bomb_revive").onClick.Invoke();
            Assert.That(m_Game.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(75));
            m_Tester.ReloadTestSave();
            Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(75));
        }

        [UnityTest]
        public IEnumerator ForcedRewardReachesSafeZoneAndFailedCashOutRetainsLootForRetry()
        {
            m_Tester.EnterTesterMode();
            m_Tester.StartAtZone(4);
            m_Tester.ForceSlot(FindSlot(slice => slice is RewardSliceData));
            yield return Spin();
            Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(m_Tester.PendingResult, Is.EqualTo("Random"));
            var loot = m_Game.GetSnapshot().RunLoot;
            m_Tester.ArmSaveFailure();
            Button("exit").onClick.Invoke();
            Assert.That(m_Game.GetSnapshot().RunLoot, Is.SameAs(loot));
            Assert.That(Text("error"), Does.Contain("could not be saved"));
            Button("exit").onClick.Invoke();
            Assert.That(m_Game.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
        }

        [UnityTest]
        public IEnumerator ReplacingSessionDuringAnimationCannotAdvanceOrRewardTheNewRun()
        {
            m_Tester.EnterTesterMode();
            m_Tester.ForceSlot(FindSlot(slice => slice is RewardSliceData));
            Button("wheel_spin").onClick.Invoke();
            Assert.That(m_Game.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
            m_Tester.StartAtZone(30);
            var run = m_Game.GetSnapshot().RunId;
            yield return new WaitForSecondsRealtime(1);
            Assert.That(m_Game.GetSnapshot().RunId, Is.EqualTo(run));
            Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(30));
            Assert.That(m_Game.GetSnapshot().RunLoot.Amounts, Is.Empty);
        }

        [Test]
        public void DisablingPanelAfterDebuggerShutdownDoesNotAccessStoppedServices()
        {
            var panel = m_Game.gameObject.AddComponent<GameDebugPanel>();
            var debugger = SRDebug.Instance;
            var options = (GameDebugOptions)typeof(GameDebugPanel)
                .GetField("m_Options", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
            var shutdown = typeof(SRServiceManager).GetField("_hasQuit", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = shutdown.GetValue(null);
            try
            {
                // Reproduce the SDK state between its quit callback and our OnDisable.
                shutdown.SetValue(null, true);
                Assert.That(SRDebug.Instance, Is.Null);
                panel.enabled = false;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                shutdown.SetValue(null, previous);
                debugger.RemoveOptionContainer(options.Container);
            }
        }

        [UnityTest]
        public IEnumerator RealOptionsPanelRegistersRendersAndRemovesControlsOnDisable()
        {
            var install = typeof(GameDebugPanel).GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
            install.Invoke(null, null);
            install.Invoke(null, null);
            Assert.That(m_Game.GetComponents<GameDebugPanel>().Length, Is.EqualTo(1));
            var panel = m_Game.GetComponent<GameDebugPanel>();
            SRDebug.Instance.ShowDebugPanel(DefaultTabs.Options);
            yield return null;
            yield return null;
            Assert.That(SRDebug.Instance.IsDebugPanelVisible, Is.True);
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Any(text => text.text == "<color=#67E89B>Enter Test Mode</color>"), Is.True);
            var capturePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Tester-options.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
            ScreenCapture.CaptureScreenshot(capturePath);
            yield return null;
            yield return null;
            panel.enabled = false;
            yield return null;
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Any(text => text.text == "<color=#67E89B>Enter Test Mode</color>"), Is.False);
            panel.enabled = true;
            yield return null;
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(text => text.text == "<color=#67E89B>Enter Test Mode</color>"), Is.EqualTo(1));
            SRDebug.Instance.HideDebugPanel();
        }

        [UnityTest]
        public IEnumerator OptionsButtonsExecuteCommandsAndResetRequiresItsCheckbox()
        {
            var options = new GameDebugOptions(m_Tester);
            SRDebug.Instance.AddOptionContainer(options.Container);
            try
            {
                SRDebug.Instance.ShowDebugPanel(DefaultTabs.Options);
                yield return null;
                Assert.That(options.Container.Options.All(option => option.Category == "01 Tester"), Is.True);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Any(text => text.text == "Start before next Gold"), Is.False);
                ClickOption("<color=#67E89B>Enter Test Mode</color>");
                Assert.That(m_Tester.IsTesting, Is.True);
                yield return null;
                yield return null;
                var exitLabel = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Single(text => text.text == "<color=#FF7373>Exit Test Mode</color>");
                Assert.That(exitLabel.supportRichText, Is.True);
                Assert.That(options.Container.Options.Count(option => option.Category == "01 Tester" && option.IsMethod), Is.EqualTo(1));
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Tester-mode-active.png")));
                yield return null;
                ClickOption("Start before next Gold");
                Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(29));
                options.Container.Options.Single(option => option.Name == "Gold amount").Property.SetValue(123);
                ClickOption("Set Gold");
                Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(123));
                ClickOption("Reset test save and restart");
                Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(123));
                Assert.That(m_Tester.Message, Does.Contain("Confirm test save reset"));
                options.Container.Options.Single(option => option.Name == "Confirm test save reset").Property.SetValue(true);
                ClickOption("Reset test save and restart");
                Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(1));
                Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency),
                    Is.EqualTo(m_Game.Settings.CreateStartingRewards().GetAmount(m_Currency)));
                Assert.That(m_NormalStore.Load().GetAmount(m_Currency), Is.EqualTo(999));
                ClickOption("<color=#FF7373>Exit Test Mode</color>");
                Assert.That(m_Tester.IsTesting, Is.False);
                Assert.That(m_Game.GetSnapshot().Progress.GetAmount(m_Currency), Is.EqualTo(999));
                yield return null;
                yield return null;
                var enterLabel = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Single(text => text.text == "<color=#67E89B>Enter Test Mode</color>");
                Assert.That(enterLabel.supportRichText, Is.True);
                Assert.That(options.Container.Options.Count(option => option.Category == "01 Tester" && option.IsMethod), Is.EqualTo(1));
                Assert.That(options.Container.Options.All(option => option.Category == "01 Tester"), Is.True);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Any(text => text.text == "Start before next Gold"), Is.False);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Tester-mode-normal.png")));
                yield return null;
                ClickOption("<color=#67E89B>Enter Test Mode</color>");
                yield return null;
                yield return null;
                Assert.That(options.Container.Options.Count(option => option.Name == "Start before next Gold"), Is.EqualTo(1));
                ClickOption("Start before next Gold");
                Assert.That(m_Game.GetSnapshot().Round.Zone, Is.EqualTo(29));
            }
            finally
            {
                SRDebug.Instance.RemoveOptionContainer(options.Container);
                SRDebug.Instance.HideDebugPanel();
            }
        }

        private static void ClickOption(string label)
        {
            Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(text => text.text == label).GetComponentInParent<Button>().onClick.Invoke();
        }

        private IEnumerator Spin()
        {
            Button("wheel_spin").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 8;
            while (m_Game.GetSnapshot().Status == SessionStatus.Resolving && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(m_Game.GetSnapshot().Status, Is.Not.EqualTo(SessionStatus.Resolving));
        }

        private int FindSlot(Predicate<SliceData> predicate)
        {
            var round = m_Game.GetSnapshot().Round;
            return Enumerable.Range(0, round.Slices.Count).First(index => predicate(round.Slices[index]));
        }

        private Button Button(string suffix) => m_Root.GetComponentsInChildren<Button>(true)
            .Single(button => button.name == "ui_button_" + suffix + "_value");

        private string Text(string suffix) => m_Root.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "ui_text_" + suffix + "_value").text;

        private static void SetDuration(Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
