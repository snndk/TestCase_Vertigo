using System;
using System.Collections.Generic;
using SRDebugger;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Debug._Game.Debug
{
    public class GameDebugOptions
    {
        private readonly GameDebugSession m_Session;
        private readonly List<OptionDefinition> m_Slots = new List<OptionDefinition>();
        private readonly List<OptionDefinition> m_TestOptions = new List<OptionDefinition>();
        private Round m_Round;
        private int m_TargetZone = 1;
        private int m_GoldAmount = 250;
        private bool m_ConfirmReset;
        private OptionDefinition m_ModeButton;
        private bool m_DisplayedTestMode;

        public DynamicOptionContainer Container { get; } = new DynamicOptionContainer();

        public GameDebugOptions(GameDebugSession session)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            Container.AddOption(OptionDefinition.Create("Mode",
                () => session.IsTesting ? "TEST MODE ACTIVE - Test save" : "NORMAL MODE - Normal save", category: "01 Tester"));
            Container.AddOption(OptionDefinition.Create("Last action", () => session.Message, category: "01 Tester"));
            Read("New session", () => "Discards uncollected loot", "02 Zones");
            m_TestOptions.Add(OptionDefinition.Create("Target zone", () => m_TargetZone, value => m_TargetZone = value,
                "02 Zones", attributes: new Attribute[] { new NumberRangeAttribute(1, 1000000) }));
            Button("Start at target zone", () => session.StartAtZone(m_TargetZone), "02 Zones");
            Button("Start before next Silver", session.StartBeforeSilver, "02 Zones");
            Button("Start before next Gold", session.StartBeforeGold, "02 Zones");
            Button("Start new run at zone 1", () => session.StartAtZone(1), "02 Zones");
            m_TestOptions.Add(OptionDefinition.Create("Gold amount", () => m_GoldAmount, value => m_GoldAmount = value,
                "03 Gold", attributes: new Attribute[] { new NumberRangeAttribute(0, int.MaxValue) }));
            Button("Set Gold", () => session.SetGold(m_GoldAmount), "03 Gold");
            Button("Add Gold", () => session.AddGold(m_GoldAmount), "03 Gold");
            Read("Next result", () => session.PendingResult, "04 Next spin");
            Button("Cancel forced result", session.CancelForcedResult, "04 Next spin");
            Read("Test file", () => session.SavePath, "05 Save");
            Read("Next save", () => session.SaveFailureArmed ? "WILL FAIL ONCE" : "Normal", "05 Save");
            Button("Fail next save once", session.ArmSaveFailure, "05 Save");
            Button("Cancel save failure", session.CancelSaveFailure, "05 Save");
            Button("Reload test save and restart", session.ReloadTestSave, "05 Save");
            m_TestOptions.Add(OptionDefinition.Create("Confirm test save reset", () => m_ConfirmReset,
                value => m_ConfirmReset = value, "05 Save"));
            Button("Reset test save and restart", ResetSave, "05 Save");
            Read("Zone / tier", () => session.Snapshot == null ? "Not started" :
                $"{session.Snapshot.Round.Zone} / {session.Snapshot.Round.Tier}", "06 Live state");
            Read("Session", () => session.Snapshot?.Status.ToString() ?? "Not started", "06 Live state");
            Read("Gold", () => session.Snapshot?.Progress.GetAmount(session.CurrencyId).ToString() ?? "0", "06 Live state");
            Read("Revive cost", () => session.Snapshot?.ReviveCost?.Amount.ToString() ?? "Disabled", "06 Live state");
            Read("Lucky target", () => session.Snapshot?.AutomaticRun?.TargetZone.ToString() ?? "Inactive", "06 Live state");
            Read("Run ID", () => session.Snapshot?.RunId.ToString() ?? "None", "06 Live state");
            Read("Pending operation", () => session.Snapshot?.PendingSpin?.OperationId.Sequence.ToString() ?? "None", "06 Live state");
            Refresh();
        }

        public void Refresh()
        {
            m_Session.Refresh();
            RefreshMode();
            var round = m_Session.IsTesting ? m_Session.Snapshot?.Round : null;
            if (!ReferenceEquals(round, m_Round))
            {
                foreach (var option in m_Slots)
                    Container.RemoveOption(option);
                m_Slots.Clear();
                m_Round = round;
                if (round != null)
                {
                    for (var index = 0; index < round.Slices.Count; index++)
                    {
                        if (round.SpinWeights[index] <= 0)
                            continue;
                        var slot = index;
                        var slice = round.Slices[index];
                        var label = $"Next: {slot + 1} - {slice.Id}";
                        if (slice is RewardSliceData reward)
                            label += $" (+{reward.Reward.Amount})";
                        var option = OptionDefinition.FromMethod(label,
                            () => m_Session.Execute(() => m_Session.ForceSlot(slot, round)), "04 Next spin", slot + 10);
                        m_Slots.Add(option);
                        Container.AddOption(option);
                    }
                }
            }
            foreach (var option in Container.Options)
                option.Property?.NotifyValueChanged();
        }

        private void RefreshMode()
        {
            if (m_ModeButton != null && m_DisplayedTestMode == m_Session.IsTesting)
                return;

            if (m_ModeButton != null)
                Container.RemoveOption(m_ModeButton);

            m_DisplayedTestMode = m_Session.IsTesting;
            var label = m_DisplayedTestMode
                ? "<color=#FF7373>Exit Test Mode</color>"
                : "<color=#67E89B>Enter Test Mode</color>";
            // Option names are immutable; replace only when the active mode changes.
            m_ModeButton = OptionDefinition.FromMethod(label, ToggleTestMode, "01 Tester", -1);
            Container.AddOption(m_ModeButton);

            foreach (var option in m_TestOptions)
            {
                if (m_DisplayedTestMode)
                    Container.AddOption(option);
                else
                    Container.RemoveOption(option);
            }
            if (!m_DisplayedTestMode)
                m_ConfirmReset = false;
        }

        private void ToggleTestMode()
        {
            m_Session.Execute(() =>
            {
                if (m_Session.IsTesting)
                    m_Session.ReturnToNormal();
                else
                    m_Session.EnterTesterMode();
            });
            Refresh();
        }

        private void ResetSave()
        {
            if (!m_ConfirmReset)
                throw new InvalidOperationException("Enable 'Confirm test save reset' first.");
            m_ConfirmReset = false;
            m_Session.ResetTestSave();
        }

        private void Read(string name, Func<string> getter, string category)
        {
            m_TestOptions.Add(OptionDefinition.Create(name, getter, category: category));
        }

        private void Button(string name, Action action, string category)
        {
            m_TestOptions.Add(OptionDefinition.FromMethod(name, () =>
            {
                m_Session.Execute(action);
                Refresh();
            }, category));
        }
    }
}
