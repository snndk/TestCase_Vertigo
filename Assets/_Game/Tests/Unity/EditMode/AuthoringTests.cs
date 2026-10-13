using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Unity._Game.Unity.Authoring;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.EditMode
{
    public class AuthoringTests
    {
        private const string m_ConfigPath = "Assets/_Game/Settings/WheelConfiguration.asset";

        [TestCase(1, WheelTier.Bronze, 1)]
        [TestCase(5, WheelTier.Silver, 0)]
        [TestCase(30, WheelTier.Gold, 0)]
        public void AuthoredTiersGenerateRequiredContents(int zone, WheelTier tier, int bombs)
        {
            var configuration = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>(m_ConfigPath);
            var round = new RoundGenerator(configuration.CreateRuntime(), new Vertigo.Unity._Game.Unity.Bootstrap.SystemRandomSource()).Generate(zone);
            Assert.That(round.Tier, Is.EqualTo(tier));
            Assert.That(round.Slices.Count, Is.EqualTo(8));
            Assert.That(round.Slices.Count(slice => slice is BombSliceData), Is.EqualTo(bombs));
            for (var index = 0; index < round.Slices.Count; index++)
                Assert.That(configuration.GetTier(tier).GetSlice(round.SourceIndices[index]).Id, Is.EqualTo(round.Slices[index].Id));
        }

        [Test]
        public void InspectorSliceOrderDeterminesRuntimeOrderWithoutChangingSourceAsset()
        {
            var source = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>(m_ConfigPath);
            var configuration = Object.Instantiate(source);
            var silver = Object.Instantiate(source.GetTier(WheelTier.Silver));
            try
            {
                var tierData = new SerializedObject(silver);
                tierData.FindProperty("pool").MoveArrayElement(0, 1);
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configData = new SerializedObject(configuration);
                configData.FindProperty("silver").objectReferenceValue = silver;
                configData.ApplyModifiedPropertiesWithoutUndo();
                var round = new RoundGenerator(configuration.CreateRuntime(), new Vertigo.Unity._Game.Unity.Bootstrap.SystemRandomSource()).Generate(5);
                Assert.That(round.Slices[0].Id, Is.EqualTo("Cash"));
                Assert.That(source.GetTier(WheelTier.Silver).GetSlice(0).Id, Is.EqualTo("Gold"));
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(silver);
            }
        }

        [Test]
        public void InspectorSpinWeightIsIndependentFromInclusionWeight()
        {
            var source = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>(m_ConfigPath);
            var tier = Object.Instantiate(source.GetTier(WheelTier.Silver));
            try
            {
                var data = new SerializedObject(tier);
                var entry = data.FindProperty("pool").GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("weight").intValue = 25;
                entry.FindPropertyRelative("spinWeight").intValue = 70;
                data.ApplyModifiedPropertiesWithoutUndo();
                var runtime = tier.CreateCandidates()[0];
                Assert.That(runtime.Weight, Is.EqualTo(25));
                Assert.That(runtime.SpinWeight, Is.EqualTo(70));
                Assert.That(source.GetTier(WheelTier.Silver).CreateCandidates()[0].SpinWeight, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(tier);
            }
        }

        [Test]
        public void DifferentAssetsWithTheSameRewardIdAreRejected()
        {
            var source = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>(m_ConfigPath);
            var configuration = Object.Instantiate(source);
            var gold = Object.Instantiate(source.GetTier(WheelTier.Gold));
            var slice = Object.Instantiate((RewardSliceAsset)gold.GetSlice(0));
            var reward = Object.Instantiate(slice.Reward);
            try
            {
                var sliceData = new SerializedObject(slice);
                sliceData.FindProperty("reward").objectReferenceValue = reward;
                sliceData.ApplyModifiedPropertiesWithoutUndo();
                var tierData = new SerializedObject(gold);
                tierData.FindProperty("pool").GetArrayElementAtIndex(0).FindPropertyRelative("slice").objectReferenceValue = slice;
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configData = new SerializedObject(configuration);
                configData.FindProperty("gold").objectReferenceValue = gold;
                configData.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => configuration.CreateRuntime());
                Assert.Throws<InvalidOperationException>(() => new Vertigo.Unity._Game.Unity.Views.RewardCatalog(configuration));
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(gold);
                Object.DestroyImmediate(slice);
                Object.DestroyImmediate(reward);
            }
        }

        [Test]
        public void RewardCatalogDiscoversRewardsFromAnIndependentSliceType()
        {
            var source = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>(m_ConfigPath);
            var configuration = Object.Instantiate(source);
            var bronze = Object.Instantiate(source.GetTier(WheelTier.Bronze));
            var reward = Object.Instantiate(((RewardSliceAsset)bronze.GetSlice(0)).Reward);
            var custom = ScriptableObject.CreateInstance<AdditionalRewardSliceAsset>();
            try
            {
                var rewardData = new SerializedObject(reward);
                rewardData.FindProperty("rewardId").stringValue = "IndependentReward";
                rewardData.ApplyModifiedPropertiesWithoutUndo();
                var gold = ((RewardSliceAsset)bronze.GetSlice(0)).Reward;
                custom.Initialize(gold, reward);
                var tierData = new SerializedObject(bronze);
                tierData.FindProperty("pool").GetArrayElementAtIndex(0).FindPropertyRelative("slice").objectReferenceValue = custom;
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configData = new SerializedObject(configuration);
                configData.FindProperty("bronze").objectReferenceValue = bronze;
                configData.ApplyModifiedPropertiesWithoutUndo();
                var catalog = new Vertigo.Unity._Game.Unity.Views.RewardCatalog(configuration);
                Assert.That(catalog.Find("IndependentReward"), Is.SameAs(reward));
                Assert.That(catalog.Find(gold.Id), Is.SameAs(gold));
                rewardData.FindProperty("rewardId").stringValue = gold.Id;
                rewardData.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => new Vertigo.Unity._Game.Unity.Views.RewardCatalog(configuration));
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(bronze);
                Object.DestroyImmediate(reward);
                Object.DestroyImmediate(custom);
            }
        }

        [Test]
        public void MissingTierRejectsConfiguration()
        {
            var configuration = ScriptableObject.CreateInstance<WheelConfigurationAsset>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => configuration.CreateRuntime());
            }
            finally
            {
                Object.DestroyImmediate(configuration);
            }
        }
    }
}
