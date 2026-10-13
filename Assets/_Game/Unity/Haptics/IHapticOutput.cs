using Lofelt.NiceVibrations;

namespace Vertigo.Unity._Game.Unity.Haptics
{
    public interface IHapticOutput
    {
        void Play(HapticPatterns.PresetType preset);
        void Stop();
    }
}
