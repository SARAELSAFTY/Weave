using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Shows the start menu and raises an event when the player begins a run.</summary>
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField, Tooltip("Button that begins the run.")] private Button playButton;

        /// <summary>Raised when the player presses Play.</summary>
        public event Action PlayRequested;

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError($"[StartScreenView] Missing required Inspector reference '{nameof(playButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            playButton.onClick.AddListener(RequestPlay);
        }

        /// <summary>Hides the start screen.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RequestPlay()
        {
            PlayRequested?.Invoke();
        }
    }
}