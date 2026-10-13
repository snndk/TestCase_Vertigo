#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class GoldPolishTests : GameViewTestFixture
    {
        [UnityTest]
        public IEnumerator GoldHighlightCancellationRestoresWheelAndHidesCelebration()
        {
            EnterGoldZone();
            var result = Session.RequestSpin().Result;
            View.Wheel.Highlight(result, status => { });
            yield return null;
            var ring = Root.GetComponentInChildren<PulseRingGraphic>(true);
            Assert.That(ring.gameObject.activeSelf, Is.True);
            View.Wheel.Accelerate();
            View.Wheel.Cancel();
            Assert.That(ring.gameObject.activeSelf, Is.False);
            var contents = Root.GetComponentsInChildren<CanvasGroup>(true)
                .Single(group => group.name == "ui_group_wheel_rotor_value");
            Assert.That(contents.alpha, Is.EqualTo(1));
            Session.CompleteResolution(result.OperationId);
        }

        [UnityTest]
        public IEnumerator AcceleratedGoldRewardFinishesWithoutLeavingCelebrationVisible()
        {
            EnterGoldZone();
            Button("spin").onClick.Invoke();
            View.Wheel.Accelerate();
            var deadline = Time.realtimeSinceStartup + 5;
            while (Session.GetSnapshot().Status == SessionStatus.Resolving && Time.realtimeSinceStartup < deadline)
            {
                View.Wheel.Accelerate();
                Presenter.AdvancePresentation(System.TimeSpan.FromSeconds(Time.unscaledDeltaTime));
                yield return null;
            }
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(31));
            Assert.That(Text("error"), Is.Empty);
            Assert.That(Root.GetComponentInChildren<PulseRingGraphic>(true).gameObject.activeSelf, Is.False);
        }

        private void EnterGoldZone()
        {
            Presenter.Detach();
            for (var zone = 1; zone < 30; zone++)
            {
                var result = Session.RequestSpin().Result;
                Session.CompleteResolution(result.OperationId);
            }
            Presenter.Attach();
            View.Wheel.GetComponent<WheelTierTransition>().Finish();
        }
    }
}
#endif
