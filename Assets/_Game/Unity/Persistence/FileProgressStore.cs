using System;
using System.IO;
using System.Text;
using Vertigo.Runtime._Game.Runtime.Contracts;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Unity._Game.Unity.Persistence
{
    public class FileProgressStore : IProgressStore
    {
        private readonly string m_Path;
        private readonly string m_BackupPath;
        private readonly string m_TemporaryPath;
        private readonly RewardCollection m_StartingRewards;
        private readonly string m_CurrencyId;

        public bool RecoveredFromBackup { get; private set; }

        public FileProgressStore(string path, RewardCollection startingRewards, string currencyId)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A save path is required.", nameof(path));
            if (string.IsNullOrWhiteSpace(currencyId))
                throw new ArgumentException("A currency ID is required.", nameof(currencyId));

            m_Path = Path.GetFullPath(path);
            m_BackupPath = m_Path + ".bak";
            m_TemporaryPath = m_Path + ".tmp";
            m_CurrencyId = currencyId;
            m_StartingRewards = SelectCurrency(startingRewards);
        }

        public RewardCollection Load()
        {
            RecoveredFromBackup = false;
            Exception primaryError = null;
            try
            {
                var progress = Read(m_Path);
                if (progress != null)
                    return progress;
            }
            catch (Exception error) when (IsStorageError(error) || error is InvalidDataException)
            {
                primaryError = error;
            }

            var backup = Read(m_BackupPath);
            if (backup == null)
            {
                if (primaryError != null)
                    throw new InvalidDataException("Saved progress could not be loaded and no backup is available.", primaryError);
                return m_StartingRewards;
            }

            // Repair before allowing economy writes, keeping the known-good backup intact.
            if (!Write(ProgressJson.Serialize(backup), false))
                throw new IOException("The progress backup could not be restored.");
            RecoveredFromBackup = true;
            return backup;
        }

        public bool TrySave(RewardCollection progress)
        {
            return Write(ProgressJson.Serialize(SelectCurrency(progress)), true);
        }

        private RewardCollection SelectCurrency(RewardCollection progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            var amount = progress.GetAmount(m_CurrencyId);
            return amount == 0 ? RewardCollection.Empty :
                new RewardCollection(new[] { new Reward(m_CurrencyId, amount) });
        }

        private bool Write(string json, bool keepPrevious)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(m_Path));
                var bytes = Encoding.UTF8.GetBytes(json);
                using (var stream = new FileStream(m_TemporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                // The rename/replace is the commit point. Nothing fallible runs afterwards.
                if (File.Exists(m_Path))
                    File.Replace(m_TemporaryPath, m_Path, keepPrevious ? m_BackupPath : null);
                else
                    File.Move(m_TemporaryPath, m_Path);
                return true;
            }
            catch (Exception error) when (IsStorageError(error))
            {
                return false;
            }
        }

        private RewardCollection Read(string path)
        {
            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            return SelectCurrency(ProgressJson.Deserialize(json));
        }

        private static bool IsStorageError(Exception error)
        {
            return error is IOException || error is UnauthorizedAccessException;
        }
    }
}
