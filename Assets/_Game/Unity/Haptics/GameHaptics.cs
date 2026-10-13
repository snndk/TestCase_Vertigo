using System;
using Lofelt.NiceVibrations;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Haptics
{
    public class GameHaptics : MonoBehaviour
    {
        [SerializeField] private HapticProfile profile;
        private IHapticOutput m_Output;
        private bool m_HapticsEnabled = true;
        private bool m_Paused;
        private bool m_Focused = true;

        public bool HapticsEnabled
        {
            get => m_HapticsEnabled;
            set
            {
                m_HapticsEnabled = value;
                if (!value)
                    Stop();
            }
        }

        private void Awake()
        {
            if (m_Output == null)
                m_Output = new NiceVibrationsOutput();
        }

        public void SetOutput(IHapticOutput output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            Stop();
            m_Output = output;
        }

        public void Play(HapticCue cue)
        {
            if (!isActiveAndEnabled || !m_HapticsEnabled || m_Paused || !m_Focused ||
                profile == null || !profile.HapticsEnabled)
                return;
            var preset = profile.GetPreset(cue);
            if (preset != HapticPatterns.PresetType.None)
                m_Output?.Play(preset);
        }

        public void Stop() => m_Output?.Stop();

        private void OnApplicationPause(bool paused)
        {
            m_Paused = paused;
            if (paused)
                Stop();
        }

        private void OnApplicationFocus(bool focused)
        {
            m_Focused = focused;
            if (!focused)
                Stop();
        }

        private void OnDisable() => Stop();
    }
}
