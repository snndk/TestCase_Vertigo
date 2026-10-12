using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.EditMode
{
    public class GameSettingsTests
    {
        private GameSettings m_Settings;
        private SerializedObject m_Data;

        [SetUp]
        public void SetUp()
        {
            m_Settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/_Game/Settings/GameSettings.asset"));
            m_Data = new SerializedObject(m_Settings);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Settings);

        [TestCase(true)]
        [TestCase(false)]
        public void SettingsControlReviveAvailabilityAndProduceIndependentRuntimeData(bool enabled)
        {
            m_Data.FindProperty("reviveEnabled").boolValue = enabled;
            m_Data.FindProperty("initialReviveCost").longValue = 17;
            m_Data.FindProperty("reviveIncreasePercent").intValue = 50;
            m_Data.FindProperty("startingBalance").longValue = 420;
            m_Data.ApplyModifiedPropertiesWithoutUndo();
            var progress = m_Settings.CreateStartingRewards();
            var cost = m_Settings.CreateReviveCost();
            var session = new GameSession(m_Settings.WheelConfiguration.CreateRuntime(), new SystemRandomSource(),
                new InMemoryProgressStore(progress), cost, m_Settings.ReviveIncreasePercent);
            var snapshot = session.GetSnapshot();
            Assert.That(snapshot.Progress.GetAmount(m_Settings.Currency.Id), Is.EqualTo(420));
            if (enabled)
                Assert.That(snapshot.ReviveCost.Amount, Is.EqualTo(17));
            else
                Assert.That(snapshot.ReviveCost, Is.Null);
            m_Data.FindProperty("startingBalance").longValue = 1;
            m_Data.FindProperty("initialReviveCost").longValue = 1;
            m_Data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(session.GetSnapshot().Progress, Is.SameAs(progress));
            Assert.That(session.GetSnapshot().ReviveCost, Is.SameAs(cost));
        }

        [Test]
        public void ZeroStartingBalanceCreatesAnEmptyWallet()
        {
            m_Data.FindProperty("startingBalance").longValue = 0;
            m_Data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(m_Settings.CreateStartingRewards().Amounts, Is.Empty);
        }

        [TestCase("wheelConfiguration")]
        [TestCase("currency")]
        public void MissingReferencesAreRejected(string field)
        {
            m_Data.FindProperty(field).objectReferenceValue = null;
            m_Data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => m_Settings.CreateStartingRewards());
        }

        [TestCase("startingBalance", -1)]
        [TestCase("initialReviveCost", 0)]
        [TestCase("reviveIncreasePercent", -1)]
        public void InvalidEconomicSettingsAreRejected(string field, long value)
        {
            m_Data.FindProperty(field).longValue = value;
            m_Data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => m_Settings.CreateReviveCost());
        }
    }
}
