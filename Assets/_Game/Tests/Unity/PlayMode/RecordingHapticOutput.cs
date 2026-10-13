using System.Collections.Generic;
using Lofelt.NiceVibrations;
using Vertigo.Unity._Game.Unity.Haptics;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class RecordingHapticOutput : IHapticOutput
    {
        public List<HapticPatterns.PresetType> Played { get; } = new List<HapticPatterns.PresetType>();
        public int StopCount { get; private set; }
        public void Play(HapticPatterns.PresetType preset) => Played.Add(preset);
        public void Stop() => StopCount++;
    }
}
