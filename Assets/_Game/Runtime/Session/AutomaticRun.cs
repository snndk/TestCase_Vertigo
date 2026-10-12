using System;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    internal class AutomaticRun
    {
        private readonly Predicate<SliceData> m_Eligible;
        internal AutomaticRunSnapshot Snapshot { get; }

        internal AutomaticRun(int targetZone, Predicate<SliceData> eligible, string displayName)
        {
            m_Eligible = eligible ?? throw new ArgumentNullException(nameof(eligible));
            Snapshot = new AutomaticRunSnapshot(displayName, targetZone);
        }

        internal bool CanSelect(SliceData slice) => m_Eligible(slice);

        internal AutomaticRun AtZone(int zone) => zone < Snapshot.TargetZone ? this : null;
    }
}
