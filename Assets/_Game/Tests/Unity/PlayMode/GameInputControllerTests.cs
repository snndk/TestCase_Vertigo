#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class GameInputControllerTests : GameViewTestFixture
    {
        [Test]
        public void DisableUnsubscribesAndReenableDoesNotDuplicateInput()
        {
            Presenter.Detach();
            var calls = 0;
            View.Input.SpinRequested += () => calls++;
            for (var index = 0; index < 3; index++)
            {
                View.Input.enabled = false;
                Button("spin").onClick.Invoke();
                Assert.That(calls, Is.EqualTo(index));
                View.Input.enabled = true;
                Button("spin").onClick.Invoke();
                Assert.That(calls, Is.EqualTo(index + 1));
            }
        }

        [Test]
        public void TapCaptureFollowsControllerLifetimeAndCurrentAnimation()
        {
            var tap = Root.GetComponentInChildren<AnimationTapView>();
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            Button("spin").onClick.Invoke();
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            View.Input.enabled = false;
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
            View.Input.enabled = true;
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            Presenter.Detach();
            Assert.That(tap.IsRaycastLocationValid(Vector2.zero, null), Is.False);
        }

        [Test]
        public void SeparateScreensDoNotShareInputOrSubscriptions()
        {
            Presenter.Detach();
            var originalCalls = 0;
            View.Input.SpinRequested += () => originalCalls++;
            var clone = Object.Instantiate(Root);
            try
            {
                var other = clone.GetComponentInChildren<GameScreenView>();
                other.Input.Apply(new GameScreenState(Session.GetSnapshot(), Session.GetSnapshot().RunLoot, null));
                var otherCalls = 0;
                other.Input.SpinRequested += () => otherCalls++;
                var buttons = other.GetComponentsInChildren<Button>(true);
                var otherSpin = System.Array.Find(buttons, button => button.name == Button("spin").name);
                otherSpin.onClick.Invoke();
                Assert.That(originalCalls, Is.Zero);
                Assert.That(otherCalls, Is.EqualTo(1));
                Button("spin").onClick.Invoke();
                Assert.That(originalCalls, Is.EqualTo(1));
                Assert.That(otherCalls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }
    }
}
#endif
