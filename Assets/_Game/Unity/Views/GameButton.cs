using System;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    [RequireComponent(typeof(Button))]
    public class GameButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        public event Action Clicked;
        public void SetInteractable(bool value) => button.interactable = value;
        private void OnValidate() => button = GetComponent<Button>();
        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }
        private void OnEnable() => button.onClick.AddListener(OnClicked);
        private void OnDisable() => button.onClick.RemoveListener(OnClicked);
        private void OnClicked() => Clicked?.Invoke();
    }
}
