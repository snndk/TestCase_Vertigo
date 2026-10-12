#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public abstract class GameViewTestFixture
    {
        private readonly List<Object> m_ConfigurationAssets = new List<Object>();
        private GameObject m_Root;
        private GameScreenView m_View;
        private GameScreenPresenter m_Presenter;
        private GameSession m_Session;
        private FixedRandomSource m_Random;

        protected GameObject Root => m_Root;
        protected GameScreenView View => m_View;
        protected FixedRandomSource Random => m_Random;
        protected GameScreenPresenter Presenter
        {
            get => m_Presenter;
            set => m_Presenter = value;
        }
        protected GameSession Session
        {
            get => m_Session;
            set => m_Session = value;
        }

        [SetUp]
        public void SetUp()
        {
            // Activate only after removing the production composition root: tests own an isolated in-memory session.
            m_Root = new GameObject("Test game");
            m_Root.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
            var instance = Object.Instantiate(prefab, m_Root.transform);
            Object.DestroyImmediate(instance.GetComponent<GameBootstrap>());
            m_View = instance.GetComponent<GameScreenView>();
            var configuration = CreateFixedConfiguration();
            m_View.Initialize(configuration, AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset"));
            SetDuration(m_View.Wheel, "spinDuration", 0.03f);
            SetDuration(instance.GetComponentInChildren<RewardOutcomeView>(), "flightDuration", 0.03f);
            m_Random = new FixedRandomSource();
            m_Session = new GameSession(configuration.CreateRuntime(), m_Random, new InMemoryProgressStore());
            m_Presenter = new GameScreenPresenter(m_Session, m_View, m_View.Input,
                new SpinPresentationSequence(m_View.Wheel, TimeSpan.FromSeconds(5)));
            m_Root.SetActive(true);
            m_Presenter.Attach();
        }

        [TearDown]
        public void TearDown()
        {
            m_Presenter?.Detach();
            if (m_Root != null)
                Object.DestroyImmediate(m_Root);
            foreach (var asset in m_ConfigurationAssets)
                Object.DestroyImmediate(asset);
            m_ConfigurationAssets.Clear();
        }

        protected WheelConfigurationAsset CreateFixedConfiguration()
        {
            // Flow tests target known outcomes; production pool balance may change independently.
            var source = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>("Assets/_Game/Settings/WheelConfiguration.asset");
            var configuration = Object.Instantiate(source);
            var bronze = Object.Instantiate(source.GetTier(WheelTier.Bronze));
            m_ConfigurationAssets.Add(configuration);
            m_ConfigurationAssets.Add(bronze);
            var tierData = new SerializedObject(bronze);
            var pool = tierData.FindProperty("pool");
            pool.arraySize = 8;
            for (var index = 0; index < pool.arraySize; index++)
                pool.GetArrayElementAtIndex(index).FindPropertyRelative("weight").intValue = 100;
            tierData.ApplyModifiedPropertiesWithoutUndo();
            var configData = new SerializedObject(configuration);
            configData.FindProperty("bronze").objectReferenceValue = bronze;
            configData.ApplyModifiedPropertiesWithoutUndo();
            return configuration;
        }

        protected IEnumerator SpinAndWait()
        {
            Button("spin").onClick.Invoke();
            Assert.That(Button("spin").interactable, Is.False);
            Assert.That(Button("cashout").interactable, Is.False);
            var deadline = Time.realtimeSinceStartup + 5;
            while (m_Session.GetSnapshot().Status == SessionStatus.Resolving && Time.realtimeSinceStartup < deadline)
            {
                m_Presenter.AdvancePresentation(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
                yield return null;
            }
            Assert.That(m_Session.GetSnapshot().Status, Is.Not.EqualTo(SessionStatus.Resolving));
            var transition = m_View.Wheel.GetComponent<WheelTierTransition>();
            while (transition.IsPlaying && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(transition.IsPlaying, Is.False);
            Assert.That(Text("error"), Is.Empty);
        }

        protected Button Button(string name)
        {
            var suffix = name;
            switch (name)
            {
                case "spin":
                    suffix = "wheel_spin";
                    break;
                case "cashout":
                    suffix = "exit";
                    break;
                case "restart":
                    suffix = m_Session.GetSnapshot().Status == SessionStatus.CashedOut
                        ? "cash_out_new_run"
                        : "bomb_give_up";
                    break;
            }
            var objectName = "ui_button_" + suffix + "_value";
            return m_Root.GetComponentsInChildren<Button>(true).Single(button => button.name == objectName);
        }

        protected string Text(string name)
        {
            var suffix = name;
            if (name == "safety")
                suffix = "wheel_title";
            else if (name == "result_title")
                suffix = "cash_out_title";

            var objectName = "ui_text_" + suffix + "_value";
            return m_Root.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == objectName).text;
        }

        protected static void SetDuration(Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
