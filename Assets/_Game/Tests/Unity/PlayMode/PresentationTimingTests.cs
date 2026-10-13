#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class PresentationTimingTests
    {
        private GameObject m_Root;
        private GameBootstrap m_Bootstrap;
        private GameSession m_Session;
        private WheelView m_Wheel;
        private float m_TimeScale;
        private float m_MaximumDeltaTime;

        [SetUp]
        public void SetUp()
        {
            m_TimeScale = Time.timeScale;
            m_MaximumDeltaTime = Time.maximumDeltaTime;
            Time.maximumDeltaTime = 1f / 3f;
            m_Root = new GameObject("Presentation timing test");
            m_Root.SetActive(false);
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/ui_canvas_game.prefab"), m_Root.transform);
            m_Bootstrap = instance.GetComponent<GameBootstrap>();
            m_Session = new GameSession(m_Bootstrap.Settings.WheelConfiguration.CreateRuntime(),
                new FixedRandomSource(), new ProgressService(new InMemoryProgressStore()), startingZone: 5);
            m_Bootstrap.ReplaceSession(m_Session, true);
            m_Wheel = instance.GetComponentInChildren<WheelView>(true);
            typeof(WheelView).GetField("spinDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(m_Wheel, .05f);
            m_Root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
                Object.DestroyImmediate(m_Root);
            Time.timeScale = m_TimeScale;
            Time.maximumDeltaTime = m_MaximumDeltaTime;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LongFrameDoesNotCancelAnOtherwiseHealthyAnimation(int stage)
        {
            StartSpin();
            for (var index = 0; index < stage; index++)
                CurrentTween().Complete(true);
            var rewards = m_Session.GetSnapshot().RunLoot;

            Advance(30f);

            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
            Assert.That(ErrorText(), Is.Empty);
            for (var index = stage; index < 3; index++)
                CurrentTween().Complete(true);
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(6));
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(rewards));
            Assert.That(ErrorText(), Is.Empty);
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void UnfinishedAnimationStillTimesOutAfterTenSecondsOfActiveFrames(float timeScale)
        {
            Time.timeScale = timeScale;
            StartSpin();
            CurrentTween().Pause();
            for (var index = 0; index < 99; index++)
                Advance(.1f);
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));

            LogAssert.Expect(LogType.Exception, new Regex("TimeoutException: Spin presentation did not finish"));
            Advance(.2f);

            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(6));
            Assert.That(CurrentTween(), Is.Null);
            Assert.That(ErrorText(), Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator AnimationAndWatchdogRemainUnscaled()
        {
            Time.timeScale = 0;
            StartSpin();
            var deadline = Time.realtimeSinceStartup + 4;
            while (m_Session.GetSnapshot().Status == SessionStatus.Resolving && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(6));
            Assert.That(ErrorText(), Is.Empty);
        }

        private void StartSpin()
        {
            m_Root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "ui_button_wheel_spin_value").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Resolving));
        }

        private void Advance(float elapsed)
        {
            typeof(GameBootstrap).GetMethod("AdvancePresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(m_Bootstrap, new object[] { elapsed });
        }

        private Tween CurrentTween()
        {
            var operation = typeof(WheelView).GetField("m_Operation", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(m_Wheel);
            return (Tween)typeof(TweenOperation).GetField("m_Tween", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(operation);
        }

        private string ErrorText() => m_Root.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "ui_text_error_value").text;
    }
}
#endif
