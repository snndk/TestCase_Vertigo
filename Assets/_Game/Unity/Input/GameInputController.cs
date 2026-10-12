using System;
using UnityEngine;
using Vertigo.Presentation._Game.Presentation.GameScreen;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Input
{
    public class GameInputController : MonoBehaviour, IGameInput
    {
        [SerializeField] private GameButton spin;
        [SerializeField] private GameButton cashOut;
        [SerializeField] private GameButton revive;
        [SerializeField] private GameButton giveUp;
        [SerializeField] private GameButton restart;
        [SerializeField] private AnimationTapView tap;
        [SerializeField] private WheelView wheel;
        private GameScreenState m_State;
        public bool InputBlocked => m_State != null && m_State.InputBlocked;
        public event Action SpinRequested;
        public event Action CashOutRequested;
        public event Action ReviveRequested;
        public event Action GiveUpRequested;
        public event Action RestartRequested;

        public void Apply(GameScreenState state)
        {
            m_State = state;
            spin.SetInteractable(state != null && state.CanSpin);
            cashOut.SetInteractable(state != null && state.CanCashOut);
            revive.gameObject.SetActive(state != null && state.AwaitingRevive);
            revive.SetInteractable(state != null && state.CanRevive);
            giveUp.SetInteractable(state != null && state.ShowBomb);
            restart.SetInteractable(state != null && state.CanRestart);
        }

        private void OnEnable()
        {
            spin.Clicked += OnSpin;
            cashOut.Clicked += OnCashOut;
            revive.Clicked += OnRevive;
            giveUp.Clicked += OnGiveUp;
            restart.Clicked += OnRestart;
            tap.Tapped += OnTap;
            tap.SetCaptureFilter(ShouldCaptureTap);
        }

        private void OnDisable()
        {
            spin.Clicked -= OnSpin;
            cashOut.Clicked -= OnCashOut;
            revive.Clicked -= OnRevive;
            giveUp.Clicked -= OnGiveUp;
            restart.Clicked -= OnRestart;
            tap.Tapped -= OnTap;
            tap.SetCaptureFilter(null);
        }

        private bool ShouldCaptureTap()
        {
            return isActiveAndEnabled && m_State != null && (InputBlocked || wheel.CanAccelerate);
        }

        private void OnTap()
        {
            if (!InputBlocked && wheel.CanAccelerate)
                wheel.Accelerate();
        }

        private void OnSpin()
        {
            if (m_State != null && m_State.CanSpin)
                SpinRequested?.Invoke();
        }

        private void OnCashOut()
        {
            if (m_State != null && m_State.CanCashOut)
                CashOutRequested?.Invoke();
        }

        private void OnRevive()
        {
            if (m_State != null && m_State.CanRevive)
                ReviveRequested?.Invoke();
        }

        private void OnGiveUp()
        {
            if (m_State == null || !m_State.ShowBomb)
                return;
            if (m_State.AwaitingRevive)
                GiveUpRequested?.Invoke();
            else
                RestartRequested?.Invoke();
        }

        private void OnRestart()
        {
            if (m_State != null && m_State.CanRestart)
                RestartRequested?.Invoke();
        }
    }
}
