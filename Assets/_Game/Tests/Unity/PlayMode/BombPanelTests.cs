#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class BombPanelTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator RecoverableRewardsAppearWithWarningAtAllTargetAspectRatios()
        {
            Presenter.Detach();
            Session = new GameSession(CreateFixedConfiguration().CreateRuntime(), Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward("Gold", 250) })), new Reward("Gold", 25));
            Presenter = new GameScreenPresenter(Session, View, View.Input,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            yield return SpinAndWait();
            Random.Index = 4;
            yield return SpinAndWait();
            var rewards = Session.GetSnapshot().RunLoot;
            Random.Index = 7;
            yield return SpinAndWait();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Text("bomb_rewards_title"), Is.EqualTo("REWARDS AT RISK"));
            Assert.That(Text("bomb_body"), Does.Contain("lose all rewards"));
            var list = BombList();
            Assert.That(list.GetComponentsInChildren<RewardRowView>().Length, Is.EqualTo(rewards.Amounts.Count));
            foreach (var pair in rewards.Amounts)
            {
                var row = list.GetComponentsInChildren<RewardRowView>().Single(item => item.name == "ui_group_reward_row_" + pair.Key + "_value");
                Assert.That(row.GetComponentsInChildren<TMPro.TMP_Text>().Any(text => text.text.Contains(pair.Value.ToString())), Is.True);
            }

            var resolution = typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeWindow")
                .GetMethod("SetCustomRenderingResolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var previousWindow = EditorWindow.focusedWindow;
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Focus();
            yield return null;
            var original = new Vector2Int(Screen.width, Screen.height);
            try
            {
                foreach (var width in new[] { 2400, 1920, 1440 })
                {
                    resolution.Invoke(null, new object[] { (uint)width, 1080u, "Bomb panel review" });
                    yield return null;
                    yield return null;
                    Assert.That(Screen.width, Is.EqualTo(width));
                    Assert.That(Screen.height, Is.EqualTo(1080));
                    var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Bomb-panel-" + width + "x1080.png"));
                    ScreenCapture.CaptureScreenshot(path);
                    yield return null;
                    yield return null;
                }
            }
            finally
            {
                resolution.Invoke(null, new object[] { (uint)original.x, (uint)original.y, "Restored resolution" });
                if (previousWindow != null)
                    previousWindow.Focus();
            }
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Session.GetSnapshot().RunLoot, Is.SameAs(rewards));
            Assert.That(Session.GetSnapshot().FailedRunLoot.Amounts, Is.Empty);
        }

        [UnityTest]
        public IEnumerator LostRewardsRemainVisibleWithoutReviveAndEmptyRunsDoNotShowStaleRows()
        {
            yield return SpinAndWait();
            Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(Text("bomb_rewards_title"), Is.EqualTo("REWARDS LOST"));
            Assert.That(BombList().GetComponentsInChildren<RewardRowView>().Length, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Button("restart").onClick.Invoke();
            yield return SpinAndWait();
            Assert.That(Text("bomb_rewards_title"), Is.EqualTo("NO REWARDS COLLECTED YET"));
            Assert.That(BombList().gameObject.activeSelf, Is.False);
        }

        private RewardListView BombList() => Root.GetComponentsInChildren<RewardListView>(true)
            .Single(list => list.name == "ui_scroll_bomb_rewards_scroll_view");
    }
}
#endif
