#if UNITY_EDITOR
using DG.Tweening;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class RewardAnimationTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator RewardRowCountsOnlyNewArrivalsAndSettlesWhenDisabled()
        {
            var reward = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset");
            var list = Root.GetComponentsInChildren<RewardListView>().First();
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
            var row = Root.GetComponentsInChildren<RewardListView>().First().Reserve(reward.Id);
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
            var list = Root.GetComponentsInChildren<RewardListView>().First();
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
            var effect = Root.GetComponentInChildren<RewardOutcomeView>();
            var slice = Root.GetComponentsInChildren<WheelSliceView>()
                .First(item => item.Slice is Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice);
            var source = slice.IconRect.GetComponent<Image>();
            var burst = effect.Highlight(slice);
            burst.Complete();
            var particles = Root.GetComponentsInChildren<Image>()
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).ToArray();
            var main = Root.GetComponentsInChildren<Image>()
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
            var target = Root.GetComponentsInChildren<RewardListView>()
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
            var effect = Root.GetComponentInChildren<RewardOutcomeView>();
            var slice = Root.GetComponentsInChildren<WheelSliceView>()
                .First(item => item.Slice is Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice);
            SetDuration(effect, "flightDuration", 0.45f);
            effect.Highlight(slice).Complete();
            var particles = Root.GetComponentsInChildren<Image>()
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).OrderBy(item => item.name).ToArray();
            var flight = effect.Play(slice);
            var reward = ((Vertigo.Runtime._Game.Runtime.Rounds.RewardSlice)slice.Slice).Reward;
            var row = Root.GetComponentsInChildren<RewardRowView>()
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
            icon.SetParent(Root.transform, false);
            target.SetParent(Root.transform, false);
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
            var result = Session.RequestSpin().Result;
            var callbacks = 0;
            View.Wheel.Highlight(result, status =>
            {
                Assert.That(status.Status, Is.EqualTo(VisualStatus.Cancelled));
                callbacks++;
            });
            var particles = Root.GetComponentsInChildren<Image>(true)
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")).ToArray();
            View.Wheel.Cancel();
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(particles.All(item => !item.gameObject.activeSelf), Is.True);
            View.Wheel.Highlight(result, status => callbacks++);
            CollectionAssert.AreEquivalent(particles, Root.GetComponentsInChildren<Image>(true)
                .Where(item => item.name.StartsWith("ui_image_reward_burst_")));
            View.Wheel.Cancel();
            Assert.That(callbacks, Is.EqualTo(2));
            Assert.That(Root.GetComponentsInChildren<WheelSliceView>().All(item => item.IconRect.GetComponent<Image>().enabled), Is.True);
        }
    }
}
#endif
