#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vertigo.Unity._Game.Unity.Bootstrap;
using Vertigo.Unity._Game.Unity.Views;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class TapToStartTests
    {
        private GameObject m_Root;

        [UnityTest]
        public IEnumerator OpeningTapStartsOnceWithoutSpinningAndDoesNotReturnOnReenable()
        {
            m_Root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/ui_canvas_game.prefab"));
            yield return null;
            var start = m_Root.GetComponentInChildren<TapToStartView>(true);
            var bootstrap = m_Root.GetComponent<GameBootstrap>();
            var presenter = (GameScreenPresenter)typeof(GameBootstrap)
                .GetField("m_Presenter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var session = (GameSession)typeof(GameScreenPresenter)
                .GetField("m_Session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);
            var spin = m_Root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "ui_button_wheel_spin_value");

            Assert.That(start.gameObject.activeInHierarchy, Is.True);
            spin.onClick.Invoke();
            Assert.That(session.GetSnapshot().PendingSpin, Is.Null, "Presenter must wait for entry.");

            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, spin.transform.position)
            };
            m_Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(start.gameObject), "Entry must cover the spin button.");

            var button = start.GetComponent<Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(session.GetSnapshot().PendingSpin, Is.Null);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(start.gameObject.activeSelf, Is.False);

            m_Root.SetActive(false);
            m_Root.SetActive(true);
            yield return null;
            Assert.That(start.gameObject.activeSelf, Is.False);
            spin.onClick.Invoke();
            Assert.That(session.GetSnapshot().PendingSpin, Is.Not.Null, "Game input must work after entry.");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
                Object.DestroyImmediate(m_Root);
        }
    }
}
#endif
