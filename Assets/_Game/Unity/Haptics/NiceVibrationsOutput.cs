using System;
using Lofelt.NiceVibrations;

namespace Vertigo.Unity._Game.Unity.Haptics
{
    public class NiceVibrationsOutput : IHapticOutput
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private bool m_Unavailable;

        public NiceVibrationsOutput()
        {
            try
            {
                HapticController.Init();
            }
            catch (Exception error)
            {
                DisableOutput(error);
            }
        }
#endif

        public void Play(HapticPatterns.PresetType preset)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (m_Unavailable)
                return;
            try
            {
                HapticPatterns.PlayPreset(preset);
            }
            catch (Exception error)
            {
                DisableOutput(error);
            }
#endif
        }

        public void Stop()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (m_Unavailable)
                return;
            try
            {
                HapticController.Stop();
            }
            catch (Exception error)
            {
                DisableOutput(error);
            }
#endif
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private void DisableOutput(Exception error)
        {
            // Optional device feedback must never interrupt a spin or an economy operation.
            m_Unavailable = true;
            UnityEngine.Debug.LogWarning("Haptics unavailable: " + error.Message);
        }
#endif
    }
}
