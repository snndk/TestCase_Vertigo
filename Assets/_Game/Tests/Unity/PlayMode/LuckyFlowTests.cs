#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class LuckyFlowTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator RealLuckyRewardAutomaticallyCollectsUntilSafeZoneAndBlocksScreenInput()
        {
            ConfigureLucky();
            SetDuration(View.Wheel, "spinDuration", .3f);
            SetDuration(View.Wheel, "spinSlowdownDuration", .05f);
            Button("spin").onClick.Invoke();
            Random.Index = 0;
            var deadline = Time.realtimeSinceStartup + 10f;
            while (Session.GetSnapshot().Round.Zone == 1 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            Assert.That(View.Input.InputBlocked, Is.True);
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Empty);

            var tap = Root.GetComponentInChildren<AnimationTapView>();
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.True, "Block gaps between animations as well.");
            var events = new GameObject("Lucky input test", typeof(EventSystem));
            events.transform.SetParent(Root.transform, false);
            var canvas = View.GetComponent<Canvas>();
            var pointer = new PointerEventData(events.GetComponent<EventSystem>())
            {
                position = new Vector2(canvas.pixelRect.xMin + 10, canvas.pixelRect.yMax - 10)
            };
            var hits = new List<RaycastResult>();
            View.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(tap.gameObject));
            Button("spin").onClick.Invoke();
            Button("cashout").onClick.Invoke();
            ExecuteEvents.Execute(tap.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(Session.GetSnapshot().PendingSpin, Is.Null);

            var automaticRounds = new HashSet<int>();
            while (Session.GetSnapshot().Round.Zone < 5 && Time.realtimeSinceStartup < deadline)
            {
                Presenter.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
                var pending = Session.GetSnapshot().PendingSpin;
                if (pending != null)
                {
                    automaticRounds.Add(pending.Round.Zone);
                    Assert.That(pending.IsAutomatic, Is.True);
                    Assert.That(pending.WinningSlice, Is.TypeOf<RewardSliceData>());
                    Assert.That(View.Input.InputBlocked, Is.True);
                }
                yield return null;
            }
            Assert.That(automaticRounds, Is.EquivalentTo(new[] { 2, 3, 4 }));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(Session.GetSnapshot().PendingSpin, Is.Null);
            Assert.That(View.Input.InputBlocked, Is.False);
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            Assert.That(Button("spin").interactable, Is.True);
            Assert.That(Button("cashout").interactable, Is.True);
            Assert.That(Text("error"), Is.Empty);
            var loot = Session.GetSnapshot().RunLoot;
            Button("cashout").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Progress.Amounts, Is.EqualTo(loot.Amounts));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AutomaticVisualStagesUseExistingSpeedsAndCleanupRestoresManualSpeed(bool cancelThroughView)
        {
            ConfigureLucky();
            Presenter.Detach();
            var lucky = Session.RequestSpin().Result;
            Session.CompleteResolution(lucky.OperationId);
            Random.Index = 0;
            var automatic = Session.RequestAutomaticSpin().Result;
            SetDuration(View.Wheel, "spinDuration", 4f);
            View.Wheel.Bind(automatic.Round);
            View.Wheel.Spin(automatic, result => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(16f));
            View.Wheel.Cancel();
            View.Wheel.Highlight(automatic, result => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(4f));
            CurrentTween().Complete(true);
            View.Wheel.PlayOutcome(automatic, result => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(4f));
            if (cancelThroughView)
                View.Wheel.Cancel();
            else
                CurrentTween().Kill(false);
            View.Wheel.Spin(lucky, result => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(1f));
            View.Wheel.Cancel();
            Session.CompleteResolution(automatic.OperationId);
        }

        [Test]
        public void AutomaticPreparationFailureReleasesRetryButtonAndRetryRestoresInputBlock()
        {
            ConfigureLucky();
            Presenter.Detach();
            var lucky = Session.RequestSpin().Result;
            Session.CompleteResolution(lucky.OperationId);
            Presenter.Attach();
            Random.Index = -1;
            LogAssert.Expect(LogType.Exception,
                new System.Text.RegularExpressions.Regex("InvalidOperationException: The random source returned an invalid weighted roll"));
            Presenter.AdvancePresentation(TimeSpan.Zero);

            var tap = Root.GetComponentInChildren<AnimationTapView>();
            Assert.That(Button("spin").interactable, Is.True);
            Assert.That(Button("cashout").interactable, Is.False);
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            StringAssert.Contains("Tap SPIN to retry", Text("error"));
            Presenter.AdvancePresentation(TimeSpan.FromSeconds(1));
            Assert.That(Session.GetSnapshot().PendingSpin, Is.Null);

            Random.Index = 0;
            Button("spin").onClick.Invoke();
            Assert.That(Session.GetSnapshot().PendingSpin.IsAutomatic, Is.True);
            Assert.That(Session.GetSnapshot().PendingSpin.WinningSlice, Is.TypeOf<RewardSliceData>());
            Assert.That(Button("spin").interactable, Is.False);
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            Assert.That(Text("error"), Is.Empty);
        }

        private void ConfigureLucky()
        {
            Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            var data = new SerializedObject(configuration.GetTier(WheelTier.Bronze));
            data.FindProperty("pool").GetArrayElementAtIndex(6).FindPropertyRelative("slice").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<LuckySliceAsset>("Assets/_Game/Settings/Slices/Lucky.asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            View.Initialize(configuration, AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset"));
            Session = new GameSession(configuration.CreateRuntime(), Random, new InMemoryProgressStore());
            Presenter = new GameScreenPresenter(Session, View, View.Input, new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            Random.Index = 6;
        }

        private Tween CurrentTween()
        {
            var operation = typeof(WheelView).GetField("m_Operation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(View.Wheel);
            return (Tween)typeof(TweenOperation).GetField("m_Tween", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(operation);
        }
    }
}
#endif
