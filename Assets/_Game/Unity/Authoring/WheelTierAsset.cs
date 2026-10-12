using System;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Wheel tier")]
    public class WheelTierAsset : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField, TextArea] private string statusMessage;
        [SerializeField] private Sprite wheel;
        [SerializeField] private Sprite indicator;
        [SerializeField] private WheelPoolEntry[] pool = Array.Empty<WheelPoolEntry>();
        public string Title => title;
        public Color TitleColor => titleColor;
        public string StatusMessage => statusMessage;
        public Sprite Wheel => wheel;
        public Sprite Indicator => indicator;
        public int CandidateCount => pool.Length;
        public SliceAsset GetSlice(int index) => pool[index].Slice;

        public WeightedSlice[] CreateCandidates()
        {
            if (wheel == null || indicator == null)
                throw new InvalidOperationException(name + " needs wheel and indicator sprites.");
            var candidates = new WeightedSlice[pool.Length];
            for (var index = 0; index < pool.Length; index++)
            {
                if (pool[index] == null)
                    throw new InvalidOperationException(name + " has a missing pool entry at " + index + ".");
                candidates[index] = pool[index].CreateRuntime();
            }
            return candidates;
        }
    }
}
