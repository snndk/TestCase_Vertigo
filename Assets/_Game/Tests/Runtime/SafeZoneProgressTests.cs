using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class SafeZoneProgressTests
    {
        [TestCase(1, 1, 5, 0f, WheelTier.Silver)]
        [TestCase(2, 1, 5, .25f, WheelTier.Silver)]
        [TestCase(5, 1, 5, 1f, WheelTier.Silver)]
        [TestCase(6, 6, 10, 0f, WheelTier.Silver)]
        [TestCase(29, 26, 30, .75f, WheelTier.Gold)]
        [TestCase(30, 26, 30, 1f, WheelTier.Gold)]
        [TestCase(31, 31, 35, 0f, WheelTier.Silver)]
        public void ProgressTracksTheNextSafeZoneIncludingGold(int zone, int first, int target, float progress, WheelTier tier)
        {
            var result = ZoneRules.GetSafeZoneProgress(zone);
            Assert.That(result.CurrentZone, Is.EqualTo(zone));
            Assert.That(result.FirstZone, Is.EqualTo(first));
            Assert.That(result.TargetZone, Is.EqualTo(target));
            Assert.That(result.RemainingZones, Is.EqualTo(target - zone));
            Assert.That(result.Progress, Is.EqualTo(progress));
            Assert.That(result.Tier, Is.EqualTo(tier));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidZonesAreRejected(int zone)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoneRules.GetSafeZoneProgress(zone));
        }
    }
}
