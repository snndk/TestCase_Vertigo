#if UNITY_EDITOR
using NUnit.Framework;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class GameScreenViewTests : GameViewTestFixture
    {
        [Test]
        public void DisablingSafeZoneDuringAnimationRestoresTheTargetProgress()
        {
            var safe = Root.GetComponentInChildren<SafeZoneView>();
            safe.Show(ZoneRules.GetSafeZoneProgress(1));
            safe.Show(ZoneRules.GetSafeZoneProgress(2));
            safe.gameObject.SetActive(false);
            safe.gameObject.SetActive(true);
            safe.Show(ZoneRules.GetSafeZoneProgress(2));
            var track = Root.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "ui_group_safe_zone_bar_track");
            var fill = Root.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "ui_image_safe_zone_bar_track_fill_value");
            Assert.That(fill.rect.height / track.rect.height, Is.EqualTo(.25f).Within(.001f));
        }

        [Test]
        public void SafeAreaKeepsTheSourceLayoutPadding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ui_canvas_game.prefab");
            var authored = prefab.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == "ui_group_safe_area");
            var shown = Root.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == "ui_group_safe_area");
            Assert.That(shown.offsetMin, Is.EqualTo(authored.offsetMin));
            Assert.That(shown.offsetMax, Is.EqualTo(authored.offsetMax));
            Assert.That(authored.anchorMin, Is.Not.EqualTo(Vector2.zero));
            var area = Screen.safeArea;
            var minimum = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            var extent = new Vector2(area.width / Screen.width, area.height / Screen.height);
            Assert.That(shown.anchorMin, Is.EqualTo(minimum + Vector2.Scale(authored.anchorMin, extent)));
            Assert.That(shown.anchorMax, Is.EqualTo(minimum + Vector2.Scale(authored.anchorMax, extent)));
        }

        [Test]
        public void ZoneIndicatorReachesItsTargetAfterDisableAndSameStateRender()
        {
            var zones = Root.GetComponentInChildren<ZoneProgressView>();
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
            Random.Index = 7;
            yield return SpinAndWait();
            var panels = Root.GetComponentInChildren<ResultPanelsView>(true);
            var data = new SerializedObject(panels);
            var bombMotion = (RectTransform)data.FindProperty("bombMotion").objectReferenceValue;
            panels.enabled = false;
            panels.enabled = true;
            View.Render(new GameScreenState(Session.GetSnapshot(), RewardCollection.Empty, null));
            Assert.That(bombMotion.localScale, Is.EqualTo(Vector3.one));
            Button("restart").onClick.Invoke();
            Random.Index = 0;
            for (var zone = 1; zone < 5; zone++)
                yield return SpinAndWait();
            Button("cashout").onClick.Invoke();
            var cashOutMotion = (RectTransform)data.FindProperty("cashOutMotion").objectReferenceValue;
            panels.enabled = false;
            panels.enabled = true;
            View.Render(new GameScreenState(Session.GetSnapshot(), RewardCollection.Empty, null));
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
            View.Initialize(configuration, AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset"));
            View.Render(new GameScreenState(Session.GetSnapshot(), RewardCollection.Empty, null));
            Assert.That(Text("safety"), Is.EqualTo("CUSTOM BRONZE"));
            var title = Root.GetComponentsInChildren<TMP_Text>(true).Single(item => item.name == "ui_text_wheel_title_value");
            Assert.That(title.color, Is.EqualTo(Color.magenta));
            var status = (TMP_Text)new SerializedObject(View).FindProperty("status").objectReferenceValue;
            Assert.That(status.text, Is.EqualTo("CUSTOM STATUS"));
        }
    }
}
#endif
