using System;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Tests.Runtime._Game.Tests.Runtime
{
    public class SliceContextTests
    {
        [Test]
        public void CompoundBehaviorUsesCurrentLootAndPreparesOnlyOneRound()
        {
            var session = SliceContextTestFactory.Create(context =>
            {
                context.AddReward(new Reward("coins", 3));
                context.AddReward(new Reward("tickets", context.Loot.GetAmount("coins")));
                context.ContinueToNextRound();
            });
            var first = session.RequestSpin().Result;
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(1), "Animation still owns completion timing.");
            session.CompleteResolution(first.OperationId);
            var second = session.RequestSpin().Result;
            session.CompleteResolution(second.OperationId);
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(3));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("coins"), Is.EqualTo(6));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("tickets"), Is.EqualTo(9));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailureRollsBackCommandsAndClosesEscapedContext(bool afterTransition)
        {
            SliceContext escaped = null;
            var session = SliceContextTestFactory.Create(context =>
            {
                escaped = context;
                context.AddReward(new Reward("coins", 99));
                context.StartAutomaticRun(5, slice => slice is RewardSliceData, "TEST RUN");
                if (afterTransition)
                    context.ContinueToNextRound();
                throw new InvalidOperationException("Behavior failed.");
            });
            var before = session.GetSnapshot();
            Assert.Throws<InvalidOperationException>(() => session.RequestSpin());
            var after = session.GetSnapshot();
            Assert.That(after.Round, Is.SameAs(before.Round));
            Assert.That(after.RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(after.AutomaticRun, Is.Null);
            Assert.That(after.PendingSpin, Is.Null);
            Assert.That(after.Status, Is.EqualTo(SessionStatus.Ready));
            Assert.Throws<InvalidOperationException>(() => escaped.AddReward(new Reward("coins", 1)));
        }

        [Test]
        public void NextRoundGenerationFailureRollsBackReward()
        {
            var session = SliceContextTestFactory.Create(context =>
            {
                context.AddReward(new Reward("tickets", 7));
                context.ContinueToNextRound();
            }, baseAmount: long.MaxValue);
            Assert.Throws<OverflowException>(() => session.RequestSpin());
            Assert.That(session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(session.GetSnapshot().CanSpin, Is.True);
        }

        [Test]
        public void MissingTransitionRejectsTheWholeOperation()
        {
            var session = SliceContextTestFactory.Create(context => context.AddReward(new Reward("coins", 7)));
            Assert.Throws<InvalidOperationException>(() => session.RequestSpin());
            Assert.That(session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(session.GetSnapshot().CanSpin, Is.True);
        }

        [Test]
        public void SecondTransitionRejectsTheWholeOperation()
        {
            var session = SliceContextTestFactory.Create(context =>
            {
                context.ContinueToNextRound();
                context.LoseRun();
            });
            Assert.Throws<InvalidOperationException>(() => session.RequestSpin());
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(session.GetSnapshot().CanSpin, Is.True);
        }

        [Test]
        public void ResolvedBehaviorAndClosedContextCannotBeReused()
        {
            SliceContext escaped = null;
            RoundSlice behavior = null;
            var session = SliceContextTestFactory.Create(context =>
            {
                escaped = context;
                context.AddReward(new Reward("coins", 7));
                context.ContinueToNextRound();
            }, slice => behavior = slice);
            var spin = session.RequestSpin().Result;
            Assert.Throws<InvalidOperationException>(() => behavior.Resolve());
            Assert.Throws<InvalidOperationException>(() => escaped.AddReward(new Reward("coins", 1)));
            session.CompleteResolution(spin.OperationId);
            Assert.Throws<InvalidOperationException>(() => escaped.StartAutomaticRun(5, slice => true, "STALE"));
            Assert.That(session.CompleteResolution(spin.OperationId), Is.EqualTo(CommandStatus.StaleOperation));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("coins"), Is.EqualTo(7));
        }

        [Test]
        public void PreparingBehaviorRejectsReentrantSpinAndCashOut()
        {
            GameSession session = null;
            session = SliceContextTestFactory.Create(context =>
            {
                Assert.That(session.RequestSpin().Status, Is.EqualTo(CommandStatus.NotReady));
                Assert.That(session.RequestCashOut().Status, Is.EqualTo(CommandStatus.NotReady));
                context.AddReward(new Reward("coins", 1));
                context.ContinueToNextRound();
            });
            for (var zone = 1; zone <= 5; zone++)
                session.CompleteResolution(session.RequestSpin().Result.OperationId);
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(6));
            Assert.That(session.GetSnapshot().RunLoot.GetAmount("coins"), Is.EqualTo(5));
        }

        [Test]
        public void DifferentSliceCanAwardAndStartFilteredAutomaticRunWithoutLucky()
        {
            var session = SliceContextTestFactory.Create(context =>
            {
                context.AddReward(new Reward("coins", 9));
                context.StartAutomaticRun(4, slice => slice is RewardSliceData reward &&
                    reward.Reward.RewardId == "tickets", "TICKET RUN");
                context.ContinueToNextRound();
            });
            session.CompleteResolution(session.RequestSpin().Result.OperationId);
            Assert.That(session.GetSnapshot().AutomaticRun.DisplayName, Is.EqualTo("TICKET RUN"));
            while (session.GetSnapshot().CanAutomaticSpin)
            {
                var spin = session.RequestAutomaticSpin().Result;
                Assert.That(((RewardSliceData)spin.WinningSlice).Reward.RewardId, Is.EqualTo("tickets"));
                session.CompleteResolution(spin.OperationId);
            }
            Assert.That(session.GetSnapshot().Round.Zone, Is.EqualTo(4));
            Assert.That(session.GetSnapshot().CanSpin, Is.True);
            Assert.That(session.GetSnapshot().AutomaticRun, Is.Null);
        }
    }
}
