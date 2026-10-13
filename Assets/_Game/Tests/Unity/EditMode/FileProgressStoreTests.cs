using System;
using System.IO;
using NUnit.Framework;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Wallet;
using Vertigo.Unity._Game.Unity.Persistence;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.EditMode
{
    public class FileProgressStoreTests
    {
        private string m_Directory;
        private string m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Directory = Path.Combine(Path.GetTempPath(), "VertigoProgressTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Directory);
            m_Path = Path.Combine(m_Directory, "progress.json");
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(m_Directory, true);
        }

        [Test]
        public void MissingSaveUsesStartingRewardsButExistingEmptySaveDoesNotGrantThemAgain()
        {
            var store = new FileProgressStore(m_Path, Rewards(250), "gold");
            Assert.That(store.Load().GetAmount("gold"), Is.EqualTo(250));
            Assert.That(store.TrySave(RewardCollection.Empty), Is.True);
            Assert.That(new FileProgressStore(m_Path, Rewards(999), "gold").Load().Amounts, Is.Empty);
        }

        [Test]
        public void OnlyCurrencyIsWrittenAndLoadedIncludingLargeAmounts()
        {
            var rewards = new RewardCollection(new[]
            {
                new Reward("gold", long.MaxValue), new Reward("ticket", 18), new Reward("future-reward", 3)
            });
            Assert.That(CreateStore().TrySave(rewards), Is.True);
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(long.MaxValue));
            Assert.That(CreateStore().Load().Amounts.Count, Is.EqualTo(1));
            Assert.That(File.ReadAllText(m_Path), Does.Not.Contain("ticket").And.Not.Contain("future-reward"));
        }

        [Test]
        public void PreviousMultiRewardSaveLoadsOnlyCurrency()
        {
            File.WriteAllText(m_Path, "{\"version\":1,\"rewards\":[{\"id\":\"gold\",\"amount\":42},{\"id\":\"ticket\",\"amount\":7}]}");
            var loaded = CreateStore().Load();
            Assert.That(loaded.GetAmount("gold"), Is.EqualTo(42));
            Assert.That(loaded.Amounts.Count, Is.EqualTo(1));
        }

        [Test]
        public void CollectAndSpendSurviveRecreationWithoutGrantingStartingBalanceAgain()
        {
            var service = new ProgressService(new FileProgressStore(m_Path, Rewards(250), "gold"));
            Assert.That(service.TryCollect(new RewardCollection(new[] { new Reward("gold", 100), new Reward("ticket", 7) })), Is.True);
            Assert.That(service.Rewards.GetAmount("ticket"), Is.EqualTo(7));
            service = new ProgressService(CreateStore());
            Assert.That(service.TrySpend(new Reward("gold", 25)), Is.True);
            var restored = new ProgressService(CreateStore());
            Assert.That(restored.Rewards.GetAmount("gold"), Is.EqualTo(325));
            Assert.That(restored.Rewards.GetAmount("ticket"), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedWritePreservesDiskAndMemoryAndCanBeRetried(bool failAtReplace)
        {
            var store = CreateStore();
            Assert.That(store.TrySave(Rewards(100)), Is.True);
            var service = new ProgressService(store);
            var blocker = m_Path + (failAtReplace ? ".bak" : ".tmp");
            Directory.CreateDirectory(blocker);
            var before = File.ReadAllText(m_Path);
            Assert.That(service.TrySpend(new Reward("gold", 25)), Is.False);
            Assert.That(service.Rewards.GetAmount("gold"), Is.EqualTo(100));
            Assert.That(File.ReadAllText(m_Path), Is.EqualTo(before));
            Directory.Delete(blocker);
            Assert.That(service.TrySpend(new Reward("gold", 25)), Is.True);
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(75));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrCorruptPrimaryRecoversBackupAndRepairsBeforeNextWrite(bool missing)
        {
            var store = CreateStore();
            Assert.That(store.TrySave(Rewards(100)), Is.True);
            Assert.That(store.TrySave(Rewards(200)), Is.True);
            if (missing)
                File.Delete(m_Path);
            else
                File.WriteAllText(m_Path, "{broken");

            store = CreateStore();
            Assert.That(store.Load().GetAmount("gold"), Is.EqualTo(100));
            Assert.That(store.RecoveredFromBackup, Is.True);
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(100));
            Assert.That(store.TrySave(Rewards(300)), Is.True);
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(300));
            File.WriteAllText(m_Path, "broken again");
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(100), "The corrupt primary must never replace the valid backup.");
        }

        [Test]
        public void FailedRecoveryLeavesBackupIntactAndDoesNotReturnFreshProgress()
        {
            var store = CreateStore();
            store.TrySave(Rewards(100));
            store.TrySave(Rewards(200));
            File.WriteAllText(m_Path, "broken");
            var backup = File.ReadAllText(m_Path + ".bak");
            Directory.CreateDirectory(m_Path + ".tmp");
            Assert.Throws<IOException>(() => CreateStore().Load());
            Assert.That(File.ReadAllText(m_Path), Is.EqualTo("broken"));
            Assert.That(File.ReadAllText(m_Path + ".bak"), Is.EqualTo(backup));
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{broken")]
        [TestCase("{\"version\":1}")]
        [TestCase("{\"version\":1,\"rewards\":[{\"id\":\"gold\",\"amount\":0}]}")]
        [TestCase("{\"version\":1,\"rewards\":[{\"id\":\"gold\",\"amount\":-1}]}")]
        [TestCase("{\"version\":1,\"rewards\":[{\"id\":\"\",\"amount\":10}]}")]
        [TestCase("{\"version\":1,\"rewards\":[{\"id\":\"gold\",\"amount\":10},{\"id\":\"gold\",\"amount\":20}]}")]
        public void InvalidSaveIsReportedAndNeverOverwrittenWithStartingRewards(string json)
        {
            File.WriteAllText(m_Path, json);
            Assert.Throws<InvalidDataException>(() => new FileProgressStore(m_Path, Rewards(250), "gold").Load());
            Assert.That(File.ReadAllText(m_Path), Is.EqualTo(json));
        }

        [Test]
        public void UnsupportedVersionDoesNotRollBackToAnOlderBackup()
        {
            CreateStore().TrySave(Rewards(100));
            CreateStore().TrySave(Rewards(200));
            const string futureSave = "{\"version\":2}";
            File.WriteAllText(m_Path, futureSave);
            Assert.Throws<NotSupportedException>(() => CreateStore().Load());
            Assert.That(File.ReadAllText(m_Path), Is.EqualTo(futureSave));
        }

        [Test]
        public void UncommittedTemporaryFileIsNeverLoaded()
        {
            CreateStore().TrySave(Rewards(100));
            File.WriteAllText(m_Path + ".tmp", "{\"version\":1,\"rewards\":[{\"id\":\"gold\",\"amount\":999}]}");
            Assert.That(CreateStore().Load().GetAmount("gold"), Is.EqualTo(100));
            File.Delete(m_Path);
            Assert.That(CreateStore().Load().Amounts, Is.Empty);
        }

        [Test]
        public void CorruptPrimaryAndBackupAreReportedWithoutModifyingEitherFile()
        {
            File.WriteAllText(m_Path, "broken primary");
            File.WriteAllText(m_Path + ".bak", "broken backup");
            Assert.Throws<InvalidDataException>(() => CreateStore().Load());
            Assert.That(File.ReadAllText(m_Path), Is.EqualTo("broken primary"));
            Assert.That(File.ReadAllText(m_Path + ".bak"), Is.EqualTo("broken backup"));
        }

        private FileProgressStore CreateStore()
        {
            return new FileProgressStore(m_Path, RewardCollection.Empty, "gold");
        }

        private static RewardCollection Rewards(long amount)
        {
            return new RewardCollection(new[] { new Reward("gold", amount) });
        }
    }
}
