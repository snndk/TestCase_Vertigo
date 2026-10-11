using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Tests.Runtime
{
    public class RewardCollectionTests
    {
        [Test]
        public void AddAndCombinePreserveInputsAndAggregateDuplicateIds()
        {
            var original = new RewardCollection(new[] { new Reward("gold", 2), new Reward("gold", 3) });
            var added = original.Add(new Reward("ticket", 4));
            var other = new RewardCollection(new[] { new Reward("gold", 7), new Reward("gem", 1) });
            var combined = added.Combine(other);

            Assert.That(original.GetAmount("gold"), Is.EqualTo(5));
            Assert.That(original.GetAmount("ticket"), Is.Zero);
            Assert.That(added.GetAmount("gold"), Is.EqualTo(5));
            Assert.That(other.GetAmount("gold"), Is.EqualTo(7));
            Assert.That(combined.GetAmount("gold"), Is.EqualTo(12));
            Assert.That(combined.GetAmount("ticket"), Is.EqualTo(4));
            Assert.That(combined.GetAmount("gem"), Is.EqualTo(1));
            Assert.That(RewardCollection.Empty.Amounts, Is.Empty);
        }
    }
}
