using System;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Game settings")]
    public class GameSettings : ScriptableObject
    {
        [SerializeField] private WheelConfigurationAsset wheelConfiguration;
        [SerializeField] private RewardAsset currency;
        [SerializeField, Min(0)] private long startingBalance = 250;
        [SerializeField] private bool reviveEnabled = true;
        [SerializeField, Min(1)] private long initialReviveCost = 25;
        [SerializeField, Min(0)] private int reviveIncreasePercent = 100;

        public WheelConfigurationAsset WheelConfiguration => wheelConfiguration;
        public RewardAsset Currency => currency;
        public int ReviveIncreasePercent => reviveIncreasePercent;

        public RewardCollection CreateStartingRewards()
        {
            Validate();
            return startingBalance == 0 ? RewardCollection.Empty :
                new RewardCollection(new[] { new Reward(currency.Id, startingBalance) });
        }

        public Reward CreateReviveCost()
        {
            Validate();
            return reviveEnabled ? new Reward(currency.Id, initialReviveCost) : null;
        }

        private void Validate()
        {
            if (wheelConfiguration == null)
                throw new InvalidOperationException("Game settings require a wheel configuration.");
            if (currency == null || string.IsNullOrWhiteSpace(currency.Id) || currency.Icon == null)
                throw new InvalidOperationException("Game settings require a currency with an identity and icon.");
            if (startingBalance < 0)
                throw new InvalidOperationException("Starting balance cannot be negative.");
            if (reviveIncreasePercent < 0 || (reviveEnabled && initialReviveCost < 1))
                throw new InvalidOperationException("Revive requires a positive initial cost and a non-negative increase percentage.");
        }
    }
}
