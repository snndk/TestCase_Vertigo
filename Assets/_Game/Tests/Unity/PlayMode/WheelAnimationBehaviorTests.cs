#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class WheelAnimationBehaviorTests : GameViewTestFixture
    {
        [TestCase(5, WheelTier.Silver, WheelTier.Gold, true)]
        [TestCase(30, WheelTier.Gold, WheelTier.Bronze, false)]
        public void AssignedBehaviorControlsEffectsIndependentlyOfGameplayTier(
            int zone, WheelTier targetTier, WheelTier behaviorSource, bool celebrates)
        {
            var configuration = CreateFixedConfiguration();
            var tier = Object.Instantiate(configuration.GetTier(targetTier));
            try
            {
                var tierData = new SerializedObject(tier);
                tierData.FindProperty("animation").objectReferenceValue = configuration.GetTier(behaviorSource).Animation;
                tierData.ApplyModifiedPropertiesWithoutUndo();
                var configurationData = new SerializedObject(configuration);
                configurationData.FindProperty(targetTier.ToString().ToLowerInvariant()).objectReferenceValue = tier;
                configurationData.ApplyModifiedPropertiesWithoutUndo();
                var session = new GameSession(configuration.CreateRuntime(), Random, new InMemoryProgressStore());
                for (var index = 1; index < zone; index++)
                {
                    var spin = session.RequestSpin().Result;
                    session.CompleteResolution(spin.OperationId);
                }

                View.Wheel.Initialize(configuration);
                View.Wheel.Bind(session.GetSnapshot().Round);
                Assert.That(session.GetSnapshot().Round.Tier, Is.EqualTo(targetTier));
                var announcement = Root.GetComponentsInChildren<TMP_Text>(true)
                    .Single(text => text.name == "ui_text_tier_announcement_motion_value");
                Assert.That(announcement.gameObject.activeSelf, Is.EqualTo(celebrates));
                View.Wheel.GetComponent<WheelTierTransition>().Finish();
                Assert.That(announcement.gameObject.activeSelf, Is.False);

                View.Wheel.Highlight(session.RequestSpin().Result, status => { });
                var ring = Root.GetComponentInChildren<PulseRingGraphic>(true);
                Assert.That(ring.gameObject.activeSelf, Is.EqualTo(celebrates));
                View.Wheel.Cancel();
                Assert.That(ring.gameObject.activeSelf, Is.False);
            }
            finally
            {
                View.Wheel.Cancel();
                Object.DestroyImmediate(tier);
            }
        }
    }
}
#endif
