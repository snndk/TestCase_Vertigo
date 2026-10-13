using Lofelt.NiceVibrations;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Haptics;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Haptic profile")]
    public class HapticProfile : ScriptableObject
    {
        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField] private HapticPatterns.PresetType spinStart = HapticPatterns.PresetType.LightImpact;
        [SerializeField] private HapticPatterns.PresetType wheelStop = HapticPatterns.PresetType.MediumImpact;
        [SerializeField] private HapticPatterns.PresetType reward = HapticPatterns.PresetType.Success;
        [SerializeField] private HapticPatterns.PresetType bomb = HapticPatterns.PresetType.Failure;
        [SerializeField] private HapticPatterns.PresetType revive = HapticPatterns.PresetType.Success;
        [SerializeField] private HapticPatterns.PresetType cashOut = HapticPatterns.PresetType.Success;

        public bool HapticsEnabled => hapticsEnabled;

        public HapticPatterns.PresetType GetPreset(HapticCue cue)
        {
            switch (cue)
            {
                case HapticCue.SpinStart: return spinStart;
                case HapticCue.WheelStop: return wheelStop;
                case HapticCue.Reward: return reward;
                case HapticCue.Bomb: return bomb;
                case HapticCue.Revive: return revive;
                case HapticCue.CashOut: return cashOut;
                default: return HapticPatterns.PresetType.None;
            }
        }
    }
}
