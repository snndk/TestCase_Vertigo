#if UNITY_EDITOR
using DG.Tweening;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class WheelAnimationTests : GameViewTestFixture
    {
        [TestCase(5, 4)]
        [TestCase(30, 8)]
        [TestCase(6, 0)]
        public void TierTransitionSwapsAtTheSmallestScaleAndRestoresTheWheel(int zone, int sparkleCount)
        {
            var configuration = CreateFixedConfiguration();
            var generator = new Vertigo.Runtime._Game.Runtime.Rounds.RoundGenerator(configuration.CreateRuntime(), Random);
            var wheel = View.Wheel;
            var transition = wheel.GetComponent<WheelTierTransition>();
            if (zone == 6)
            {
                wheel.Bind(generator.Generate(5));
                transition.Finish();
            }
            var motion = wheel.GetComponentsInChildren<RectTransform>()
                .Single(item => item.name == "ui_group_wheel_motion");
            var image = wheel.GetComponentsInChildren<Image>()
                .Single(item => item.name == "ui_image_wheel_base_value");
            var originalSprite = image.sprite;
            var originalScale = motion.localScale;
            var round = generator.Generate(zone);
            wheel.Bind(round);
            Assert.That(transition.IsPlaying, Is.True);
            var sequence = GetTierTransitionSequence(transition);
            wheel.Bind(round);
            Assert.That(GetTierTransitionSequence(transition), Is.SameAs(sequence));
            sequence.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(0.07f, 0.07f);
            Assert.That(image.sprite, Is.SameAs(originalSprite));
            Assert.That(motion.localScale.x, Is.LessThan(originalScale.x));
            DOTween.ManualUpdate(0.08f, 0.08f);
            Assert.That(image.sprite, Is.SameAs(configuration.GetTier(round.Tier).Wheel));
            Assert.That(transition.GetComponentsInChildren<Image>()
                .Count(item => item.name.StartsWith("ui_image_wheel_tier_sparkle_")), Is.EqualTo(sparkleCount));
            sequence.Complete(true);
            Assert.That(transition.IsPlaying, Is.False);
            Assert.That(motion.localScale, Is.EqualTo(originalScale));
        }

        [Test]
        public void TierTransitionSkipsSameTierAndSettlesLatestRoundWhenDisabled()
        {
            var configuration = CreateFixedConfiguration();
            var generator = new Vertigo.Runtime._Game.Runtime.Rounds.RoundGenerator(configuration.CreateRuntime(), Random);
            var wheel = View.Wheel;
            var transition = wheel.GetComponent<WheelTierTransition>();
            wheel.Bind(generator.Generate(2));
            Assert.That(transition.IsPlaying, Is.False);
            wheel.Bind(generator.Generate(5));
            wheel.Bind(generator.Generate(30));
            transition.enabled = false;
            var image = wheel.GetComponentsInChildren<Image>().Single(item => item.name == "ui_image_wheel_base_value");
            Assert.That(image.sprite, Is.SameAs(configuration.GetTier(WheelTier.Gold).Wheel));
            Assert.That(transition.IsPlaying, Is.False);
            Assert.That(wheel.GetComponentsInChildren<RectTransform>()
                .Single(item => item.name == "ui_group_wheel_motion").localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void StartingSpinDuringTierTransitionSettlesTheNewWheelFirst()
        {
            for (var index = 0; index < 4; index++)
            {
                var result = Session.RequestSpin().Result;
                Session.CompleteResolution(result.OperationId);
            }
            var round = Session.GetSnapshot().Round;
            View.Wheel.Bind(round);
            var transition = View.Wheel.GetComponent<WheelTierTransition>();
            Assert.That(transition.IsPlaying, Is.True);
            var spin = Session.RequestSpin().Result;
            View.Wheel.Spin(spin, result => { });
            Assert.That(transition.IsPlaying, Is.False);
            var slices = Root.GetComponentsInChildren<WheelSliceView>();
            for (var index = 0; index < slices.Length; index++)
                Assert.That(slices[index].Slice, Is.SameAs(round.Slices[index]));
            View.Wheel.Cancel();
        }

        [UnityTest]
        public IEnumerator EveryWinningIndexStopsUnderThePointerAndCompletesOnce()
        {
            for (var index = 0; index < 8; index++)
            {
                if (Session.GetSnapshot().Status == SessionStatus.Lost)
                    Session.StartNewRun();
                Random.Index = index;
                var spin = Session.RequestSpin().Result;
                var callbacks = 0;
                VisualResult result = null;
                View.Wheel.Bind(spin.Round);
                View.Wheel.Spin(spin, completed => { result = completed; callbacks++; });
                var deadline = Time.realtimeSinceStartup + 3;
                while (callbacks == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(result.Status, Is.EqualTo(VisualStatus.Completed));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(View.Wheel.Rotation, index * 45)), Is.LessThan(0.1f));
                yield return null;
                Assert.That(callbacks, Is.EqualTo(1), "DOTween auto-kill must not complete twice.");
                Session.CompleteResolution(spin.OperationId);
            }
        }

        private static Sequence GetTierTransitionSequence(WheelTierTransition transition)
        {
            return (Sequence)typeof(WheelTierTransition).GetField("m_Sequence",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(transition);
        }
    }
}
#endif
