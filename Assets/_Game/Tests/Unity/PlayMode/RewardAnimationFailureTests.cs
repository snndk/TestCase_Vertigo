#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Views;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class RewardAnimationFailureTests : GameViewTestFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ConstructionFailureKillsPartialSequenceAndAllowsAnotherAnimation(bool failOnArrival)
        {
            var behavior = InstallFailure(failOnArrival);
            var outcome = Root.GetComponentInChildren<RewardOutcomeView>(true);
            var slice = Root.GetComponentsInChildren<WheelSliceView>()
                .First(view => view.Slice is RewardSliceData);
            try
            {
                if (failOnArrival)
                    outcome.Highlight(slice).Complete(true);

                var error = Assert.Throws<InvalidOperationException>(() =>
                {
                    if (failOnArrival)
                        outcome.Play(slice);
                    else
                        outcome.Highlight(slice);
                });
                Assert.That(error, Is.SameAs(behavior.Failure));
                AssertClean(behavior, slice);
                DOTween.ManualUpdate(1f, 1f);
                Assert.That(behavior.CompletionRan, Is.False);

                behavior.FailureEnabled = false;
                outcome.Highlight(slice).Complete(true);
                outcome.Play(slice).Complete(true);
                outcome.ResetVisuals();
                Assert.That(slice.IconRect.GetComponent<Image>().enabled, Is.True);
            }
            finally
            {
                outcome.ResetVisuals();
                Object.DestroyImmediate(behavior);
            }
        }

        [UnityTest]
        public IEnumerator HighlightFailureCompletesTheAwardedRoundOnce()
        {
            return VerifyPresentationRecovery(false);
        }

        [UnityTest]
        public IEnumerator ArrivalFailureCompletesTheAwardedRoundOnce()
        {
            return VerifyPresentationRecovery(true);
        }

        private IEnumerator VerifyPresentationRecovery(bool failOnArrival)
        {
            var behavior = InstallFailure(failOnArrival);
            try
            {
                LogAssert.Expect(LogType.Exception, "InvalidOperationException: Test animation construction failed.");
                Button("spin").onClick.Invoke();
                var expectedLoot = Session.GetSnapshot().RunLoot;
                var deadline = Time.realtimeSinceStartup + 5;
                while (Session.GetSnapshot().Status == SessionStatus.Resolving && Time.realtimeSinceStartup < deadline)
                {
                    Presenter.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
                    yield return null;
                }

                Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
                Assert.That(Session.GetSnapshot().RunLoot, Is.SameAs(expectedLoot));
                Assert.That(Text("error"), Is.Not.Empty);
                // Flush deferred disposal when Kill was requested inside DOTween's update loop.
                DOTween.ManualUpdate(1f, 1f);
                Assert.That(behavior.SequenceKilled, Is.True);
                Assert.That(behavior.TweenKilled, Is.True);
                Assert.That(behavior.CompletionRan, Is.False);
                Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
                Assert.That(Session.GetSnapshot().RunLoot, Is.SameAs(expectedLoot));
            }
            finally
            {
                View.Wheel.Cancel();
                Object.DestroyImmediate(behavior);
            }
        }

        private FailingWheelAnimationAsset InstallFailure(bool failOnArrival)
        {
            Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            var behavior = ScriptableObject.CreateInstance<FailingWheelAnimationAsset>();
            behavior.FailOnArrival = failOnArrival;
            var tierData = new SerializedObject(configuration.GetTier(WheelTier.Bronze));
            tierData.FindProperty("animation").objectReferenceValue = behavior;
            tierData.ApplyModifiedPropertiesWithoutUndo();
            Session = new GameSession(configuration.CreateRuntime(), Random, new InMemoryProgressStore());
            View.Wheel.Initialize(configuration);
            Presenter = new GameScreenPresenter(Session, View, View.Input,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            return behavior;
        }

        private void AssertClean(FailingWheelAnimationAsset behavior, WheelSliceView slice)
        {
            Assert.That(behavior.FailedSequence.IsActive(), Is.False);
            Assert.That(behavior.FailedTween.IsActive(), Is.False);
            Assert.That(slice.IconRect.GetComponent<Image>().enabled, Is.True);
            Assert.That(Root.GetComponentInChildren<PulseRingGraphic>(true).gameObject.activeSelf, Is.False);
            var contents = Root.GetComponentsInChildren<CanvasGroup>(true)
                .Single(group => group.name == "ui_group_wheel_rotor_value");
            Assert.That(contents.alpha, Is.EqualTo(1f));
            Assert.That(Root.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("ui_image_reward_burst_"))
                .All(image => !image.gameObject.activeSelf), Is.True);
        }
    }
}
#endif
