namespace Vertigo.Runtime._Game.Runtime.Rules
{
    public class SafeZoneProgress
    {
        public int CurrentZone { get; }
        public int FirstZone { get; }
        public int TargetZone { get; }
        public int RemainingZones => TargetZone - CurrentZone;
        public WheelTier Tier { get; }
        public float Progress { get; }

        internal SafeZoneProgress(int currentZone, int firstZone, int targetZone, WheelTier tier)
        {
            CurrentZone = currentZone;
            FirstZone = firstZone;
            TargetZone = targetZone;
            Tier = tier;
            Progress = (currentZone - firstZone) / (float)(targetZone - firstZone);
        }
    }
}
