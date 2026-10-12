#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class AnimationAccelerationTests : GameViewTestFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void FinishingAccelerationRemovesOnlyItsOwnUpdateCallbackBeforeStartingAnotherOperation(bool complete)
        {
            var operation = new TweenOperation();
            var value = 0f;
            TweenCallback originalUpdate = () => { };
            var tween = DOTween.To(() => value, next => value = next, 1f, 4f)
                .SetAutoKill(false).OnUpdate(originalUpdate).Pause();
            var nextTween = DOTween.Sequence().AppendInterval(1f).Pause();
            var callbacks = 0;
            try
            {
                operation.Play(tween, result =>
                {
                    callbacks++;
                    Assert.That(result.Status, Is.EqualTo(complete ? VisualStatus.Completed : VisualStatus.Cancelled));
                    Assert.That(tween.onUpdate, Is.EqualTo(originalUpdate));
                    Assert.That(operation.IsPlaying, Is.False);
                    operation.Play(nextTween, next => { });
                });
                operation.Accelerate(16f, 1.8f);
                operation.Accelerate(16f, 1.8f);
                Assert.That(tween.onUpdate.GetInvocationList().Length, Is.EqualTo(2));
                if (complete)
                    tween.Complete(true);
                else
                    operation.Cancel();
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(operation.IsPlaying, Is.True);
                Assert.That(nextTween.timeScale, Is.EqualTo(1f));
                Assert.That(nextTween.onUpdate, Is.Null);
            }
            finally
            {
                operation.Cancel();
                tween.Kill(false);
                nextTween.Kill(false);
            }
        }

        [Test]
        public void MissingCompletionCallbackDoesNotLeaveAnOperationRunning()
        {
            var operation = new TweenOperation();
            var tween = DOTween.Sequence().AppendInterval(1f).Pause();
            try
            {
                Assert.Throws<System.ArgumentNullException>(() => operation.Play(tween, null));
                Assert.That(operation.IsPlaying, Is.False);
                operation.Play(tween, result => { });
                Assert.That(operation.IsPlaying, Is.True);
            }
            finally
            {
                operation.Cancel();
                tween.Kill(false);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SpinTapSpeedsUpTravelButPreservesBrakingAndWinningIndex(int index)
        {
            SetDuration(View.Wheel, "spinDuration", 4f);
            Random.Index = index;
            var result = Session.RequestSpin().Result;
            var callbacks = 0;
            View.Wheel.Spin(result, status =>
            {
                Assert.That(status.Status, Is.EqualTo(VisualStatus.Completed));
                callbacks++;
            });
            var tween = CurrentTween();
            tween.SetUpdate(UpdateType.Manual);
            Tap();
            Tap();
            Assert.That(tween.timeScale, Is.EqualTo(16f));
            for (var step = 0; step < 30 && tween.timeScale > 1f; step++)
                DOTween.ManualUpdate(.01f, .01f);

            Assert.That(tween.timeScale, Is.EqualTo(1f));
            Assert.That(tween.Duration() - tween.Elapsed(), Is.GreaterThan(1f));
            Assert.That(callbacks, Is.Zero);
            Tap();
            Assert.That(tween.timeScale, Is.EqualTo(1f));
            tween.Complete(true);
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(View.Wheel.Rotation, index * 45)), Is.LessThan(.1f));
            Assert.That(View.Wheel.CanAccelerate, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RewardTapKeepsFlightAndCountingTogetherAcrossStages(bool tapDuringHighlight)
        {
            var effect = Root.GetComponentInChildren<RewardOutcomeView>();
            SetDuration(effect, "flightDuration", .45f);
            var result = Session.RequestSpin().Result;
            var callbacks = 0;
            View.Wheel.Highlight(result, status => View.Wheel.PlayOutcome(result, outcome => callbacks++));
            if (tapDuringHighlight)
                Tap();
            CurrentTween().Complete(true);
            var flight = CurrentTween();
            Assert.That(flight.timeScale, Is.EqualTo(tapDuringHighlight ? 4f : 1f));
            if (!tapDuringHighlight)
                Tap();
            Tap();
            Assert.That(flight.timeScale, Is.EqualTo(4f));

            var row = Root.GetComponentInChildren<RewardRowView>();
            var amount = row.GetComponentsInChildren<TMP_Text>().Single(text => text.name.Contains("amount"));
            var reward = ((RewardSlice)result.WinningSlice).Reward;
            flight.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(.11f, .11f);
            Assert.That(amount.text, Is.EqualTo("0"));
            DOTween.ManualUpdate(.0275f, .0275f);
            Assert.That(long.Parse(amount.text), Is.InRange(1L, reward.Amount - 1));
            var feedback = row.GetComponent<RewardRowFeedback>();
            Assert.That(Field<Tween>(feedback, "m_GainTween").timeScale, Is.EqualTo(4f));
            Assert.That(Field<Tween>(feedback, "m_IconPulseTween").timeScale, Is.EqualTo(4f));
            flight.Complete(true);
            Assert.That(amount.text, Is.EqualTo(reward.Amount.ToString("N0")));
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(View.Wheel.CanAccelerate, Is.False);
            Assert.That(Root.GetComponentsInChildren<WheelSliceView>()
                .All(slice => slice.IconRect.GetComponent<Image>().enabled), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CancellationClearsAccelerationAndCompletesOnlyOnce(bool duringSpin)
        {
            SetDuration(View.Wheel, "spinDuration", 4f);
            var result = Session.RequestSpin().Result;
            var callbacks = 0;
            System.Action<VisualResult> cancelled = status =>
            {
                Assert.That(status.Status, Is.EqualTo(VisualStatus.Cancelled));
                callbacks++;
            };
            if (duringSpin)
                View.Wheel.Spin(result, cancelled);
            else
                View.Wheel.Highlight(result, cancelled);
            Tap();
            View.Wheel.Cancel();
            View.Wheel.Cancel();
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(View.Wheel.CanAccelerate, Is.False);
            View.Wheel.Highlight(result, status => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(1f));
            View.Wheel.Cancel();
            View.Wheel.Spin(result, status => { });
            Assert.That(CurrentTween().timeScale, Is.EqualTo(1f));
            View.Wheel.Cancel();
        }

        [Test]
        public void FullScreenTapSurfaceOnlyInterceptsAcceleratableAnimations()
        {
            var tap = Root.GetComponentInChildren<AnimationTapView>();
            var rect = (RectTransform)tap.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            Tap();
            Assert.That(View.Wheel.CanAccelerate, Is.False);
            Random.Index = 7;
            var result = Session.RequestSpin().Result;
            Assert.That(result.WinningSlice, Is.TypeOf<BombSlice>());
            View.Wheel.Spin(result, status => { });
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            View.Wheel.Cancel();
            View.Wheel.Highlight(result, status => { });
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            Tap();
            Assert.That(CurrentTween().timeScale, Is.EqualTo(1f));
            View.Wheel.Cancel();
        }

        [UnityTest]
        public IEnumerator ScreenCornerTapsAccelerateARealRunWithoutChangingItsReward()
        {
            SetDuration(View.Wheel, "spinDuration", 4f);
            var events = new GameObject("Tap test events", typeof(EventSystem));
            events.transform.SetParent(Root.transform, false);
            var eventSystem = events.GetComponent<EventSystem>();
            var canvas = View.GetComponent<Canvas>();
            var raycaster = View.GetComponent<GraphicRaycaster>();
            var tap = Root.GetComponentInChildren<AnimationTapView>();
            yield return null;
            var point = new Vector2(canvas.pixelRect.xMin + 10f, canvas.pixelRect.yMax - 10f);
            var pointer = new PointerEventData(eventSystem) { position = point };
            var hits = new List<RaycastResult>();
            Button("spin").onClick.Invoke();
            raycaster.Raycast(pointer, hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(tap.gameObject));
            ExecuteEvents.Execute(hits.First().gameObject, pointer, ExecuteEvents.pointerDownHandler);
            var spin = CurrentTween();
            var started = Time.realtimeSinceStartup;
            while (ReferenceEquals(CurrentTween(), spin) && Time.realtimeSinceStartup - started < 3f)
                yield return null;
            Assert.That(ReferenceEquals(CurrentTween(), spin), Is.False);
            Assert.That(CurrentTween().timeScale, Is.EqualTo(1f), "Spin tap must not speed up reward playback.");
            Tap();
            while (View.Wheel.CanAccelerate && Time.realtimeSinceStartup - started < 3f)
                yield return null;
            Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(3f));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(2));
            Assert.That(Session.GetSnapshot().RunLoot.GetAmount("Gold"), Is.EqualTo(10));
            Assert.That(Button("spin").interactable, Is.True);
            hits.Clear();
            raycaster.Raycast(pointer, hits);
            Assert.That(hits.Any(hit => hit.gameObject == tap.gameObject), Is.False);
        }

        private void Tap()
        {
            var tap = Root.GetComponentInChildren<AnimationTapView>();
            ExecuteEvents.Execute(tap.gameObject, new PointerEventData(null), ExecuteEvents.pointerDownHandler);
        }

        private Tween CurrentTween() => Field<Tween>(Field<TweenOperation>(View.Wheel, "m_Operation"), "m_Tween");

        private static T Field<T>(object owner, string name)
        {
            return (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        }
    }
}
#endif
