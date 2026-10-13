#if UNITY_EDITOR
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class CashOutButtonFeedbackTests : GameViewTestFixture
    {
        [Test]
        public void SafeZoneEmphasizesExitAndSpinImmediatelyResetsIt()
        {
            var button = Button("cashout");
            var feedback = button.GetComponent<CashOutButtonFeedback>();
            var background = (Image)button.targetGraphic;
            var inactiveSprite = background.sprite;
            EnterSafeZone();
            Assert.That(button.interactable, Is.True);
            Assert.That(background.sprite, Is.Not.SameAs(inactiveSprite));
            var entrance = GetTween(feedback, "m_Entrance");
            entrance.Goto(0.18f);
            Assert.That(background.transform.localScale.x, Is.GreaterThan(1));
            Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
            feedback.SetAvailable(true);
            Assert.That(GetTween(feedback, "m_Entrance"), Is.SameAs(entrance));

            Button("spin").onClick.Invoke();
            Assert.That(button.interactable, Is.False);
            Assert.That(background.sprite, Is.SameAs(inactiveSprite));
            Assert.That(background.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(entrance.IsActive(), Is.False);
        }

        [Test]
        public void DisablingStopsBreathingAndReenablingRestartsAvailableFeedback()
        {
            EnterSafeZone();
            var button = Button("cashout");
            var feedback = button.GetComponent<CashOutButtonFeedback>();
            GetTween(feedback, "m_Entrance").Complete(true);
            var breathing = GetTween(feedback, "m_Breathing");
            Assert.That(breathing.IsActive(), Is.True);
            button.gameObject.SetActive(false);
            Assert.That(breathing.IsActive(), Is.False);
            Assert.That(button.targetGraphic.transform.localScale, Is.EqualTo(Vector3.one));
            button.gameObject.SetActive(true);
            Assert.That(GetTween(feedback, "m_Entrance").IsActive(), Is.True);
        }

        private void EnterSafeZone()
        {
            Presenter.Detach();
            for (var zone = 1; zone < 5; zone++)
            {
                var spin = Session.RequestSpin().Result;
                Session.CompleteResolution(spin.OperationId);
            }
            Presenter.Attach();
        }

        private static Tween GetTween(CashOutButtonFeedback feedback, string field)
        {
            return (Tween)typeof(CashOutButtonFeedback).GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feedback);
        }
    }
}
#endif
