#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;
using Object = UnityEngine.Object;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class GameFlowTests
    {
        private readonly List<Object> m_ConfigurationAssets = new List<Object>();
        private GameObject m_Root;
        private GameScreenView m_View;
        private GameScreenPresenter m_Presenter;
        private GameSession m_Session;
        private FixedRandomSource m_Random;

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
            m_Presenter = new GameScreenPresenter(m_Session, m_View,
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

        [UnityTest]
        public IEnumerator RewardRowCountsOnlyNewArrivalsAndSettlesWhenDisabled()
        {
            var reward = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset");
            var list = m_Root.GetComponentsInChildren<RewardListView>().First();
            var row = list.Reserve(reward.Id);
            row.Bind(reward.Id, reward, 100);
            var amount = row.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name.Contains("amount"));
            var gain = row.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name.Contains("gain"));
            Assert.That(amount.text, Is.EqualTo("100"));
            Assert.That(gain.gameObject.activeSelf, Is.False);
            row.PrepareArrival(reward.Id, reward);
            row.PulseArrival();
            row.CreateArrivalCount(50, 0.35f);
            Assert.That(amount.text, Is.EqualTo("100"));
            Assert.That(gain.gameObject.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(gain.text, Is.EqualTo("+50"));
            Assert.That(long.Parse(amount.text), Is.InRange(101L, 149L));
            row.gameObject.SetActive(false);
            Assert.That(amount.text, Is.EqualTo("150"));
            Assert.That(gain.gameObject.activeSelf, Is.False);
            row.gameObject.SetActive(true);
            row.Bind(reward.Id, reward, 150);
            Assert.That(gain.gameObject.activeSelf, Is.False);
            Assert.That(row.GetComponentsInChildren<Image>(true).Any(item => item.name.Contains("flash")), Is.False);
        }

        [UnityTest]
        public IEnumerator RewardRowCountEndsAtTheExactLargeIntegerTotal()
        {
            var reward = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset");
            var row = m_Root.GetComponentsInChildren<RewardListView>().First().Reserve(reward.Id);
            row.Bind(reward.Id, reward, long.MaxValue - 100);
            row.PrepareArrival(reward.Id, reward);
            row.CreateArrivalCount(100, 0.35f);
            yield return new WaitForSecondsRealtime(0.4f);
            var amount = row.GetComponentsInChildren<TMP_Text>().Single(item => item.name.Contains("amount"));
            Assert.That(amount.text, Is.EqualTo(long.MaxValue.ToString("N0")));
        }

        [Test]
        public void RewardListSmoothlyRevealsAnOffscreenRowWithoutMovingItsLayoutRoot()
        {
            var list = m_Root.GetComponentsInChildren<RewardListView>().First();
            var configuration = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>("Assets/_Game/Settings/WheelConfiguration.asset");
            RewardRowView row = null;
            foreach (var reward in configuration.GetRewards())
                row = list.PrepareArrival(reward.Id);
            Canvas.ForceUpdateCanvases();
            var scroll = list.GetComponentInChildren<ScrollRect>();
            if (scroll == null)
                scroll = list.GetComponent<ScrollRect>();
            var content = (RectTransform)row.transform.parent;
            scroll = content.GetComponentInParent<ScrollRect>();
            content.anchoredPosition = Vector2.zero;
            var rootPosition = ((RectTransform)row.transform).anchoredPosition;
            var tween = list.RevealArrival(row);
            tween.Goto(0.11f);
            var midway = content.anchoredPosition.y;
            Assert.That(midway, Is.GreaterThan(0));
            tween.Complete();
            Assert.That(content.anchoredPosition.y, Is.GreaterThan(midway));
            Assert.That(((RectTransform)row.transform).anchoredPosition, Is.EqualTo(rootPosition));
            Assert.That(row.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, row.transform);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1f));
        }

        [Test]
        public void RewardBurstSpreadsSixIconsAndFliesToTheRewardRow()
        {
            var effect = m_Root.GetComponentInChildren<RewardOutcomeView>();
            var slice = m_Root.GetComponentsInChildren<WheelSliceView>()
                .First(item => item.Slice is Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice);
            var source = slice.IconRect.GetComponent<Image>();
            var burst = effect.Highlight(slice);
            burst.Complete();
            var particles = m_Root.GetComponentsInChildren<Image>()
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).ToArray();
            var main = m_Root.GetComponentsInChildren<Image>()
                .Single(item => item.name == "ui_image_reward_flight_value");
            Assert.That(particles.Length, Is.EqualTo(6));
            Assert.That(source.enabled, Is.False);
            Assert.That(main.rectTransform.rect.width * main.transform.lossyScale.x /
                (slice.IconRect.rect.width * slice.IconRect.lossyScale.x), Is.EqualTo(1.2f).Within(.001f));
            foreach (var particle in particles)
            {
                Assert.That(particle.sprite, Is.EqualTo(slice.Icon));
                Assert.That(particle.raycastTarget || particle.maskable, Is.False);
                Assert.That(particle.transform.lossyScale.x / slice.IconRect.lossyScale.x, Is.EqualTo(.28f).Within(.001f));
                Assert.That(Vector3.Distance(particle.transform.position, main.transform.position), Is.GreaterThan(1f));
            }
            var flight = effect.Play(slice);
            flight.Complete();
            var reward = ((Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice)slice.Slice).Reward;
            var target = m_Root.GetComponentsInChildren<RewardListView>()
                .Select(list => list.GetComponentInChildren<RewardRowView>())
                .First(row => row != null && row.name.Contains(reward.RewardId)).IconRect;
            Assert.That(Vector3.Distance(main.transform.position, target.position), Is.LessThan(.01f));
            foreach (var particle in particles)
                Assert.That(Vector3.Distance(particle.transform.position, target.position), Is.LessThan(.01f));
            effect.ResetVisuals();
            Assert.That(source.enabled, Is.True);
            Assert.That(particles.All(item => !item.gameObject.activeSelf), Is.True);
        }

        [Test]
        public void RewardFlightsArriveInOrderAndCancellationRestoresTheLandingIcon()
        {
            var effect = m_Root.GetComponentInChildren<RewardOutcomeView>();
            var slice = m_Root.GetComponentsInChildren<WheelSliceView>()
                .First(item => item.Slice is Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice);
            SetDuration(effect, "flightDuration", 0.45f);
            effect.Highlight(slice).Complete();
            var particles = m_Root.GetComponentsInChildren<Image>()
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).OrderBy(item => item.name).ToArray();
            var flight = effect.Play(slice);
            var reward = ((Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice)slice.Slice).Reward;
            var row = m_Root.GetComponentsInChildren<RewardRowView>()
                .Single(item => item.name.Contains(reward.RewardId));
            var restScale = row.IconRect.localScale;
            var amount = row.GetComponentsInChildren<TMP_Text>().Single(item => item.name.Contains("amount"));
            Assert.That(amount.text, Is.EqualTo("0"));
            Assert.That(row.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            flight.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(0.44f, 0.44f);
            Assert.That(amount.text, Is.EqualTo("0"));
            Assert.That(particles.All(item => item.gameObject.activeSelf), Is.True);
            DOTween.ManualUpdate(0.04f, 0.04f);
            Assert.That(particles[0].gameObject.activeSelf, Is.False);
            Assert.That(particles.Skip(1).All(item => item.gameObject.activeSelf), Is.True);
            Assert.That(long.Parse(amount.text), Is.InRange(1L, reward.Amount - 1));
            DOTween.ManualUpdate(0.22f, 0.22f);
            Assert.That(amount.text, Is.EqualTo(reward.Amount.ToString("N0")));
            effect.ResetVisuals();
            Assert.That(row.IconRect.localScale, Is.EqualTo(restScale));
            Assert.That(particles.All(item => !item.gameObject.activeSelf), Is.True);
            Assert.That(slice.IconRect.GetComponent<Image>().enabled, Is.True);
            Assert.That(flight.IsActive(), Is.False);
        }

        [Test]
        public void RewardFlightPullsBackThenCurvesToTheTarget()
        {
            var icon = new GameObject("Flight test", typeof(RectTransform)).GetComponent<RectTransform>();
            var target = new GameObject("Target test", typeof(RectTransform)).GetComponent<RectTransform>();
            icon.SetParent(m_Root.transform, false);
            target.SetParent(m_Root.transform, false);
            target.localPosition = new Vector3(100, 0, 0);
            var tween = RewardIconFlight.Create(icon, target, 1f, 60f);
            tween.Goto(0.3f);
            Assert.That(icon.localPosition.x, Is.LessThan(0f));
            tween.Goto(0.85f);
            Assert.That(icon.localPosition.y, Is.GreaterThan(5f));
            tween.Complete();
            Assert.That(Vector3.Distance(icon.position, target.position), Is.LessThan(.001f));
        }

        [Test]
        public void CancellingRewardBurstRestoresSourceAndReusesParticleImages()
        {
            var result = m_Session.RequestSpin().Result;
            var callbacks = 0;
            m_View.Wheel.Highlight(result, status =>
            {
                Assert.That(status.Status, Is.EqualTo(VisualStatus.Cancelled));
                callbacks++;
            });
            var particles = m_Root.GetComponentsInChildren<Image>(true)
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).ToArray();
            m_View.Wheel.Cancel();
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(particles.All(item => !item.gameObject.activeSelf), Is.True);
            m_View.Wheel.Highlight(result, status => callbacks++);
            CollectionAssert.AreEquivalent(particles, m_Root.GetComponentsInChildren<Image>(true)
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")));
            m_View.Wheel.Cancel();
            Assert.That(callbacks, Is.EqualTo(2));
            Assert.That(m_Root.GetComponentsInChildren<WheelSliceView>().All(item => item.IconRect.GetComponent<Image>().enabled), Is.True);
        }

        [Test]
        public void DisablingSafeZoneDuringAnimationRestoresTheTargetProgress()
        {
            var safe = m_Root.GetComponentInChildren<SafeZoneView>();
            safe.Show(ZoneRules.GetSafeZoneProgress(1));
            safe.Show(ZoneRules.GetSafeZoneProgress(2));
            safe.gameObject.SetActive(false);
            safe.gameObject.SetActive(true);
            safe.Show(ZoneRules.GetSafeZoneProgress(2));
            var track = m_Root.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "ui_group_safe_zone_bar_track");
            var fill = m_Root.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "ui_image_safe_zone_bar_track_fill_value");
            Assert.That(fill.rect.height / track.rect.height, Is.EqualTo(.25f).Within(.001f));
        }

        [Test]
        public void SafeAreaKeepsTheSourceLayoutPadding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
            var authored = prefab.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == "ui_group_safe_area");
            var shown = m_Root.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == "ui_group_safe_area");
            Assert.That(shown.offsetMin, Is.EqualTo(authored.offsetMin));
            Assert.That(shown.offsetMax, Is.EqualTo(authored.offsetMax));
            Assert.That(authored.anchorMin, Is.Not.EqualTo(Vector2.zero));
            var area = Screen.safeArea;
            var minimum = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            var extent = new Vector2(area.width / Screen.width, area.height / Screen.height);
            Assert.That(shown.anchorMin, Is.EqualTo(minimum + Vector2.Scale(authored.anchorMin, extent)));
            Assert.That(shown.anchorMax, Is.EqualTo(minimum + Vector2.Scale(authored.anchorMax, extent)));
        }

        [UnityTest]
        public IEnumerator SafeCashOutShowsAllWonRewardTypesAndRestartKeepsProgress()
        {
            Assert.That(Button("cashout").interactable, Is.False);
            for (var index = 0; index < 4; index++)
            {
                m_Random.Index = index == 3 ? 4 : index;
                yield return SpinAndWait();
            }
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(5));
            Assert.That(Text("safety"), Does.Contain("SILVER"));
            Assert.That(Button("cashout").interactable, Is.True);
            var loot = m_Session.GetSnapshot().RunLoot;
            Button("cashout").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
            Assert.That(Text("result_title"), Does.Contain("REWARDS COLLECTED"));
            foreach (var pair in loot.Amounts)
            {
                Assert.That(m_Session.GetSnapshot().Progress.GetAmount(pair.Key), Is.EqualTo(pair.Value));
                Assert.That(m_Root.GetComponentsInChildren<RewardRowView>().Length, Is.GreaterThanOrEqualTo(loot.Amounts.Count));
            }
            Button("restart").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(m_Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(4));
            Assert.That(m_Session.GetSnapshot().RunLoot.Amounts.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BombAnimationEndsInLossAndRestartCreatesANewRun()
        {
            yield return SpinAndWait();
            var runId = m_Session.GetSnapshot().RunId;
            m_Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(m_Root.GetComponentsInChildren<Transform>().Any(item => item.name == "ui_group_bomb_value"), Is.True);
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(m_Session.GetSnapshot().RunLoot.Amounts.Count, Is.Zero);
            var flash = m_Root.GetComponentsInChildren<CanvasGroup>(true).Single(group => group.name == "ui_image_bomb_flash_value");
            Assert.That(flash.alpha, Is.Zero);
            Button("restart").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().RunId, Is.Not.EqualTo(runId));
            Assert.That(Button("spin").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator EveryWinningIndexStopsUnderThePointerAndCompletesOnce()
        {
            for (var index = 0; index < 8; index++)
            {
                if (m_Session.GetSnapshot().Status == SessionStatus.Lost)
                    m_Session.StartNewRun();
                m_Random.Index = index;
                var spin = m_Session.RequestSpin().Result;
                var callbacks = 0;
                VisualResult result = null;
                m_View.Wheel.Bind(spin.Round);
                m_View.Wheel.Spin(spin, completed => { result = completed; callbacks++; });
                var deadline = Time.realtimeSinceStartup + 3;
                while (callbacks == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(result.Status, Is.EqualTo(VisualStatus.Completed));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(m_View.Wheel.Rotation, index * 45)), Is.LessThan(0.1f));
                yield return null;
                Assert.That(callbacks, Is.EqualTo(1), "DOTween auto-kill must not complete twice.");
                m_Session.CompleteResolution(spin.OperationId);
            }
        }

        [UnityTest]
        public IEnumerator DetachDuringRealAnimationCompletesOnceAndReattachDoesNotDuplicateListeners()
        {
            Button("spin").onClick.Invoke();
            m_Presenter.Detach();
            m_Root.SetActive(false);
            yield return null;
            m_Root.SetActive(true);
            m_Presenter.Attach();
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            yield return SpinAndWait();
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            Assert.That(m_Session.GetSnapshot().RunLoot.GetAmount("Gold"), Is.EqualTo(22));
        }

        [UnityTest]
        public IEnumerator GoldTierUsesRealVisualsAndAwardsSpecialReward()
        {
            for (var zone = 1; zone < 30; zone++)
            {
                var result = m_Session.RequestSpin().Result;
                m_Session.CompleteResolution(result.OperationId);
            }
            m_Presenter.Detach();
            m_Presenter.Attach();
            Assert.That(Text("safety"), Does.Contain("GOLDEN"));
            var safeIcons = m_Root.GetComponentsInChildren<Image>()
                .Where(icon => icon.name.StartsWith("ui_image_zone_bar_") && icon.name.EndsWith("_safe_zone_icon_value"));
            Assert.That(safeIcons.Count(icon => icon.enabled), Is.EqualTo(3));
            Assert.That(Button("cashout").interactable, Is.True);
            m_Random.Index = 3;
            yield return SpinAndWait();
            Assert.That(m_Session.GetSnapshot().RunLoot.GetAmount("Super_chests"), Is.EqualTo(30));
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(31));
            Assert.That(Button("cashout").interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator ReviveButtonRestoresLootAndGiveUpStartsFreshRun()
        {
            m_Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            m_Session = new GameSession(configuration.CreateRuntime(), m_Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward("Gold", 250) })), new Reward("Gold", 25));
            m_Presenter = new GameScreenPresenter(m_Session, m_View,
                new SpinPresentationSequence(m_View.Wheel, TimeSpan.FromSeconds(5)));
            m_Presenter.Attach();
            yield return SpinAndWait();
            var loot = m_Session.GetSnapshot().RunLoot;
            var runId = m_Session.GetSnapshot().RunId;
            m_Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.AwaitingRevive));
            Assert.That(Button("bomb_revive").interactable, Is.True);
            Button("bomb_revive").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().RunLoot, Is.SameAs(loot));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("Gold"), Is.EqualTo(225));
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            Assert.That(m_Session.GetSnapshot().RunId, Is.EqualTo(runId));
            Assert.That(Text("wallet_amount"), Is.EqualTo("225"));
            yield return SpinAndWait();
            Button("bomb_give_up").onClick.Invoke();
            Assert.That(m_Session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(m_Session.GetSnapshot().RunId, Is.Not.EqualTo(runId));
            Assert.That(m_Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount("Gold"), Is.EqualTo(225));
        }

        [UnityTest]
        public IEnumerator CashCurrencyDrivesWalletIconReviveLabelAndPayment()
        {
            m_Presenter.Detach();
            var configuration = CreateFixedConfiguration();
            var currency = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Cash.asset");
            m_View.Initialize(configuration, currency);
            m_Session = new GameSession(configuration.CreateRuntime(), m_Random,
                new InMemoryProgressStore(new RewardCollection(new[] { new Reward(currency.Id, 100) })),
                new Reward(currency.Id, 25));
            m_Presenter = new GameScreenPresenter(m_Session, m_View,
                new SpinPresentationSequence(m_View.Wheel, TimeSpan.FromSeconds(5)));
            m_Presenter.Attach();
            Assert.That(Text("wallet_amount"), Is.EqualTo("100"));
            var icon = m_Root.GetComponentsInChildren<Image>(true).Single(item => item.name == "ui_image_wallet_icon_value");
            Assert.That(icon.sprite, Is.EqualTo(currency.Icon));
            Assert.That(icon.rectTransform.rect.width / icon.rectTransform.rect.height,
                Is.EqualTo(currency.Icon.rect.width / currency.Icon.rect.height).Within(.001f));
            m_Random.Index = 7;
            yield return SpinAndWait();
            Assert.That(Text("bomb_revive_label"), Is.EqualTo("REVIVE 25 " + currency.DisplayName));
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Text("wallet_amount"), Is.EqualTo("75"));
            Assert.That(m_Session.GetSnapshot().Progress.GetAmount(currency.Id), Is.EqualTo(75));
            Assert.That(m_Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
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
            m_Presenter.Detach();
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
                m_View.Initialize(configuration, original.Reward);
                m_Session = new GameSession(configuration.CreateRuntime(), m_Random, new InMemoryProgressStore());
                m_Presenter = new GameScreenPresenter(m_Session, m_View,
                    new SpinPresentationSequence(m_View.Wheel, TimeSpan.FromSeconds(5)));
                m_Presenter.Attach();
                var views = m_View.Wheel.GetComponentsInChildren<WheelSliceView>();
                Assert.That(views[0].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x10"));
                Assert.That(views[1].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x20"));
                Assert.That(views[2].GetComponentInChildren<TMP_Text>().text, Is.EqualTo("x10"));
                Assert.That(views[1].Icon, Is.EqualTo(views[0].Icon));
                foreach (var index in new[] { 1, 2, 0, 0 })
                {
                    m_Random.Index = index;
                    yield return SpinAndWait();
                }
                Assert.That(m_Session.GetSnapshot().RunLoot.GetAmount("Gold"), Is.EqualTo(62));
                Button("cashout").onClick.Invoke();
                Assert.That(m_Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(1));
                Assert.That(Text("wallet_amount"), Is.EqualTo("62"));
                Assert.That(source.GetTier(WheelTier.Bronze).GetSlice(1).Id, Is.EqualTo("Cash"));
            }
            finally
            {
                m_Presenter.Detach();
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(bronze);
                Object.DestroyImmediate(larger);
            }
        }

        [UnityTest]
        public IEnumerator FiftyEntryPoolBindsSelectedVisualsAndPaysTheSelectedSlot()
        {
            m_Presenter.Detach();
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
                m_Session = new GameSession(configuration.CreateRuntime(), random, new InMemoryProgressStore());
                m_View.Initialize(configuration, small.Reward);
                m_Presenter = new GameScreenPresenter(m_Session, m_View,
                    new SpinPresentationSequence(m_View.Wheel, TimeSpan.FromSeconds(5)));
                m_Presenter.Attach();
                for (var zone = 1; zone < 5; zone++)
                    yield return SpinAndWait();
                var round = m_Session.GetSnapshot().Round;
                Assert.That(round.SourceIndices, Is.EqualTo(Enumerable.Range(40, 8)));
                var views = m_View.Wheel.GetComponentsInChildren<WheelSliceView>();
                for (var index = 0; index < views.Length; index++)
                {
                    Assert.That(views[index].Icon, Is.EqualTo(index % 2 == 0 ? cash.Icon : large.Icon));
                    Assert.That(views[index].GetComponentInChildren<TMP_Text>().text, Is.EqualTo(index % 2 == 0 ? "x180" : "x28"));
                }
                yield return SpinAndWait();
                Assert.That(m_Session.GetSnapshot().RunLoot.GetAmount("Cash"), Is.EqualTo(180));
                Assert.That(source.GetTier(WheelTier.Silver).CandidateCount, Is.EqualTo(8));
            }
            finally
            {
                m_Presenter.Detach();
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(silver);
            }
        }

        [Test]
        public void ZoneIndicatorReachesItsTargetAfterDisableAndSameStateRender()
        {
            var zones = m_Root.GetComponentInChildren<ZoneProgressView>();
            var data = new SerializedObject(zones);
            var indicator = (RectTransform)data.FindProperty("indicator").objectReferenceValue;
            var viewport = (RectTransform)data.FindProperty("viewport").objectReferenceValue;
            var visible = data.FindProperty("cells").arraySize - 2;
            zones.Show(1);
            var target = indicator.anchoredPosition.x + (viewport.rect.width - 70) / (visible - 1);
            zones.Show(2);
            zones.gameObject.SetActive(false);
            zones.gameObject.SetActive(true);
            zones.Show(2);
            Assert.That(indicator.anchoredPosition.x, Is.EqualTo(target).Within(.001f));
        }

        [UnityTest]
        public IEnumerator ResultPanelsRestoreFullScaleAfterDisableDuringOpening()
        {
            m_Random.Index = 7;
            yield return SpinAndWait();
            var panels = m_Root.GetComponentInChildren<ResultPanelsView>(true);
            var data = new SerializedObject(panels);
            var bombMotion = (RectTransform)data.FindProperty("bombMotion").objectReferenceValue;
            panels.enabled = false;
            panels.enabled = true;
            m_View.Render(new GameScreenState(m_Session.GetSnapshot(), RewardCollection.Empty, null));
            Assert.That(bombMotion.localScale, Is.EqualTo(Vector3.one));
            Button("restart").onClick.Invoke();
            m_Random.Index = 0;
            for (var zone = 1; zone < 5; zone++)
                yield return SpinAndWait();
            Button("cashout").onClick.Invoke();
            var cashOutMotion = (RectTransform)data.FindProperty("cashOutMotion").objectReferenceValue;
            panels.enabled = false;
            panels.enabled = true;
            m_View.Render(new GameScreenState(m_Session.GetSnapshot(), RewardCollection.Empty, null));
            Assert.That(cashOutMotion.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void TierTitleColorAndStatusUseAuthoredPresentationData()
        {
            var configuration = CreateFixedConfiguration();
            var tier = configuration.GetTier(WheelTier.Bronze);
            var data = new SerializedObject(tier);
            data.FindProperty("title").stringValue = "CUSTOM BRONZE";
            data.FindProperty("titleColor").colorValue = Color.magenta;
            data.FindProperty("statusMessage").stringValue = "CUSTOM STATUS";
            data.ApplyModifiedPropertiesWithoutUndo();
            m_View.Initialize(configuration, AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset"));
            m_View.Render(new GameScreenState(m_Session.GetSnapshot(), RewardCollection.Empty, null));
            Assert.That(Text("safety"), Is.EqualTo("CUSTOM BRONZE"));
            var title = m_Root.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name == "ui_text_wheel_title_value");
            Assert.That(title.color, Is.EqualTo(Color.magenta));
            var status = (TMP_Text)new SerializedObject(m_View).FindProperty("status").objectReferenceValue;
            Assert.That(status.text, Is.EqualTo("CUSTOM STATUS"));
        }

        private WheelConfigurationAsset CreateFixedConfiguration()
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

        private IEnumerator SpinAndWait()
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
            Assert.That(Text("error"), Is.Empty);
        }

        private Button Button(string name) => m_Root.GetComponentsInChildren<Button>(true)
            .Single(button => button.name == "ui_button_" + (name == "spin" ? "wheel_spin" : name == "cashout" ? "exit" : name == "restart" ? (m_Session.GetSnapshot().Status == SessionStatus.CashedOut ? "cash_out_new_run" : "bomb_give_up") : name) + "_value");

        private string Text(string name) => m_Root.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.name == "ui_text_" + (name == "safety" ? "wheel_title" : name == "result_title" ? "cash_out_title" : name) + "_value").text;

        private static void SetDuration(Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

