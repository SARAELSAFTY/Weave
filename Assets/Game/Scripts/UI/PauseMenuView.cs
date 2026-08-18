using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField, Tooltip("Button that resumes the run.")] private Button resumeButton;
        [SerializeField, Tooltip("Button that quits the game.")] private Button quitButton;

        public event Action ResumeRequested;
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

        public void Show()
        {
            gameObject.SetActive(true);
        }

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
