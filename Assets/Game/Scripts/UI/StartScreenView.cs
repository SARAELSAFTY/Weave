using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Shows the start menu and raises events when the player begins a run or quits.</summary>
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField, Tooltip("Button that begins the run.")] private Button playButton;
        [SerializeField, Tooltip("Button that quits the game.")] private Button quitButton;

        /// <summary>Raised when the player presses Play.</summary>
        public event Action PlayRequested;

        /// <summary>Raised when the player presses Quit.</summary>
        public event Action QuitRequested;

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError($"[StartScreenView] Missing required Inspector reference '{nameof(playButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            if (quitButton == null)
            {
                Debug.LogError($"[StartScreenView] Missing required Inspector reference '{nameof(quitButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            playButton.onClick.AddListener(RequestPlay);
            quitButton.onClick.AddListener(RequestQuit);
        }

        /// <summary>Hides the start screen.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Shows the start screen.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void RequestPlay()
        {
            PlayRequested?.Invoke();
        }

        private void RequestQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}