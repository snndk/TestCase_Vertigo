#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class ZoneProgressAnimationTests : GameViewTestFixture
    {
        [TestCase(2, false)]
        [TestCase(5, true)]
        [TestCase(30, true)]
        public void ArrivalPulsesAfterSlidingAndOnlySafeCellsIlluminate(int zone, bool illuminates)
        {
            var bar = Root.GetComponentInChildren<ZoneProgressView>();
            bar.Show(zone - 1);
            bar.Show(zone);
            var sequence = GetTransition(bar);
            sequence.SetUpdate(UpdateType.Manual);
            var cell = FindCell(bar, zone);
            var feedback = cell.GetComponent<ZoneCellFeedback>();
            var data = new SerializedObject(feedback);
            var motion = (RectTransform)data.FindProperty("motion").objectReferenceValue;
            var glow = (Image)data.FindProperty("glow").objectReferenceValue;
            var startPosition = cell.Rect.anchoredPosition;
            DOTween.ManualUpdate(.175f, .175f);
            var halfwayPosition = cell.Rect.anchoredPosition;
            Assert.That(motion.localScale, Is.EqualTo(Vector3.one));
            Assert.That(glow.gameObject.activeSelf, Is.False);
            if (zone > 5)
                Assert.That(halfwayPosition.x, Is.LessThan(startPosition.x));
            DOTween.ManualUpdate(.225f, .225f);
            Assert.That(motion.localScale.x, Is.GreaterThan(1f));
            Assert.That(glow.gameObject.activeSelf, Is.EqualTo(illuminates));
            if (illuminates)
                Assert.That(glow.color.a, Is.GreaterThan(0f));
            var settledPosition = cell.Rect.anchoredPosition;
            sequence.Complete(true);
            Assert.That(cell.Rect.anchoredPosition, Is.EqualTo(settledPosition));
            Assert.That(motion.localScale, Is.EqualTo(Vector3.one));
            Assert.That(glow.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SameZoneDoesNotRestartAndDisablingSettlesTheIndicatorAndCell()
        {
            var bar = Root.GetComponentInChildren<ZoneProgressView>();
            bar.Show(4);
            bar.Show(5);
            var sequence = GetTransition(bar);
            sequence.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(.4f, .4f);
            bar.Show(5);
            Assert.That(GetTransition(bar), Is.SameAs(sequence));
            var cell = FindCell(bar, 5);
            var feedbackData = new SerializedObject(cell.GetComponent<ZoneCellFeedback>());
            var motion = (RectTransform)feedbackData.FindProperty("motion").objectReferenceValue;
            var glow = (Image)feedbackData.FindProperty("glow").objectReferenceValue;
            bar.gameObject.SetActive(false);
            bar.gameObject.SetActive(true);
            bar.Show(5);
            Assert.That(sequence.IsActive(), Is.False);
            Assert.That(motion.localScale, Is.EqualTo(Vector3.one));
            Assert.That(glow.gameObject.activeSelf, Is.False);
            var indicator = (RectTransform)new SerializedObject(bar).FindProperty("indicator").objectReferenceValue;
            Assert.That(indicator.anchoredPosition, Is.EqualTo(cell.Rect.anchoredPosition));
        }

        [Test]
        public void NewZoneCancelsPreviousArrivalWithoutLeavingAHighlight()
        {
            var bar = Root.GetComponentInChildren<ZoneProgressView>();
            bar.Show(4);
            bar.Show(5);
            var sequence = GetTransition(bar);
            sequence.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(.4f, .4f);
            bar.Show(6);
            Assert.That(sequence.IsActive(), Is.False);
            GetTransition(bar).Complete(true);
            foreach (var cell in bar.GetComponentsInChildren<ZoneCellFeedback>())
            {
                var data = new SerializedObject(cell);
                Assert.That(((RectTransform)data.FindProperty("motion").objectReferenceValue).localScale, Is.EqualTo(Vector3.one));
                Assert.That(((Image)data.FindProperty("glow").objectReferenceValue).gameObject.activeSelf, Is.False);
            }
            Assert.That(FindCell(bar, 5).GetComponent<CanvasGroup>().alpha, Is.LessThan(1f));
        }

        private static ZoneCellView FindCell(ZoneProgressView bar, int zone)
        {
            return bar.GetComponentsInChildren<ZoneCellView>()
                .Single(cell => cell.GetComponentInChildren<TMP_Text>().text == zone.ToString());
        }

        private static Sequence GetTransition(ZoneProgressView bar)
        {
            return (Sequence)typeof(ZoneProgressView).GetField("m_Transition",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bar);
        }
    }
}
#endif
