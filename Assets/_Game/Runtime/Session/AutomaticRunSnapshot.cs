namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class AutomaticRunSnapshot
    {
        public string DisplayName { get; }
        public int TargetZone { get; }

        internal AutomaticRunSnapshot(string displayName, int targetZone)
        {
            DisplayName = displayName;
            TargetZone = targetZone;
        }
    }
}
