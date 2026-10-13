using System.IO;
using SRDebugger.Services;
using SRF.Service;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vertigo.Unity._Game.Unity.Bootstrap;

namespace Vertigo.Debug._Game.Debug
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameBootstrap))]
    public class GameDebugPanel : MonoBehaviour
    {
        private GameDebugSession m_Session;
        private GameDebugOptions m_Options;
        private float m_NextRefresh;
        private IPinnedUIService m_Pins;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (var index = 0; index < SceneManager.sceneCount; index++)
                Attach(SceneManager.GetSceneAt(index));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);

        private static void Attach(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var game in root.GetComponentsInChildren<GameBootstrap>(true))
                {
                    if (game.GetComponent<GameDebugPanel>() == null)
                        game.gameObject.AddComponent<GameDebugPanel>();
                }
            }
        }

        private void OnEnable()
        {
            var game = GetComponent<GameBootstrap>();
            if (game.GetSnapshot() == null)
                return;
            if (m_Session == null)
            {
                m_Session = new GameDebugSession(game,
                    Path.Combine(Application.persistentDataPath, "Tester", "progress.json"), game.CreateProgressStore);
                m_Options = new GameDebugOptions(m_Session);
            }
            m_Pins = SRServiceManager.GetService<IPinnedUIService>();
            m_Options.Refresh();
            SRDebug.Instance.AddOptionContainer(m_Options.Container);
        }

        private void OnDisable()
        {
            if (m_Options != null)
            {
                foreach (var option in m_Options.Container.Options)
                    m_Pins?.Unpin(option);
                SRDebug.Instance.RemoveOptionContainer(m_Options.Container);
            }
            m_Session?.ClearOverrides();
        }

        private void Update()
        {
            if (m_Options == null || Time.unscaledTime < m_NextRefresh)
                return;
            m_NextRefresh = Time.unscaledTime + 0.25f;
            if (!SRDebug.Instance.IsDebugPanelVisible && !HasPinnedOptions())
                return;
            m_Options.Refresh();
        }

        private bool HasPinnedOptions()
        {
            var options = m_Options.Container.Options;
            for (var index = 0; index < options.Count; index++)
            {
                if (m_Pins.HasPinned(options[index]))
                    return true;
            }
            return false;
        }
    }
}
