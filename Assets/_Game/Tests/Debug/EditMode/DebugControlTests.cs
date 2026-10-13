using System;
using NUnit.Framework;
using Vertigo.Debug._Game.Debug;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Rules;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Runtime._Game.Runtime.Wallet;

namespace Vertigo.Tests.Debug._Game.Tests.Debug.EditMode
{
    public class DebugControlTests
    {
        [Test]
        public void ForcedResultIsConsumedOnceAndNormalRandomResumes()
        {
            var round = new RoundGenerator(Configuration(), new DebugRandomSource()).Generate(1);
            var selector = new ForcedSpinSelector(new SpinSelector(new DebugRandomSource()));
            selector.Arm(round, 1);
            Assert.That(selector.SelectSlice(round), Is.EqualTo(1));
            Assert.That(selector.Pending, Is.EqualTo("Random"));
            Assert.That(selector.SelectSlice(round), Is.Zero);
        }

        [Test]
        public void ChangedRoundOrIneligibleResultCannotUseTheOverride()
        {
            var generator = new RoundGenerator(Configuration(), new DebugRandomSource());
            var oldRound = generator.Generate(1);
            var nextRound = generator.Generate(2);
            var selector = new ForcedSpinSelector(new SpinSelector(new DebugRandomSource()));
            selector.Arm(oldRound, 1);
            Assert.That(selector.SelectSlice(nextRound), Is.Zero);
            selector.Arm(nextRound, 1);
            Assert.That(selector.SelectSlice(nextRound, slice => slice is RewardSliceData), Is.Zero);
            Assert.That(selector.Pending, Is.EqualTo("Random"));
        }

        [Test]
        public void InvalidSlotIsRejectedAndCancellationRestoresRandom()
        {
            var round = new RoundGenerator(Configuration(), new DebugRandomSource()).Generate(1);
            var selector = new ForcedSpinSelector(new SpinSelector(new DebugRandomSource()));
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Arm(round, 4));
            selector.Arm(round, 1);
            selector.Clear();
            Assert.That(selector.SelectSlice(round), Is.Zero);
        }

        [Test]
        public void SaveFailureHappensOnceWithoutTouchingTheUnderlyingBalance()
        {
            var inner = new InMemoryProgressStore(new RewardCollection(new[] { new Reward("gold", 100) }));
            var store = new DebugProgressStore(inner) { FailNextSave = true };
            var progress = new ProgressService(store);
            Assert.That(progress.TrySpend(new Reward("gold", 25)), Is.False);
            Assert.That(inner.Load().GetAmount("gold"), Is.EqualTo(100));
            Assert.That(progress.Rewards.GetAmount("gold"), Is.EqualTo(100));
            Assert.That(store.FailNextSave, Is.False);
            Assert.That(progress.TrySpend(new Reward("gold", 25)), Is.True);
            Assert.That(inner.Load().GetAmount("gold"), Is.EqualTo(75));
        }

        [TestCase(5, WheelTier.Silver)]
        [TestCase(30, WheelTier.Gold)]
        [TestCase(300, WheelTier.Gold)]
        [TestCase(999, WheelTier.Bronze)]
        public void StartingZoneUsesNormalTierRulesAndEmptyRun(int zone, WheelTier tier)
        {
            var session = new GameSession(Configuration(), new DebugRandomSource(),
                new ProgressService(new InMemoryProgressStore()), startingZone: zone);
            var state = session.GetSnapshot();
            Assert.That(state.Round.Zone, Is.EqualTo(zone));
            Assert.That(state.Round.Tier, Is.EqualTo(tier));
            Assert.That(state.RunLoot.Amounts, Is.Empty);
            if (tier != WheelTier.Bronze)
            {
                Assert.That(session.RequestCashOut().Status, Is.EqualTo(CommandStatus.Accepted));
                session.StartNewRun();
                Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            }
        }

        [Test]
        public void ExternalSelectorCannotBypassLuckyEligibility()
        {
            var selector = new FixedSpinSelector { Index = 2 };
            var session = new GameSession(Configuration(), new DebugRandomSource(),
                new ProgressService(new InMemoryProgressStore()), spinSelector: selector);
            var lucky = session.RequestSpin().Result;
            session.CompleteResolution(lucky.OperationId);
            selector.Index = 1;
            Assert.Throws<InvalidOperationException>(() => session.RequestAutomaticSpin());
            Assert.That(session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(session.GetSnapshot().PendingSpin, Is.Null);
            selector.Index = 0;
            Assert.That(session.RequestAutomaticSpin().Status, Is.EqualTo(CommandStatus.Accepted));
        }

        [Test]
        public void SharedEconomyUpdatesReviveEligibilityWithoutReplacingTheRun()
        {
            var progress = new ProgressService(new InMemoryProgressStore());
            var selector = new FixedSpinSelector { Index = 1 };
            var session = new GameSession(Configuration(), new DebugRandomSource(), progress, new Reward("gold", 25), spinSelector: selector);
            var spin = session.RequestSpin().Result;
            session.CompleteResolution(spin.OperationId);
            var before = session.GetSnapshot();
            Assert.That(before.CanRevive, Is.False);
            progress.TryCollect(new RewardCollection(new[] { new Reward("gold", 50) }));
            Assert.That(session.GetSnapshot().CanRevive, Is.True);
            Assert.That(session.GetSnapshot().RunId, Is.EqualTo(before.RunId));
            Assert.That(session.RequestRevive(), Is.EqualTo(CommandStatus.Accepted));
            Assert.That(session.GetSnapshot().Progress.GetAmount("gold"), Is.EqualTo(25));
        }

        private static WheelConfiguration Configuration()
        {
            var gold = new RewardSliceDefinition("gold", new RewardDefinition("gold", 1), 10);
            var ticket = new RewardSliceDefinition("ticket", new RewardDefinition("ticket", 1), 1);
            var lucky = new LuckySliceDefinition("lucky");
            return new WheelConfiguration(new SliceDefinition[] { gold, new BombSliceDefinition("bomb"), lucky, ticket },
                new SliceDefinition[] { gold, ticket, lucky, gold }, new SliceDefinition[] { gold, ticket, lucky, gold });
        }
    }
}
