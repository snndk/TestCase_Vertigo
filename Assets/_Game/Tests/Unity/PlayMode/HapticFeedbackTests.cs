#if UNITY_EDITOR
using System;
using System.Collections;
using Lofelt.NiceVibrations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Haptics;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class HapticFeedbackTests : GameViewTestFixture
    {
        private GameHaptics m_Haptics;
        private RecordingHapticOutput m_Output;

        [SetUp]
        public void SetUpHaptics()
        {
            m_Haptics = Root.GetComponentInChildren<GameHaptics>();
            m_Output = new RecordingHapticOutput();
            m_Haptics.SetOutput(m_Output);
            m_Haptics.SendMessage("OnApplicationFocus", true);
        }

        [UnityTest]
        public IEnumerator SpinPlaysStartStopAndRewardOnceWhileCancelledSpinHasNoResultHaptics()
        {
            yield return SpinAndWait();
            CollectionAssert.AreEqual(new[] { HapticPatterns.PresetType.LightImpact,
                HapticPatterns.PresetType.MediumImpact, HapticPatterns.PresetType.Success }, m_Output.Played);
            Presenter.Refresh();
            Assert.That(m_Output.Played.Count, Is.EqualTo(3));
            m_Output.Played.Clear();
            Button("spin").onClick.Invoke();
            View.Wheel.Cancel();
            yield return null;
            CollectionAssert.AreEqual(new[] { HapticPatterns.PresetType.LightImpact }, m_Output.Played);
            Assert.That(m_Output.StopCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator BombAndSuccessfulReviveUseDifferentCuesWithoutRenderReplay()
        {
            Presenter.Detach();
            Session = new GameSession(CreateFixedConfiguration().CreateRuntime(), Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward("Gold", 250) })), new Reward("Gold", 25));
            Presenter = new GameScreenPresenter(Session, View, View.Input,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(m_Output.Played[m_Output.Played.Count - 1], Is.EqualTo(HapticPatterns.PresetType.Failure));
            var count = m_Output.Played.Count;
            Button("bomb_revive").onClick.Invoke();
            Assert.That(m_Output.Played.Count, Is.EqualTo(count + 1));
            Assert.That(m_Output.Played[count], Is.EqualTo(HapticPatterns.PresetType.Success));
            Presenter.Refresh();
            Button("bomb_revive").onClick.Invoke();
            Assert.That(m_Output.Played.Count, Is.EqualTo(count + 1));
        }

        [UnityTest]
        public IEnumerator CashOutPlaysOnlyOnAcceptedCollection()
        {
            for (var index = 0; index < 4; index++)
                yield return SpinAndWait();
            m_Output.Played.Clear();
            Button("cashout").onClick.Invoke();
            Presenter.Refresh();
            Button("cashout").onClick.Invoke();
            CollectionAssert.AreEqual(new[] { HapticPatterns.PresetType.Success }, m_Output.Played);
        }

        [Test]
        public void ProfileOverridesNoneGlobalDisableAndLifecycleAreRespected()
        {
            var profile = ScriptableObject.CreateInstance<HapticProfile>();
            try
            {
                var component = new SerializedObject(m_Haptics);
                component.FindProperty("profile").objectReferenceValue = profile;
                component.ApplyModifiedPropertiesWithoutUndo();
                var data = new SerializedObject(profile);
                data.FindProperty("spinStart").intValue = (int)HapticPatterns.PresetType.HeavyImpact;
                data.FindProperty("reward").intValue = (int)HapticPatterns.PresetType.None;
                data.ApplyModifiedPropertiesWithoutUndo();
                m_Haptics.Play(HapticCue.SpinStart);
                m_Haptics.Play(HapticCue.Reward);
                CollectionAssert.AreEqual(new[] { HapticPatterns.PresetType.HeavyImpact }, m_Output.Played);
                m_Haptics.HapticsEnabled = false;
                m_Haptics.Play(HapticCue.Bomb);
                Assert.That(m_Output.StopCount, Is.EqualTo(1));
                m_Haptics.HapticsEnabled = true;
                m_Haptics.SendMessage("OnApplicationPause", true);
                m_Haptics.Play(HapticCue.Bomb);
                m_Haptics.SendMessage("OnApplicationPause", false);
                m_Haptics.SendMessage("OnApplicationFocus", false);
                m_Haptics.Play(HapticCue.Bomb);
                m_Haptics.SendMessage("OnApplicationFocus", true);
                data.FindProperty("hapticsEnabled").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
                m_Haptics.Play(HapticCue.Bomb);
                m_Haptics.enabled = false;
                Assert.That(m_Output.Played.Count, Is.EqualTo(1));
                Assert.That(m_Output.StopCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
#endif
