using System;
using Vertigo.Presentation._Game.Presentation.Spin;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Rewards;
using Vertigo.Runtime._Game.Runtime.Session;

namespace Vertigo.Presentation._Game.Presentation.GameScreen
{
    public class GameScreenPresenter
    {
        private readonly GameSession m_Session;
        private readonly IGameScreenView m_View;
        private readonly SpinPresentationSequence m_Spin;
        private RewardCollection m_DisplayedLoot;
        private PresentationError m_Error;
        private bool m_IsAttached;
        private bool m_IsHandlingInput;
        private bool m_IsRendering;
        private long m_BindingVersion;

        public GameScreenPresenter(GameSession session, IGameScreenView view, SpinPresentationSequence spin)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_View = view ?? throw new ArgumentNullException(nameof(view));
            m_Spin = spin ?? throw new ArgumentNullException(nameof(spin));
        }

        public void Attach()
        {
            if (m_IsAttached)
                return;

            var snapshot = m_Session.GetSnapshot();
            if (m_View.SliceCount != snapshot.Round.Slices.Count)
                throw new InvalidOperationException("View capacity does not match the runtime wheel.");

            // A returning screen skips any pending visuals rather than replaying earned rewards.
            if (snapshot.PendingSpin != null)
                m_Session.CompleteResolution(snapshot.PendingSpin.OperationId);

            m_DisplayedLoot = m_Session.GetSnapshot().RunLoot;
            m_IsAttached = true;
            m_BindingVersion++;
            m_View.SpinRequested += OnSpinRequested;
            m_View.CashOutRequested += OnCashOutRequested;
            m_View.RestartRequested += OnRestartRequested;
            try
            {
                Render();
            }
            catch
            {
                Detach();
                throw;
            }
        }

        public void Detach()
        {
            if (!m_IsAttached)
                return;

            m_IsAttached = false;
            m_BindingVersion++;
            m_View.SpinRequested -= OnSpinRequested;
            m_View.CashOutRequested -= OnCashOutRequested;
            m_View.RestartRequested -= OnRestartRequested;
            try
            {
                m_Spin.Cancel();
            }
            finally
            {
                // Also covers detaching during the initial render, before animation starts.
                var pending = m_Session.GetSnapshot().PendingSpin;
                if (pending != null)
                    m_Session.CompleteResolution(pending.OperationId);
            }
        }

        public void AdvancePresentation(TimeSpan elapsed)
        {
            m_Spin.Advance(elapsed);
        }

        private void OnSpinRequested()
        {
            HandleInput(() =>
            {
                var response = m_Session.RequestSpin();
                if (response.Status != CommandStatus.Accepted)
                    return;

                m_Error = null;
                var binding = m_BindingVersion;
                try
                {
                    Render();
                    if (m_IsAttached && binding == m_BindingVersion)
                        m_Spin.Play(response.Result, result => FinishSpin(response.Result.OperationId, result));
                }
                catch
                {
                    m_Session.CompleteResolution(response.Result.OperationId);
                    throw;
                }
            });
        }

        private void OnCashOutRequested()
        {
            HandleInput(() =>
            {
                var response = m_Session.RequestCashOut();
                if (response.Status == CommandStatus.SaveFailed)
                    m_Error = new PresentationError("Rewards could not be saved. Please try again.");
                else if (response.Status == CommandStatus.Accepted)
                    m_Error = null;
                else
                    return;

                Synchronize();
            });
        }

        private void OnRestartRequested()
        {
            HandleInput(() =>
            {
                if (m_Session.StartNewRun() != CommandStatus.Accepted)
                    return;

                m_Error = null;
                Synchronize();
            });
        }

        private void HandleInput(Action action)
        {
            if (!m_IsAttached || m_IsHandlingInput || m_IsRendering)
                return;

            m_IsHandlingInput = true;
            try
            {
                action();
            }
            catch (Exception error)
            {
                m_Error = new PresentationError("The action could not be displayed or completed. Please try again.", error);
                Synchronize();
            }
            finally
            {
                m_IsHandlingInput = false;
            }
        }

        private void FinishSpin(OperationId operationId, VisualResult result)
        {
            if (m_Session.CompleteResolution(operationId) != CommandStatus.Accepted)
                return;

            if (result.Status == VisualStatus.Failed)
                m_Error = new PresentationError("The animation could not finish. Your result has been kept.", result.Error);

            Synchronize();
        }

        private void Synchronize()
        {
            m_DisplayedLoot = m_Session.GetSnapshot().RunLoot;
            if (m_IsAttached)
                Render();
        }

        private void Render()
        {
            m_IsRendering = true;
            try
            {
                m_View.Render(new GameScreenState(m_Session.GetSnapshot(), m_DisplayedLoot, m_Error));
            }
            finally
            {
                m_IsRendering = false;
            }
        }
    }
}
