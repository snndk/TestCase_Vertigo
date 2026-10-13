using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.EditMode
{
    public class PrefabWiringTests
    {
        [Test]
        public void OnValidateRestoresButtonReference()
        {
            var root = new GameObject("Test button", typeof(RectTransform), typeof(Button), typeof(GameButton));
            try
            {
                var binding = root.GetComponent<GameButton>();
                var data = new SerializedObject(binding);
                data.FindProperty("button").objectReferenceValue = null;
                data.ApplyModifiedPropertiesWithoutUndo();
                typeof(GameButton).GetMethod("OnValidate", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(binding, null);
                data.Update();
                Assert.That(data.FindProperty("button").objectReferenceValue, Is.EqualTo(root.GetComponent<Button>()));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GamePrefabHasCompleteReferencesAndNoPersistentButtonListeners()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
            Assert.That(prefab, Is.Not.Null);
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Assembly != typeof(GameBootstrap).Assembly)
                    continue;
                var property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                    // UI graphics legitimately use Unity's default material when this field is empty.
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        !(component is Graphic && property.propertyPath == "m_Material"))
                        Assert.That(property.objectReferenceValue, Is.Not.Null, component.GetType().Name + "." + property.propertyPath);
            }
            foreach (var button in prefab.GetComponentsInChildren<Button>(true))
                Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
            Assert.That(prefab.GetComponent<GameScreenView>().SliceCount, Is.EqualTo(8));
        }

        [Test]
        public void CanvasAndGraphicsFollowCaseSettings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            foreach (var graphic in prefab.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is MaskableGraphic maskable)
                    Assert.That(maskable.maskable, Is.EqualTo(graphic.GetComponentInParent<RectMask2D>(true) != null), graphic.name);
                if (graphic.GetComponent<AnimationTapView>() != null)
                    Assert.That(graphic.raycastTarget, Is.True, "The tap surface must intercept accelerated playback and Lucky input.");
                else if (graphic.GetComponent<Button>() == null && !graphic.name.Contains("scrim") && !graphic.name.Contains("viewport"))
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                if (graphic is Image image)
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), image.name);
                if (graphic is TMP_Text text)
                    Assert.That(text.font, Is.Not.Null, text.name);
            }
        }

        [Test]
        public void AtlasContainsEveryAuthoredWheelAndRewardSprite()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_Game/Art/Atlases/GameUI.spriteatlasv2");
            var icons = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_Game/Art/Atlases/RewardIcons.spriteatlasv2");
            var configuration = AssetDatabase.LoadAssetAtPath<WheelConfigurationAsset>("Assets/_Game/Settings/WheelConfiguration.asset");
            foreach (Vertigo.Runtime._Game.Runtime.Rules.WheelTier tier in System.Enum.GetValues(typeof(Vertigo.Runtime._Game.Runtime.Rules.WheelTier)))
            {
                var visuals = configuration.GetTier(tier);
                Assert.That(atlas.CanBindTo(visuals.Wheel), Is.True);
                Assert.That(atlas.CanBindTo(visuals.Indicator), Is.True);
                for (var index = 0; index < visuals.CandidateCount; index++)
                {
                    var icon = visuals.GetSlice(index).Icon;
                    Assert.That(icons.CanBindTo(icon) || atlas.CanBindTo(icon), Is.True, icon.name);
                }
            }
        }
    }
}
