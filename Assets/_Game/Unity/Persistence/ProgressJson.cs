using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Unity._Game.Unity.Persistence
{
    internal static class ProgressJson
    {
        private const int m_CurrentVersion = 1;

        public static string Serialize(RewardCollection progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            var rewards = new SavedReward[progress.Amounts.Count];
            var index = 0;
            foreach (var pair in progress.Amounts)
                rewards[index++] = new SavedReward { id = pair.Key, amount = pair.Value };

            return JsonUtility.ToJson(new ProgressSaveData { version = m_CurrentVersion, rewards = rewards }, true);
        }

        public static RewardCollection Deserialize(string json)
        {
            ProgressSaveData data;
            try
            {
                data = JsonUtility.FromJson<ProgressSaveData>(json);
            }
            catch (ArgumentException error)
            {
                throw new InvalidDataException("The progress file is not valid JSON.", error);
            }

            if (data == null || data.version <= 0)
                throw new InvalidDataException("The progress file is incomplete.");
            if (data.version != m_CurrentVersion)
                throw new NotSupportedException("The progress file version is not supported by this game.");
            if (data.rewards == null)
                throw new InvalidDataException("The progress file is missing its rewards.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var rewards = new List<Reward>(data.rewards.Length);
            foreach (var saved in data.rewards)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.id) || saved.amount <= 0 || !ids.Add(saved.id))
                    throw new InvalidDataException("The progress file contains an invalid or duplicate reward.");
                rewards.Add(new Reward(saved.id, saved.amount));
            }

            return new RewardCollection(rewards);
        }
    }
}
