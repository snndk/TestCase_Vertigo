using System;

namespace Vertigo.Runtime._Game.Runtime.Rules
{
    public static class ZoneRules
    {
        public static WheelTier GetTier(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone));

            if (zone % 30 == 0)
                return WheelTier.Gold;

            return zone % 5 == 0 ? WheelTier.Silver : WheelTier.Bronze;
        }

        public static bool IsSafe(int zone)
        {
            return GetTier(zone) != WheelTier.Bronze;
        }
    }
}
