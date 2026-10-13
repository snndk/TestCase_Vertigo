#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Unity._Game.Unity.Persistence;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class PersistentProgressTests : GameViewTestFixture
    {
        private string m_Directory;
        private string m_Path;
        private string m_CurrencyId;

        [SetUp]
        public void SetUpStorage()
        {
            m_Directory = Path.Combine(Path.GetTempPath(), "VertigoProgressTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Directory);
            m_Path = Path.Combine(m_Directory, "progress.json");
            m_CurrencyId = AssetDatabase.LoadAssetAtPath<RewardAsset>("Assets/_Game/Settings/Rewards/Gold.asset").Id;
            AttachStoredSession();
        }

        [TearDown]
        public void TearDownStorage()
        {
            Directory.Delete(m_Directory, true);
        }

        [UnityTest]
        public IEnumerator CashOutAndRevivePersistAcrossNewSessionsWhileUncollectedLootDoesNot()
        {
            for (var index = 0; index < 4; index++)
            {
                Random.Index = index == 3 ? 4 : index;
                yield return SpinAndWait();
            }
            var expected = Session.GetSnapshot().Progress.Combine(Session.GetSnapshot().RunLoot);
            Button("cashout").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
            CollectionAssert.AreEquivalent(expected.Amounts, Session.GetSnapshot().Progress.Amounts);
            Assert.That(expected.Amounts.Count, Is.GreaterThan(1));
            AttachStoredSession();
            Assert.That(Session.GetSnapshot().Progress.GetAmount(m_CurrencyId), Is.EqualTo(expected.GetAmount(m_CurrencyId)));
            Assert.That(Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Empty);

            Random.Index = 7;
            yield return SpinAndWait();
            Button("bomb_revive").onClick.Invoke();
            expected = expected.Subtract(new Reward(m_CurrencyId, 25));
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Random.Index = 0;
            yield return SpinAndWait();
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Not.Empty);
            AttachStoredSession();
            Assert.That(Session.GetSnapshot().Progress.GetAmount(m_CurrencyId), Is.EqualTo(expected.GetAmount(m_CurrencyId)));
            Assert.That(Session.GetSnapshot().Progress.Amounts.Count, Is.EqualTo(1));
            Assert.That(Session.GetSnapshot().RunLoot.Amounts, Is.Empty);
            Assert.That(Text("wallet_amount"), Is.EqualTo(expected.GetAmount(m_CurrencyId).ToString("N0")));
        }

        [UnityTest]
        public IEnumerator CashOutWriteFailureShowsErrorKeepsLootAndAllowsRetry()
        {
            for (var index = 0; index < 4; index++)
                yield return SpinAndWait();
            var before = Session.GetSnapshot();
            Directory.CreateDirectory(m_Path + ".tmp");
            Button("cashout").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(Session.GetSnapshot().Progress, Is.SameAs(before.Progress));
            Assert.That(Session.GetSnapshot().RunLoot, Is.SameAs(before.RunLoot));
            Assert.That(Text("error"), Does.Contain("could not be saved"));
            Assert.That(Button("cashout").interactable, Is.True);
            Directory.Delete(m_Path + ".tmp");
            Button("cashout").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.CashedOut));
            Assert.That(CreateStore().Load().GetAmount(m_CurrencyId),
                Is.EqualTo(before.Progress.Combine(before.RunLoot).GetAmount(m_CurrencyId)));
        }

        [UnityTest]
        public IEnumerator ReviveWriteFailureDoesNotSpendOrAdvanceAndRetryPersistsOnePayment()
        {
            Random.Index = 7;
            yield return SpinAndWait();
            var before = Session.GetSnapshot();
            Directory.CreateDirectory(m_Path + ".tmp");
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(before.Status));
            Assert.That(Session.GetSnapshot().Progress, Is.SameAs(before.Progress));
            Assert.That(Session.GetSnapshot().Round.Zone, Is.EqualTo(before.Round.Zone));
            Assert.That(Text("error"), Does.Contain("could not be saved"));
            Directory.Delete(m_Path + ".tmp");
            Button("bomb_revive").onClick.Invoke();
            Assert.That(Session.GetSnapshot().Status, Is.EqualTo(SessionStatus.Ready));
            Assert.That(CreateStore().Load().GetAmount(m_CurrencyId), Is.EqualTo(225));
        }

        private void AttachStoredSession()
        {
            Presenter.Detach();
            Random.Index = 0;
            Session = new GameSession(CreateFixedConfiguration().CreateRuntime(), Random, CreateStore(), new Reward(m_CurrencyId, 25));
            Presenter = new GameScreenPresenter(Session, View, View.Input,
                new SpinPresentationSequence(View.Wheel, TimeSpan.FromSeconds(5)));
            Presenter.Attach();
        }

        private FileProgressStore CreateStore()
        {
            return new FileProgressStore(m_Path, new RewardCollection(new[] { new Reward(m_CurrencyId, 250) }), m_CurrencyId);
        }
    }
}
#endif
