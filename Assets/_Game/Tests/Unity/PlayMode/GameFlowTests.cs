#if UNITY_EDITOR
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class GameFlowTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator SafeCashOutShowsAllWonRewardTypesAndRestartKeepsProgress()
        {
            Assert.That(Button("cashout").interactable, Is.False);
            for (var index = 0; index < 4; index++)
            {
                Random.Index = index == 3 ? 4 : index;
                yield return SpinAndWait();
            }
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(Text("safety"), Does.Contain("SILVER"));
            Assert.That(Button("cashout").interactable, Is.True);
            var loot = Session.GetSnapshot().RunLoot;
            Button("cashout").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
            Assert.That(Text("result_title"), Does.Contain("REWARDS COLLECTED"));
            foreach (var pair in loot.Amounts)
            {
                Assert.That(Session.GetSnapshot().Progress.GetAmount(pair.Key), Is.EqualTo(pair.Value));
                Assert.That(Root.GetComponentsInChildren<RewardRowView>().Length, Is.GreaterThanOrEqualTo(loot.Amounts.Count));
            }
            Button("restart").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(4));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BombAnimationEndsInLossAndRestartCreatesANewRun()
        {
            yield return SpinAndWait();
            var runId = Session.GetSnapshot().RunId;
            Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(Root.GetComponentsInChildren<Transform>().Any(item => item.name == "ui_group_bomb_value"), Is.True);
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts.Count, Is.Zero);
            var flash = Root.GetComponentsInChildren<CanvasGroup>(true).Single(group => group.name == "ui_image_bomb_flash_value");
            Assert.That(flash.alpha, Is.Zero);
            Button("restart").onClick.Invoke();
            Assert.That(Session.GetSnapshot().RunId, Is.Not.EqualTo(runId));
            Assert.That(Button("spin").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator DetachDuringRealAnimationCompletesOnceAndReattachDoesNotDuplicateListeners()
        {
            Button("spin").onClick.Invoke();
            Presenter.Detach();
            Root.SetActive(false);
            yield return null;
            Root.SetActive(true);
            Presenter.Attach();
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            yield return SpinAndWait();
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            Assert.That(Session.GetSnapshot().RunLoot.GetAmount("Gold"), Is.EqualTo(22));
        }

        [UnityTest]
        public IEnumerator GoldTierUsesRealVisualsAndAwardsSpecialReward()
        {
            for (var zone = 1; zone < 30; zone++)
            {
                var result = Session.RequestSpin().Result;
                Session.CompleteResolution(result.OperationId);
            }
            Presenter.Detach();
            Presenter.Attach();
            Assert.That(Text("safety"), Does.Contain("GOLDEN"));
            Assert.That(Button("cashout").interactable, Is.True);
            Random.Index = 3;
            yield return SpinAndWait();
            Assert.That(Session.GetSnapshot().RunLoot.GetAmount("Super_chests"), Is.EqualTo(30));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(31));
            Assert.That(Button("cashout").interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator ReviveButtonRestoresLootAndGiveUpStartsFreshRun()
        {
            Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            Session = new GameSession(configuration.CreateRuntime(), Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward("Gold", 250) })), new Reward("Gold", 25));
            Presenter = new GameScreenPresenter(Session, View,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            yield return SpinAndWait();
            var loot = Session.GetSnapshot().RunLoot;
            var runId = Session.GetSnapshot().RunId;
            Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(Button("bomb_revive").interactable, Is.True);
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Session.GetSnapshot().RunLoot, Is.SameAs(loot));
            Assert.That(Session.GetSnapshot().Progress.GetAmount("Gold"), Is.EqualTo(225));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            Assert.That(Session.GetSnapshot().RunId, Is.EqualTo(runId));
            Assert.That(Text("wallet_amount"), Is.EqualTo("225"));
            yield return SpinAndWait();
            StringAssert.StartsWith("REVIVE 50 ", Text("bomb_revive_label"));
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Progress.GetAmount("Gold"), Is.EqualTo(175));
            yield return SpinAndWait();
            StringAssert.StartsWith("REVIVE 100 ", Text("bomb_revive_label"));
            Button("bomb_give_up").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().RunId, Is.Not.EqualTo(runId));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(Session.GetSnapshot().Progress.GetAmount("Gold"), Is.EqualTo(175));
            Assert.That(Session.GetSnapshot().ReviveCost.Amount, Is.EqualTo(25));
        }

        [UnityTest]
        public IEnumerator CashCurrencyDrivesWalletIconReviveLabelAndPayment()
        {
            Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            var currency = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Cash.asset");
            View.Initialize(configuration, currency);
            Session = new GameSession(configuration.CreateRuntime(), Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward(currency.Id, 100) })),
                new Reward(currency.Id, 25));
            Presenter = new GameScreenPresenter(Session, View,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
            Assert.That(Text("wallet_amount"), Is.EqualTo("100"));
            var icon = Root.GetComponentsInChildren<Image>(true).Single(item => item.name == "ui_image_wallet_icon_value");
            Assert.That(icon.sprite, Is.EqualTo(currency.Icon));
            Assert.That(icon.rectTransform.rect.width / icon.rectTransform.rect.height,
                Is.EqualTo(currency.Icon.rect.width / currency.Icon.rect.height).Within(.001f));
            Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(Text("bomb_revive_label"), Is.EqualTo("REVIVE 25 " + currency.DisplayName));
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Text("wallet_amount"), Is.EqualTo("75"));
            Assert.That(Session.GetSnapshot().Progress.GetAmount(currency.Id), Is.EqualTo(75));
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
        }

        [Test]
        public void BootstrapUsesTheSelectedCurrencyForStartingBalance()
        {
            var parent = new GameObject("Currency bootstrap test");
            parent.SetActive(false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
                var instance = Object.Instantiate(prefab, parent.transform);
                var currency = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Cash.asset");
                var data = new SerializedObject(instance.GetComponent<GameBootstrap>());
                data.FindProperty("currency").objectReferenceValue = currency;
                data.ApplyModifiedPropertiesWithoutUndo();
                parent.SetActive(true);
                var wallet = instance.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name == "ui_text_wallet_amount_value");
                Assert.That(wallet.text, Is.EqualTo("250"));
                var icon = instance.GetComponentsInChildren<Image>(true).Single(item => item.name == "ui_image_wallet_icon_value");
                Assert.That(icon.sprite, Is.EqualTo(currency.Icon));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedRewardSlotsRenderTheirAmountsAndCollectIntoOneBalance()
        {
            Presenter.Detach();
            var source = CreateFixedConfiguration();
            var configuration = Object.Instantiate(source);
            var bronze = Object.Instantiate(source.GetTier(WheelTier.Bronze));
            var original = (RewardSliceAsset)bronze.GetSlice(0);
            var larger = Object.Instantiate(original);
            try
            {
                var sliceData = new SerializedObject(larger);
                sliceData.FindProperty("baseAmount").longValue = 20;
                sliceData.ApplyModifiedPropertiesWithoutUndo();
                var tierData = new SerializedObject(bronze);
                tierData.FindProperty("pool").GetArrayElementAtIndex(1).FindPropertyRelative("slice").objectReferenceValue = larger;
                tierData.FindProperty("pool").GetArrayElementAtIndex(2).FindPropertyRelative("slice").objectReferenceValue = original;
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configData = new SerializedObject(configuration);
                configData.FindProperty("bronze").objectReferenceValue = bronze;
                configData.ApplyModifiedPropertiesWithoutUndo();
                View.Initialize(configuration, original.Reward);
                Session = new GameSession(configuration.CreateRuntime(), Random, new InMemoryProgressStore());
                Presenter = new GameScreenPresenter(Session, View,
                    new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
                Presenter.Attach();
                var views = View.Wheel.GetComponentsInChildren<WheelSliceView>();
                Assert.That(views[0].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x10"));
                Assert.That(views[1].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x20"));
                Assert.That(views[2].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x10"));
                Assert.That(views[1].Icon, Is.EqualTo(views[0].Icon));
                foreach (var index in new[] { 1, 2, 0, 0 })
                {
                    Random.Index = index;
                    yield return SpinAndWait();
                }
                Assert.That(Session.GetSnapshot().RunLoot.GetAmount("Gold"), Is.EqualTo(62));
                Button("cashout").onClick.Invoke();
                Assert.That(Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(1));
                Assert.That(Text("wallet_amount"), Is.EqualTo("62"));
                Assert.That(source.GetTier(WheelTier.Bronze).GetSlice(1).Id, Is.EqualTo("Cash"));
            }
            finally
            {
                Presenter.Detach();
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(bronze);
                Object.DestroyImmediate(larger);
            }
        }

        [UnityTest]
        public IEnumerator FiftyEntryPoolBindsSelectedVisualsAndPaysTheSelectedSlot()
        {
            Presenter.Detach();
            var source = CreateFixedConfiguration();
            var configuration = Object.Instantiate(source);
            var silver = Object.Instantiate(source.GetTier(WheelTier.Silver));
            var small = AssetDatabase.LoadAssetAtPath<RewardSliceAsset>("Assets/_Game/Settings/Slices/GoldSmall.asset");
            var large = AssetDatabase.LoadAssetAtPath<RewardSliceAsset>("Assets/_Game/Settings/Slices/GoldLarge.asset");
            var cash = AssetDatabase.LoadAssetAtPath<RewardSliceAsset>("Assets/_Game/Settings/Slices/Cash.asset");
            try
            {
                var tierData = new SerializedObject(silver);
                var pool = tierData.FindProperty("pool");
                pool.arraySize = 50;
                for (var index = 0; index < pool.arraySize; index++)
                {
                    var entry = pool.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("slice").objectReferenceValue = index < 40 ? small : index % 2 == 0 ? cash : large;
                    entry.FindPropertyRelative("weight").intValue = 1;
                }
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configData = new SerializedObject(configuration);
                configData.FindProperty("silver").objectReferenceValue = silver;
                configData.ApplyModifiedPropertiesWithoutUndo();
                var random = new SequenceRandomSource(new[] { 0, 0, 0, 0 }.Concat(Enumerable.Repeat(40, 8)));
                Session = new GameSession(configuration.CreateRuntime(), random, new InMemoryProgressStore());
                View.Initialize(configuration, small.Reward);
                Presenter = new GameScreenPresenter(Session, View,
                    new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
                Presenter.Attach();
                for (var zone = 1; zone < 5; zone++)
                    yield return SpinAndWait();
                var round = Session.GetSnapshot().Round;
                Assert.That(round.SourceIndices, Is.EqualTo(Enumerable.Range(40, 8)));
                var views = View.Wheel.GetComponentsInChildren<WheelSliceView>();
                for (var index = 0; index < views.Length; index++)
                {
                    Assert.That(views[index].Icon, Is.EqualTo(index % 2 == 0 ? cash.Icon : large.Icon));
                    Assert.That(views[index].GetComponentInChildren<TMP_Text>().text, Is.EqualTo(index % 2 == 0 ? "x180" : "x28"));
                }
                yield return SpinAndWait();
                Assert.That(Session.GetSnapshot().RunLoot.GetAmount("Cash"), Is.EqualTo(180));
                Assert.That(source.GetTier(WheelTier.Silver).CandidateCount, Is.EqualTo(8));
            }
            finally
            {
                Presenter.Detach();
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(silver);
            }
        }
    }
}
#endif
