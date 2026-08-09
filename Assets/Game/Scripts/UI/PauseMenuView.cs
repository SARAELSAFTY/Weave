using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Shows the pause overlay and raises events when the player resumes or quits.</summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField, Tooltip("Button that resumes the run.")] private Button resumeButton;
        [SerializeField, Tooltip("Button that quits the game.")] private Button quitButton;

        /// <summary>Raised when the player presses Resume.</summary>
        public event Action ResumeRequested;

        /// <summary>Raised when the player presses Quit.</summary>
        public event Action QuitRequested;

        private void Awake()
        {
            if (resumeButton == null)
            {
                Debug.LogError($"[PauseMenuView] Missing required Inspector reference '{nameof(resumeButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            if (quitButton == null)
            {
                Debug.LogError($"[PauseMenuView] Missing required Inspector reference '{nameof(quitButton)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            resumeButton.onClick.AddListener(RequestResume);
            quitButton.onClick.AddListener(RequestQuit);

            gameObject.SetActive(false);
        }

        /// <summary>Shows the pause overlay.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hides the pause overlay.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RequestResume()
        {
            ResumeRequested?.Invoke();
        }

        private void RequestQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}