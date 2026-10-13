using System;

namespace Vertigo.Unity._Game.Unity.Persistence
{
    [Serializable]
    internal class ProgressSaveData
    {
        public int version;
        public SavedReward[] rewards;
    }
}
